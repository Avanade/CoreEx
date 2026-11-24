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

    private const string Name = "coordination";

    [SetUp]
    [TearDown]
    public void ResetExecutionContext() => CoreEx.ExecutionContext.Reset();

    private static CoreEx.Hosting.IHostSettings Settings(string domainName) => new CoreEx.Hosting.HostSettings { SolutionName = "Tests", DomainName = domainName, EnvironmentName = "Test" };

    private static CoreEx.Redis.RedisCoordination Provider(StackExchange.Redis.IConnectionMultiplexer connection, CoreEx.Coordination.CoordinationOptions? options = null, string domainName = "redis-tests")
        => new(connection, Settings(domainName), Microsoft.Extensions.Logging.Abstractions.NullLogger<CoreEx.Redis.RedisCoordination>.Instance, options ?? new());

    private static CoreEx.Entities.CompositeKey Key(string id) => CoreEx.Entities.CompositeKey.Create(id);

    private static void SetTenant(string? tenantId)
    {
        CoreEx.ExecutionContext.Reset();
        if (tenantId is not null)
            CoreEx.ExecutionContext.SetCurrent(new CoreEx.ExecutionContext { TenantId = tenantId });
    }

    [Test]
    public async Task IndependentClients_ConcurrentAcquisitionHasOneOwner_AndReleaseAllowsReentry()
    {
        var key = Key(Guid.NewGuid().ToString());
        var a = Provider(_first);
        var b = Provider(_second);
        var leases = await Task.WhenAll(a.TryAcquireAsync(Name, key), b.TryAcquireAsync(Name, key));
        leases.Count(x => x is not null).Should().Be(1);
        await leases.Single(x => x is not null)!.DisposeAsync();
        await using var next = await b.TryAcquireAsync(Name, key);
        next.Should().NotBeNull();
    }

    [TestCase("")]
    [TestCase(" ")]
    public async Task BlankCanonicalKeysAreRejected(string value)
    {
        var provider = Provider(_first);
        var key = Key(value);

        await FluentActions.Awaiting(() => provider.TryAcquireAsync(Name, key)).Should().ThrowAsync<ArgumentException>().WithParameterName("key");
        await FluentActions.Awaiting(() => provider.TryBeginAsync(Name, key, "request")).Should().ThrowAsync<ArgumentException>().WithParameterName("key");
        await FluentActions.Awaiting(() => provider.TryResolveAsync(Name, key, "revision", null)).Should().ThrowAsync<ArgumentException>().WithParameterName("key");
    }

    [Test]
    public async Task IndependentClients_IdempotencyIsAtomic_AndCompletedErrorsAreOpaqueReplayData()
    {
        var key = Key(Guid.NewGuid().ToString());
        var a = Provider(_first);
        var b = Provider(_second);
        var results = await Task.WhenAll(a.TryBeginAsync(Name, key, "same"), b.TryBeginAsync(Name, key, "same"));
        results.Count(x => x.Outcome == CoreEx.Idempotency.IdempotencyOutcome.Acquired).Should().Be(1);
        var owner = results.Single(x => x.Attempt is not null);
        await owner.Attempt!.CompleteAsync(new BinaryData("HTTP 500: terminal application result"));
        await owner.Attempt.DisposeAsync();
        var replay = await b.TryBeginAsync(Name, key, "same");
        replay.Outcome.Should().Be(CoreEx.Idempotency.IdempotencyOutcome.Completed);
        replay.Response!.ToString().Should().Be("HTTP 500: terminal application result");
        (await b.TryBeginAsync(Name, key, "different")).Outcome.Should().Be(CoreEx.Idempotency.IdempotencyOutcome.DifferentRequest);
    }

    [Test]
    public async Task IndependentClients_ExplicitReleaseAllowsSameKeyToRetry()
    {
        var key = Key(Guid.NewGuid().ToString());
        var first = await Provider(_first).TryBeginAsync(Name, key, "same");
        var releasable = first.Attempt.Should().BeAssignableTo<CoreEx.Idempotency.IIdempotencyAttemptRelease>().Subject;

        await releasable.ReleaseForRetryAsync();
        var retry = await Provider(_second).TryBeginAsync(Name, key, "same");

        retry.Outcome.Should().Be(CoreEx.Idempotency.IdempotencyOutcome.Acquired);
        await retry.Attempt!.CompleteAsync(new BinaryData("retried"));
        (await Provider(_first).TryBeginAsync(Name, key, "same")).Outcome.Should().Be(CoreEx.Idempotency.IdempotencyOutcome.Completed);
        await first.Attempt!.DisposeAsync();
        await retry.Attempt.DisposeAsync();
    }

    [Test]
    public async Task UnknownOutcome_RequiresRevisionCheckedReconciliation_NotAutomaticTakeover()
    {
        var key = Key(Guid.NewGuid().ToString());
        var a = Provider(_first);
        var b = Provider(_second);
        var result = await a.TryBeginAsync(Name, key, "same");
        (await b.TryResolveAsync(Name, key, result.Revision!, null, true)).Should().BeFalse();
        await result.Attempt!.DisposeAsync();
        (await b.TryBeginAsync(Name, key, "same")).Outcome.Should().Be(CoreEx.Idempotency.IdempotencyOutcome.Indeterminate);
        (await b.TryResolveAsync(Name, key, "wrong", null, true)).Should().BeFalse();
        (await b.TryResolveAsync(Name, key, result.Revision!, null, true)).Should().BeTrue();
        var successor = await b.TryBeginAsync(Name, key, "same");
        await successor.Attempt!.CompleteAsync(new BinaryData("successor"));
        Func<Task> late = () => result.Attempt.CompleteAsync(new BinaryData("stale"));
        await late.Should().ThrowAsync<InvalidOperationException>();
        await successor.Attempt.DisposeAsync();
        (await a.TryBeginAsync(Name, key, "same")).Response!.ToString().Should().Be("successor");
    }

    [Test]
    public async Task IndeterminateOutcome_CanBeReconciledToTerminalResponse_OnlyOnce()
    {
        var key = Key(Guid.NewGuid().ToString());
        var a = Provider(_first);
        var b = Provider(_second);
        var result = await a.TryBeginAsync(Name, key, "same");
        await result.Attempt!.DisposeAsync();
        (await b.TryResolveAsync(Name, key, result.Revision!, new BinaryData("reconciled"))).Should().BeTrue();
        (await b.TryResolveAsync(Name, key, result.Revision!, new BinaryData("again"))).Should().BeFalse();
        var replay = await a.TryBeginAsync(Name, key, "same");
        replay.Outcome.Should().Be(CoreEx.Idempotency.IdempotencyOutcome.Completed);
        replay.Response!.ToString().Should().Be("reconciled");

        var other = Key(Guid.NewGuid().ToString());
        var unavailable = await a.TryBeginAsync(Name, other, "same");
        await unavailable.Attempt!.DisposeAsync();
        (await b.TryResolveAsync(Name, other, unavailable.Revision!, null)).Should().BeTrue();
        (await a.TryBeginAsync(Name, other, "same")).Outcome.Should().Be(CoreEx.Idempotency.IdempotencyOutcome.ResponseUnavailable);
    }

    [Test]
    public async Task EmptyReplayableResponse_IsRejected_ByProviderAndScripts()
    {
        var key = Key(Guid.NewGuid().ToString());
        var provider = Provider(_first);
        var begun = await provider.TryBeginAsync(Name, key, "same");
        Func<Task> complete = () => begun.Attempt!.CompleteAsync(BinaryData.Empty);
        await complete.Should().ThrowAsync<ArgumentException>();
        await begun.Attempt!.DisposeAsync();
        Func<Task> resolve = () => provider.TryResolveAsync(Name, key, begun.Revision!, BinaryData.Empty);
        await resolve.Should().ThrowAsync<ArgumentException>();

        var database = _first.GetDatabase();
        var physical = (StackExchange.Redis.RedisKey)PhysicalKey("idempotency", key);
        Func<Task> transitionScript = () => database.ScriptEvaluateAsync(Script("IdempotencyTransition.lua"), [physical], [begun.Revision!, "complete", 1000, 1000, "C", ""]);
        await transitionScript.Should().ThrowAsync<StackExchange.Redis.RedisServerException>().WithMessage("*C needs response*");
        Func<Task> resolveScript = () => database.ScriptEvaluateAsync(Script("IdempotencyResolve.lua"), [physical], [begun.Revision!, "C", "", 1000]);
        await resolveScript.Should().ThrowAsync<StackExchange.Redis.RedisServerException>().WithMessage("*C needs response*");
        (await provider.TryBeginAsync(Name, key, "same")).Outcome.Should().Be(CoreEx.Idempotency.IdempotencyOutcome.Indeterminate);
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
        await using var lease = await a.TryAcquireAsync(Name, key);
        await Task.Delay(1300);
        lease!.OwnershipLost.IsCancellationRequested.Should().BeFalse();
        (await b.TryAcquireAsync(Name, key)).Should().BeNull();
        var record = await a.TryBeginAsync(Name, key, "same");
        await record.Attempt!.CompleteAsync(new BinaryData("terminal"));
        await Task.Delay(800);
        record.Attempt.OwnershipLost.IsCancellationRequested.Should().BeFalse();
        await record.Attempt.DisposeAsync();
        (await b.TryBeginAsync(Name, key, "same")).Outcome.Should().Be(CoreEx.Idempotency.IdempotencyOutcome.Completed);
    }

    [Test]
    public async Task DistributedCacheAndCoordination_UseTheSameHostOwnedMultiplexer()
    {
        var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder();
        builder.Configuration["ConnectionStrings:redis"] = Environment.GetEnvironmentVariable("COREEX_REDIS_TEST_CONNECTION") ?? "localhost:6379";
        builder.AddRedisDistributedCache("redis");
        builder.Services.AddSingleton(Settings("redis-tests"));
        builder.Services.AddRedisCoordination();
        using var host = builder.Build();
        var connection = host.Services.GetRequiredService<StackExchange.Redis.IConnectionMultiplexer>();
        var cacheOptions = host.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<Microsoft.Extensions.Caching.StackExchangeRedis.RedisCacheOptions>>().Value;
        (await cacheOptions.ConnectionMultiplexerFactory!()).Should().BeSameAs(connection);
        host.Services.GetServices<StackExchange.Redis.IConnectionMultiplexer>().Should().ContainSingle();
        await using var scope = host.Services.CreateAsyncScope();
        await using var lease = await scope.ServiceProvider.GetRequiredService<CoreEx.Coordination.ISynchronizer>().TryAcquireAsync(Name, Key(Guid.NewGuid().ToString()));
        lease.Should().NotBeNull();
    }

    [Test]
    public async Task RedisKeys_ExposeEscapedIdentitySegments_ForBothCoordinationPurposes()
    {
        var id = $"batch:42/{Guid.NewGuid():N}";
        var key = Key(id);
        var provider = Provider(_first, domainName: "orders:eu");
        var other = Provider(_first, domainName: "orders");
        var suffix = $"domain:orders%3Aeu:tenant:tenant%2Fa%2A:operation:import%5Ball%5D:instance:batch%3A42%2F{id[(id.LastIndexOf('/') + 1)..]}";
        var leaseKey = $"CoreEx:coordination:v1:lease:{suffix}";
        var attemptKey = $"CoreEx:coordination:v1:idempotency:{suffix}";
        SetTenant("tenant/a*");
        await using var lease = await provider.TryAcquireAsync("import[all]", key);
        var attempt = await provider.TryBeginAsync("import[all]", key, "request");
        SetTenant("eu:tenant/a*");
        await using var otherLease = await other.TryAcquireAsync("import[all]", key);
        SetTenant(null);
        await using var domainLease = await provider.TryAcquireAsync("import[all]", key);
        try
        {
            lease.Should().NotBeNull();
            otherLease.Should().NotBeNull();
            domainLease.Should().NotBeNull();
            (await _first.GetDatabase().KeyExistsAsync(leaseKey)).Should().BeTrue();
            (await _first.GetDatabase().KeyExistsAsync(attemptKey)).Should().BeTrue();
            var domainKey = $"CoreEx:coordination:v1:lease:domain:orders%3Aeu:operation:import%5Ball%5D:instance:batch%3A42%2F{id[(id.LastIndexOf('/') + 1)..]}";
            (await _first.GetDatabase().KeyExistsAsync(domainKey)).Should().BeTrue();
            var matches = new List<string>();
            await foreach (var redisKey in _first.GetServer(_first.GetEndPoints()[0]).KeysAsync(pattern: $"CoreEx:coordination:v1:*:{suffix}"))
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
        SetTenant("tenant");
        await using var replacementLease = await Provider(_first, domainName: "orders\uFFFD").TryAcquireAsync("import", Key(id));
        await using var supplementaryLease = await Provider(_first, domainName: "orders\uD83D\uDE80").TryAcquireAsync("import", Key(id));
        replacementLease.Should().NotBeNull();
        supplementaryLease.Should().NotBeNull();

        var invalid = new (string Domain, string Tenant, string Name, string Id)[]
        {
            ("orders\uD800", "tenant", "import", id),
            ("orders", "tenant\uD801", "import", id),
            ("orders", "tenant", "import\uDC00", id),
            ("orders", "tenant", "import", id + "\uD800")
        };
        foreach (var identity in invalid)
        {
            SetTenant(identity.Tenant);
            var provider = Provider(_first, domainName: identity.Domain);
            Func<Task> acquire = () => provider.TryAcquireAsync(identity.Name, Key(identity.Id));
            await acquire.Should().ThrowAsync<ArgumentException>();
            Func<Task> begin = () => provider.TryBeginAsync(identity.Name, Key(identity.Id), "request");
            await begin.Should().ThrowAsync<ArgumentException>();
        }
    }
    [Test]
    public async Task HttpIdempotency_WithRedis_ReplaysAndSeparatesTenantsWithoutExposingCallerOrHeader()
    {
        var id = Guid.NewGuid().ToString("N");
        var first = new CoreEx.AspNetCore.Idempotency.IdempotencyProvider(Provider(_first, domainName: "orders")) { CallerIdentityProvider = _ => "caller-a" };
        var second = new CoreEx.AspNetCore.Idempotency.IdempotencyProvider(Provider(_second, domainName: "orders")) { CallerIdentityProvider = _ => "caller-a" };
        var attribute = new CoreEx.AspNetCore.Mvc.IdempotencyKeyAttribute();
        var original = HttpContext(id, "same");
        var key = first.CreateKey(original, id);
        second.CreateKey(original, id).Should().Be(key);
        var operationA = PhysicalKey("idempotency", "orders", "tenant-a", CoreEx.AspNetCore.Idempotency.IdempotencyProvider.OperationName, key);
        var operationB = PhysicalKey("idempotency", "orders", "tenant-b", CoreEx.AspNetCore.Idempotency.IdempotencyProvider.OperationName, key);
        SetTenant("tenant-a");        var calls = 0;

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

            SetTenant("tenant-b");
            var otherTenant = HttpContext(id, "same");
            await second.OnInvokeAsync(attribute, otherTenant, Execute);
            Encoding.UTF8.GetString(((MemoryStream)otherTenant.Response.Body).ToArray()).Should().Be("response-2");
            operationA.Should().StartWith("CoreEx:coordination:v1:idempotency:domain:orders:tenant:tenant-a:operation:http-idempotency:instance:")
                .And.NotContain(id).And.NotContain("caller-a");
            (await _first.GetDatabase().KeyExistsAsync(operationA)).Should().BeTrue();
            (await _first.GetDatabase().KeyExistsAsync(operationB)).Should().BeTrue();
        }
        finally
        {
            await _first.GetDatabase().KeyDeleteAsync(operationA);
            await _first.GetDatabase().KeyDeleteAsync(operationB);
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
    public async Task Identity_IsolatesDomainTenantAndPurpose_UsingAmbientTenantAndHostDomain()
    {
        var key = Key(Guid.NewGuid().ToString());
        await using var services = new ServiceCollection().AddLogging().AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(_first)
            .AddSingleton(Settings("orders")).AddRedisCoordination().BuildServiceProvider();
        await using var scope = services.CreateAsyncScope();
        var a = scope.ServiceProvider.GetRequiredService<CoreEx.Coordination.ISynchronizer>();
        var store = scope.ServiceProvider.GetRequiredService<CoreEx.Idempotency.IIdempotencyStore>();
        var b = Provider(_second, domainName: "orders");
        var products = Provider(_second, domainName: "products");

        SetTenant("tenant-a");
        await using var first = await a.TryAcquireAsync("import", key);
        first.Should().NotBeNull();
        (await b.TryAcquireAsync("import", key)).Should().BeNull();
        await using var domain = await products.TryAcquireAsync("import", key);
        SetTenant("tenant-b");
        await using var tenant = await b.TryAcquireAsync("import", key);
        SetTenant(null);
        await using var domainWide = await b.TryAcquireAsync("import", key);
        tenant.Should().NotBeNull();
        domain.Should().NotBeNull();
        domainWide.Should().NotBeNull();

        SetTenant("tenant-a");
        var attempt = await store.TryBeginAsync("import", key, "request");
        attempt.Outcome.Should().Be(CoreEx.Idempotency.IdempotencyOutcome.Acquired);
        await attempt.Attempt!.DisposeAsync();
    }

    [Test]
    public async Task Identity_TenantIsDistinctFromDomainWide()
    {
        var key = Key(Guid.NewGuid().ToString());
        var a = Provider(_first, domainName: "orders");
        var b = Provider(_second, domainName: "orders");

        SetTenant(null);
        await using var domainWide = await a.TryAcquireAsync("import", key);
        domainWide.Should().NotBeNull();
        var begun = await a.TryBeginAsync("import", key, "request");
        begun.Outcome.Should().Be(CoreEx.Idempotency.IdempotencyOutcome.Acquired);
        await begun.Attempt!.CompleteAsync(new BinaryData("domain-wide"));
        await begun.Attempt.DisposeAsync();

        SetTenant("acme");
        await using var tenantLease = await b.TryAcquireAsync("import", key);
        tenantLease.Should().NotBeNull();
        var tenant = await b.TryBeginAsync("import", key, "request");
        tenant.Outcome.Should().Be(CoreEx.Idempotency.IdempotencyOutcome.Acquired);
        tenant.Response.Should().BeNull();
        await tenant.Attempt!.DisposeAsync();

        SetTenant(null);
        (await b.TryBeginAsync("import", key, "request")).Response!.ToString().Should().Be("domain-wide");
    }

    [Test]
    public async Task IdempotencyKeys_ReplayAndReconcileOnlyTheMatchingIdentity()
    {
        var key = Key(Guid.NewGuid().ToString());
        var a = Provider(_first, domainName: "orders");
        var b = Provider(_second, domainName: "orders");
        SetTenant("tenant-a");
        var results = await Task.WhenAll(a.TryBeginAsync("import", key, "same"), b.TryBeginAsync("import", key, "same"));
        results.Count(x => x.Outcome == CoreEx.Idempotency.IdempotencyOutcome.Acquired).Should().Be(1);
        var owner = results.Single(x => x.Attempt is not null);
        await owner.Attempt!.DisposeAsync();

        SetTenant("tenant-b");
        (await b.TryResolveAsync("import", key, owner.Revision!, null, true)).Should().BeFalse();
        SetTenant("tenant-a");
        (await b.TryBeginAsync("import", key, "same")).Outcome.Should().Be(CoreEx.Idempotency.IdempotencyOutcome.Indeterminate);
        (await b.TryResolveAsync("import", key, owner.Revision!, null, true)).Should().BeTrue();
        var retry = await a.TryBeginAsync("import", key, "same");
        await retry.Attempt!.CompleteAsync(new BinaryData("response"));
        await retry.Attempt.DisposeAsync();
        (await b.TryBeginAsync("import", key, "same")).Response!.ToString().Should().Be("response");
        (await b.TryBeginAsync("import", key, "different")).Outcome.Should().Be(CoreEx.Idempotency.IdempotencyOutcome.DifferentRequest);
    }
    [Test]
    public async Task PreCancelledAcquisition_DoesNotCreateLeaseOrProcessingRecord()
    {
        var provider = Provider(_first);
        var key = Key(Guid.NewGuid().ToString());
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Func<Task> lease = () => provider.TryAcquireAsync(Name, key, cancellation.Token);
        Func<Task> attempt = () => provider.TryBeginAsync(Name, key, "request", cancellation.Token);
        await lease.Should().ThrowAsync<OperationCanceledException>();
        await attempt.Should().ThrowAsync<OperationCanceledException>();
        await using var acquired = await provider.TryAcquireAsync(Name, key);
        acquired.Should().NotBeNull();
        var begun = await provider.TryBeginAsync(Name, key, "request");
        begun.Outcome.Should().Be(CoreEx.Idempotency.IdempotencyOutcome.Acquired);
        await begun.Attempt!.DisposeAsync();
    }

    [Test]
    public async Task StaleLeaseDisposal_CannotDeleteSuccessor()
    {
        var key = Key(Guid.NewGuid().ToString());
        var provider = Provider(_first);
        var original = await provider.TryAcquireAsync(Name, key);
        var physical = PhysicalKey("lease", key);
        await _second.GetDatabase().KeyDeleteAsync(physical);
        await using var successor = await Provider(_second).TryAcquireAsync(Name, key);
        await original!.DisposeAsync();
        (await provider.TryAcquireAsync(Name, key)).Should().BeNull();
    }

    [Test]
    public async Task ExpiredAttempt_CannotCompleteOrAffectReconciledSuccessor()
    {
        var key = Key(Guid.NewGuid().ToString());
        var a = Provider(_first);
        var b = Provider(_second);
        var original = await a.TryBeginAsync(Name, key, "same");
        await _second.GetDatabase().HashSetAsync(PhysicalKey("idempotency", key), "d", 0);
        Func<Task> expired = () => original.Attempt!.CompleteAsync(new BinaryData("stale"));
        await expired.Should().ThrowAsync<InvalidOperationException>();
        (await b.TryResolveAsync(Name, key, original.Revision!, null, true)).Should().BeTrue();
        var successor = await b.TryBeginAsync(Name, key, "same");
        await original.Attempt!.DisposeAsync();
        await successor.Attempt!.CompleteAsync(new BinaryData("successor"));
        await successor.Attempt.DisposeAsync();
        (await a.TryBeginAsync(Name, key, "same")).Response!.ToString().Should().Be("successor");
    }

    [Test]
    public async Task DuplicateReads_DoNotExtendTerminalRetention()
    {
        var options = new CoreEx.Coordination.CoordinationOptions { LeaseDuration = TimeSpan.FromMilliseconds(700), RenewalInterval = TimeSpan.FromMilliseconds(150), OperationTimeout = TimeSpan.FromMilliseconds(200), Retention = TimeSpan.FromSeconds(1) };
        var key = Key(Guid.NewGuid().ToString());
        var provider = Provider(_first, options);
        var original = await provider.TryBeginAsync(Name, key, "same");
        await original.Attempt!.CompleteAsync(null);
        await original.Attempt.DisposeAsync();
        await Task.Delay(400);
        (await provider.TryBeginAsync(Name, key, "same")).Outcome.Should().Be(CoreEx.Idempotency.IdempotencyOutcome.ResponseUnavailable);
        await Task.Delay(700);
        var next = await provider.TryBeginAsync(Name, key, "same");
        next.Outcome.Should().Be(CoreEx.Idempotency.IdempotencyOutcome.Acquired);
        await next.Attempt!.DisposeAsync();
    }

    [Test]
    public async Task BackendFailure_ThrowsRatherThanReturningContentionOrStartingWork()
    {
        using var connection = await StackExchange.Redis.ConnectionMultiplexer.ConnectAsync(Environment.GetEnvironmentVariable("COREEX_REDIS_TEST_CONNECTION") ?? "localhost:6379");
        var provider = Provider(connection);
        await connection.CloseAsync();
        Func<Task> acquire = () => provider.TryAcquireAsync(Name, Key(Guid.NewGuid().ToString()));
        Func<Task> begin = () => provider.TryBeginAsync(Name, Key(Guid.NewGuid().ToString()), "same");
        await acquire.Should().ThrowAsync<Exception>();
        await begin.Should().ThrowAsync<Exception>();
    }

    private static string PhysicalKey(string purpose, CoreEx.Entities.CompositeKey key) => PhysicalKey(purpose, "redis-tests", null, Name, key);

    private static string PhysicalKey(string purpose, string domainName, string? tenantId, string operationName, CoreEx.Entities.CompositeKey key)
        => $"CoreEx:coordination:v1:{purpose}:domain:{Uri.EscapeDataString(domainName)}{(tenantId is null ? string.Empty : $":tenant:{Uri.EscapeDataString(tenantId)}")}:operation:{Uri.EscapeDataString(operationName)}:instance:{Uri.EscapeDataString(key.ToCanonicalString())}";

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
        await using var recovered = await Provider(_second).TryAcquireAsync(Name, key);
        recovered.Should().NotBeNull();
    }

    [Test]
    public async Task SeparateProcesses_UseTheSameNameAndKey()
    {
        var key = Key(Guid.NewGuid().ToString());
        var provider = Provider(_second, domainName: "orders");
        using var owner = StartWorker("lease", "orders", "tenant-a", "import", key);
        try
        {
            (await owner.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(15))).Should().Be("acquired");
            SetTenant("tenant-a");
            (await provider.TryAcquireAsync("import", key)).Should().BeNull();
            SetTenant("tenant-b");
            await using var otherTenant = await provider.TryAcquireAsync("import", key);
            otherTenant.Should().NotBeNull();
        }
        finally { StopWorker(owner); }

        await Task.Delay(1300);
        SetTenant("tenant-a");
        await using var recovered = await provider.TryAcquireAsync("import", key);
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
        var result = await provider.TryBeginAsync(Name, key, "request");
        result.Outcome.Should().Be(CoreEx.Idempotency.IdempotencyOutcome.Indeterminate);
        (await provider.TryResolveAsync(Name, key, result.Revision!, null, true)).Should().BeTrue();
    }

    private static Process StartWorker(string operation, CoreEx.Entities.CompositeKey key) => StartWorker(operation, "redis-tests", null, Name, key);

    private static Process StartWorker(string operation, string domainName, string? tenantId, string name, CoreEx.Entities.CompositeKey key)
    {
        var configuration = new DirectoryInfo(TestContext.CurrentContext.TestDirectory).Parent!.Name;
        var framework = new DirectoryInfo(TestContext.CurrentContext.TestDirectory).Name;
        var worker = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "CoreEx.Redis.Test.Worker", "bin", configuration, framework, "CoreEx.Redis.Test.Worker.dll"));
        var start = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        start.ArgumentList.Add(worker);
        start.ArgumentList.Add(operation);
        start.ArgumentList.Add(domainName);
        start.ArgumentList.Add(tenantId ?? "-");
        start.ArgumentList.Add(name);
        start.ArgumentList.Add(key.ToCanonicalString());
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

}
