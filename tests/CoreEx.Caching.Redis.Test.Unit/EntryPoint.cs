using CoreEx.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Backplane.StackExchangeRedis;

namespace CoreEx.Caching.Redis.Test.Unit;

public class EntryPoint
{
    public static void ConfigureApplication(IHostApplicationBuilder builder)
    {
        // Add CoreEx host settings.
        builder.AddHostSettings("CoreEx.Caching", "UnitTest");

        // Add CoreEx services.
        builder.Services
            .AddExecutionContext();

        // Add core caching services.
        builder.Services.AddMemoryCache();              // Add in-memory cache - L1.
        builder.AddRedisDistributedCache("redis");      // Add Redis as the distributed cache (using Aspire library) - L2.

        builder.Services.AddFusionCache()               // Add and wire-up FusionCache including backplane.
            .WithRegisteredMemoryCache()
            .WithRegisteredDistributedCache()
            .WithBackplane(sp => new RedisBackplane(new RedisBackplaneOptions { Configuration = sp.GetRequiredService<IOptions<ConfigurationOptions>>().Value.ToString() }))
            .WithSystemTextJsonSerializer(JsonDefaults.SerializerOptions)
            .WithOptions(new FusionCacheOptions { EnableSyncEventHandlersExecution = true });    // ** NOTE: Do NOT use; this is to enable backplane unit tests only! **

        // Add CoreEx caching services.
        builder.Services.AddFusionHybridCache();        // Adds the scoped CoreEx.Caching.IHybridCache for FusionCache.
        builder.Services.AddDefaultCacheKeyProvider();  // Adds the default CoreEx.Caching.ICacheKeyProvider.

        // Add the HybridCacheSynchronizer to test also.
        builder.Services.AddHybridCacheSynchronizer();
    }
}