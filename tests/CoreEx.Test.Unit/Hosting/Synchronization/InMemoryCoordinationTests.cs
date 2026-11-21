namespace CoreEx.Test.Unit.Hosting.Synchronization;

[TestFixture]
public class InMemoryCoordinationTests
{
    private static Microsoft.Extensions.DependencyInjection.ServiceProvider Services()
        => new Microsoft.Extensions.DependencyInjection.ServiceCollection().AddLogging().AddInMemoryCoordination().BuildServiceProvider();

    private static OperationKey Key(string id) => OperationKey.ForDomain("tests", "coordination", id);

    [Test]
    public async Task IndependentScopes_OnlyOneOwner_ThenReleaseAllowsReentry()
    {
        await using var services = Services();
        await using var first = services.CreateAsyncScope();
        await using var second = services.CreateAsyncScope();
        var a = first.ServiceProvider.GetRequiredService<ISynchronizer>();
        var b = second.ServiceProvider.GetRequiredService<ISynchronizer>();
        var attempts = await Task.WhenAll(a.TryAcquireAsync(Key("one")), b.TryAcquireAsync(Key("one")));
        attempts.Count(x => x is not null).Should().Be(1);
        await attempts.Single(x => x is not null)!.DisposeAsync();
        await using var reacquired = await b.TryAcquireAsync(Key("one"));
        reacquired.Should().NotBeNull();
    }

    [Test]
    public async Task UnknownOutcome_RemainsBlocked_AndRevisionCheckedReconciliationAllowsRetry()
    {
        await using var services = Services();
        var store = services.GetRequiredService<CoreEx.Idempotency.IIdempotencyStore>();
        var first = await store.TryBeginAsync(Key("one"), "request");
        (await store.TryBeginAsync(Key("one"), "request")).Outcome.Should().Be(CoreEx.Idempotency.IdempotencyOutcome.InProgress);
        (await store.TryResolveAsync(Key("one"), first.Revision!, null, true)).Should().BeFalse();
        await first.Attempt!.DisposeAsync();
        (await store.TryBeginAsync(Key("one"), "request")).Outcome.Should().Be(CoreEx.Idempotency.IdempotencyOutcome.Indeterminate);
        (await store.TryResolveAsync(Key("one"), "wrong", null, true)).Should().BeFalse();
        (await store.TryResolveAsync(Key("one"), first.Revision!, null, true)).Should().BeTrue();
        var retry = await store.TryBeginAsync(Key("one"), "request");
        retry.Outcome.Should().Be(CoreEx.Idempotency.IdempotencyOutcome.Acquired);
        await retry.Attempt!.DisposeAsync();
    }

    [Test]
    public async Task CompletionReplays_AndOldOwnerCannotOverwriteTerminalResponse()
    {
        await using var services = Services();
        var store = services.GetRequiredService<CoreEx.Idempotency.IIdempotencyStore>();
        var first = await store.TryBeginAsync(Key("one"), "request");
        await first.Attempt!.CompleteAsync(new BinaryData("response"));
        await first.Attempt.DisposeAsync();
        var replay = await store.TryBeginAsync(Key("one"), "request");
        replay.Outcome.Should().Be(CoreEx.Idempotency.IdempotencyOutcome.Completed);
        replay.Response!.ToString().Should().Be("response");
        (await store.TryBeginAsync(Key("one"), "changed")).Outcome.Should().Be(CoreEx.Idempotency.IdempotencyOutcome.DifferentRequest);
    }

    [Test]
    public async Task ExplicitReleaseAllowsOnlyTheOwnedAttemptToRetry()
    {
        await using var services = Services();
        var store = services.GetRequiredService<CoreEx.Idempotency.IIdempotencyStore>();
        var first = await store.TryBeginAsync(Key("release"), "request");
        var attempt = first.Attempt.Should().BeOfType<CoreEx.Idempotency.IdempotencyAttempt>().Subject;

        await attempt.ReleaseForRetryAsync();
        var retry = await store.TryBeginAsync(Key("release"), "request");

        retry.Outcome.Should().Be(CoreEx.Idempotency.IdempotencyOutcome.Acquired);
        await retry.Attempt!.CompleteAsync(new BinaryData("response"));
        (await store.TryBeginAsync(Key("release"), "request")).Outcome.Should().Be(CoreEx.Idempotency.IdempotencyOutcome.Completed);
        await attempt.DisposeAsync();
    }

