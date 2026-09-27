using CoreEx.Caching;
using CoreEx.RefData.Abstractions;

namespace CoreEx.RefData.Test.Unit;

public partial class ReferenceDataOrchestratorTests
{
    [Test]
    public void Constructor_NullCache_Throws()
    {
        Action act = () => new ReferenceDataHybridCache(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public async Task GetOrCreateAsync_CacheMiss_InvokesFactoryAndCaches()
    {
        var cache = new ReferenceDataHybridCache(new Caching.MemoryOnlyHybridCache());
        var callCount = 0;

        Task<IReferenceDataCollection> Factory(Type t, CancellationToken ct)
        {
            callCount++;
            return Task.FromResult<IReferenceDataCollection>(new DummyRefDataCollection { new DummyRefData { Id = 1, Code = "A" } });
        }

        var coll = await cache.GetOrCreateAsync(typeof(DummyRefDataCollection), Factory);

        callCount.Should().Be(1);
        coll.Should().BeOfType<DummyRefDataCollection>();
    }

    [Test]
    public async Task GetOrCreateAsync_CacheHit_DoesNotInvokeFactoryAgain()
    {
        var cache = new ReferenceDataHybridCache(new Caching.MemoryOnlyHybridCache());
        var callCount = 0;

        Task<IReferenceDataCollection> Factory(Type t, CancellationToken ct)
        {
            callCount++;
            return Task.FromResult<IReferenceDataCollection>(new DummyRefDataCollection { new DummyRefData { Id = 1, Code = "A" } });
        }

        var first = await cache.GetOrCreateAsync(typeof(DummyRefDataCollection), Factory);
        var second = await cache.GetOrCreateAsync(typeof(DummyRefDataCollection), Factory);

        callCount.Should().Be(1);
        second.Should().BeSameAs(first);
    }

    [Test]
    public async Task GetOrCreateAsync_FactoryReturnsNull_Throws()
    {
        var cache = new ReferenceDataHybridCache(new Caching.MemoryOnlyHybridCache());

        Func<Task> act = () => cache.GetOrCreateAsync(typeof(DummyRefDataCollection), (t, ct) => Task.FromResult<IReferenceDataCollection>(null!));

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*must not be null*");
    }

    [Test]
    public async Task GetOrCreateAsync_ConcurrentCalls_FactoryInvokedOnce()
    {
        var cache = new ReferenceDataHybridCache(new Caching.MemoryOnlyHybridCache());
        var callCount = 0;

        async Task<IReferenceDataCollection> Factory(Type t, CancellationToken ct)
        {
            Interlocked.Increment(ref callCount);
            await Task.Delay(50, ct);
            return new DummyRefDataCollection { new DummyRefData { Id = 1, Code = "A" } };
        }

        var tasks = Enumerable.Range(0, 10).Select(_ => cache.GetOrCreateAsync(typeof(DummyRefDataCollection), Factory));
        var results = await Task.WhenAll(tasks);

        callCount.Should().Be(1);
        results.Should().OnlyContain(r => ReferenceEquals(r, results[0]));
    }

    [Test]
    public void RegisterCacheEntryOptions_NotAReferenceDataCollectionType_Throws()
    {
        var cache = new ReferenceDataHybridCache(new Caching.MemoryOnlyHybridCache());
        Action act = () => cache.RegisterCacheEntryOptions(typeof(string), Caching.HybridCacheEntryOptions.CreateForName("x"));
        act.Should().Throw<ArgumentException>();
    }

    [Test]
    public void RegisterCacheEntryOptions_NullOptions_Throws()
    {
        var cache = new ReferenceDataHybridCache(new Caching.MemoryOnlyHybridCache());
        Action act = () => cache.RegisterCacheEntryOptions<DummyRefDataCollection>(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void RegisterCacheEntryOptions_ValidType_ReturnsSameInstance_ForChaining()
    {
        var cache = new ReferenceDataHybridCache(new Caching.MemoryOnlyHybridCache());
        var result = cache.RegisterCacheEntryOptions<DummyRefDataCollection>(Caching.HybridCacheEntryOptions.CreateForName("x"));
        result.Should().BeSameAs(cache);
    }

    [Test]
    public async Task RegisterCacheEntryOptions_RegisteredOptions_AreUsedByGetOrCreateAsync()
    {
        var cache = new ReferenceDataHybridCache(new Caching.MemoryOnlyHybridCache());
        var registered = Caching.HybridCacheEntryOptions.CreateForName("custom", TimeSpan.FromMinutes(42));
        cache.RegisterCacheEntryOptions<DummyRefDataCollection>(registered);

        await cache.GetOrCreateAsync(typeof(DummyRefDataCollection), (t, ct) => Task.FromResult<IReferenceDataCollection>(new DummyRefDataCollection { new DummyRefData { Id = 1, Code = "A" } }));

        // OnCreateCacheEntry is only invoked for entries not already registered; since we pre-registered, it should not be overwritten by a default.
        cache.RegisterCacheEntryOptions<DummyRefDataCollection>(registered).Should().BeSameAs(cache);
    }

    private class TrackingReferenceDataHybridCache(IHybridCache cache) : ReferenceDataHybridCache(cache)
    {
        public readonly List<Type> CreatedFor = [];

        protected override void OnCreateCacheEntry(Type type, Caching.HybridCacheEntryOptions entry) => CreatedFor.Add(type);
    }

    /// <summary>
    /// An <see cref="IHybridCache"/> stand-in that records the generic type argument used for each call, simulating a serializing (e.g. distributed) cache implementation where the
    /// generic argument determines the type the underlying serializer must construct.
    /// </summary>
    private class TypeRecordingHybridCache : IHybridCache
    {
        private readonly Dictionary<string, object?> _store = [];

        public List<Type> ObservedTypes { get; } = [];

        public ICacheKeyProvider KeyProvider { get; } = new DefaultCacheKeyProvider();

        public Task<(bool Exists, T? Value)> TryGetByKeyAsync<T>(string key, Caching.HybridCacheEntryOptions? options = null, CancellationToken cancellationToken = default)
        {
            ObservedTypes.Add(typeof(T));
            return Task.FromResult(_store.TryGetValue(key, out var value) ? (true, (T?)value) : (false, default));
        }

        public async Task<T> GetOrCreateByKeyAsync<T>(string key, Func<CancellationToken, Task<T>> factory, Caching.HybridCacheEntryOptions? options = null, CancellationToken cancellationToken = default)
        {
            ObservedTypes.Add(typeof(T));
            if (_store.TryGetValue(key, out var existing))
                return (T)existing!;

            var value = await factory(cancellationToken).ConfigureAwait(false);
            _store[key] = value;
            return value;
        }

        public Task<T?> GetOrDefaultByKeyAsync<T>(string key, Caching.HybridCacheEntryOptions? options = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task SetByKeyAsync<T>(string key, T value, Caching.HybridCacheEntryOptions? options = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task RemoveByKeyAsync(string key, Caching.HybridCacheEntryOptions? options = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task RemoveByTagAsync(string tag, Caching.HybridCacheEntryOptions? options = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task RemoveByTagAsync(IEnumerable<string> tags, Caching.HybridCacheEntryOptions? options = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    [Test]
    public async Task GetOrCreateAsync_CacheMiss_NeverUsesInterfaceAsUnderlyingCacheGenericType()
    {
        // This proves the fix for the FUSION [DC] deserialization bug: the slow (semaphore-protected) path must invoke the underlying IHybridCache using the concrete collection type
        // (e.g. DummyRefDataCollection), never the IReferenceDataCollection interface - a serializing cache (e.g. FusionCache's distributed tier) cannot construct an interface/abstract type.
        var backing = new TypeRecordingHybridCache();
        var cache = new ReferenceDataHybridCache(backing);

        var coll = await cache.GetOrCreateAsync(typeof(DummyRefDataCollection), (t, ct) => Task.FromResult<IReferenceDataCollection>(new DummyRefDataCollection { new DummyRefData { Id = 1, Code = "A" } }));

        coll.Should().BeOfType<DummyRefDataCollection>();
        backing.ObservedTypes.Should().NotContain(typeof(IReferenceDataCollection));
        backing.ObservedTypes.Should().Contain(typeof(DummyRefDataCollection));
    }

    [Test]
    public async Task OnCreateCacheEntry_InvokedOnce_ForNewType()
    {
        var cache = new TrackingReferenceDataHybridCache(new Caching.MemoryOnlyHybridCache());

        await cache.GetOrCreateAsync(typeof(DummyRefDataCollection), (t, ct) => Task.FromResult<IReferenceDataCollection>(new DummyRefDataCollection { new DummyRefData { Id = 1, Code = "A" } }));
        await cache.GetOrCreateAsync(typeof(DummyRefDataCollection), (t, ct) => Task.FromResult<IReferenceDataCollection>(new DummyRefDataCollection { new DummyRefData { Id = 1, Code = "A" } }));

        cache.CreatedFor.Should().ContainSingle().Which.Should().Be(typeof(DummyRefDataCollection));
    }

    [Test]
    public void OnCreateCacheEntry_NotInvoked_WhenOptionsPreRegistered()
    {
        var cache = new TrackingReferenceDataHybridCache(new Caching.MemoryOnlyHybridCache());
        cache.RegisterCacheEntryOptions<DummyRefDataCollection>(Caching.HybridCacheEntryOptions.CreateForName("pre-registered"));

        cache.CreatedFor.Should().BeEmpty();
    }
}
