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