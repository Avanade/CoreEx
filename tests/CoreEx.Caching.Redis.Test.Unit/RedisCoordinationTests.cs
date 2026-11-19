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

    private static CoreEx.Redis.RedisCoordination Provider(StackExchange.Redis.IConnectionMultiplexer connection, CoreEx.Coordination.CoordinationOptions? options = null, CoreEx.Caching.ICacheKeyProvider? keyProvider = null)
        => new(connection, keyProvider ?? new CoreEx.Caching.DefaultCacheKeyProvider(), Microsoft.Extensions.Logging.Abstractions.NullLogger<CoreEx.Redis.RedisCoordination>.Instance, options ?? new());

    [Test]
    public async Task IndependentClients_ConcurrentAcquisitionHasOneOwner_AndReleaseAllowsReentry()
    {
        var key = Guid.NewGuid().ToString();
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
        var key = Guid.NewGuid().ToString();
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
    public async Task UnknownOutcome_RequiresRevisionCheckedReconciliation_NotAutomaticTakeover()
    {
        var key = Guid.NewGuid().ToString();
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
        var key = Guid.NewGuid().ToString();
        var a = Provider(_first);
        var b = Provider(_second);
        var result = await a.TryBeginAsync(key, "same");
        await result.Attempt!.DisposeAsync();
        (await b.TryResolveAsync(key, result.Revision!, new BinaryData("reconciled"))).Should().BeTrue();
        (await b.TryResolveAsync(key, result.Revision!, new BinaryData("again"))).Should().BeFalse();
        var replay = await a.TryBeginAsync(key, "same");
        replay.Outcome.Should().Be(CoreEx.Idempotency.IdempotencyOutcome.Completed);
        replay.Response!.ToString().Should().Be("reconciled");

        var other = Guid.NewGuid().ToString();
        var unavailable = await a.TryBeginAsync(other, "same");
        await unavailable.Attempt!.DisposeAsync();
        (await b.TryResolveAsync(other, unavailable.Revision!, null)).Should().BeTrue();
        (await a.TryBeginAsync(other, "same")).Outcome.Should().Be(CoreEx.Idempotency.IdempotencyOutcome.ResponseUnavailable);
    }

    [Test]
    public async Task EmptyReplayableResponse_IsRejected_ByProviderAndScripts()
    {
        var key = Guid.NewGuid().ToString();
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
        var key = Guid.NewGuid().ToString();
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
        await using var lease = await scope.ServiceProvider.GetRequiredService<CoreEx.Coordination.ISynchronizer>().TryAcquireAsync(Guid.NewGuid().ToString());
        lease.Should().NotBeNull();
    }

    [Test]
    public async Task CustomKeyProvider_IsRespectedAndCapturedAcrossTenantChanges()
    {
        var key = Guid.NewGuid().ToString();
        var qualifier = new ChangingQualifier();
        var a = Provider(_first, keyProvider: qualifier);
        var b = Provider(_second, keyProvider: new ChangingQualifier());
        var original = await a.TryAcquireAsync(key);
        (await b.TryAcquireAsync(key)).Should().BeNull();
        qualifier.Prefix = "tenant-b";
        await using var isolated = await a.TryAcquireAsync(key);
        isolated.Should().NotBeNull();
        await original!.DisposeAsync();
        await using var next = await b.TryAcquireAsync(key);
        next.Should().NotBeNull();
        qualifier.Calls.Should().Be(2);
    }

    [Test]
    public async Task DefaultKeyProvider_IsolatesDomainsAndTenants_OnTheSameRedis()
    {
        var key = Guid.NewGuid().ToString();
        var settings = Moq.Mock.Of<CoreEx.Hosting.IHostSettings>(x => x.DomainName == "domain-a");
        var otherSettings = Moq.Mock.Of<CoreEx.Hosting.IHostSettings>(x => x.DomainName == "domain-b");
        var context = new CoreEx.ExecutionContext { TenantId = "tenant-a" };
        var a = Provider(_first, keyProvider: new CoreEx.Caching.DefaultCacheKeyProvider(settings, context));
        var matching = Provider(_second, keyProvider: new CoreEx.Caching.DefaultCacheKeyProvider(settings, new CoreEx.ExecutionContext { TenantId = "tenant-a" }));
        var otherTenant = Provider(_second, keyProvider: new CoreEx.Caching.DefaultCacheKeyProvider(settings, new CoreEx.ExecutionContext { TenantId = "tenant-b" }));
        var otherDomain = Provider(_second, keyProvider: new CoreEx.Caching.DefaultCacheKeyProvider(otherSettings, context));
        await using var first = await a.TryAcquireAsync(key);
        (await matching.TryAcquireAsync(key)).Should().BeNull();
        await using var tenant = await otherTenant.TryAcquireAsync(key);
        await using var domain = await otherDomain.TryAcquireAsync(key);
        tenant.Should().NotBeNull();
        domain.Should().NotBeNull();
        context.TenantId = "changed-after-acquisition";
    }

    [Test]
    public async Task PreCancelledAcquisition_DoesNotCreateLeaseOrProcessingRecord()
    {
        var provider = Provider(_first);
        var key = Guid.NewGuid().ToString();
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
        var key = Guid.NewGuid().ToString();
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
        var key = Guid.NewGuid().ToString();
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
        var key = Guid.NewGuid().ToString();
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
        Func<Task> acquire = () => provider.TryAcquireAsync(Guid.NewGuid().ToString());
        Func<Task> begin = () => provider.TryBeginAsync(Guid.NewGuid().ToString(), "same");
        await acquire.Should().ThrowAsync<Exception>();
        await begin.Should().ThrowAsync<Exception>();
    }

    private static string PhysicalKey(string purpose, string key)
        => "CoreEx:coordination:v1:" + purpose + ":" + Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(key)));

    [Test]
    public async Task SeparateProcesses_ExcludeConcurrentOwner_AndCrashPermitsLeaseRecovery()
    {
        var key = Guid.NewGuid().ToString();
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
    public async Task SeparateProcessCrash_LeavesIdempotencyIndeterminateInsteadOfTakingOver()
    {
        var key = Guid.NewGuid().ToString();
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

    private static Process StartWorker(string operation, string key)
    {
        var configuration = new DirectoryInfo(TestContext.CurrentContext.TestDirectory).Parent!.Name;
        var framework = new DirectoryInfo(TestContext.CurrentContext.TestDirectory).Name;
        var worker = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "CoreEx.Redis.Test.Worker", "bin", configuration, framework, "CoreEx.Redis.Test.Worker.dll"));
        var start = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        start.ArgumentList.Add(worker);
        start.ArgumentList.Add(operation);
        start.ArgumentList.Add(key);
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
