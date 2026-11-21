namespace CoreEx.Caching.Redis.Test.Unit;

[TestFixture]
public class RedisCoordinationTests
{
    private StackExchange.Redis.ConnectionMultiplexer _first = null!;
    private StackExchange.Redis.ConnectionMultiplexer _second = null!;

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

    private static CoreEx.Redis.RedisCoordination Provider(StackExchange.Redis.IConnectionMultiplexer connection, CoreEx.Coordination.CoordinationOptions? options = null)
        => new(connection, Microsoft.Extensions.Logging.Abstractions.NullLogger<CoreEx.Redis.RedisCoordination>.Instance, options ?? new());

    private static CoreEx.Operations.OperationKey Key(string id) => CoreEx.Operations.OperationKey.ForDomain("redis-tests", "coordination", id);

    [Test]
    public async Task IndependentClients_ConcurrentAcquisitionHasOneOwner_AndReleaseAllowsReentry()
    {
        var key = Key(Guid.NewGuid().ToString());
        var a = Provider(_first);
        var b = Provider(_second);
        var leases = await Task.WhenAll(a.TryAcquireAsync(key), b.TryAcquireAsync(key));
        leases.Count(x => x is not null).Should().Be(1);
        await leases.Single(x => x is not null)!.DisposeAsync();
        await using var next = await b.TryAcquireAsync(key);
        next.Should().NotBeNull();
    }

    [Test]
    public async Task IndependentClients_IdempotencyIsAtomic_AndCompletedErrorsAreOpaqueReplayData()
    {
        var key = Key(Guid.NewGuid().ToString());
        var a = Provider(_first);
        var b = Provider(_second);
        var results = await Task.WhenAll(a.TryBeginAsync(key, "same"), b.TryBeginAsync(key, "same"));
        results.Count(x => x.Outcome == CoreEx.Idempotency.IdempotencyOutcome.Acquired).Should().Be(1);
        var owner = results.Single(x => x.Attempt is not null);
        await owner.Attempt!.CompleteAsync(new BinaryData("HTTP 500: terminal application result"));
        await owner.Attempt.DisposeAsync();
        var replay = await b.TryBeginAsync(key, "same");
        replay.Outcome.Should().Be(CoreEx.Idempotency.IdempotencyOutcome.Completed);
        replay.Response!.ToString().Should().Be("HTTP 500: terminal application result");
        (await b.TryBeginAsync(key, "different")).Outcome.Should().Be(CoreEx.Idempotency.IdempotencyOutcome.DifferentRequest);
    }

    [Test]
    public async Task IndependentClients_ExplicitReleaseAllowsSameKeyToRetry()
    {
        var key = Key(Guid.NewGuid().ToString());
        var first = await Provider(_first).TryBeginAsync(key, "same");
        var releasable = first.Attempt.Should().BeAssignableTo<CoreEx.Idempotency.IIdempotencyAttemptRelease>().Subject;

        await releasable.ReleaseForRetryAsync();
        var retry = await Provider(_second).TryBeginAsync(key, "same");

        retry.Outcome.Should().Be(CoreEx.Idempotency.IdempotencyOutcome.Acquired);
        await retry.Attempt!.CompleteAsync(new BinaryData("retried"));
        (await Provider(_first).TryBeginAsync(key, "same")).Outcome.Should().Be(CoreEx.Idempotency.IdempotencyOutcome.Completed);
        await first.Attempt!.DisposeAsync();
        await retry.Attempt.DisposeAsync();
    }

    [Test]
    public async Task UnknownOutcome_RequiresRevisionCheckedReconciliation_NotAutomaticTakeover()
    {
        var key = Key(Guid.NewGuid().ToString());
        var a = Provider(_first);
        var b = Provider(_second);
        var result = await a.TryBeginAsync(key, "same");
        (await b.TryResolveAsync(key, result.Revision!, null, true)).Should().BeFalse();
        await result.Attempt!.DisposeAsync();
        (await b.TryBeginAsync(key, "same")).Outcome.Should().Be(CoreEx.Idempotency.IdempotencyOutcome.Indeterminate);
        (await b.TryResolveAsync(key, "wrong", null, true)).Should().BeFalse();
        (await b.TryResolveAsync(key, result.Revision!, null, true)).Should().BeTrue();
        var successor = await b.TryBeginAsync(key, "same");
        await successor.Attempt!.CompleteAsync(new BinaryData("successor"));
        Func<Task> late = () => result.Attempt.CompleteAsync(new BinaryData("stale"));
        await late.Should().ThrowAsync<InvalidOperationException>();
        await successor.Attempt.DisposeAsync();
        (await a.TryBeginAsync(key, "same")).Response!.ToString().Should().Be("successor");
    }

