namespace CoreEx.Azure.Functions.Test.Unit;

public class CatalogForwardingTests
{
    [TestCase(false, "http://127.0.0.1:10000/devstoreaccount1")]
    [TestCase(true, "http://127.0.0.1:11000/devstoreaccount1")]
    public void WorkerBlobClient_UsesBindingFallbackUnlessExplicitlyConfigured(bool useNamedConnection, string endpoint)
    {
        var configuration = new Dictionary<string, string?>
        {
            ["ImportStorage"] = "UseDevelopmentStorage=true",
            ["ConnectionStrings:ImportStorage"] = useNamedConnection
                ? "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=AA==;BlobEndpoint=http://127.0.0.1:11000/devstoreaccount1"
                : null
        };

        var builder = FunctionsApplication.CreateBuilder([]);
        builder.ConfigureFunctionsWebApplication();
        builder.Configuration.AddInMemoryCollection(configuration);
        builder.AddImportStorageClient();
        using var host = builder.Build();
        host.Services.GetRequiredService<BlobServiceClient>().Uri.Should().Be(new Uri(endpoint));
    }

    [Test]
    public void ImportCatalog_SecondPreSendFailureCapturesOnlyFirstCommandAndCompletesReport()
    {
        var reports = new RecordingBlobContainerClient();
        using var test = CreateTester(reports);
        var attempts = 0;
        test.OnEventPublish(ServiceBusPublisher.DefaultServiceKey, () =>
        {
            if (++attempts == 2)
                throw new IOException("Injected before the second send.");
        });

        test.Trigger<CatalogForwardingFunction>()
            .ExpectAzureServiceBusEvents(events => events.Assert(captured =>
            {
                var command = captured.Should().ContainSingle().Which;
                command.Destination.Should().Be(CatalogItemUpsert.Queue);
                command.Event.EncodeToJsonElement().GetProperty("data").GetProperty("sku").GetString().Should().Be("SKU-1");
            }))
            .Run(f => f.ImportCatalog(Blob("catalog-valid.csv"), CancellationToken.None))
            .AssertException<IOException>();

        attempts.Should().Be(2);
        AssertReport(reports, "Faulted", 1);
    }

    [Test]
    public void ImportCatalog_LateRejectionDoesNotPublishEarlierRows()
    {
        var reports = new RecordingBlobContainerClient();
        using var test = CreateTester(reports);
        test.Trigger<CatalogForwardingFunction>()
            .ExpectNoAzureServiceBusEvents()
            .Run(f => f.ImportCatalog(Blob("catalog-invalid.csv"), CancellationToken.None))
            .AssertSuccess();

        AssertReport(reports, "ContentFailed", 0);
        reports.Blobs.Values.Single().Content.ToString().Should().Contain("\"kind\":\"diagnostic\"");
    }

    [Test]
    public void ImportCatalog_ReplayKeepsCommandIdsButWritesANewAttempt()
    {
        var reports = new RecordingBlobContainerClient();
        var blob = Blob("catalog-valid.csv");
        var reads = 0;
        blob.BeforeRead = () => reads++;

        string[] RunAttempt()
        {
            using var test = CreateTester(reports);
            string[] ids = [];
            test.Trigger<CatalogForwardingFunction>()
                .ExpectAzureServiceBusEvents(events => events.Assert(captured =>
                    ids = [.. captured.Select(command => command.Event.Id ?? throw new InvalidOperationException("A command ID is required."))]))
                .Run(f => f.ImportCatalog(blob, CancellationToken.None))
                .AssertSuccess();
            return ids;
        }

        var first = RunAttempt();
        first.Should().HaveCount(3).And.OnlyHaveUniqueItems();
        RunAttempt().Should().Equal(first);
        reads.Should().Be(4);
        reports.Blobs.Should().HaveCount(2).And.OnlyContain(report => report.Value.Metadata["outcome"] == "Succeeded");
    }

    [Test]
    public void BinaryCommand_RoundTripsIdentityAndPreservesZeroPrice()
    {
        using var test = FunctionTester.Create<CatalogForwardingFunction>();
        var formatter = test.Services.GetRequiredService<IEventFormatter>();
        var cloudEvent = formatter.ConvertToCloudEvent(formatter.Format(new CatalogItemUpsert("stable-id", "SKU-1", 0m).CreateEvent()));
        var outgoing = cloudEvent.ToServiceBusMessage(ContentMode.Binary, includeAttributes: true, formatter);
        outgoing.MessageId.Should().Be("stable-id");
        outgoing.Subject.Should().Be("northstar.catalog.item.upsert.v1");
        outgoing.PartitionKey.Should().Be("SKU-1");
        outgoing.ContentType.Should().Be("application/json");
        using var body = JsonDocument.Parse(outgoing.Body.ToMemory());
        body.RootElement.GetProperty("commandId").GetString().Should().Be(outgoing.MessageId);
        body.RootElement.GetProperty("sku").GetString().Should().Be("SKU-1");
        body.RootElement.GetProperty("unitPrice").GetDecimal().Should().Be(0m);

        var received = ServiceBusModelFactory.ServiceBusReceivedMessage(body: outgoing.Body, messageId: outgoing.MessageId,
            subject: outgoing.Subject, contentType: outgoing.ContentType, properties: outgoing.ApplicationProperties);
        var decoded = received.ToCloudEvent(ContentMode.Binary);
        decoded.Id.Should().Be(outgoing.MessageId);
        decoded.Type.Should().Be(outgoing.Subject);
        decoded.Source.Should().Be(new Uri("urn:northstar:supplier:catalog"));
    }

    private static UnitTestEx.Azure.Functions.FunctionTester<CatalogForwardingFunction> CreateTester(RecordingBlobContainerClient reports)
    {
        var test = FunctionTester.Create<CatalogForwardingFunction>();
        test.ConfigureServices(services =>
        {
            // UnitTestEx's fallback activation does not honor FromKeyedServices.
            services.AddScoped<CatalogForwardingFunction>();
            services.AddKeyedSingleton<BlobContainerClient>(CatalogForwardingFunction.ReportContainerKey, reports);
            services.UseExpectedAzureServiceBusPublisher(bypassPassThrough: true);
        });
        return test;
    }

    private static StubBlobClient Blob(string resource)
        => StubBlobClient.FromResource<CatalogForwardingTests>(resource, new Uri("https://storage.test/catalog-imports/catalog.csv"));

    private static void AssertReport(RecordingBlobContainerClient reports, string outcome, long sendsReturned)
    {
        var report = reports.Blobs.Values.Should().ContainSingle().Which;
        report.Metadata["outcome"].Should().Be(outcome);
        using var last = JsonDocument.Parse(report.Content.ToString().TrimEnd().Split('\n')[^1]);
        last.RootElement.GetProperty("outcome").GetString().Should().Be(outcome);
        last.RootElement.GetProperty("sendsReturned").GetInt64().Should().Be(sendsReturned);
    }
}
