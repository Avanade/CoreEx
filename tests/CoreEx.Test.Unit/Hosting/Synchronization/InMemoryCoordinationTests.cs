namespace CoreEx.Test.Unit.Hosting.Synchronization;

[TestFixture]
public class InMemoryCoordinationTests
{
    private static Microsoft.Extensions.DependencyInjection.ServiceProvider Services()
        => new Microsoft.Extensions.DependencyInjection.ServiceCollection().AddLogging().AddInMemoryCoordination().BuildServiceProvider();

    [Test]
    public async Task IndependentScopes_OnlyOneOwner_ThenReleaseAllowsReentry()
    {
        await using var services = Services();
        await using var first = services.CreateAsyncScope();
        await using var second = services.CreateAsyncScope();
        var a = first.ServiceProvider.GetRequiredService<ISynchronizer>();
        var b = second.ServiceProvider.GetRequiredService<ISynchronizer>();
        var attempts = await Task.WhenAll(a.TryAcquireAsync("one"), b.TryAcquireAsync("one"));
        attempts.Count(x => x is not null).Should().Be(1);
        await attempts.Single(x => x is not null)!.DisposeAsync();
        await using var reacquired = await b.TryAcquireAsync("one");
        reacquired.Should().NotBeNull();
    }

    [Test]
    public async Task UnknownOutcome_RemainsBlocked_AndRevisionCheckedReconciliationAllowsRetry()
    {
        await using var services = Services();
        var store = services.GetRequiredService<CoreEx.Idempotency.IIdempotencyStore>();
        var first = await store.TryBeginAsync("one", "request");
        (await store.TryBeginAsync("one", "request")).Outcome.Should().Be(CoreEx.Idempotency.IdempotencyOutcome.InProgress);
        (await store.TryResolveAsync("one", first.Revision!, null, true)).Should().BeFalse();
        await first.Attempt!.DisposeAsync();
        (await store.TryBeginAsync("one", "request")).Outcome.Should().Be(CoreEx.Idempotency.IdempotencyOutcome.Indeterminate);
        (await store.TryResolveAsync("one", "wrong", null, true)).Should().BeFalse();
        (await store.TryResolveAsync("one", first.Revision!, null, true)).Should().BeTrue();
        var retry = await store.TryBeginAsync("one", "request");
        retry.Outcome.Should().Be(CoreEx.Idempotency.IdempotencyOutcome.Acquired);
        await retry.Attempt!.DisposeAsync();
    }

    [Test]
    public async Task CompletionReplays_AndOldOwnerCannotOverwriteTerminalResponse()
    {
        await using var services = Services();
        var store = services.GetRequiredService<CoreEx.Idempotency.IIdempotencyStore>();
        var first = await store.TryBeginAsync("one", "request");
        await first.Attempt!.CompleteAsync(new BinaryData("response"));
        await first.Attempt.DisposeAsync();
        var replay = await store.TryBeginAsync("one", "request");
        replay.Outcome.Should().Be(CoreEx.Idempotency.IdempotencyOutcome.Completed);
        replay.Response!.ToString().Should().Be("response");
        (await store.TryBeginAsync("one", "changed")).Outcome.Should().Be(CoreEx.Idempotency.IdempotencyOutcome.DifferentRequest);
    }

    [Test]
    public async Task KeyProviderIsCapturedOnce_AndLaterContextChangeDoesNotRedirectRelease()
    {
        var qualifier = new ChangingQualifier();
        await using var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection().AddLogging()
            .AddSingleton<CoreEx.Caching.ICacheKeyProvider>(qualifier).AddInMemoryCoordination().BuildServiceProvider();
        var synchronizer = services.GetRequiredService<ISynchronizer>();
        var lease = await synchronizer.TryAcquireAsync("one");
        qualifier.Prefix = "tenant-b";
        await lease!.DisposeAsync();
        qualifier.Prefix = "tenant-a";
        await using var next = await synchronizer.TryAcquireAsync("one");
        next.Should().NotBeNull();
        qualifier.Calls.Should().Be(2);
    }

    [Test]
    public async Task RepeatedCompletion_RequiresIdenticalOpaqueBytes()
    {
        await using var services = Services();
        var store = services.GetRequiredService<CoreEx.Idempotency.IIdempotencyStore>();
        var begun = await store.TryBeginAsync("opaque", "request");
        await using var attempt = begun.Attempt!;
        await attempt.CompleteAsync(new BinaryData(new byte[] { 0x80 }));
        await attempt.CompleteAsync(new BinaryData(new byte[] { 0x80 }));
        Func<Task> changed = () => attempt.CompleteAsync(new BinaryData(new byte[] { 0x81 }));
        await changed.Should().ThrowAsync<InvalidOperationException>();
        (await store.TryBeginAsync("opaque", "request")).Response!.ToArray().Should().Equal(0x80);
    }

    private sealed class ChangingQualifier : CoreEx.Caching.ICacheKeyProvider
    {
        public string Prefix { get; set; } = "tenant-a";
        public int Calls { get; private set; }
        public string GetFullyQualifiedCacheKey(string key) { Calls++; return Prefix + ":" + key; }
        public string GetEntityCacheKey<T>(CompositeKey key) where T : CoreEx.Entities.IEntityKey => throw new NotSupportedException();
    }
}
