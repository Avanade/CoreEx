using CoreEx.Hosting.Work;
using System.Security.Cryptography;

namespace CoreEx.Caching.Redis.Test.Unit;

[TestFixture]
public class RedisWorkProviderTests
{
    private StackExchange.Redis.ConnectionMultiplexer _first = null!;
    private StackExchange.Redis.ConnectionMultiplexer _second = null!;

    /// <summary>Provides a controllable clock for Redis work tests.</summary>
    private sealed class ManualTimeProvider(DateTimeOffset now) : TimeProvider
    {
        /// <summary>Gets or sets the current test time.</summary>
        public DateTimeOffset Now { get; set; } = now;

        /// <inheritdoc/>
        public override DateTimeOffset GetUtcNow() => Now;
    }

    [OneTimeSetUp]
    public async Task SetUp()
    {
        var connection = Environment.GetEnvironmentVariable("COREEX_REDIS_TEST_CONNECTION") ?? "localhost:6379";
        _first = await StackExchange.Redis.ConnectionMultiplexer.ConnectAsync(connection);
        _second = await StackExchange.Redis.ConnectionMultiplexer.ConnectAsync(connection);
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        _first.Dispose();
        _second.Dispose();
    }

    [Test]
    public async Task IndependentClients_CreateAndStartHaveSingleWinner()
    {
        var id = Guid.NewGuid().ToString();
        var first = new WorkOrchestrator(new CoreEx.Redis.RedisWorkProvider(_first));
        var second = new WorkOrchestrator(new CoreEx.Redis.RedisWorkProvider(_second));
        (await first.CreateAsync(new WorkArgs("T", id))).IsSuccess.Should().BeTrue();

        var starts = await Task.WhenAll(first.StartAsync(id, "worker-a"), second.StartAsync(id, "worker-b"));
        starts.Count(result => result.Outcome == WorkTransitionOutcome.Applied).Should().Be(1);
        starts.Count(result => result.Outcome == WorkTransitionOutcome.LeaseHeld).Should().Be(1);
        var lease = starts.Single(result => result.Outcome == WorkTransitionOutcome.Applied).Lease!;

        (await first.CompleteAsync(lease, "/items/1")).Outcome.Should().Be(WorkTransitionOutcome.Applied);
        (await second.CancelAsync(id, "late")).Outcome.Should().Be(WorkTransitionOutcome.AlreadyFinished);
        (await second.DeleteAsync(id)).IsSuccess.Should().BeTrue();
        (await first.GetAsync(id)).Should().BeNull();
    }

    [Test]
    public async Task Read_Returns_Redis_Server_Time_And_ETag()
    {
        var provider = new CoreEx.Redis.RedisWorkProvider(_first);
        var before = DateTimeOffset.UtcNow;
        var read = await provider.GetAsync(Guid.NewGuid().ToString(), default);
        var after = DateTimeOffset.UtcNow;

        read.State.Should().BeNull();
        read.Now.Should().BeOnOrAfter(before.AddSeconds(-1));
        read.Now.Should().BeOnOrBefore(after.AddSeconds(1));
    }

    [Test]
    public async Task Missing_Execution_Context_Persists_Environment_User()
    {
        CoreEx.ExecutionContext.Reset();
        var provider = new CoreEx.Redis.RedisWorkProvider(_first);
        var orchestrator = new WorkOrchestrator(provider);
        var id = Guid.NewGuid().ToString();

        var created = await orchestrator.CreateAsync(new WorkArgs("T", id));
        created.IsSuccess.Should().BeTrue();
        created.Value.User.Should().Be(CoreEx.Security.AuthenticationUser.EnvironmentUser);

        var stored = (await provider.GetAsync(id, default)).State!;
        stored.User.Should().Be(CoreEx.Security.AuthenticationUser.EnvironmentUser);
        (await provider.DeleteAsync(stored, default)).Should().BeTrue();
    }

    [Test]
    public async Task Explicit_TimeProvider_Enables_Deterministic_Lease_Takeover()
    {
        var clock = new ManualTimeProvider(new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero));
        var provider = new CoreEx.Redis.RedisWorkProvider(_first, new CoreEx.Redis.RedisWorkProviderOptions { TimeProvider = clock });
        var orchestrator = new WorkOrchestrator(provider, new WorkOrchestratorOptions { Expiry = TimeSpan.FromHours(1), LeaseDuration = TimeSpan.FromSeconds(30) });
        var id = Guid.NewGuid().ToString();

        (await orchestrator.CreateAsync(new WorkArgs("T", id))).IsSuccess.Should().BeTrue();
        var first = (await orchestrator.StartAsync(id, "worker-a")).Lease!;
        clock.Now = clock.Now.AddSeconds(31);
        var takeover = await orchestrator.StartAsync(id, "worker-b");

