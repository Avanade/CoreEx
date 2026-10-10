using CoreEx.Data;
using CoreEx.Database.Outbox;
using CoreEx.Database.SqlServer;
using CoreEx.Events;
using CoreEx.Events.Publishing;
using Microsoft.Data.SqlClient;
using System.Diagnostics;

namespace CoreEx.Database.Test.Unit.Outbox;

[TestFixture]
public class DatabaseOutboxRelayBaseTests
{
    private static SqlServerDatabase CreateDatabase() => new((SqlConnection)SqlClientFactory.Instance.CreateConnection());

    private static DatabaseOutboxRelayArgs CreateArgs(DatabaseOutboxRelayResiliencyExecutor? resiliencyExecutor = null, TimeSpan? leaseDuration = null) => new()
    {
        // partitionSize == perWorkerPartitionCount triggers PartitionPicker's "probe all partitions" path - deterministic, covers every partition every call.
        PartitionPicker = new PartitionPicker(partitionSize: 2, perWorkerPartitionCount: 2),
        BatchSize = 10,
        LeaseDuration = leaseDuration ?? TimeSpan.FromSeconds(5),
        BackOffDuration = TimeSpan.FromSeconds(1),
        ResiliencyExecutor = resiliencyExecutor ?? ((work, ct) => work(ct))
    };

    [Test]
    public async Task RelayAsync_OnePartitionFailure_DoesNotBlockSiblingPartitions()
    {
        // Regression: a failure for one partition must not abort the whole tick - every other assigned partition must still be attempted.
        var relay = new TestOutboxRelay(CreateDatabase(), new NoOpEventPublisher()) { FailingPartitionId = 0 };

        await relay.RelayAsync(CreateArgs(), CancellationToken.None);

        relay.AttemptedPartitions.Should().BeEquivalentTo([0, 1]);
    }

    [Test]
    public async Task RelayAsync_ResiliencyExecutor_ObservesEachPartitionOutcome()
    {
        // The resiliency executor (owned by a caller such as DatabaseOutboxRelayHostedServiceBase) must be invoked once per partition, seeing both the failure and the success.
        var relay = new TestOutboxRelay(CreateDatabase(), new NoOpEventPublisher()) { FailingPartitionId = 0 };

        var observed = new List<bool>();
        DatabaseOutboxRelayResiliencyExecutor executor = async (work, ct) =>
        {
            var result = await work(ct).ConfigureAwait(false);
            observed.Add(result.IsSuccess);
            return result;
        };

        await relay.RelayAsync(CreateArgs(executor), CancellationToken.None);

        observed.Should().HaveCount(2);
        observed.Should().Contain(false); // partition 0, failed
        observed.Should().Contain(true);  // partition 1, succeeded
    }

    [Test]
    public async Task RelayAsync_EmitsPerEventRelayMarker_ParentedToOriginatingTrace()
    {
        // Simulate the original producer's trace (e.g. the API request that raised the event) - completely independent of, and unaware of, the relay.
        using var producerSource = new ActivitySource($"test.producer.{Guid.NewGuid()}");
        using var producerListener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == producerSource.Name,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(producerListener);

        using var producerActivity = producerSource.StartActivity("original-request");
        producerActivity.Should().NotBeNull();

        var cloudEvent = new CloudNative.CloudEvents.CloudEvent { Id = "evt-1", Type = "test.event", Source = new Uri("urn:test"), Time = DateTimeOffset.UtcNow };
        cloudEvent.SetExtensionAttribute("traceparent", producerActivity!.Id);

        var markers = new List<Activity>();
        using var markerListener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == CloudEventTracingExtensions.RelayMarkerActivitySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = a => { lock (markers) markers.Add(a); }
        };
        ActivitySource.AddActivityListener(markerListener);

        var relay = new TestOutboxRelay(CreateDatabase(), new NoOpEventPublisher())
        {
            EventsForPartition = partitionId => partitionId == 0 ? [new DestinationEvent("test-destination", cloudEvent)] : []
        };

        await relay.RelayAsync(CreateArgs(), CancellationToken.None);

