namespace Contoso.Customers.Test.Relay;

public class RelayTests : WithApiTester<Contoso.Customers.Relay.Program>
{
    private CosmosClient _client = null!; // Initialized by OneTimeSetUp before any test executes.
    private readonly List<PublishedEvents> _backlog = [];
    private static readonly string[] _services = ["cosmos-outbox-relay-customers-00", "cosmos-outbox-relay-ref-data-00"];

    [OneTimeSetUp]
    public async Task OneTimeSetUpAsync()
    {
        // Even Test.Configuration constructs Program and captures its start boundary; provision before accessing the tester.
        var configuration = new ConfigurationBuilder().SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.Development.json").AddJsonFile("appsettings.unittest.json").AddEnvironmentVariables().Build();
        var connectionString = configuration.GetConnectionString("Cosmos") ?? throw new InvalidOperationException("The Cosmos connection string is missing.");
        _client = CosmosDbClientFactory.Create(connectionString);
        var args = Contoso.Customers.Database.Program.ConfigureProvisionArgs(new CosmosDbProvisionArgs { DatabaseId = "contoso" });
        await new CosmosDbProvisioner(_client, args).RunAsync(CosmosDbProvisionCommand.Create | CosmosDbProvisionCommand.ResetAndData).ConfigureAwait(false);

        var serviceBusConnection = configuration.GetConnectionString("ServiceBus") ?? throw new InvalidOperationException("The Service Bus connection string is missing.");
        await UnitTestExExtensions.ResetAzureServiceBusAsync(UnitTestExExtensions.CreateAzureServiceBusAdminConnectionString(serviceBusConnection), topicsAndSubscriptions: Broker.GetTopicsAndSubscriptions()).ConfigureAwait(false);

        _backlog.Add(await PublishAsync("customers").ConfigureAwait(false));
        _backlog.Add(await PublishAsync("ref-data").ConfigureAwait(false));
        // Cosmos change-feed timestamps have second precision; make the pre-start writes unambiguously older.
        await Task.Delay(TimeSpan.FromSeconds(2)).ConfigureAwait(false);

        foreach (var service in _services)
            await WaitForStatusAsync(service, "Running").ConfigureAwait(false);
    }

    [OneTimeTearDown]
    public void OneTimeTearDown() => _client?.Dispose();

    [Test]
    public async Task Outbox_Relay_BothContainers_And_FirstStartBoundary()
    {
        await SetPausedAsync(true).ConfigureAwait(false);
        PublishedEvents customers;
        PublishedEvents refData;
        try
        {
            customers = await PublishAsync("customers").ConfigureAwait(false);
            refData = await PublishAsync("ref-data").ConfigureAwait(false);
            await AssertDocumentsAsync(customers, deleted: false).ConfigureAwait(false);
            await AssertDocumentsAsync(refData, deleted: false).ConfigureAwait(false);
        }
        finally
        {
            await SetPausedAsync(false).ConfigureAwait(false);
        }

        var expected = customers.Events.Concat(refData.Events).ToArray();
        var received = await Broker.ReceiveEventsAsync(Test.Services.GetRequiredService<ServiceBusClient>(), expected.Select(EventKey).ToArray()).ConfigureAwait(false);
        foreach (var e in expected)
        {
            var message = received[EventKey(e)];
            message.MessageId.Should().Be(e.Id);
            message.SessionId.Should().Be(CoreEx.Data.PartitionKey.GetPartitionIdAsString(e.GetPartitionKey() ?? throw new InvalidOperationException("The test event has no partition key.")));
            message.PartitionKey.Should().Be(message.SessionId);
            ObjectComparer.AssertJson(e.EncodeToJsonElement().ToString(), message.Body.ToString());
        }

        await AssertDocumentsAsync(customers, deleted: true).ConfigureAwait(false);
        await AssertDocumentsAsync(refData, deleted: true).ConfigureAwait(false);
        foreach (var backlog in _backlog)
        {
            received.Keys.Should().NotContain(key => key.Subject == backlog.BusinessId);
            await AssertDocumentsAsync(backlog, deleted: false).ConfigureAwait(false);
        }
    }

    [Test]
    public async Task Outbox_Relay_ExplicitPartitionKey()
    {
        await SetPausedAsync(true).ConfigureAwait(false);
        PublishedEvents events;
        try
        {
            events = await PublishAsync("customers", Runtime.NewId()).ConfigureAwait(false);
        }
        finally
        {
            await SetPausedAsync(false).ConfigureAwait(false);
        }

        var received = await Broker.ReceiveEventsAsync(Test.Services.GetRequiredService<ServiceBusClient>(), events.Events.Select(EventKey).ToArray()).ConfigureAwait(false);
        foreach (var e in events.Events)
        {
            var message = received[EventKey(e)];
            message.MessageId.Should().Be(e.Id);
            message.SessionId.Should().Be(CoreEx.Data.PartitionKey.GetPartitionIdAsString(events.PartitionKey ?? throw new InvalidOperationException()));
            ObjectComparer.AssertJson(e.EncodeToJsonElement().ToString(), message.Body.ToString());
        }

        await AssertDocumentsAsync(events, deleted: true).ConfigureAwait(false);
    }

