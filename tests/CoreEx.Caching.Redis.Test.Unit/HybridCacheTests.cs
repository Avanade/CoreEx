using CoreEx.Caching.FusionCache;
using CoreEx.Entities;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using ZiggyCreatures.Caching.Fusion;

namespace CoreEx.Caching.Redis.Test.Unit;

public class HybridCacheTests : WithGenericTester<EntryPoint>
{
    [Test]
    public void CacheStrategy_Hybrid()
    {
        Test.ScopedType<IServiceProvider>(async test =>
        {
            var key = "hybrid-key";
            var val = "hybrid-value";

            // Remove before we start.
            await ClearKeyAsync(test.Services, key);

            // Prepare usage.
            var cache = test.Services.GetRequiredService<IHybridCache>();
            var options = new HybridCacheEntryOptions { Strategy = CacheStrategy.Hybrid };

            // Create on first access.
            var value = await cache.GetOrCreateByKeyAsync(key, async ct =>
            {
                await Task.Delay(1, ct).ConfigureAwait(false);
                return val;
            }, options);

            value.Should().NotBeNull().And.Be(val);

            // Should be created already.
            value = await cache.GetOrCreateByKeyAsync<string>(key, _ => throw new InvalidOperationException("Should be cached?!"), options);
            value.Should().NotBeNull().And.Be(val);

            // Check strategy adhered to.
            IsInDistributedCache(test.Services, key).Should().BeTrue();
            IsInMemoryCache(test.Services, key).Should().BeTrue();
        });
    }

    [Test]
    public void CacheStrategy_Local()
    {
        Test.ScopedType<IServiceProvider>(async test =>
        {
            var key = "local-key";
            var val = "local-value";

            // Remove before we start.
            await ClearKeyAsync(test.Services, key);

            // Prepare usage.
            var cache = test.Services.GetRequiredService<IHybridCache>();
            var options = new HybridCacheEntryOptions { Strategy = CacheStrategy.Local };

            // Create on first access.
            var value = await cache.GetOrCreateByKeyAsync(key, async ct =>
            {
                await Task.Delay(1, ct).ConfigureAwait(false);
                return val;
            }, options);

            value.Should().NotBeNull().And.Be(val);

            // Should be created already.
            value = await cache.GetOrCreateByKeyAsync<string>(key, _ => throw new InvalidOperationException("Should be cached?!"), options);
            value.Should().NotBeNull().And.Be(val);

            // Check strategy adhered to.
            IsInDistributedCache(test.Services, key).Should().BeFalse();
            IsInMemoryCache(test.Services, key).Should().BeTrue();
        });
    }

    [Test]
    public void CacheStrategy_Distributed()
    {
        Test.ScopedType<IServiceProvider>(async test =>
        {
            var key = "distributed-key";
            var val = "distributed-value";

            // Remove before we start.
            await ClearKeyAsync(test.Services, key);

            // Prepare usage.
            var cache = test.Services.GetRequiredService<IHybridCache>();
            var options = new HybridCacheEntryOptions { Strategy = CacheStrategy.Distributed };

            // Create on first access.
            var value = await cache.GetOrCreateByKeyAsync(key, async ct =>
            {
                await Task.Delay(1, ct).ConfigureAwait(false);
                return val;
            }, options);

            value.Should().NotBeNull().And.Be(val);

            // Should be created already.
            value = await cache.GetOrCreateByKeyAsync<string>(key, _ => throw new InvalidOperationException("Should be cached?!"), options);
            value.Should().NotBeNull().And.Be(val);

            // Check strategy adhered to.
            IsInDistributedCache(test.Services, key).Should().BeTrue();
            IsInMemoryCache(test.Services, key).Should().BeFalse();
        });
    }

