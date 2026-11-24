namespace CoreEx.Test.Unit.Hosting.Synchronization;

[TestFixture]
public class InMemoryCoordinationTests
{
    private const string Name = "coordination";

    [SetUp]
    [TearDown]
    public void ResetExecutionContext() => ExecutionContext.Reset();

    private static Microsoft.Extensions.DependencyInjection.ServiceProvider Services(string domainName = "tests", InMemoryCoordinationState? state = null)
    {
        var sc = new Microsoft.Extensions.DependencyInjection.ServiceCollection().AddLogging()
            .AddSingleton<IHostSettings>(new HostSettings { SolutionName = "Tests", DomainName = domainName, EnvironmentName = "Test" });

        if (state is not null)
            sc.AddSingleton(state);

        return sc.AddInMemoryCoordination().BuildServiceProvider();
    }

    private static CompositeKey Key(string id) => CompositeKey.Create(id);

    private static void SetTenant(string? tenantId)
    {
        ExecutionContext.Reset();
        if (tenantId is not null)
            ExecutionContext.SetCurrent(new ExecutionContext { TenantId = tenantId });
    }

    [Test]
    public async Task IndependentScopes_OnlyOneOwner_ThenReleaseAllowsReentry()
    {
        await using var services = Services();
        await using var first = services.CreateAsyncScope();
        await using var second = services.CreateAsyncScope();
        var a = first.ServiceProvider.GetRequiredService<ISynchronizer>();
        var b = second.ServiceProvider.GetRequiredService<ISynchronizer>();
        var attempts = await Task.WhenAll(a.TryAcquireAsync(Name, Key("one")), b.TryAcquireAsync(Name, Key("one")));
        attempts.Count(x => x is not null).Should().Be(1);
        await attempts.Single(x => x is not null)!.DisposeAsync();
        await using var reacquired = await b.TryAcquireAsync(Name, Key("one"));
        reacquired.Should().NotBeNull();
    }

    [TestCase("")]
    [TestCase(" ")]
    public async Task BlankCanonicalKeysAreRejected(string value)
    {
        await using var services = Services();
        var synchronizer = services.GetRequiredService<ISynchronizer>();
        var store = services.GetRequiredService<CoreEx.Idempotency.IIdempotencyStore>();
        var key = Key(value);

        await FluentActions.Awaiting(() => synchronizer.TryAcquireAsync(Name, key)).Should().ThrowAsync<ArgumentException>().WithParameterName("key");
        await FluentActions.Awaiting(() => store.TryBeginAsync(Name, key, "request")).Should().ThrowAsync<ArgumentException>().WithParameterName("key");
        await FluentActions.Awaiting(() => store.TryResolveAsync(Name, key, "revision", null)).Should().ThrowAsync<ArgumentException>().WithParameterName("key");
    }

    [Test]
    public async Task TypeQualified_UnnamedAndNamedDefaultLeasesAreDistinct()
    {
        await using var services = Services();
        var synchronizer = services.GetRequiredService<ISynchronizer>();
        await using var unnamed = await synchronizer.TryAcquireAsync<InMemoryCoordinationTests>();
        await using var named = await synchronizer.TryAcquireAsync<InMemoryCoordinationTests>("default");

        unnamed.Should().NotBeNull();
        named.Should().NotBeNull();
        (await synchronizer.TryAcquireAsync<InMemoryCoordinationTests>()).Should().BeNull();
        (await synchronizer.TryAcquireAsync<InMemoryCoordinationTests>("default")).Should().BeNull();
    }

    [Test]
    public async Task UnknownOutcome_RemainsBlocked_AndRevisionCheckedReconciliationAllowsRetry()
    {
        await using var services = Services();
        var store = services.GetRequiredService<CoreEx.Idempotency.IIdempotencyStore>();
        var first = await store.TryBeginAsync(Name, Key("one"), "request");
        (await store.TryBeginAsync(Name, Key("one"), "request")).Outcome.Should().Be(CoreEx.Idempotency.IdempotencyOutcome.InProgress);
        (await store.TryResolveAsync(Name, Key("one"), first.Revision!, null, true)).Should().BeFalse();
        await first.Attempt!.DisposeAsync();
        (await store.TryBeginAsync(Name, Key("one"), "request")).Outcome.Should().Be(CoreEx.Idempotency.IdempotencyOutcome.Indeterminate);
        (await store.TryResolveAsync(Name, Key("one"), "wrong", null, true)).Should().BeFalse();
        (await store.TryResolveAsync(Name, Key("one"), first.Revision!, null, true)).Should().BeTrue();
        var retry = await store.TryBeginAsync(Name, Key("one"), "request");
        retry.Outcome.Should().Be(CoreEx.Idempotency.IdempotencyOutcome.Acquired);
        await retry.Attempt!.DisposeAsync();
    }