    [Test]
    public async Task ExplicitKeys_SeparateDomainsTenantsAndPurposes_WithoutAmbientQualification()
    {
        var qualifier = new ChangingQualifier();
        await using var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection().AddLogging()
            .AddSingleton<CoreEx.Caching.ICacheKeyProvider>(qualifier).AddInMemoryCoordination().BuildServiceProvider();
        var synchronizer = services.GetRequiredService<ISynchronizer>();
        var store = services.GetRequiredService<CoreEx.Idempotency.IIdempotencyStore>();
        var key = OperationKey.ForTenant("orders", "tenant-a", "import", "same");

        await using var lease = await synchronizer.TryAcquireAsync(key);
        qualifier.Prefix = "tenant-b";
        (await synchronizer.TryAcquireAsync(key)).Should().BeNull();
        await using var otherTenant = await synchronizer.TryAcquireAsync(OperationKey.ForTenant("orders", "tenant-b", "import", "same"));
        await using var otherDomain = await synchronizer.TryAcquireAsync(OperationKey.ForTenant("products", "tenant-a", "import", "same"));
        await using var domainWide = await synchronizer.TryAcquireAsync(OperationKey.ForDomain("orders", "import", "same"));
        otherTenant.Should().NotBeNull();
        otherDomain.Should().NotBeNull();
        domainWide.Should().NotBeNull();

        var attempt = await store.TryBeginAsync(key, "request");
        attempt.Outcome.Should().Be(CoreEx.Idempotency.IdempotencyOutcome.Acquired);
        await attempt.Attempt!.DisposeAsync();
        qualifier.Calls.Should().Be(0);
    }

    [Test]
    public async Task ExplicitIdempotencyResolution_UsesTheSameCompleteIdentity()
    {
        await using var services = Services();
        var store = services.GetRequiredService<CoreEx.Idempotency.IIdempotencyStore>();
        var key = OperationKey.ForTenant("orders", "tenant-a", "import", "same");
        var otherTenant = OperationKey.ForTenant("orders", "tenant-b", "import", "same");
        var first = await store.TryBeginAsync(key, "request");
        await first.Attempt!.DisposeAsync();

        (await store.TryResolveAsync(otherTenant, first.Revision!, null, true)).Should().BeFalse();
        (await store.TryBeginAsync(key, "request")).Outcome.Should().Be(CoreEx.Idempotency.IdempotencyOutcome.Indeterminate);
        (await store.TryResolveAsync(key, first.Revision!, null, true)).Should().BeTrue();
        var retry = await store.TryBeginAsync(key, "request");
        retry.Outcome.Should().Be(CoreEx.Idempotency.IdempotencyOutcome.Acquired);
        await retry.Attempt!.CompleteAsync(new BinaryData("response"));
        await retry.Attempt.DisposeAsync();
        (await store.TryBeginAsync(key, "request")).Response!.ToString().Should().Be("response");
        (await store.TryBeginAsync(key, "changed")).Outcome.Should().Be(CoreEx.Idempotency.IdempotencyOutcome.DifferentRequest);
    }

    [Test]
    public async Task RepeatedCompletion_RequiresIdenticalOpaqueBytes()
    {
        await using var services = Services();
        var store = services.GetRequiredService<CoreEx.Idempotency.IIdempotencyStore>();
        var begun = await store.TryBeginAsync(Key("opaque"), "request");
        await using var attempt = begun.Attempt!;
        await attempt.CompleteAsync(new BinaryData(new byte[] { 0x80 }));
        await attempt.CompleteAsync(new BinaryData(new byte[] { 0x80 }));
        Func<Task> changed = () => attempt.CompleteAsync(new BinaryData(new byte[] { 0x81 }));
        await changed.Should().ThrowAsync<InvalidOperationException>();
        (await store.TryBeginAsync(Key("opaque"), "request")).Response!.ToArray().Should().Equal(0x80);
    }

    private sealed class ChangingQualifier : CoreEx.Caching.ICacheKeyProvider
    {
        public string Prefix { get; set; } = "tenant-a";
        public int Calls { get; private set; }
        public string GetFullyQualifiedCacheKey(string key) { Calls++; return Prefix + ":" + key; }
        public string GetEntityCacheKey<T>(CompositeKey key) where T : CoreEx.Entities.IEntityKey => throw new NotSupportedException();
    }
}