    [Test]
    public async Task IndeterminateOutcome_CanBeReconciledToTerminalResponse_OnlyOnce()
    {
        var key = Key(Guid.NewGuid().ToString());
        var a = Provider(_first);
        var b = Provider(_second);
        var result = await a.TryBeginAsync(key, "same");
        await result.Attempt!.DisposeAsync();
        (await b.TryResolveAsync(key, result.Revision!, new BinaryData("reconciled"))).Should().BeTrue();
        (await b.TryResolveAsync(key, result.Revision!, new BinaryData("again"))).Should().BeFalse();
        var replay = await a.TryBeginAsync(key, "same");
        replay.Outcome.Should().Be(CoreEx.Idempotency.IdempotencyOutcome.Completed);
        replay.Response!.ToString().Should().Be("reconciled");

        var other = Key(Guid.NewGuid().ToString());
        var unavailable = await a.TryBeginAsync(other, "same");
        await unavailable.Attempt!.DisposeAsync();
        (await b.TryResolveAsync(other, unavailable.Revision!, null)).Should().BeTrue();
        (await a.TryBeginAsync(other, "same")).Outcome.Should().Be(CoreEx.Idempotency.IdempotencyOutcome.ResponseUnavailable);
    }

    [Test]
    public async Task EmptyReplayableResponse_IsRejected_ByProviderAndScripts()
    {
        var key = Key(Guid.NewGuid().ToString());
        var provider = Provider(_first);
        var begun = await provider.TryBeginAsync(key, "same");
        Func<Task> complete = () => begun.Attempt!.CompleteAsync(BinaryData.Empty);
        await complete.Should().ThrowAsync<ArgumentException>();
        await begun.Attempt!.DisposeAsync();
        Func<Task> resolve = () => provider.TryResolveAsync(key, begun.Revision!, BinaryData.Empty);
        await resolve.Should().ThrowAsync<ArgumentException>();

        var database = _first.GetDatabase();
        var physical = (StackExchange.Redis.RedisKey)PhysicalKey("idempotency", key);
        Func<Task> transitionScript = () => database.ScriptEvaluateAsync(Script("IdempotencyTransition.lua"), [physical], [begun.Revision!, "complete", 1000, 1000, "C", ""]);
        await transitionScript.Should().ThrowAsync<StackExchange.Redis.RedisServerException>().WithMessage("*C needs response*");
        Func<Task> resolveScript = () => database.ScriptEvaluateAsync(Script("IdempotencyResolve.lua"), [physical], [begun.Revision!, "C", "", 1000]);
        await resolveScript.Should().ThrowAsync<StackExchange.Redis.RedisServerException>().WithMessage("*C needs response*");
        (await provider.TryBeginAsync(key, "same")).Outcome.Should().Be(CoreEx.Idempotency.IdempotencyOutcome.Indeterminate);
    }

    private static string Script(string name)
    {
        using var reader = new StreamReader(CoreEx.Abstractions.Resource.GetStream<CoreEx.Redis.RedisCoordination>("Scripts." + name));
        return reader.ReadToEnd();
    }

    [Test]
    public async Task RenewalKeepsLeaseValid_AndCompletionStopsHeartbeatWithoutExpiringResponse()
    {
        var options = new CoreEx.Coordination.CoordinationOptions { LeaseDuration = TimeSpan.FromMilliseconds(600), RenewalInterval = TimeSpan.FromMilliseconds(100), OperationTimeout = TimeSpan.FromMilliseconds(200), Retention = TimeSpan.FromSeconds(4) };
        var key = Key(Guid.NewGuid().ToString());
        var a = Provider(_first, options);
        var b = Provider(_second, options);
        await using var lease = await a.TryAcquireAsync(key);
        await Task.Delay(1300);
        lease!.OwnershipLost.IsCancellationRequested.Should().BeFalse();
        (await b.TryAcquireAsync(key)).Should().BeNull();
        var record = await a.TryBeginAsync(key, "same");
        await record.Attempt!.CompleteAsync(new BinaryData("terminal"));
        await Task.Delay(800);
        record.Attempt.OwnershipLost.IsCancellationRequested.Should().BeFalse();
        await record.Attempt.DisposeAsync();
        (await b.TryBeginAsync(key, "same")).Outcome.Should().Be(CoreEx.Idempotency.IdempotencyOutcome.Completed);
    }