    [Test]
    public async Task Health_And_HostedServices()
    {
        foreach (var path in new[] { "/health/live", "/health/startup", "/health/ready" })
        {
            var watch = Stopwatch.StartNew();
            while (Test.Http().Run(HttpMethod.Get, path).Response.StatusCode != HttpStatusCode.OK)
            {
                if (watch.Elapsed > TimeSpan.FromSeconds(30))
                    Assert.Fail($"Relay health endpoint '{path}' did not become healthy.");
                await Task.Delay(200).ConfigureAwait(false);
            }
        }

        Test.Http().Run(HttpMethod.Get, "/health/ready/detailed").AssertOK().GetContent()
            .Should().ContainAll(["cosmos-database", .. _services]);

        await SetPausedAsync(true).ConfigureAwait(false);
        try
        {
            using var health = JsonDocument.Parse(Test.Http().Run(HttpMethod.Get, "/health/ready/detailed").AssertOK().GetContent() ?? throw new InvalidOperationException("The health response is empty."));
            foreach (var service in _services)
                health.RootElement.GetProperty("entries").GetProperty(service).GetProperty("status").GetString().Should().Be("Degraded");
        }
        finally
        {
            await SetPausedAsync(false).ConfigureAwait(false);
        }
    }

    private async Task SetPausedAsync(bool paused)
    {
        foreach (var service in _services)
        {
            Test.Http().Run(HttpMethod.Post, $"/hosted-services/{service}/{(paused ? "pause" : "resume")}").Response.StatusCode.Should().Be(HttpStatusCode.Accepted);
            await WaitForStatusAsync(service, paused ? "Paused" : "Running").ConfigureAwait(false);
        }
    }

    private async Task WaitForStatusAsync(string service, string status)
    {
        var watch = Stopwatch.StartNew();
        while (true)
        {
            var actual = Test.Http<string>().Run(HttpMethod.Get, $"/hosted-services/{service}/status").AssertOK().Value;
            if (actual == status)
                return;
            if (watch.Elapsed > TimeSpan.FromSeconds(30))
                Assert.Fail($"Relay service '{service}' stayed '{actual}', expected '{status}'.");
            await Task.Delay(200).ConfigureAwait(false);
        }
    }

    private async Task<PublishedEvents> PublishAsync(string containerId, string? partitionKey = null)
    {
        var db = new CustomersCosmosDb(_client, "contoso");
        var publisher = new CosmosDbEventPublisher(db);
        var unitOfWork = new CosmosDbUnitOfWork(db, publisher);
        var id = Runtime.NewId();
        var type = containerId == "customers" ? "customer" : "customertype";
        CloudEvent[] events = [CreateEvent("created"), CreateEvent("updated")];

        CloudEvent CreateEvent(string action)
        {
            var e = new CloudEvent
            {
                Id = Runtime.NewId(),
                Source = new Uri("https://contoso/customers"),
                Type = $"contoso.customers.{type}.{action}.v1",
                Subject = id,
                Time = Runtime.UtcNow,
                DataContentType = "application/json",
                Data = JsonSerializer.SerializeToElement(new { id, text = "Relay test" })
            };
            e.SetPartitionKey(partitionKey ?? id);
            return e;
        }

        await unitOfWork.TransactionAsync(async ct =>
        {
            if (containerId == "customers")
            {
                var model = new Infrastructure.Persistence.Customer { Id = id, PartitionKey = partitionKey, FirstName = "Relay", LastName = "Test", Email = "relay@example.com" };
                var container = db.Container<Infrastructure.Persistence.Customer>(containerId, o => o.WithPartitionKey(m => m.PartitionKey));
                await container.CreateAsync(model, ct).ConfigureAwait(false);
            }
            else
            {
                await db.CustomerTypes.CreateAsync(new Infrastructure.Persistence.CustomerType { Id = id, Code = id, Text = "Relay test", TypeDiscriminator = "CustomerType" }, ct).ConfigureAwait(false);
            }

            publisher.Add("contoso", events);
        }).ConfigureAwait(false);

        var documents = new List<CosmosDbOutboxEvent>();
        using var iterator = db.GetContainer(containerId).GetItemLinqQueryable<CosmosDbOutboxEvent>().Where(e => e.Id.StartsWith(CosmosDbOutboxEvent.OutboxKeyPrefix)).ToFeedIterator();
        while (iterator.HasMoreResults)
            documents.AddRange(await iterator.ReadNextAsync().ConfigureAwait(false));
        var matching = documents.Where(d => d.Event.GetProperty("subject").GetString() == id).ToArray();
        matching.Should().HaveCount(events.Length);
        return new PublishedEvents(containerId, id, partitionKey, events, matching);
    }

    private async Task AssertDocumentsAsync(PublishedEvents events, bool deleted)
    {
        var container = _client.GetDatabase("contoso").GetContainer(events.ContainerId);
        var partitionKey = events.PartitionKey is null ? PartitionKey.None : new PartitionKey(events.PartitionKey);
        var watch = Stopwatch.StartNew();
        foreach (var document in events.Documents)
        {
            while (true)
            {
                using var response = await container.ReadItemStreamAsync(document.Id, partitionKey).ConfigureAwait(false);
                if (response.StatusCode == (deleted ? HttpStatusCode.NotFound : HttpStatusCode.OK))
                    break;
                if (!deleted || watch.Elapsed > TimeSpan.FromSeconds(30))
                    Assert.Fail($"Outbox document '{document.Id}' returned {response.StatusCode}; expected {(deleted ? "cleanup deletion" : "retention")}.");
                await Task.Delay(100).ConfigureAwait(false);
            }
        }

        using var business = await container.ReadItemStreamAsync(events.BusinessId, partitionKey).ConfigureAwait(false);
        business.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static (string Subject, string Type) EventKey(CloudEvent e)
        => (e.Subject ?? throw new InvalidOperationException("The test event has no subject."), e.Type ?? throw new InvalidOperationException("The test event has no type."));

    private sealed record PublishedEvents(string ContainerId, string BusinessId, string? PartitionKey, CloudEvent[] Events, CosmosDbOutboxEvent[] Documents);
}