        markers.Should().ContainSingle();
        markers[0].TraceId.Should().Be(producerActivity.TraceId);
        markers[0].ParentSpanId.Should().Be(producerActivity.SpanId);
        markers[0].GetTagItem("outbox.event.id").Should().Be("evt-1");
        markers[0].GetTagItem("outbox.destination").Should().Be("test-destination");
    }

    [Test]
    public async Task RelayAsync_LeaseDurationExceeded_ReleasesClaimAndContinues()
    {
        // Regression: a publish exceeding the lease duration (surfacing as a nested AggregateException-wrapped cancellation) must release the claim using a live token, and must not abort the tick.
        var relay = new TestOutboxRelay(CreateDatabase(), new NoOpEventPublisher())
        {
            EventsForPartition = _ => [new DestinationEvent("test-destination", new CloudNative.CloudEvents.CloudEvent { Id = Guid.NewGuid().ToString(), Type = "test.event", Source = new Uri("urn:test") })],
            WaitForLeaseExpiry = true,
            PublishException = ct => new AggregateException(new AggregateException(new TaskCanceledException(null, null, ct)))
        };

        await relay.RelayAsync(CreateArgs(leaseDuration: TimeSpan.FromMilliseconds(50)), CancellationToken.None);

        relay.AttemptedPartitions.Should().BeEquivalentTo([0, 1]);
        relay.CancelBatchTokenWasCanceled.Should().BeEquivalentTo([false, false]);
    }

    [Test]
    public async Task RelayAsync_CallerCancellation_Rethrows()
    {
        using var cts = new CancellationTokenSource();
        var relay = new TestOutboxRelay(CreateDatabase(), new NoOpEventPublisher())
        {
            EventsForPartition = _ => [new DestinationEvent("test-destination", new CloudNative.CloudEvents.CloudEvent { Id = Guid.NewGuid().ToString(), Type = "test.event", Source = new Uri("urn:test") })],
            PublishException = _ => { cts.Cancel(); return new AggregateException(new OperationCanceledException(cts.Token)); }
        };

        var act = () => relay.RelayAsync(CreateArgs(), cts.Token);
        await act.Should().ThrowAsync<AggregateException>();
        relay.AttemptedPartitions.Should().ContainSingle();
    }

    private sealed class TestOutboxRelay(SqlServerDatabase database, IEventPublisher eventPublisher) : DatabaseOutboxRelayBase<SqlServerDatabase, TestOutboxRelay>(database, eventPublisher)
    {
        public List<int> AttemptedPartitions { get; } = [];

        public List<bool> CancelBatchTokenWasCanceled { get; } = [];

        public int? FailingPartitionId { get; set; }

        public Func<int, List<DestinationEvent>>? EventsForPartition { get; set; }

        public Func<CancellationToken, Exception>? PublishException { get; set; }

        public bool WaitForLeaseExpiry { get; set; }

        public override void SetStatementsByConvention(string? schema = null) { }

        protected override Task<List<DestinationEvent>> ClaimNextBatchAsync(DatabaseOutboxRelayArgs args, Guid leaseId, int partitionId, CancellationToken cancellationToken)
        {
            lock (AttemptedPartitions)
                AttemptedPartitions.Add(partitionId);

            if (partitionId == FailingPartitionId)
                throw new InvalidOperationException($"Simulated claim failure for partition {partitionId}.");

            // No events claimed by default - keeps the pre-existing tests focused purely on the outer per-partition loop, not the claim/publish/complete pipeline.
            return Task.FromResult(EventsForPartition?.Invoke(partitionId) ?? []);
        }

        // Simulates a publish failure (CompleteBatchAsync runs inside the same try as the publish); otherwise a no-op - avoids requiring a real database connection for tests that DO claim events
        // (CreateDatabase() has no live connection); the claim/publish path is exercised via ClaimNextBatchAsync/EventPublisher instead, which is all these tests care about.
        protected override async Task CompleteBatchAsync(DatabaseOutboxRelayArgs args, Guid leaseId, CancellationToken cancellationToken)
        {
            if (PublishException is null)
                return;

            if (WaitForLeaseExpiry)
                await Task.Delay(Timeout.Infinite, cancellationToken).ContinueWith(_ => { }, TaskScheduler.Default);

            throw PublishException(cancellationToken);
        }

        protected override Task CancelBatchAsync(DatabaseOutboxRelayArgs args, Guid leaseId, CancellationToken cancellationToken)
        {
            lock (CancelBatchTokenWasCanceled)
                CancelBatchTokenWasCanceled.Add(cancellationToken.IsCancellationRequested);

            return Task.CompletedTask;
        }
    }
}