    [Test]
    public async Task DistributedCacheAndCoordination_UseTheSameHostOwnedMultiplexer()
    {
        var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder();
        builder.Configuration["ConnectionStrings:redis"] = Environment.GetEnvironmentVariable("COREEX_REDIS_TEST_CONNECTION") ?? "localhost:6379";
        builder.AddRedisDistributedCache("redis");
        builder.Services.AddRedisCoordination();
        using var host = builder.Build();
        var connection = host.Services.GetRequiredService<StackExchange.Redis.IConnectionMultiplexer>();
        var cacheOptions = host.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<Microsoft.Extensions.Caching.StackExchangeRedis.RedisCacheOptions>>().Value;
        (await cacheOptions.ConnectionMultiplexerFactory!()).Should().BeSameAs(connection);
        host.Services.GetServices<StackExchange.Redis.IConnectionMultiplexer>().Should().ContainSingle();
        await using var scope = host.Services.CreateAsyncScope();
        await using var lease = await scope.ServiceProvider.GetRequiredService<CoreEx.Coordination.ISynchronizer>().TryAcquireAsync(Key(Guid.NewGuid().ToString()));
        lease.Should().NotBeNull();
    }

    [Test]
    public async Task RedisKeys_ExposeEscapedIdentitySegments_ForBothCoordinationPurposes()
    {
        var id = $"batch:42/{Guid.NewGuid():N}";
        var key = CoreEx.Operations.OperationKey.ForTenant("orders:eu", "tenant/a*", "import[all]", id);
        var provider = Provider(_first);
        var suffix = $"domain:orders%3Aeu:scope:tenant:tenant:tenant%2Fa%2A:operation:import%5Ball%5D:instance:batch%3A42%2F{id[(id.LastIndexOf('/') + 1)..]}";
        var leaseKey = $"CoreEx:coordination:v2:lease:{suffix}";
        var attemptKey = $"CoreEx:coordination:v2:idempotency:{suffix}";
        await using var lease = await provider.TryAcquireAsync(key);
        var other = CoreEx.Operations.OperationKey.ForTenant("orders", "eu:tenant/a*", "import[all]", id);
        await using var otherLease = await provider.TryAcquireAsync(other);
        var domainWide = CoreEx.Operations.OperationKey.ForDomain("orders:eu", "import[all]", id);
        await using var domainLease = await provider.TryAcquireAsync(domainWide);
        var attempt = await provider.TryBeginAsync(key, "request");
        try
        {
            lease.Should().NotBeNull();
            otherLease.Should().NotBeNull();
            domainLease.Should().NotBeNull();
            (await _first.GetDatabase().KeyExistsAsync(leaseKey)).Should().BeTrue();
            (await _first.GetDatabase().KeyExistsAsync(attemptKey)).Should().BeTrue();
            var domainKey = $"CoreEx:coordination:v2:lease:domain:orders%3Aeu:scope:domain:tenant:-:operation:import%5Ball%5D:instance:batch%3A42%2F{id[(id.LastIndexOf('/') + 1)..]}";
            (await _first.GetDatabase().KeyExistsAsync(domainKey)).Should().BeTrue();
            var matches = new List<string>();
            await foreach (var redisKey in _first.GetServer(_first.GetEndPoints()[0]).KeysAsync(pattern: $"CoreEx:coordination:v2:*:{suffix}"))
                matches.Add(redisKey.ToString());

            matches.Should().Contain(leaseKey).And.Contain(attemptKey);
        }
        finally
        {
            await attempt.Attempt!.DisposeAsync();
            await _first.GetDatabase().KeyDeleteAsync(attemptKey);
        }
    }

