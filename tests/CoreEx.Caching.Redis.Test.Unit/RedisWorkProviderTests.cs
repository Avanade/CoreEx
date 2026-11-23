using CoreEx.Hosting.Work;
using System.Security.Cryptography;

namespace CoreEx.Caching.Redis.Test.Unit;

[TestFixture]
public class RedisWorkProviderTests
{
    private StackExchange.Redis.ConnectionMultiplexer _first = null!;
    private StackExchange.Redis.ConnectionMultiplexer _second = null!;

    private const string DomainName = "CoreEx.Caching.Redis.Test.Unit";

    private static readonly CoreEx.Hosting.IHostSettings _settings = new CoreEx.Hosting.HostSettings { SolutionName = "CoreEx", DomainName = DomainName, EnvironmentName = "Test" };

    private static CoreEx.Entities.CompositeKey CKey(string id) => CoreEx.Entities.CompositeKey.Create(id);

    private static WorkArgs Args(string id) => new("T", "T", CKey(id));

    private static WorkState State(string id, string domainName = DomainName, string? tenantId = null, string operationName = "T") => new()
    {
        DomainName = domainName,
        TenantId = tenantId,
        OperationName = operationName,
        Id = id,
        TypeName = "T"
    };

    private static Task<WorkSnapshot> GetAsync(CoreEx.Redis.RedisWorkProvider provider, string id) => GetAsync(provider, State(id));

    private static Task<WorkSnapshot> GetAsync(CoreEx.Redis.RedisWorkProvider provider, WorkState state)
        => provider.GetAsync(state.DomainName, state.TenantId, state.OperationName, state.Id ?? throw new InvalidOperationException("Missing work identifier."), default);

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
        var first = new WorkOrchestrator(new CoreEx.Redis.RedisWorkProvider(_first), _settings);
        var second = new WorkOrchestrator(new CoreEx.Redis.RedisWorkProvider(_second), _settings);
        (await first.CreateAsync(Args(id))).IsSuccess.Should().BeTrue();

        var starts = await Task.WhenAll(first.StartAsync("T", CKey(id), "worker-a"), second.StartAsync("T", CKey(id), "worker-b"));
        starts.Count(result => result.Outcome == WorkTransitionOutcome.Applied).Should().Be(1);
        starts.Count(result => result.Outcome == WorkTransitionOutcome.LeaseHeld).Should().Be(1);
        var lease = starts.Single(result => result.Outcome == WorkTransitionOutcome.Applied).Lease!;

