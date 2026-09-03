using CoreEx.Data;
using CoreEx.Database.Outbox;
using CoreEx.Database.SqlServer;
using CoreEx.Events.Publishing;
using Microsoft.Data.SqlClient;

namespace CoreEx.Database.Test.Unit.Outbox;

[TestFixture]
public class DatabaseOutboxRelayBaseTests
{
    private static SqlServerDatabase CreateDatabase() => new((SqlConnection)SqlClientFactory.Instance.CreateConnection());

    private static DatabaseOutboxRelayArgs CreateArgs(DatabaseOutboxRelayResiliencyExecutor? resiliencyExecutor = null) => new()
    {
        // partitionSize == perWorkerPartitionCount triggers PartitionPicker's "probe all partitions" path - deterministic, covers every partition every call.
        PartitionPicker = new PartitionPicker(partitionSize: 2, perWorkerPartitionCount: 2),
        BatchSize = 10,
        LeaseDuration = TimeSpan.FromSeconds(5),
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

    private sealed class TestOutboxRelay(SqlServerDatabase database, IEventPublisher eventPublisher) : DatabaseOutboxRelayBase<SqlServerDatabase, TestOutboxRelay>(database, eventPublisher)
    {
        public List<int> AttemptedPartitions { get; } = [];

        public int? FailingPartitionId { get; set; }

        public override void SetStatementsByConvention(string? schema = null) { }

        protected override Task<List<DestinationEvent>> ClaimNextBatchAsync(DatabaseOutboxRelayArgs args, Guid leaseId, int partitionId, CancellationToken cancellationToken)
        {
            lock (AttemptedPartitions)
                AttemptedPartitions.Add(partitionId);

            if (partitionId == FailingPartitionId)
                throw new InvalidOperationException($"Simulated claim failure for partition {partitionId}.");

            // No events claimed - keeps the test focused purely on the outer per-partition loop, not the claim/publish/complete pipeline.
            return Task.FromResult(new List<DestinationEvent>());
        }
    }
}
