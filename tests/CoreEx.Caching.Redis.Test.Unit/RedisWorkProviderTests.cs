using CoreEx.Hosting.Work;
using System.Security.Cryptography;

namespace CoreEx.Caching.Redis.Test.Unit;

[TestFixture]
public class RedisWorkProviderTests
{
    private StackExchange.Redis.ConnectionMultiplexer _first = null!;
    private StackExchange.Redis.ConnectionMultiplexer _second = null!;

    private static CoreEx.Operations.OperationKey Key(string id) => CoreEx.Operations.OperationKey.ForDomain("CoreEx.Caching.Redis.Test.Unit", "T", id);

    private static WorkArgs Args(string id) => new("T", Key(id));

    private static WorkState State(string id) => State(Key(id));

    private static WorkState State(CoreEx.Operations.OperationKey key) => new()
    {
        Scope = key.Scope,
        DomainName = key.DomainName,
        TenantId = key.TenantId,
        OperationName = key.OperationName,
        Id = key.InstanceId,
        TypeName = "T"
    };

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
        (await first.CreateAsync(Args(id))).IsSuccess.Should().BeTrue();

        var starts = await Task.WhenAll(first.StartAsync(Key(id), "worker-a"), second.StartAsync(Key(id), "worker-b"));
        starts.Count(result => result.Outcome == WorkTransitionOutcome.Applied).Should().Be(1);
        starts.Count(result => result.Outcome == WorkTransitionOutcome.LeaseHeld).Should().Be(1);
        var lease = starts.Single(result => result.Outcome == WorkTransitionOutcome.Applied).Lease!;