        (await first.CompleteAsync(lease, "/items/1")).Outcome.Should().Be(WorkTransitionOutcome.Applied);
        (await second.CancelAsync("T", CKey(id), "late")).Outcome.Should().Be(WorkTransitionOutcome.AlreadyFinished);
        (await second.DeleteAsync("T", CKey(id))).IsSuccess.Should().BeTrue();
        (await first.GetAsync("T", CKey(id))).Should().BeNull();
    }

    [Test]
    public async Task Same_InstanceId_Is_Isolated_By_Tenant_And_Domain()
    {
        var provider = new CoreEx.Redis.RedisWorkProvider(_first);
        var id = Guid.NewGuid().ToString();
        var tenantA = State(id, "orders", "tenant-a", "import");
        var tenantB = State(id, "orders", "tenant-b", "import");
        var domainWide = State(id, "orders", null, "import");
        var otherDomain = State(id, "products", null, "import");
        var ttl = TimeSpan.FromMinutes(1);

        foreach (var state in new[] { tenantA, tenantB, domainWide, otherDomain })
            (await provider.CreateAsync(state, ttl, default)).Should().BeTrue();

        (await _first.GetDatabase().KeyExistsAsync($"CoreEx:work:v1:domain:orders:operation:import:instance:{id}")).Should().BeTrue();
        (await GetAsync(provider, tenantA)).State!.TenantId.Should().Be("tenant-a");
        (await GetAsync(provider, tenantB)).State!.TenantId.Should().Be("tenant-b");
        (await GetAsync(provider, domainWide)).State!.TenantId.Should().BeNull();
        (await GetAsync(provider, otherDomain)).State!.DomainName.Should().Be("products");

        foreach (var state in new[] { tenantA, tenantB, domainWide, otherDomain })
            (await provider.DeleteAsync((await GetAsync(provider, state)).State!, default)).Should().BeTrue();
    }

    [Test]
    public async Task Empty_Tenant_Is_DomainWide()
    {
        var provider = new CoreEx.Redis.RedisWorkProvider(_first);
        var id = Guid.NewGuid().ToString();
        var state = State(id, tenantId: "");

        state.TenantId.Should().BeNull();
        (await provider.CreateAsync(state, TimeSpan.FromMinutes(1), default)).Should().BeTrue();
        var read = await provider.GetAsync(state.DomainName, "", state.OperationName, id, default);
        read.State!.TenantId.Should().BeNull();
        (await GetAsync(provider, state)).State!.ETag.Should().Be(read.State.ETag);
        (await provider.DeleteAsync(state, default)).Should().BeTrue();
    }

    [Test]
    public async Task RedisKey_UsesReadableEscapedSegments_WithCustomPrefix()
    {
        var id = $"job/42:{Guid.NewGuid():N}";
        var provider = new CoreEx.Redis.RedisWorkProvider(_first, new CoreEx.Redis.RedisWorkProviderOptions { KeyPrefix = "custom:work" });
        var state = State(id, "orders:eu", "acme/*", "import[all]");
        var physical = $"custom:work:domain:orders%3Aeu:tenant:acme%2F%2A:operation:import%5Ball%5D:instance:job%2F42%3A{id[(id.LastIndexOf(':') + 1)..]}";

        (await provider.CreateAsync(state, TimeSpan.FromMinutes(1), default)).Should().BeTrue();
        try
        {
            (await _first.GetDatabase().KeyExistsAsync(physical)).Should().BeTrue();
            var stored = (await GetAsync(provider, state)).State!;
            stored.DomainName.Should().Be(state.DomainName);
            stored.TenantId.Should().Be(state.TenantId);
            stored.OperationName.Should().Be(state.OperationName);
            stored.Id.Should().Be(state.Id);
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
        Func<Task> read = () => provider.GetAsync("orders", null, "import", "task\uD800", default);
        (await read.Should().ThrowAsync<ArgumentException>()).WithParameterName("instanceId");
        Func<Task> domain = () => provider.GetAsync("orders\uD800", null, "import", "task", default);
        (await domain.Should().ThrowAsync<ArgumentException>()).WithParameterName("domainName");
        Func<Task> tenant = () => provider.GetAsync("orders", "tenant\uDC00", "import", "task", default);
        (await tenant.Should().ThrowAsync<ArgumentException>()).WithParameterName("tenantId");
        Func<Task> operation = () => provider.GetAsync("orders", null, "import\uD800", "task", default);
        (await operation.Should().ThrowAsync<ArgumentException>()).WithParameterName("operationName");
    }

    [Test]
    public async Task Read_Returns_Redis_Server_Time_And_ETag()
    {
        var provider = new CoreEx.Redis.RedisWorkProvider(_first);
        var before = DateTimeOffset.UtcNow;
        var read = await GetAsync(provider, Guid.NewGuid().ToString());
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
        var orchestrator = new WorkOrchestrator(provider, _settings);
        var id = Guid.NewGuid().ToString();

        var created = await orchestrator.CreateAsync(Args(id));
        created.IsSuccess.Should().BeTrue();
        created.Value.User.Should().Be(CoreEx.Security.AuthenticationUser.EnvironmentUser);

        var stored = (await GetAsync(provider, id)).State!;
        stored.User.Should().Be(CoreEx.Security.AuthenticationUser.EnvironmentUser);
        (await provider.DeleteAsync(stored, default)).Should().BeTrue();
    }

    [Test]
    public async Task Explicit_TimeProvider_Enables_Deterministic_Lease_Takeover()
    {
        var clock = new ManualTimeProvider(new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero));
        var provider = new CoreEx.Redis.RedisWorkProvider(_first, new CoreEx.Redis.RedisWorkProviderOptions { TimeProvider = clock });
        var orchestrator = new WorkOrchestrator(provider, _settings, new WorkOrchestratorOptions { Expiry = TimeSpan.FromHours(1), LeaseDuration = TimeSpan.FromSeconds(30) });
        var id = Guid.NewGuid().ToString();

        (await orchestrator.CreateAsync(Args(id))).IsSuccess.Should().BeTrue();
        var first = (await orchestrator.StartAsync("T", CKey(id), "worker-a")).Lease!;
        clock.Now = clock.Now.AddSeconds(31);
        var takeover = await orchestrator.StartAsync("T", CKey(id), "worker-b");

        takeover.Outcome.Should().Be(WorkTransitionOutcome.Applied);
        takeover.IsTakeover.Should().BeTrue();
        takeover.Lease!.Epoch.Should().Be(first.Epoch + 1);
        (await orchestrator.CompleteAsync(first)).Outcome.Should().Be(WorkTransitionOutcome.LeaseLost);
        (await orchestrator.CompleteAsync(takeover.Lease)).Outcome.Should().Be(WorkTransitionOutcome.Applied);
        (await orchestrator.DeleteAsync("T", CKey(id))).IsSuccess.Should().BeTrue();
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

        var stored = (await GetAsync(provider, id)).State;
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

        var stored = (await GetAsync(provider, state)).State;
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

        var a = (await GetAsync(provider, id)).State!;
        var b = (await GetAsync(provider, id)).State!;
        a.Status = WorkStatus.Started;
        (await provider.UpdateAsync(a, TimeSpan.FromMinutes(1), default)).Should().BeTrue();
        (await provider.UpdateAsync(b, TimeSpan.FromMinutes(1), default)).Should().BeFalse();
        (await provider.DeleteAsync(b, default)).Should().BeFalse();

        var key = $"CoreEx:work:v1:{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(id)))}";
        await _first.GetDatabase().HashSetAsync(key, "d", "legacy result");
        var current = (await GetAsync(provider, id)).State!;
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
        (await GetAsync(provider, id)).State.Should().BeNull();
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
        var stale = (await GetAsync(second, id)).State!;
        (await first.DeleteAsync(original, default)).Should().BeTrue();

        var replacement = State(id);
        (await second.CreateAsync(replacement, ttl, default)).Should().BeTrue();
        (await second.UpdateAsync(replacement, ttl, default)).Should().BeTrue();
        replacement.ETag.Should().NotBe(stale.ETag);
        (await first.UpdateAsync(stale, ttl, default)).Should().BeFalse();
        (await first.DeleteAsync(stale, default)).Should().BeFalse();
        (await GetAsync(first, id)).State!.ETag.Should().Be(replacement.ETag);
        (await second.DeleteAsync(replacement, default)).Should().BeTrue();
    }
}
