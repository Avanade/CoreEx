using CoreEx.Database.Outbox;
using CoreEx.Database.SqlServer;
using CoreEx.Database.SqlServer.Outbox;
using CoreEx.Events.Publishing;
using CoreEx.Hosting;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Diagnostics;

namespace CoreEx.Database.Test.Unit.Outbox;

[TestFixture]
public class DatabaseOutboxRelayHostedServiceBaseTests
{
    private static SqlServerDatabase CreateDatabase() => new((SqlConnection)SqlClientFactory.Instance.CreateConnection());

    private static async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
    {
        var sw = Stopwatch.StartNew();
        while (!condition())
        {
            if (sw.Elapsed > timeout)
                throw new TimeoutException("Condition was not met within the timeout.");

            await Task.Delay(10);
        }
    }

    [Test]
    public async Task DefaultSettings_DoNotThrowOnStart()
    {
        // Regression: PartitionSize (default 4) and PerWorkerPartitionCount (previously an unconditional literal 6) used to be mutually incompatible out of the box - PartitionPicker's constructor
        // throws when perWorkerPartitionCount > partitionSize, so starting with zero configuration overrides threw at startup. PerWorkerPartitionCount's default is now capped at whatever
        // PartitionSize resolves to.
        var sc = new ServiceCollection();
        sc.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        sc.AddExecutionContext();
        using var sp = sc.BuildServiceProvider();

        var svc = new TestOutboxRelayHostedService(sp, NullLogger.Instance)
        {
            RelayFactory = _ => new TestOutboxRelay(CreateDatabase(), new NoOpEventPublisher(), new TestRelayState { ThrowAlways = false })
        };

        Assert.DoesNotThrowAsync(async () => await svc.StartAsync(CancellationToken.None));
        await svc.StopAsync(CancellationToken.None);
    }

