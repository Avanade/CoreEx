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
