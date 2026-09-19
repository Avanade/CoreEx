namespace CoreEx.Cosmos.Test.Unit;

[TestFixture]
public class CosmosDbOutboxRelayTests : CosmosTestBase
{
    private const string ContainerId = "relay-items";

    private static async Task<List<CosmosDbOutboxEvent>> QueryOutboxDocsAsync(Container rawContainer, string pk)
    {
        var query = rawContainer.GetItemLinqQueryable<CosmosDbOutboxEvent>().Where(e => e.PartitionKey == pk && e.Id.StartsWith(CosmosDbOutboxEvent.OutboxKeyPrefix));
        var docs = new List<CosmosDbOutboxEvent>();
        using var iterator = query.ToFeedIterator();
        while (iterator.HasMoreResults)
            docs.AddRange(await iterator.ReadNextAsync());

        return docs;
    }

    private static async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (!condition() && sw.Elapsed < timeout)
            await Task.Delay(100);
    }

    /// <summary>
    /// Creates a test <see cref="IServiceProvider"/> for a <see cref="CosmosDbOutboxRelayProcessor"/>, with <see cref="ICosmosDb"/> registered scoped (via a factory, not a shared instance) - matching production's
    /// <c>AddScoped&lt;ICosmosDb&gt;</c> wiring so each batch's scope gets its own, empty-model-container-cache <see cref="CosmosDb"/>, exactly as it would in a real host. Sharing one <see cref="CosmosDb"/>
    /// instance between the test's own setup code and the processor's scope would incorrectly collide on <see cref="CosmosDb.Container{TModel}"/>'s per-container-id (not per-<c>TModel</c>) cache.
    /// </summary>
    private static ServiceProvider CreateServiceProvider(IEventPublisher eventPublisher)
    {
        var services = new ServiceCollection();
        services.AddScoped<ICosmosDb>(_ => CreateCosmosDb());
        services.AddSingleton(eventPublisher);
        return services.BuildServiceProvider();
    }

    [Test]
    public async Task ProcessBatchAsync_PublishesAndDeletes()
    {
        await GetOrCreateContainerAsync(ContainerId).ConfigureAwait(false);
        var cosmosDb = CreateCosmosDb();
        var container = cosmosDb.Container<TestItem>(ContainerId, o => o.WithPartitionKey(m => m.PartitionKey));
        var outbox = new CosmosDbEventPublisher(cosmosDb);
        var unitOfWork = new CosmosDbUnitOfWork(cosmosDb, outbox);

        var pk = NewId();
        var id = NewId();

        await unitOfWork.TransactionAsync(async ct =>
        {
            var created = await container.CreateAsync(new TestItem { Id = id, PartitionKey = pk, Name = "Widget" }, ct).ConfigureAwait(false);
            unitOfWork.Events.Add(EventData.CreateEventWith(created.Value, EventAction.Created).WithSource(new Uri("https://unittest/coreex-cosmos", UriKind.Absolute)).WithPartitionKey(pk));
        });

        var rawContainer = cosmosDb.GetContainer(ContainerId);
        var outboxDocs = await QueryOutboxDocsAsync(rawContainer, pk);
        outboxDocs.Should().ContainSingle();

        var testPublisher = new TestEventPublisher();
        using var sp = CreateServiceProvider(testPublisher);
        var processor = new CosmosDbOutboxRelayProcessor(sp, ContainerId, NullLogger<CosmosDbOutboxRelayProcessor>.Instance);

        await processor.ProcessBatchAsync(outboxDocs, CancellationToken.None);

        testPublisher.Published.Should().ContainSingle();
        testPublisher.Published[0].Destination.Should().Be(outboxDocs[0].Destination);

        // Cleanup - the outbox document should now be gone.
        var remaining = await QueryOutboxDocsAsync(rawContainer, pk);
        remaining.Should().BeEmpty();
    }

    [Test]
    public async Task ProcessBatchAsync_CosmosDbEventPublisherRegisteredAsDestination_ThrowsClearGuidance()
    {
        // Regression: registering CosmosDbEventPublisher (the outbox write-side publisher) as the relay's default IEventPublisher must fail immediately with an actionable message naming the
        // misconfiguration - not the deeper, less obvious "no active transaction" exception CosmosDbEventPublisher.OnPublishAsync would otherwise throw once actually invoked.
        await GetOrCreateContainerAsync(ContainerId).ConfigureAwait(false);
        var cosmosDb = CreateCosmosDb();
        var container = cosmosDb.Container<TestItem>(ContainerId, o => o.WithPartitionKey(m => m.PartitionKey));
        var outbox = new CosmosDbEventPublisher(cosmosDb);
        var unitOfWork = new CosmosDbUnitOfWork(cosmosDb, outbox);

        var pk = NewId();
        await unitOfWork.TransactionAsync(async ct =>
        {
            var created = await container.CreateAsync(new TestItem { Id = NewId(), PartitionKey = pk, Name = "Widget" }, ct).ConfigureAwait(false);
            unitOfWork.Events.Add(EventData.CreateEventWith(created.Value, EventAction.Created).WithSource(new Uri("https://unittest/coreex-cosmos", UriKind.Absolute)).WithPartitionKey(pk));
        });

        var rawContainer = cosmosDb.GetContainer(ContainerId);
        var outboxDocs = await QueryOutboxDocsAsync(rawContainer, pk);
        outboxDocs.Should().ContainSingle();

        // Misconfiguration under test: CosmosDbEventPublisher (write-side) registered as the relay's own destination IEventPublisher.
        using var sp = CreateServiceProvider(new CosmosDbEventPublisher(CreateCosmosDb()));
        var processor = new CosmosDbOutboxRelayProcessor(sp, ContainerId, NullLogger<CosmosDbOutboxRelayProcessor>.Instance);

        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => await processor.ProcessBatchAsync(outboxDocs, CancellationToken.None));
        ex!.Message.Should().Contain(nameof(CosmosDbEventPublisher)).And.Contain(ContainerId);

        // Nothing should have been deleted - the guard fires before any cleanup work is attempted.
        var remaining = await QueryOutboxDocsAsync(rawContainer, pk);
        remaining.Should().ContainSingle();
    }

    [Test]
    public async Task ProcessBatchAsync_PartitionKeyNone_PublishesAndDeletes()
    {
        // Regression: DeleteOneAsync previously always called the partitionKey-taking DeleteAsync overload with a null-forgiving bang on doc.PartitionKey - which is a real, valid value (PartitionKey.None)
        // for a model with no WithPartitionKey/WithFixedPartitionKey configured, not a missing one. That overload ThrowIfNull()s its partitionKey argument, so cleanup-delete failed for every such
        // document (silently, per ProcessBatchAsync's remarks - caught, logged, counted - so it would linger until TTL rather than actually being removed).
        await GetOrCreateContainerAsync(ContainerId).ConfigureAwait(false);
        var cosmosDb = CreateCosmosDb();
        var container = cosmosDb.Container<NoPartitionKeyItem>(ContainerId);
        var outbox = new CosmosDbEventPublisher(cosmosDb);
        var unitOfWork = new CosmosDbUnitOfWork(cosmosDb, outbox);

        var id = NewId();

        await unitOfWork.TransactionAsync(async ct =>
        {
            var created = await container.CreateAsync(new NoPartitionKeyItem { Id = id, Name = "Widget" }, ct).ConfigureAwait(false);
            unitOfWork.Events.Add(EventData.CreateEventWith(created.Value, EventAction.Created).WithSource(new Uri("https://unittest/coreex-cosmos", UriKind.Absolute)));
        });

        var rawContainer = cosmosDb.GetContainer(ContainerId);
        var query = rawContainer.GetItemLinqQueryable<CosmosDbOutboxEvent>().Where(e => e.Id.StartsWith(CosmosDbOutboxEvent.OutboxKeyPrefix));

        async Task<List<CosmosDbOutboxEvent>> QueryAllNoPartitionOutboxDocsAsync()
        {
            var docs = new List<CosmosDbOutboxEvent>();
            using var iterator = query.ToFeedIterator();
            while (iterator.HasMoreResults)
                docs.AddRange(await iterator.ReadNextAsync());

            return docs.Where(d => d.PartitionKey is null && d.Event.GetProperty("subject").GetString() == id).ToList();
        }

        var outboxDocs = await QueryAllNoPartitionOutboxDocsAsync();
        outboxDocs.Should().ContainSingle();

        var testPublisher = new TestEventPublisher();
        using var sp = CreateServiceProvider(testPublisher);
        var processor = new CosmosDbOutboxRelayProcessor(sp, ContainerId, NullLogger<CosmosDbOutboxRelayProcessor>.Instance);

        await processor.ProcessBatchAsync(outboxDocs, CancellationToken.None);

        testPublisher.Published.Should().ContainSingle();

        // Cleanup - the outbox document should now actually be gone, not left lingering due to a failed cleanup-delete.
        var remaining = await QueryAllNoPartitionOutboxDocsAsync();
        remaining.Should().BeEmpty();
    }

    [Test]
    public async Task ProcessBatchAsync_EmitsPerEventRelayMarker_ParentedToOriginatingTrace()
    {
        await GetOrCreateContainerAsync(ContainerId).ConfigureAwait(false);
        var cosmosDb = CreateCosmosDb();
        var container = cosmosDb.Container<TestItem>(ContainerId, o => o.WithPartitionKey(m => m.PartitionKey));
        var outbox = new CosmosDbEventPublisher(cosmosDb);
        var unitOfWork = new CosmosDbUnitOfWork(cosmosDb, outbox);

        var pk = NewId();
        var id = NewId();

        // A standalone, listened-to Activity simulating the originating operation (e.g. the API request that raised the event) - independent of the test runner's own ambient activity, so its
        // traceparent is deterministically embedded into the outbox document (via IEventFormatter.AddTracing, which uses Activity.Current when no explicit trace context has already been set).
        using var producerSource = new System.Diagnostics.ActivitySource($"test.producer.{NewId()}");
        using var producerListener = new System.Diagnostics.ActivityListener
        {
            ShouldListenTo = s => s.Name == producerSource.Name,
            Sample = (ref System.Diagnostics.ActivityCreationOptions<System.Diagnostics.ActivityContext> _) => System.Diagnostics.ActivitySamplingResult.AllDataAndRecorded
        };
        System.Diagnostics.ActivitySource.AddActivityListener(producerListener);
        using var producerActivity = producerSource.StartActivity("original-request");
        producerActivity.Should().NotBeNull();

        await unitOfWork.TransactionAsync(async ct =>
        {
            var created = await container.CreateAsync(new TestItem { Id = id, PartitionKey = pk, Name = "Widget" }, ct).ConfigureAwait(false);
            unitOfWork.Events.Add(EventData.CreateEventWith(created.Value, EventAction.Created).WithSource(new Uri("https://unittest/coreex-cosmos", UriKind.Absolute)).WithPartitionKey(pk));
        });

        var rawContainer = cosmosDb.GetContainer(ContainerId);
        var outboxDocs = await QueryOutboxDocsAsync(rawContainer, pk);
        outboxDocs.Should().ContainSingle();

        var markers = new List<System.Diagnostics.Activity>();
        using var markerListener = new System.Diagnostics.ActivityListener
        {
            ShouldListenTo = s => s.Name == CoreEx.Events.CloudEventTracingExtensions.RelayMarkerActivitySourceName,
            Sample = (ref System.Diagnostics.ActivityCreationOptions<System.Diagnostics.ActivityContext> _) => System.Diagnostics.ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = a => { lock (markers) markers.Add(a); }
        };
        System.Diagnostics.ActivitySource.AddActivityListener(markerListener);

        var testPublisher = new TestEventPublisher();
        using var sp = CreateServiceProvider(testPublisher);
        var processor = new CosmosDbOutboxRelayProcessor(sp, ContainerId, NullLogger<CosmosDbOutboxRelayProcessor>.Instance);

        await processor.ProcessBatchAsync(outboxDocs, CancellationToken.None);

        markers.Should().ContainSingle();
        markers[0].TraceId.Should().Be(producerActivity!.TraceId);
        markers[0].ParentSpanId.Should().Be(producerActivity.SpanId);
    }

    [Test]
    public async Task ProcessBatchAsync_PublishFailure_StillRecordsLagMetrics()
    {
        await GetOrCreateContainerAsync(ContainerId).ConfigureAwait(false);
        var cosmosDb = CreateCosmosDb();
        var container = cosmosDb.Container<TestItem>(ContainerId, o => o.WithPartitionKey(m => m.PartitionKey));
        var outbox = new CosmosDbEventPublisher(cosmosDb);
        var unitOfWork = new CosmosDbUnitOfWork(cosmosDb, outbox);

        var pk = NewId();
        await unitOfWork.TransactionAsync(async ct =>
        {
            var created = await container.CreateAsync(new TestItem { Id = NewId(), PartitionKey = pk, Name = "Widget" }, ct).ConfigureAwait(false);
            unitOfWork.Events.Add(EventData.CreateEventWith(created.Value, EventAction.Created).WithSource(new Uri("https://unittest/coreex-cosmos", UriKind.Absolute)).WithPartitionKey(pk));
        });

        var rawContainer = cosmosDb.GetContainer(ContainerId);
        var outboxDocs = await QueryOutboxDocsAsync(rawContainer, pk);
        outboxDocs.Should().ContainSingle();

        var testPublisher = new TestEventPublisher { ThrowOnPublish = true };
        using var sp = CreateServiceProvider(testPublisher);
        var processor = new CosmosDbOutboxRelayProcessor(sp, ContainerId, NullLogger<CosmosDbOutboxRelayProcessor>.Instance);

        var oldestLagRecorded = false;
        using var meterListener = new System.Diagnostics.Metrics.MeterListener();
        meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == CosmosMetrics.Meter.Name && instrument.Name == "cosmos.outbox.relay.oldest_lag")
                listener.EnableMeasurementEvents(instrument);
        };
        meterListener.SetMeasurementEventCallback<double>((_, _, _, _) => oldestLagRecorded = true);
        meterListener.Start();

        // A failed publish must still propagate (feeds the circuit breaker/Change Feed's own redelivery) - but the lag metric must be recorded regardless, so a stuck relay shows up as growing
        // lag rather than an absent metric.
        Assert.ThrowsAsync<InvalidOperationException>(async () => await processor.ProcessBatchAsync(outboxDocs, CancellationToken.None));

        oldestLagRecorded.Should().BeTrue();
    }

    [Test]
    public async Task ProcessBatchAsync_NonOutboxDocument_IsIgnored()
    {
        var testPublisher = new TestEventPublisher();
        using var sp = CreateServiceProvider(testPublisher);
        var processor = new CosmosDbOutboxRelayProcessor(sp, "irrelevant-container", NullLogger<CosmosDbOutboxRelayProcessor>.Instance);

        // A co-located business document change, as delivered verbatim by the Change Feed Processor (not $outbox,-prefixed) - must never reach the publisher.
        var businessDoc = new CosmosDbOutboxEvent { Id = NewId(), PartitionKey = "pk", Destination = "irrelevant", Event = default };

        await processor.ProcessBatchAsync([businessDoc], CancellationToken.None);

        testPublisher.Published.Should().BeEmpty();
    }

    // Fixed (not per-run GUID-suffixed) container names, matching every other fixture in this project - the local emulator caps the TOTAL number of containers across the whole account
    // (AZURE_COSMOS_EMULATOR_PARTITION_COUNT, see docker-compose.yml), and a new container pair per test run/rerun burns through that budget fast for no benefit (confirmed the hard way this session).
    private const string CircuitBreakerContainerId = "relay-cb-items";
    private const string CircuitBreakerLeaseContainerId = "relay-cb-items-leases";

    [Test]
    public async Task Relay_CircuitBreaker_TripsOnRepeatedPublishFailure_ThenSelfRecovers()
    {
        var containerId = CircuitBreakerContainerId;
        var leaseContainerId = CircuitBreakerLeaseContainerId;
        await GetOrCreateContainerAsync(containerId).ConfigureAwait(false);
        await TestDatabase.CreateContainerIfNotExistsAsync(leaseContainerId, "/id").ConfigureAwait(false);

        var cosmosDb = CreateCosmosDb();
        var container = cosmosDb.Container<TestItem>(containerId, o => o.WithPartitionKey(m => m.PartitionKey));

        var testPublisher = new TestEventPublisher { ThrowOnPublish = true };
        using var sp = CreateServiceProvider(testPublisher);
        var processor = new CosmosDbOutboxRelayProcessor(sp, containerId, NullLogger<CosmosDbOutboxRelayProcessor>.Instance);

        var options = new CosmosDbOutboxRelayOptions
        {
            ContainerId = containerId,
            LeaseContainerId = leaseContainerId,
            InstanceName = $"instance-{NewId()}",
            PollInterval = TimeSpan.FromMilliseconds(200),
            // Confirmed empirically (not assumed): the Change Feed Processor batches whatever is pending at each poll rather than delivering one item per poll, so staggering distinct item creations does not
            // reliably produce distinct pipeline executions the way a SQL claim-a-batch-per-tick loop would. What IS consistent, observed across repeated runs: the processor's own retry-of-a-failing-batch
            // backoff delivers attempt 1 near-immediately and attempt 2 within roughly 13-15s - so minimumThroughput=2 with a samplingDuration comfortably wider than that gap trips reliably after attempt 2,
            // without needing a 3rd attempt (which the backoff stretches out much further).
            Resiliency = CosmosDbOutboxRelayResiliency.CreateRelayCircuitBreakerResiliency(minimumThroughput: 2, samplingDuration: TimeSpan.FromSeconds(30), breakDuration: TimeSpan.FromMilliseconds(500))
        };

        await using var relay = new CosmosDbOutboxRelay(cosmosDb.Database, options, processor, NullLogger<CosmosDbOutboxRelay>.Instance);
        await relay.StartAsync();
        try
        {
            var outbox = new CosmosDbEventPublisher(cosmosDb);
            var unitOfWork = new CosmosDbUnitOfWork(cosmosDb, outbox);

            var pk = NewId();
            await unitOfWork.TransactionAsync(async ct =>
            {
                var created = await container.CreateAsync(new TestItem { Id = NewId(), PartitionKey = pk, Name = "AlwaysFails" }, ct).ConfigureAwait(false);
                unitOfWork.Events.Add(EventData.CreateEventWith(created.Value, EventAction.Created).WithSource(new Uri("https://unittest/coreex-cosmos", UriKind.Absolute)).WithPartitionKey(pk));
            });

            await WaitUntilAsync(() => relay.Status == ServiceStatus.Paused, TimeSpan.FromSeconds(30));
            relay.Status.Should().Be(ServiceStatus.Paused);
            relay.StatusReason.Should().Contain(containerId);

            // Remove the failure condition; the relay should self-resume and the stuck event should finally be relayed successfully.
            testPublisher.ThrowOnPublish = false;

            await WaitUntilAsync(() => relay.Status == ServiceStatus.Running, TimeSpan.FromSeconds(10));
            relay.Status.Should().Be(ServiceStatus.Running);

            await WaitUntilAsync(() => testPublisher.Published.Count > 0, TimeSpan.FromSeconds(15));
            testPublisher.Published.Should().NotBeEmpty();
        }
        finally
        {
            await relay.StopAsync();
        }
    }

    private const string DisposeLeaseContainerId = "relay-items-leases";

    [Test]
    public async Task DisposeAsync_WithoutPriorStop_StopsProcessorAndIsIdempotent()
    {
        // Regression: DisposeAsync previously only disposed the semaphore, never stopping a running Change Feed Processor - leaving it running (with its callbacks racing disposed state/resources)
        // if the caller disposed a started relay without an explicit preceding StopAsync. It must now stop the processor as part of disposal, and remain safe to call more than once.
        await GetOrCreateContainerAsync(ContainerId).ConfigureAwait(false);
        await TestDatabase.CreateContainerIfNotExistsAsync(DisposeLeaseContainerId, "/id").ConfigureAwait(false);

        var cosmosDb = CreateCosmosDb();
        using var sp = CreateServiceProvider(new TestEventPublisher());
        var processor = new CosmosDbOutboxRelayProcessor(sp, ContainerId, NullLogger<CosmosDbOutboxRelayProcessor>.Instance);

        var options = new CosmosDbOutboxRelayOptions
        {
            ContainerId = ContainerId,
            LeaseContainerId = DisposeLeaseContainerId,
            InstanceName = $"instance-{NewId()}"
        };

        var relay = new CosmosDbOutboxRelay(cosmosDb.Database, options, processor, NullLogger<CosmosDbOutboxRelay>.Instance);
        await relay.StartAsync();
        relay.Status.Should().Be(ServiceStatus.Running);

        await relay.DisposeAsync();
        relay.Status.Should().Be(ServiceStatus.Stopped);

        // Idempotent - a second dispose must not throw (e.g. re-disposing the semaphore, or calling StopAsync again against already-disposed synchronization resources).
        Func<Task> act = async () => await relay.DisposeAsync();
        await act.Should().NotThrowAsync();
    }

    private sealed class TestEventPublisher : EventPublisherBase
    {
        public List<DestinationEvent> Published { get; } = [];

        public bool ThrowOnPublish { get; set; }

        protected override Task OnPublishAsync(DestinationEvent[] events, CancellationToken cancellationToken = default)
        {
            if (ThrowOnPublish)
                throw new InvalidOperationException("Simulated publish failure.");

            lock (Published)
                Published.AddRange(events);

            return Task.CompletedTask;
        }
    }
}