    [Test]
    public async Task SharedPartitionSize_IsUsedAndReported()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
            [
                new(DatabaseOutboxConfiguration.PartitionSizeConfigurationKey, "2"),
                new("CoreEx:Host:Services:OutboxRelay:PerWorkerPartitionCount", "1")
            ])
            .Build();
        var sc = new ServiceCollection();
        sc.AddSingleton<IConfiguration>(configuration);
        sc.AddExecutionContext();
        using var sp = sc.BuildServiceProvider();

        var svc = new TestOutboxRelayHostedService(sp, NullLogger.Instance)
        {
            RelayFactory = _ => new TestOutboxRelay(CreateDatabase(), new NoOpEventPublisher(), new TestRelayState { ThrowAlways = false })
        };

        await svc.StartAsync(CancellationToken.None);
        try
        {
            svc.PartitionSize.Should().Be(2);
            var data = new Dictionary<string, object>();
            svc.GetHealthStatus(data);
            ((Dictionary<string, object>)data["OutboxRelay"])["PartitionSize"].Should().Be(2);
        }
        finally
        {
            await svc.StopAsync(CancellationToken.None);
        }
    }

    [Test]
    public void LegacyPartitionSize_FailsAtStartup()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection([new("CoreEx:Host:Services:OutboxRelay:PartitionSize", "2")])
            .Build();
        var sc = new ServiceCollection();
        sc.AddSingleton<IConfiguration>(configuration);
        sc.AddExecutionContext();
        using var sp = sc.BuildServiceProvider();

        var svc = new TestOutboxRelayHostedService(sp, NullLogger.Instance)
        {
            RelayFactory = _ => new TestOutboxRelay(CreateDatabase(), new NoOpEventPublisher(), new TestRelayState { ThrowAlways = false })
        };

        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => await svc.StartAsync(CancellationToken.None));
        ex!.Message.Should().Contain(DatabaseOutboxConfiguration.PartitionSizeConfigurationKey);
    }

    [Test]
    public void SqlServerOutboxPublisher_UsesSharedPartitionSize()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection([new(DatabaseOutboxConfiguration.PartitionSizeConfigurationKey, "3")])
            .Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();
        services.AddScoped(_ => CreateDatabase());
        services.AddSqlServerOutboxPublisher((_, publisher) => publisher.PartitionSize = 8);
        using var sp = services.BuildServiceProvider();

        var publisher = (SqlServerOutboxPublisher)sp.GetRequiredKeyedService<IEventPublisher>(SqlServerOutboxPublisher.DefaultServiceKey);
        publisher.PartitionSize.Should().Be(3);
    }

    [Test]
    public async Task Relay_CircuitBreaker_TripsOnSustainedFailure_ThenSelfRecovers()
    {
        var state = new TestRelayState();
        var sc = new ServiceCollection();

        // Explicit single-partition config here for deterministic per-partition test timing - not needed to avoid the (now-fixed) incompatible-defaults bug, see DefaultSettings_DoNotThrowOnStart.
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
            [
                new(DatabaseOutboxConfiguration.PartitionSizeConfigurationKey, "1"),
                new("CoreEx:Host:Services:OutboxRelay:PerWorkerPartitionCount", "1")
            ])
            .Build();
        sc.AddSingleton<IConfiguration>(configuration);
        sc.AddExecutionContext();
        using var sp = sc.BuildServiceProvider();

        var svc = new TestOutboxRelayHostedService(sp, NullLogger.Instance)
        {
            Interval = TimeSpan.FromMilliseconds(30),
            FirstInterval = TimeSpan.FromMilliseconds(5),
            RelayFactory = _ => new TestOutboxRelay(CreateDatabase(), new NoOpEventPublisher(), state)
        };

        await svc.StartAsync(CancellationToken.None);
        try
        {
            // Every partition attempt fails, so the breaker should trip well within a couple of ticks - pausing the hosted service without any manual ResumeAsync() call.
            await WaitUntilAsync(() => svc.Status == ServiceStatus.Paused, TimeSpan.FromSeconds(10));
            svc.Status.Should().Be(ServiceStatus.Paused);

            // Remove the failure condition; the breaker's own timer should resume the service automatically, and the next tick should succeed.
            state.ThrowAlways = false;

            await WaitUntilAsync(() => svc.Status != ServiceStatus.Paused && svc.Status != ServiceStatus.Pausing, TimeSpan.FromSeconds(5));
            svc.Status.Should().NotBe(ServiceStatus.Paused);
        }
        finally
        {
            await svc.StopAsync(CancellationToken.None);
        }
    }

    private sealed class TestRelayState
    {
        public volatile bool ThrowAlways = true;
    }

    private sealed class TestOutboxRelay(SqlServerDatabase database, IEventPublisher eventPublisher, TestRelayState state) : DatabaseOutboxRelayBase<SqlServerDatabase, TestOutboxRelay>(database, eventPublisher)
    {
        public override void SetStatementsByConvention(string? schema = null) { }

        protected override Task<List<DestinationEvent>> ClaimNextBatchAsync(DatabaseOutboxRelayArgs args, Guid leaseId, int partitionId, CancellationToken cancellationToken)
        {
            if (state.ThrowAlways)
                throw new InvalidOperationException("Simulated persistent claim failure.");

            return Task.FromResult(new List<DestinationEvent>());
        }
    }

    private sealed class TestOutboxRelayHostedService : DatabaseOutboxRelayHostedServiceBase<TestOutboxRelay>
    {
        public TestOutboxRelayHostedService(IServiceProvider serviceProvider, ILogger logger) : base(serviceProvider, logger)
            => Resiliency = CreateDefaultResiliency(minimumThroughput: 2, samplingDuration: TimeSpan.FromSeconds(30), breakDuration: TimeSpan.FromMilliseconds(200));

        public void GetHealthStatus(Dictionary<string, object> data) => _ = OnReportHealthStatus(data);
    }
}