    [Test]
    public async Task Backplane_Synchronization()
    {
        var secondary = UnitTestEx.GenericTester.Create<EntryPoint>();
        var key = "backplane-key";
        var val = "backplane-value";
        var val2 = "backplane-value2";

        // Remove before we start.
        Test.ScopedType<IServiceProvider>(async test =>
        {
            await ClearKeyAsync(test.Services, key);
        });

        // Create on secondary first.
        secondary.ScopedType<IServiceProvider>(async test =>
        {
            var cache = test.Services.GetRequiredService<IHybridCache>();
            var value = await cache.GetOrCreateByKeyAsync(key, _ => Task.FromResult(val));
            value.Should().NotBeNull().And.Be(val);

            IsInDistributedCache(test.Services, key).Should().BeTrue();
            IsInMemoryCache(test.Services, key).Should().BeTrue();
        });

        // Now check on primary.
        Test.ScopedType<IServiceProvider>(async test =>
        {
            IsInMemoryCache(test.Services, key).Should().BeFalse();

            var cache = test.Services.GetRequiredService<IHybridCache>();
            var value = await cache.GetOrCreateByKeyAsync<string>(key, _ => throw new InvalidOperationException("Should be cached?!"));
            value.Should().NotBeNull().And.Be(val);

            IsInDistributedCache(test.Services, key).Should().BeTrue();
            IsInMemoryCache(test.Services, key).Should().BeTrue();

            // Change the value and confirm.
            await cache.SetByKeyAsync(key, val2);
            value = await cache.GetOrCreateByKeyAsync<string>(key, _ => throw new InvalidOperationException("Should be cached?!"));
            value.Should().NotBeNull().And.Be(val2);
        });

        // Allow backplane to do its thing!
        await Task.Delay(1000);

        // Back to secondary to check on change.
        secondary.ScopedType<IServiceProvider>(async test =>
        {
            var cache = test.Services.GetRequiredService<IHybridCache>();
            var value = await cache.GetOrCreateByKeyAsync<string>(key, _ => throw new InvalidOperationException("Should be cached?!"));
            value.Should().NotBeNull().And.Be(val2);

            IsInDistributedCache(test.Services, key).Should().BeTrue();
            IsInMemoryCache(test.Services, key).Should().BeTrue();
        });

        // Verifying separate instances!
        var pfc = Test.Services.GetRequiredService<IFusionCache>();
        var sfc = secondary.Services.GetRequiredService<IFusionCache>();
        pfc.Should().NotBeSameAs(sfc);
    }

    [Test]
    public async Task Cache_ByKey()
    {
        var key = "bykey-key";

        Test.ScopedType<IServiceProvider>(async test =>
        {
            var cache = test.Services.GetRequiredService<IHybridCache>();

            await cache.RemoveByKeyAsync(key);  // Removes key:any.

            var p = await cache.GetOrDefaultByKeyAsync<Person>(key);
            p.Should().BeNull();

            await cache.RemoveByKeyAsync(key);  // Removes key:null.

            p = await cache.GetOrCreateByKeyAsync(key, _ => Task.FromResult(new Person { Id = "123", Name = "Bob", Age = 33 }));
            p.Should().NotBeNull();
            p.Name.Should().Be("Bob");

            var p2 = await cache.GetOrDefaultByKeyAsync<Person>(key);
            p2.Should().NotBeNull().And.BeEquivalentTo(p);
        });
    }

    [Test]
    public async Task Cache_ByCompositeKey()
    {
        var key = "composite-key";
        CompositeKey ckey = key;

        Test.ScopedType<IServiceProvider>(async test =>
        {
            var cache = test.Services.GetRequiredService<IHybridCache>();

            await cache.RemoveAsync<Person>(ckey);  // Removes key:any.

            var p = await cache.GetOrDefaultAsync<Person>(ckey);
            p.Should().BeNull();

            await cache.RemoveAsync<Person>(ckey);  // Removes key:null.

            p = await cache.GetOrCreateAsync(ckey, _ => Task.FromResult(new Person { Id = key, Name = "Bob", Age = 33 }));
            p.Should().NotBeNull();
            p.Name.Should().Be("Bob");

            var p2 = await cache.GetOrDefaultAsync<Person>(ckey);
            p2.Should().NotBeNull().And.BeEquivalentTo(p);
        });
    }

    [Test]
    public void RemoveByTagAsync_QualifiesTag_DoesNotCrossTenantInvalidate() => Test.ScopedType<IServiceProvider>(async test =>
    {
        // Regression: tags must be qualified via ICacheKeyProvider the same way keys are, otherwise two tenants/domains sharing the same
        // underlying IFusionCache and using the same tag name would invalidate each other's entries.
        var fusionCache = test.Services.GetRequiredService<IFusionCache>();

        var cacheA = new FusionHybridCache(fusionCache, new PrefixCacheKeyProvider("tenantA"));
        var cacheB = new FusionHybridCache(fusionCache, new PrefixCacheKeyProvider("tenantB"));

        const string key = "tag-qualification-key";
        const string tag = "shared-tag";

        await cacheA.RemoveByKeyAsync(key);
        await cacheB.RemoveByKeyAsync(key);

        await cacheA.SetByKeyAsync(key, "valueA", new HybridCacheEntryOptions().WithTags(tag));
        await cacheB.SetByKeyAsync(key, "valueB", new HybridCacheEntryOptions().WithTags(tag));

        // Tenant A invalidates its own "shared-tag" entries; tenant B's same-named tag must be unaffected.
        await cacheA.RemoveByTagAsync(tag);

        var (existsA, _) = await cacheA.TryGetByKeyAsync<string>(key);
        var (existsB, valueB) = await cacheB.TryGetByKeyAsync<string>(key);

        existsA.Should().BeFalse();
        existsB.Should().BeTrue();
        valueB.Should().Be("valueB");
    });