    [Test]
    public async Task RedisKeys_RejectMalformedUnicode_WithoutAliasingValidValues()
    {
        var id = Guid.NewGuid().ToString("N");
        var provider = Provider(_first);
        var replacement = CoreEx.Operations.OperationKey.ForTenant("orders\uFFFD", "tenant", "import", id);
        var supplementary = CoreEx.Operations.OperationKey.ForTenant("orders\uD83D\uDE80", "tenant", "import", id);
        await using var replacementLease = await provider.TryAcquireAsync(replacement);
        await using var supplementaryLease = await provider.TryAcquireAsync(supplementary);
        replacementLease.Should().NotBeNull();
        supplementaryLease.Should().NotBeNull();

        var invalid = new[]
        {
            CoreEx.Operations.OperationKey.ForTenant("orders\uD800", "tenant", "import", id),
            CoreEx.Operations.OperationKey.ForTenant("orders", "tenant\uD801", "import", id),
            CoreEx.Operations.OperationKey.ForTenant("orders", "tenant", "import\uDC00", id),
            CoreEx.Operations.OperationKey.ForTenant("orders", "tenant", "import", id + "\uD800")
        };
        foreach (var key in invalid)
        {
            Func<Task> acquire = () => provider.TryAcquireAsync(key);
            await acquire.Should().ThrowAsync<ArgumentException>();
            Func<Task> begin = () => provider.TryBeginAsync(key, "request");
            await begin.Should().ThrowAsync<ArgumentException>();
        }
    }

    [Test]
    public async Task HttpIdempotency_WithRedis_ReplaysAndSeparatesTenantsWithoutExposingCallerOrHeader()
    {
        var id = Guid.NewGuid().ToString("N");
        var settings = new CoreEx.Hosting.HostSettings { SolutionName = "Tests", DomainName = "orders", EnvironmentName = "Test" };
        var firstContext = new CoreEx.ExecutionContext { TenantId = "tenant-a" };
        var secondContext = new CoreEx.ExecutionContext { TenantId = "tenant-a" };
        var first = new CoreEx.AspNetCore.Idempotency.IdempotencyProvider(Provider(_first), hostSettings: settings, executionContext: firstContext)
        {
            Scope = CoreEx.Operations.OperationKeyScope.Tenant,
            CallerIdentityProvider = _ => "caller-a"
        };
        var second = new CoreEx.AspNetCore.Idempotency.IdempotencyProvider(Provider(_second), hostSettings: settings, executionContext: secondContext)
        {
            Scope = CoreEx.Operations.OperationKeyScope.Tenant,
            CallerIdentityProvider = _ => "caller-a"
        };
        var attribute = new CoreEx.AspNetCore.Mvc.IdempotencyKeyAttribute();
        var original = HttpContext(id, "same");
        var operationA = first.CreateOperationKey(original, id);
        secondContext.TenantId = "tenant-b";
        var operationB = second.CreateOperationKey(original, id);
        secondContext.TenantId = "tenant-a";
        var calls = 0;

        async Task Execute(Microsoft.AspNetCore.Http.HttpContext context)
        {
            calls++;
            await context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes($"response-{calls}"));
        }