    [Test]
    public async Task CompletionReplays_AndOldOwnerCannotOverwriteTerminalResponse()
    {
        await using var services = Services();
        var store = services.GetRequiredService<CoreEx.Idempotency.IIdempotencyStore>();
        var first = await store.TryBeginAsync(Name, Key("one"), "request");
        await first.Attempt!.CompleteAsync(new BinaryData("response"));
        await first.Attempt.DisposeAsync();
        var replay = await store.TryBeginAsync(Name, Key("one"), "request");
        replay.Outcome.Should().Be(CoreEx.Idempotency.IdempotencyOutcome.Completed);
        replay.Response!.ToString().Should().Be("response");
        (await store.TryBeginAsync(Name, Key("one"), "changed")).Outcome.Should().Be(CoreEx.Idempotency.IdempotencyOutcome.DifferentRequest);
    }

    [Test]
    public async Task ExplicitReleaseAllowsOnlyTheOwnedAttemptToRetry()
    {
        await using var services = Services();
        var store = services.GetRequiredService<CoreEx.Idempotency.IIdempotencyStore>();
        var first = await store.TryBeginAsync(Name, Key("release"), "request");
        var attempt = first.Attempt.Should().BeOfType<CoreEx.Idempotency.IdempotencyAttempt>().Subject;

        await attempt.ReleaseForRetryAsync();
        var retry = await store.TryBeginAsync(Name, Key("release"), "request");

        retry.Outcome.Should().Be(CoreEx.Idempotency.IdempotencyOutcome.Acquired);
        await retry.Attempt!.CompleteAsync(new BinaryData("response"));
        (await store.TryBeginAsync(Name, Key("release"), "request")).Outcome.Should().Be(CoreEx.Idempotency.IdempotencyOutcome.Completed);
        await attempt.DisposeAsync();
    }

    [Test]
    public async Task Identity_SeparatesDomainsTenantsAndPurposes_UsingAmbientTenantAndHostDomain()
    {
        var state = new InMemoryCoordinationState();
        await using var orders = Services("orders", state);
        await using var products = Services("products", state);
        var synchronizer = orders.GetRequiredService<ISynchronizer>();
        var otherDomainSynchronizer = products.GetRequiredService<ISynchronizer>();
        var store = orders.GetRequiredService<CoreEx.Idempotency.IIdempotencyStore>();
        var key = Key("same");

        SetTenant("tenant-a");
        await using var lease = await synchronizer.TryAcquireAsync("import", key);
        lease.Should().NotBeNull();
        (await synchronizer.TryAcquireAsync("import", key)).Should().BeNull();
        await using var otherDomain = await otherDomainSynchronizer.TryAcquireAsync("import", key);
        otherDomain.Should().NotBeNull();

        SetTenant("tenant-b");
        await using var otherTenant = await synchronizer.TryAcquireAsync("import", key);
        otherTenant.Should().NotBeNull();

        SetTenant(null);
        await using var domainWide = await synchronizer.TryAcquireAsync("import", key);
        domainWide.Should().NotBeNull();

        SetTenant("tenant-a");
        var attempt = await store.TryBeginAsync("import", key, "request");
        attempt.Outcome.Should().Be(CoreEx.Idempotency.IdempotencyOutcome.Acquired);
        await attempt.Attempt!.DisposeAsync();
    }

    [Test]
    public async Task IdempotencyResolution_UsesTheSameCompleteIdentity()
    {
        await using var services = Services("orders");
        var store = services.GetRequiredService<CoreEx.Idempotency.IIdempotencyStore>();
        var key = Key("same");
        SetTenant("tenant-a");
        var first = await store.TryBeginAsync("import", key, "request");
        await first.Attempt!.DisposeAsync();

        SetTenant("tenant-b");
        (await store.TryResolveAsync("import", key, first.Revision!, null, true)).Should().BeFalse();
        SetTenant("tenant-a");
        (await store.TryBeginAsync("import", key, "request")).Outcome.Should().Be(CoreEx.Idempotency.IdempotencyOutcome.Indeterminate);
        (await store.TryResolveAsync("import", key, first.Revision!, null, true)).Should().BeTrue();
        var retry = await store.TryBeginAsync("import", key, "request");
        retry.Outcome.Should().Be(CoreEx.Idempotency.IdempotencyOutcome.Acquired);
        await retry.Attempt!.CompleteAsync(new BinaryData("response"));
        await retry.Attempt.DisposeAsync();
        (await store.TryBeginAsync("import", key, "request")).Response!.ToString().Should().Be("response");
        (await store.TryBeginAsync("import", key, "changed")).Outcome.Should().Be(CoreEx.Idempotency.IdempotencyOutcome.DifferentRequest);
    }

    [Test]
    public async Task RepeatedCompletion_RequiresIdenticalOpaqueBytes()
    {
        await using var services = Services();
        var store = services.GetRequiredService<CoreEx.Idempotency.IIdempotencyStore>();
        var begun = await store.TryBeginAsync(Name, Key("opaque"), "request");
        await using var attempt = begun.Attempt!;
        await attempt.CompleteAsync(new BinaryData(new byte[] { 0x80 }));
        await attempt.CompleteAsync(new BinaryData(new byte[] { 0x80 }));
        Func<Task> changed = () => attempt.CompleteAsync(new BinaryData(new byte[] { 0x81 }));
        await changed.Should().ThrowAsync<InvalidOperationException>();
        (await store.TryBeginAsync(Name, Key("opaque"), "request")).Response!.ToArray().Should().Equal(0x80);
    }
}