    [Test]
    public void ConfigureEntryOptions_IsPersistent_AndSeesSourceOptions() => Test.ScopedType<IServiceProvider>(async test =>
    {
        // ConfigureEntryOptions is a durable (not one-shot) override - it applies to every subsequent operation on this instance until
        // changed/cleared - and is passed the source HybridCacheEntryOptions (e.g. Tags) so tag/strategy-aware policies are possible
        // (e.g. shortening the duration only for entries carrying a specific tag).
        var cache = (FusionHybridCache)test.Services.GetRequiredService<IHybridCache>();

        var seenTags = new List<string[]?>();
        cache.ConfigureEntryOptions((hco, fco) =>
        {
            seenTags.Add(hco.Tags);
            if (hco.Tags?.Contains("short-lived") == true)
                fco.Duration = TimeSpan.FromMilliseconds(1);
        });

        await cache.GetOrCreateByKeyAsync("persistent-configure-key-1", _ => Task.FromResult("value1"), new HybridCacheEntryOptions().WithTags("short-lived"));
        await cache.GetOrCreateByKeyAsync("persistent-configure-key-2", _ => Task.FromResult("value2"));

        // Persistent: the override fired for both calls, not just the first.
        seenTags.Should().HaveCount(2);
        seenTags[0].Should().BeEquivalentTo(["short-lived"]);
        seenTags[1].Should().BeNull();
    });

    [Test]
    public void AddFusionHybridCache_ConfigureCallback_IsInvokedOnConstructedInstance() => Test.ScopedType<IServiceProvider>(async test =>
    {
        // The AddFusionHybridCache configure callback gives DI registration-time access to the constructed FusionHybridCache
        // (e.g. to call ConfigureEntryOptions there) without needing to resolve and cast IHybridCache elsewhere.
        var services = new ServiceCollection();
        services.AddSingleton(test.Services.GetRequiredService<IFusionCache>());
        services.AddSingleton(test.Services.GetRequiredService<ICacheKeyProvider>());

        FusionHybridCache? configuredInstance = null;
        services.AddFusionHybridCache((_, fhc) => configuredInstance = fhc);

        await using var sp = services.BuildServiceProvider();
        var cache = sp.GetRequiredService<IHybridCache>();

        configuredInstance.Should().NotBeNull().And.BeSameAs(cache);
    });

    private sealed class PrefixCacheKeyProvider(string prefix) : ICacheKeyProvider
    {
        public string GetFullyQualifiedCacheKey(string key) => $"{prefix}:{key}";

        public string GetEntityCacheKey<T>(CompositeKey key) where T : IEntityKey => $"{typeof(T).Name}:{key}";
    }

    internal static async Task ClearKeyAsync(IServiceProvider sp, string key)
    {
        var cache = sp.GetRequiredService<IHybridCache>();
        await cache.RemoveByKeyAsync(key);

        IsInDistributedCache(sp, key).Should().BeFalse();
        IsInMemoryCache(sp, key).Should().BeFalse();
    }

    private static bool IsInDistributedCache(IServiceProvider sp, string key)
    {
        var dc = sp.GetRequiredService<IDistributedCache>();
        var ckp = sp.GetRequiredService<ICacheKeyProvider>();

        return dc.Get("v2:" + ckp.GetFullyQualifiedCacheKey(key)) is not null; // FusionCache prefixes by major version.
    }

    private static bool IsInMemoryCache(IServiceProvider sp, string key)
    {
        var mc = sp.GetRequiredService<IMemoryCache>();
        var ckp = sp.GetRequiredService<ICacheKeyProvider>();
        return mc.TryGetValue(ckp.GetFullyQualifiedCacheKey(key), out var _);
    }

    public record Person : IIdentifier<string?>
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public int Age { get; set; }
    }
}