        try
        {
            await first.OnInvokeAsync(attribute, original, Execute);
            var replay = HttpContext(id, "same");
            await second.OnInvokeAsync(attribute, replay, Execute);
            Encoding.UTF8.GetString(((MemoryStream)replay.Response.Body).ToArray()).Should().Be("response-1");
            calls.Should().Be(1);

            var changed = HttpContext(id, "changed");
            Func<Task> conflict = () => second.OnInvokeAsync(attribute, changed, Execute);
            await conflict.Should().ThrowAsync<CoreEx.ConflictException>();
            calls.Should().Be(1);

            secondContext.TenantId = "tenant-b";
            var otherTenant = HttpContext(id, "same");
            await second.OnInvokeAsync(attribute, otherTenant, Execute);
            Encoding.UTF8.GetString(((MemoryStream)otherTenant.Response.Body).ToArray()).Should().Be("response-2");
            var physical = PhysicalKey("idempotency", operationA);
            physical.Should().Contain(":domain:orders:scope:tenant:tenant:tenant-a:operation:http-idempotency:instance:")
                .And.NotContain(id).And.NotContain("caller-a");
            (await _first.GetDatabase().KeyExistsAsync(physical)).Should().BeTrue();
            (await _first.GetDatabase().KeyExistsAsync(PhysicalKey("idempotency", operationB))).Should().BeTrue();
        }
        finally
        {
            await _first.GetDatabase().KeyDeleteAsync(PhysicalKey("idempotency", operationA));
            await _first.GetDatabase().KeyDeleteAsync(PhysicalKey("idempotency", operationB));
        }
    }

    private static Microsoft.AspNetCore.Http.DefaultHttpContext HttpContext(string idempotencyKey, string body)
    {
        var context = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/orders";
        context.Request.Headers["Idempotency-Key"] = idempotencyKey;
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        context.Request.ContentLength = context.Request.Body.Length;
        context.Response.Body = new MemoryStream();
        return context;
    }

    [Test]
    public async Task ExplicitKeys_IgnoreAmbientQualifiers_AndIsolateDomainTenantScopeAndPurpose()
    {
        var id = Guid.NewGuid().ToString();
        var key = CoreEx.Operations.OperationKey.ForTenant("orders", "tenant-a", "import", id);
        var qualifier = new ChangingQualifier();
        await using var services = new ServiceCollection().AddLogging().AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(_first)
            .AddSingleton<CoreEx.Caching.ICacheKeyProvider>(qualifier).AddRedisCoordination().BuildServiceProvider();
        await using var scope = services.CreateAsyncScope();
        var a = scope.ServiceProvider.GetRequiredService<CoreEx.Coordination.ISynchronizer>();
        var store = scope.ServiceProvider.GetRequiredService<CoreEx.Idempotency.IIdempotencyStore>();
        var b = Provider(_second);

        await using var first = await a.TryAcquireAsync(key);
        qualifier.Prefix = "different-ambient-tenant";
        (await b.TryAcquireAsync(CoreEx.Operations.OperationKey.ForTenant("orders", "tenant-a", "import", id))).Should().BeNull();
        await using var tenant = await b.TryAcquireAsync(CoreEx.Operations.OperationKey.ForTenant("orders", "tenant-b", "import", id));
        await using var domain = await b.TryAcquireAsync(CoreEx.Operations.OperationKey.ForTenant("products", "tenant-a", "import", id));
        await using var domainWide = await b.TryAcquireAsync(CoreEx.Operations.OperationKey.ForDomain("orders", "import", id));
        tenant.Should().NotBeNull();
        domain.Should().NotBeNull();
        domainWide.Should().NotBeNull();

        var attempt = await store.TryBeginAsync(key, "request");
        attempt.Outcome.Should().Be(CoreEx.Idempotency.IdempotencyOutcome.Acquired);
        await attempt.Attempt!.DisposeAsync();
        qualifier.Calls.Should().Be(0);
    }

    [Test]
    public async Task ExplicitIdempotencyKeys_ReplayAndReconcileOnlyTheMatchingIdentity()
    {
        var id = Guid.NewGuid().ToString();
        var key = CoreEx.Operations.OperationKey.ForTenant("orders", "tenant-a", "import", id);
        var otherTenant = CoreEx.Operations.OperationKey.ForTenant("orders", "tenant-b", "import", id);
        var a = Provider(_first);
        var b = Provider(_second);
        var results = await Task.WhenAll(a.TryBeginAsync(key, "same"), b.TryBeginAsync(key, "same"));
        results.Count(x => x.Outcome == CoreEx.Idempotency.IdempotencyOutcome.Acquired).Should().Be(1);
        var owner = results.Single(x => x.Attempt is not null);
        await owner.Attempt!.DisposeAsync();

        (await b.TryResolveAsync(otherTenant, owner.Revision!, null, true)).Should().BeFalse();
        (await b.TryBeginAsync(key, "same")).Outcome.Should().Be(CoreEx.Idempotency.IdempotencyOutcome.Indeterminate);
        (await b.TryResolveAsync(key, owner.Revision!, null, true)).Should().BeTrue();
        var retry = await a.TryBeginAsync(key, "same");
        await retry.Attempt!.CompleteAsync(new BinaryData("response"));
        await retry.Attempt.DisposeAsync();
        (await b.TryBeginAsync(key, "same")).Response!.ToString().Should().Be("response");
        (await b.TryBeginAsync(key, "different")).Outcome.Should().Be(CoreEx.Idempotency.IdempotencyOutcome.DifferentRequest);
    }

    [Test]
    public async Task PreCancelledAcquisition_DoesNotCreateLeaseOrProcessingRecord()
    {
        var provider = Provider(_first);
        var key = Key(Guid.NewGuid().ToString());
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Func<Task> lease = () => provider.TryAcquireAsync(key, cancellation.Token);
        Func<Task> attempt = () => provider.TryBeginAsync(key, "request", cancellation.Token);
        await lease.Should().ThrowAsync<OperationCanceledException>();
        await attempt.Should().ThrowAsync<OperationCanceledException>();
        await using var acquired = await provider.TryAcquireAsync(key);
        acquired.Should().NotBeNull();
        var begun = await provider.TryBeginAsync(key, "request");
        begun.Outcome.Should().Be(CoreEx.Idempotency.IdempotencyOutcome.Acquired);
        await begun.Attempt!.DisposeAsync();
    }

    [Test]
    public async Task StaleLeaseDisposal_CannotDeleteSuccessor()
    {
        var key = Key(Guid.NewGuid().ToString());
        var provider = Provider(_first);
        var original = await provider.TryAcquireAsync(key);
        var physical = PhysicalKey("lease", key);
        await _second.GetDatabase().KeyDeleteAsync(physical);
        await using var successor = await Provider(_second).TryAcquireAsync(key);
        await original!.DisposeAsync();
        (await provider.TryAcquireAsync(key)).Should().BeNull();
    }

    [Test]
    public async Task ExpiredAttempt_CannotCompleteOrAffectReconciledSuccessor()
    {
        var key = Key(Guid.NewGuid().ToString());
        var a = Provider(_first);
        var b = Provider(_second);
        var original = await a.TryBeginAsync(key, "same");
        await _second.GetDatabase().HashSetAsync(PhysicalKey("idempotency", key), "d", 0);
        Func<Task> expired = () => original.Attempt!.CompleteAsync(new BinaryData("stale"));
        await expired.Should().ThrowAsync<InvalidOperationException>();
        (await b.TryResolveAsync(key, original.Revision!, null, true)).Should().BeTrue();
        var successor = await b.TryBeginAsync(key, "same");
        await original.Attempt!.DisposeAsync();
        await successor.Attempt!.CompleteAsync(new BinaryData("successor"));
        await successor.Attempt.DisposeAsync();
        (await a.TryBeginAsync(key, "same")).Response!.ToString().Should().Be("successor");
    }

    [Test]
    public async Task DuplicateReads_DoNotExtendTerminalRetention()
    {
        var options = new CoreEx.Coordination.CoordinationOptions { LeaseDuration = TimeSpan.FromMilliseconds(700), RenewalInterval = TimeSpan.FromMilliseconds(150), OperationTimeout = TimeSpan.FromMilliseconds(200), Retention = TimeSpan.FromSeconds(1) };
        var key = Key(Guid.NewGuid().ToString());
        var provider = Provider(_first, options);
        var original = await provider.TryBeginAsync(key, "same");
        await original.Attempt!.CompleteAsync(null);
        await original.Attempt.DisposeAsync();
        await Task.Delay(400);
        (await provider.TryBeginAsync(key, "same")).Outcome.Should().Be(CoreEx.Idempotency.IdempotencyOutcome.ResponseUnavailable);
        await Task.Delay(700);
        var next = await provider.TryBeginAsync(key, "same");
        next.Outcome.Should().Be(CoreEx.Idempotency.IdempotencyOutcome.Acquired);
        await next.Attempt!.DisposeAsync();
    }

    [Test]
    public async Task BackendFailure_ThrowsRatherThanReturningContentionOrStartingWork()
    {
        using var connection = await StackExchange.Redis.ConnectionMultiplexer.ConnectAsync(Environment.GetEnvironmentVariable("COREEX_REDIS_TEST_CONNECTION") ?? "localhost:6379");
        var provider = Provider(connection);
        await connection.CloseAsync();
        Func<Task> acquire = () => provider.TryAcquireAsync(Key(Guid.NewGuid().ToString()));
        Func<Task> begin = () => provider.TryBeginAsync(Key(Guid.NewGuid().ToString()), "same");
        await acquire.Should().ThrowAsync<Exception>();
        await begin.Should().ThrowAsync<Exception>();
    }

    private static string PhysicalKey(string purpose, CoreEx.Operations.OperationKey key)
        => $"CoreEx:coordination:v2:{purpose}:domain:{Uri.EscapeDataString(key.DomainName)}:scope:{(key.Scope == CoreEx.Operations.OperationKeyScope.Tenant ? "tenant" : "domain")}:tenant:{(key.TenantId is null ? "-" : Uri.EscapeDataString(key.TenantId))}:operation:{Uri.EscapeDataString(key.OperationName)}:instance:{Uri.EscapeDataString(key.InstanceId)}";

    [Test]
    public async Task SeparateProcesses_ExcludeConcurrentOwner_AndCrashPermitsLeaseRecovery()
    {
        var key = Key(Guid.NewGuid().ToString());
        using var owner = StartWorker("lease", key);
        try
        {
            (await owner.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(15))).Should().Be("acquired");
            using var competitor = StartWorker("lease", key);
            try
            {
                (await competitor.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(15))).Should().Be("contended");
            }
            finally { StopWorker(competitor); }
        }
        finally { StopWorker(owner); }

        await Task.Delay(1300);
        await using var recovered = await Provider(_second).TryAcquireAsync(key);
        recovered.Should().NotBeNull();
    }

    [Test]
    public async Task SeparateProcesses_UseTheSameExplicitOperationIdentity()
    {
        var key = CoreEx.Operations.OperationKey.ForTenant("orders", "tenant-a", "import", Guid.NewGuid().ToString());
        using var owner = StartWorker("lease", key);
        try
        {
            (await owner.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(15))).Should().Be("acquired");
            (await Provider(_second).TryAcquireAsync(key)).Should().BeNull();
            await using var otherTenant = await Provider(_second).TryAcquireAsync(CoreEx.Operations.OperationKey.ForTenant("orders", "tenant-b", "import", key.InstanceId));
            otherTenant.Should().NotBeNull();
        }
        finally { StopWorker(owner); }

        await Task.Delay(1300);
        await using var recovered = await Provider(_second).TryAcquireAsync(key);
        recovered.Should().NotBeNull();
    }

    [Test]
    public async Task SeparateProcessCrash_LeavesIdempotencyIndeterminateInsteadOfTakingOver()
    {
        var key = Key(Guid.NewGuid().ToString());
        using var owner = StartWorker("attempt", key);
        try
        {
            (await owner.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(15))).Should().Be("acquired");
        }
        finally { StopWorker(owner); }

        await Task.Delay(1300);
        var provider = Provider(_second);
        var result = await provider.TryBeginAsync(key, "request");
        result.Outcome.Should().Be(CoreEx.Idempotency.IdempotencyOutcome.Indeterminate);
        (await provider.TryResolveAsync(key, result.Revision!, null, true)).Should().BeTrue();
    }

    private static Process StartWorker(string operation, CoreEx.Operations.OperationKey key)
    {
        var configuration = new DirectoryInfo(TestContext.CurrentContext.TestDirectory).Parent!.Name;
        var framework = new DirectoryInfo(TestContext.CurrentContext.TestDirectory).Name;
        var worker = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "CoreEx.Redis.Test.Worker", "bin", configuration, framework, "CoreEx.Redis.Test.Worker.dll"));
        var start = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        start.ArgumentList.Add(worker);
        start.ArgumentList.Add(operation);
        start.ArgumentList.Add(System.Text.Json.JsonSerializer.Serialize(key));
        return Process.Start(start) ?? throw new InvalidOperationException("Could not start the Redis test worker.");
    }

    private static void StopWorker(Process process)
    {
        if (!process.HasExited)
        {
            process.Kill(entireProcessTree: true);
            process.WaitForExit();
        }
    }

    private sealed class ChangingQualifier : CoreEx.Caching.ICacheKeyProvider
    {
        public string Prefix { get; set; } = "tenant-a";

        public int Calls { get; private set; }

        public string GetFullyQualifiedCacheKey(string key)
        {
            Calls++;
            return Prefix + ":" + key;
        }

        public string GetEntityCacheKey<T>(CoreEx.Entities.CompositeKey key) where T : CoreEx.Entities.IEntityKey => throw new NotSupportedException();
    }
}