        (await first.CompleteAsync(lease, "/items/1")).Outcome.Should().Be(WorkTransitionOutcome.Applied);
        (await second.CancelAsync(Key(id), "late")).Outcome.Should().Be(WorkTransitionOutcome.AlreadyFinished);
        (await second.DeleteAsync(Key(id))).IsSuccess.Should().BeTrue();
        (await first.GetAsync(Key(id))).Should().BeNull();
    }

    [Test]
    public async Task Same_InstanceId_Is_Isolated_By_Tenant_And_Domain_Scope()
    {
        var provider = new CoreEx.Redis.RedisWorkProvider(_first);
        var id = Guid.NewGuid().ToString();
        var tenantA = CoreEx.Operations.OperationKey.ForTenant("orders", "tenant-a", "import", id);
        var tenantB = CoreEx.Operations.OperationKey.ForTenant("orders", "tenant-b", "import", id);
        var domainWide = CoreEx.Operations.OperationKey.ForDomain("orders", "import", id);
        var ttl = TimeSpan.FromMinutes(1);

        foreach (var key in new[] { tenantA, tenantB, domainWide })
            (await provider.CreateAsync(State(key), ttl, default)).Should().BeTrue();

        (await provider.GetAsync(tenantA, default)).State!.OperationKey.TenantId.Should().Be("tenant-a");
        (await provider.GetAsync(tenantB, default)).State!.OperationKey.TenantId.Should().Be("tenant-b");
        (await provider.GetAsync(domainWide, default)).State!.OperationKey.Scope.Should().Be(CoreEx.Operations.OperationKeyScope.Domain);

        foreach (var key in new[] { tenantA, tenantB, domainWide })
            (await provider.DeleteAsync((await provider.GetAsync(key, default)).State!, default)).Should().BeTrue();
    }

    [Test]
    public async Task RedisKey_UsesReadableEscapedSegments_WithCustomPrefix()
    {
        var id = $"job/42:{Guid.NewGuid():N}";
        var key = CoreEx.Operations.OperationKey.ForTenant("orders:eu", "acme/*", "import[all]", id);
        var provider = new CoreEx.Redis.RedisWorkProvider(_first, new CoreEx.Redis.RedisWorkProviderOptions { KeyPrefix = "custom:work" });
        var state = State(key);
        var physical = $"custom:work:domain:orders%3Aeu:scope:tenant:tenant:acme%2F%2A:operation:import%5Ball%5D:instance:job%2F42%3A{id[(id.LastIndexOf(':') + 1)..]}";

        (await provider.CreateAsync(state, TimeSpan.FromMinutes(1), default)).Should().BeTrue();
        try
        {
            (await _first.GetDatabase().KeyExistsAsync(physical)).Should().BeTrue();
            (await provider.GetAsync(key, default)).State!.OperationKey.Should().Be(key);
        }
        finally
        {
            await provider.DeleteAsync(state, default);
        }
    }

    [Test]
    public void RedisKey_RejectsMalformedUnicodeInCustomPrefix()
    {
        Action create = () => new CoreEx.Redis.RedisWorkProvider(_first, new CoreEx.Redis.RedisWorkProviderOptions { KeyPrefix = "custom\uD800" });
        create.Should().Throw<ArgumentException>().WithParameterName("KeyPrefix");
    }

    [Test]
    public async Task RedisKey_RejectsMalformedUnicodeInWorkIdentity()
    {
        var provider = new CoreEx.Redis.RedisWorkProvider(_first);
        var invalid = CoreEx.Operations.OperationKey.ForDomain("orders", "import", "task\uD800");
        Func<Task> read = () => provider.GetAsync(invalid, default);
        (await read.Should().ThrowAsync<ArgumentException>()).WithParameterName("InstanceId");
    }

    [Test]
    public async Task Read_Returns_Redis_Server_Time_And_ETag()
    {
        var provider = new CoreEx.Redis.RedisWorkProvider(_first);
        var before = DateTimeOffset.UtcNow;
        var read = await provider.GetAsync(Key(Guid.NewGuid().ToString()), default);
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

        var created = await orchestrator.CreateAsync(Args(id));
        created.IsSuccess.Should().BeTrue();
        created.Value.User.Should().Be(CoreEx.Security.AuthenticationUser.EnvironmentUser);

        var stored = (await provider.GetAsync(Key(id), default)).State!;
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

        (await orchestrator.CreateAsync(Args(id))).IsSuccess.Should().BeTrue();
        var first = (await orchestrator.StartAsync(Key(id), "worker-a")).Lease!;
        clock.Now = clock.Now.AddSeconds(31);
        var takeover = await orchestrator.StartAsync(Key(id), "worker-b");

        takeover.Outcome.Should().Be(WorkTransitionOutcome.Applied);
        takeover.IsTakeover.Should().BeTrue();
        takeover.Lease!.Epoch.Should().Be(first.Epoch + 1);
        (await orchestrator.CompleteAsync(first)).Outcome.Should().Be(WorkTransitionOutcome.LeaseLost);
        (await orchestrator.CompleteAsync(takeover.Lease)).Outcome.Should().Be(WorkTransitionOutcome.Applied);
        (await orchestrator.DeleteAsync(Key(id))).IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task Write_Timeout_Reports_Unknown_Although_Write_Later_Applies()
    {
        var id = Guid.NewGuid().ToString();
        var provider = new CoreEx.Redis.RedisWorkProvider(_first, new CoreEx.Redis.RedisWorkProviderOptions { OperationTimeout = TimeSpan.FromMilliseconds(200) });
        var state = State(id);
        state.Status = WorkStatus.Created;
        state.Revision = Guid.NewGuid().ToString("N");

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

        var stored = (await provider.GetAsync(Key(id), default)).State;
        stored.Should().NotBeNull();
        stored!.Revision.Should().Be(state.Revision);
        (await provider.DeleteAsync(stored, default)).Should().BeTrue();
    }

    [Test]
    public async Task Caller_Cancellation_Is_Not_Reported_As_Unknown()
    {
        var provider = new CoreEx.Redis.RedisWorkProvider(_first);
        var state = State(Guid.NewGuid().ToString());
        state.Status = WorkStatus.Created;
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

        var stored = (await provider.GetAsync(state.OperationKey, default)).State;
        if (stored is not null)
            (await provider.DeleteAsync(stored, default)).Should().BeTrue();
    }

    [Test]
    public async Task StaleETag_IsRejected_AndLegacyDataField_IsIgnored()
    {
        var provider = new CoreEx.Redis.RedisWorkProvider(_first);
        var id = Guid.NewGuid().ToString();
        var state = State(id);
        state.Status = WorkStatus.Created;
        (await provider.CreateAsync(state, TimeSpan.FromMinutes(1), default)).Should().BeTrue();

        var a = (await provider.GetAsync(Key(id), default)).State!;
        var b = (await provider.GetAsync(Key(id), default)).State!;
        a.Status = WorkStatus.Started;
        (await provider.UpdateAsync(a, TimeSpan.FromMinutes(1), default)).Should().BeTrue();
        (await provider.UpdateAsync(b, TimeSpan.FromMinutes(1), default)).Should().BeFalse();
        (await provider.DeleteAsync(b, default)).Should().BeFalse();

        var key = $"CoreEx:work:v1:{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(id)))}";
        await _first.GetDatabase().HashSetAsync(key, "d", "legacy result");
        var current = (await provider.GetAsync(Key(id), default)).State!;
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
        (await provider.CreateAsync(State(id), TimeSpan.FromMilliseconds(200), default)).Should().BeTrue();
        await Task.Delay(400);
        (await provider.GetAsync(Key(id), default)).State.Should().BeNull();
    }

    [Test]
    public async Task RecreatedRecord_Rejects_Previous_Incarnation()
    {
        var first = new CoreEx.Redis.RedisWorkProvider(_first);
        var second = new CoreEx.Redis.RedisWorkProvider(_second);
        var id = Guid.NewGuid().ToString();
        var ttl = TimeSpan.FromMinutes(1);
        var original = State(id);
        (await first.CreateAsync(original, ttl, default)).Should().BeTrue();
        (await first.UpdateAsync(original, ttl, default)).Should().BeTrue();
        var stale = (await second.GetAsync(Key(id), default)).State!;
        (await first.DeleteAsync(original, default)).Should().BeTrue();

        var replacement = State(id);
        (await second.CreateAsync(replacement, ttl, default)).Should().BeTrue();
        (await second.UpdateAsync(replacement, ttl, default)).Should().BeTrue();
        replacement.ETag.Should().NotBe(stale.ETag);
        (await first.UpdateAsync(stale, ttl, default)).Should().BeFalse();
        (await first.DeleteAsync(stale, default)).Should().BeFalse();
        (await first.GetAsync(Key(id), default)).State!.ETag.Should().Be(replacement.ETag);
        (await second.DeleteAsync(replacement, default)).Should().BeTrue();
    }
}
