using CoreEx.Caching;
using CoreEx.Hosting.Synchronization;

namespace CoreEx.Test.Unit.Hosting.Synchronization;

[TestFixture]
public class HybridCacheSynchronizerTests
{
    private class FakeHybridCache : IHybridCache
    {
        private readonly Dictionary<string, (object? Value, string[] Tags)> _store = [];

        public ICacheKeyProvider KeyProvider { get; } = new DefaultCacheKeyProvider();

        public bool ContainsKey(string key) => _store.ContainsKey(key);

        public Task<(bool Exists, T? Value)> TryGetByKeyAsync<T>(string key, HybridCacheEntryOptions? options = null, CancellationToken cancellationToken = default)
            => Task.FromResult(_store.TryGetValue(key, out var entry) ? (true, (T?)entry.Value) : (false, default));

        public Task<T?> GetOrDefaultByKeyAsync<T>(string key, HybridCacheEntryOptions? options = null, CancellationToken cancellationToken = default)
            => Task.FromResult(_store.TryGetValue(key, out var entry) ? (T?)entry.Value : default);

        public Task SetByKeyAsync<T>(string key, T value, HybridCacheEntryOptions? options = null, CancellationToken cancellationToken = default)
        {
            _store[key] = (value, options?.Tags ?? []);
            return Task.CompletedTask;
        }

        public async Task<T> GetOrCreateByKeyAsync<T>(string key, Func<CancellationToken, Task<T>> factory, HybridCacheEntryOptions? options = null, CancellationToken cancellationToken = default)
        {
            if (_store.TryGetValue(key, out var entry))
                return (T)entry.Value!;

            var result = await factory(cancellationToken);
            _store[key] = (result, options?.Tags ?? []);
            return result;
        }

        public Task RemoveByKeyAsync(string key, HybridCacheEntryOptions? options = null, CancellationToken cancellationToken = default)
        {
            _store.Remove(key);
            return Task.CompletedTask;
        }

        public Task RemoveByTagAsync(string tag, HybridCacheEntryOptions? options = null, CancellationToken cancellationToken = default)
        {
            foreach (var k in _store.Where(kv => kv.Value.Tags.Contains(tag)).Select(kv => kv.Key).ToList())
                _store.Remove(k);

            return Task.CompletedTask;
        }

        public async Task RemoveByTagAsync(IEnumerable<string> tags, HybridCacheEntryOptions? options = null, CancellationToken cancellationToken = default)
        {
            foreach (var tag in tags)
                await RemoveByTagAsync(tag, options, cancellationToken);
        }
    }

    [Test]
    public async Task EnterAsync_FirstCaller_ReturnsTrue()
    {
        var synchronizer = new HybridCacheSynchronizer(new FakeHybridCache());
        var result = await synchronizer.EnterAsync<HybridCacheSynchronizerTests>();
        result.Should().BeTrue();
    }

    [Test]
    public async Task EnterAsync_AlreadyEntered_ReturnsFalse()
    {
        var synchronizer = new HybridCacheSynchronizer(new FakeHybridCache());
        (await synchronizer.EnterAsync<HybridCacheSynchronizerTests>()).Should().BeTrue();
        (await synchronizer.EnterAsync<HybridCacheSynchronizerTests>()).Should().BeFalse();
    }

    [Test]
    public async Task EnterAsync_DifferentNames_AreTrackedIndependently()
    {
        var synchronizer = new HybridCacheSynchronizer(new FakeHybridCache());
        (await synchronizer.EnterAsync<HybridCacheSynchronizerTests>("a")).Should().BeTrue();
        (await synchronizer.EnterAsync<HybridCacheSynchronizerTests>("b")).Should().BeTrue();
    }

    [Test]
    public async Task ExitAsync_AfterEnter_AllowsReentry()
    {
        var synchronizer = new HybridCacheSynchronizer(new FakeHybridCache());
        await synchronizer.EnterAsync<HybridCacheSynchronizerTests>();

        await synchronizer.ExitAsync<HybridCacheSynchronizerTests>();

        (await synchronizer.EnterAsync<HybridCacheSynchronizerTests>()).Should().BeTrue();
    }

    [Test]
    public async Task ExitAsync_NotEntered_ThrowsInvalidOperationException()
    {
        var synchronizer = new HybridCacheSynchronizer(new FakeHybridCache());
        Func<Task> act = () => synchronizer.ExitAsync<HybridCacheSynchronizerTests>();
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Test]
    public async Task ExitAsync_TwiceForSameEntry_SecondThrows()
    {
        var synchronizer = new HybridCacheSynchronizer(new FakeHybridCache());
        await synchronizer.EnterAsync<HybridCacheSynchronizerTests>();
        await synchronizer.ExitAsync<HybridCacheSynchronizerTests>();

        Func<Task> act = () => synchronizer.ExitAsync<HybridCacheSynchronizerTests>();
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Test]
    public async Task DisposeAsync_CleansUpUnexitedLock_AllowsReentryViaNewSynchronizer()
    {
        var cache = new FakeHybridCache();
        var synchronizer = new HybridCacheSynchronizer(cache);
        await synchronizer.EnterAsync<HybridCacheSynchronizerTests>();

        await synchronizer.DisposeAsync();

        var other = new HybridCacheSynchronizer(cache);
        (await other.EnterAsync<HybridCacheSynchronizerTests>()).Should().BeTrue();
    }

    [Test]
    public async Task DisposeAsync_WithNoActiveLocks_DoesNotThrow()
    {
        var synchronizer = new HybridCacheSynchronizer(new FakeHybridCache());
        Func<Task> act = async () => await synchronizer.DisposeAsync();
        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task EnterAsync_UsesCustomOptions_WhenSet()
    {
        var synchronizer = new HybridCacheSynchronizer(new FakeHybridCache())
        {
            Options = new HybridCacheEntryOptions { LocalExpiration = TimeSpan.FromMinutes(5) }
        };

        (await synchronizer.EnterAsync<HybridCacheSynchronizerTests>()).Should().BeTrue();
    }
}