        takeover.Outcome.Should().Be(WorkTransitionOutcome.Applied);
        takeover.IsTakeover.Should().BeTrue();
        takeover.Lease!.Epoch.Should().Be(first.Epoch + 1);
        (await orchestrator.CompleteAsync(first)).Outcome.Should().Be(WorkTransitionOutcome.LeaseLost);
        (await orchestrator.CompleteAsync(takeover.Lease)).Outcome.Should().Be(WorkTransitionOutcome.Applied);
        (await orchestrator.DeleteAsync(id)).IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task Write_Timeout_Reports_Unknown_Although_Write_Later_Applies()
    {
        var id = Guid.NewGuid().ToString();
        var provider = new CoreEx.Redis.RedisWorkProvider(_first, new CoreEx.Redis.RedisWorkProviderOptions { OperationTimeout = TimeSpan.FromMilliseconds(200) });
        var state = new WorkState { Id = id, TypeName = "T", Status = WorkStatus.Created, Revision = Guid.NewGuid().ToString("N") };

        await _second.GetDatabase().ExecuteAsync("CLIENT", "PAUSE", 1000, "WRITE");
        try
        {
            var create = () => provider.CreateAsync(state, TimeSpan.FromMinutes(1), default);
            await FluentActions.Awaiting(create).Should().ThrowAsync<WorkOutcomeUnknownException>();
        }
        finally
        {
            await Task.Delay(1200);
        }

        var stored = (await provider.GetAsync(id, default)).State;
        stored.Should().NotBeNull();
        stored!.Revision.Should().Be(state.Revision);
        (await provider.DeleteAsync(stored, default)).Should().BeTrue();
    }

    [Test]
    public async Task Caller_Cancellation_Is_Not_Reported_As_Unknown()
    {
        var provider = new CoreEx.Redis.RedisWorkProvider(_first);
        var state = new WorkState { Id = Guid.NewGuid().ToString(), TypeName = "T", Status = WorkStatus.Created };
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));

        await _second.GetDatabase().ExecuteAsync("CLIENT", "PAUSE", 1000, "WRITE");
        try
        {
            var create = () => provider.CreateAsync(state, TimeSpan.FromMinutes(1), cts.Token);
            await FluentActions.Awaiting(create).Should().ThrowAsync<OperationCanceledException>();
        }
        finally
        {
            await Task.Delay(1200);
        }

        var stored = (await provider.GetAsync(state.Id, default)).State;
        if (stored is not null)
            (await provider.DeleteAsync(stored, default)).Should().BeTrue();
    }

    [Test]
    public async Task StaleETag_IsRejected_AndLegacyDataField_IsIgnored()
    {
        var provider = new CoreEx.Redis.RedisWorkProvider(_first);
        var id = Guid.NewGuid().ToString();
        (await provider.CreateAsync(new WorkState { Id = id, TypeName = "T", Status = WorkStatus.Created }, TimeSpan.FromMinutes(1), default)).Should().BeTrue();

        var a = (await provider.GetAsync(id, default)).State!;
        var b = (await provider.GetAsync(id, default)).State!;
        a.Status = WorkStatus.Started;
        (await provider.UpdateAsync(a, TimeSpan.FromMinutes(1), default)).Should().BeTrue();
        (await provider.UpdateAsync(b, TimeSpan.FromMinutes(1), default)).Should().BeFalse();
        (await provider.DeleteAsync(b, default)).Should().BeFalse();

        var key = $"CoreEx:work:v1:{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(id)))}";
        await _first.GetDatabase().HashSetAsync(key, "d", "legacy result");
        var current = (await provider.GetAsync(id, default)).State!;
        current.Status.Should().Be(WorkStatus.Started);
        (await provider.UpdateAsync(current, TimeSpan.FromMinutes(1), default)).Should().BeTrue();
        (await _first.GetDatabase().HashExistsAsync(key, "d")).Should().BeTrue();

        (await provider.DeleteAsync(current, default)).Should().BeTrue();
    }

    [Test]
    public async Task Records_Expire()
    {
        var provider = new CoreEx.Redis.RedisWorkProvider(_first);
        var id = Guid.NewGuid().ToString();
        (await provider.CreateAsync(new WorkState { Id = id, TypeName = "T" }, TimeSpan.FromMilliseconds(200), default)).Should().BeTrue();
        await Task.Delay(400);
        (await provider.GetAsync(id, default)).State.Should().BeNull();
    }

    [Test]
    public async Task RecreatedRecord_Rejects_Previous_Incarnation()
    {
        var first = new CoreEx.Redis.RedisWorkProvider(_first);
        var second = new CoreEx.Redis.RedisWorkProvider(_second);
        var id = Guid.NewGuid().ToString();
        var ttl = TimeSpan.FromMinutes(1);
        var original = new WorkState { Id = id, TypeName = "T" };
        (await first.CreateAsync(original, ttl, default)).Should().BeTrue();
        (await first.UpdateAsync(original, ttl, default)).Should().BeTrue();
        var stale = (await second.GetAsync(id, default)).State!;
        (await first.DeleteAsync(original, default)).Should().BeTrue();

        var replacement = new WorkState { Id = id, TypeName = "T" };
        (await second.CreateAsync(replacement, ttl, default)).Should().BeTrue();
        (await second.UpdateAsync(replacement, ttl, default)).Should().BeTrue();
        replacement.ETag.Should().NotBe(stale.ETag);
        (await first.UpdateAsync(stale, ttl, default)).Should().BeFalse();
        (await first.DeleteAsync(stale, default)).Should().BeFalse();
        (await first.GetAsync(id, default)).State!.ETag.Should().Be(replacement.ETag);
        (await second.DeleteAsync(replacement, default)).Should().BeTrue();
    }
}
