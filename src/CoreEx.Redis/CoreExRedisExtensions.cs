#pragma warning disable IDE0130 // Namespace does not match folder structure; by design.
namespace Microsoft.Extensions.DependencyInjection;
#pragma warning restore IDE0130 // Namespace does not match folder structure

/// <summary>
/// Registers Redis coordination independently of ordinary caching.
/// </summary>
public static class CoreExRedisExtensions
{
    /// <summary>
    /// Adds scoped Redis providers over the host-owned connection and existing key qualifier.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="options">The optional ownership options.</param>
    /// <param name="maxIdempotencyPayloadSize">The maximum serialized terminal response payload size in bytes. Defaults to 1 MiB; use <see langword="null"/> for no configured limit.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddRedisCoordination(this IServiceCollection services, CoordinationOptions? options = null, int? maxIdempotencyPayloadSize = 1024 * 1024)
    {
        options ??= new();
        options.Validate();
        if (maxIdempotencyPayloadSize is <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxIdempotencyPayloadSize));

        services.TryAddScoped<ICacheKeyProvider, DefaultCacheKeyProvider>();
        services.AddScoped(sp => new CoreEx.Redis.RedisCoordination(sp.GetRequiredService<IConnectionMultiplexer>(), sp.GetRequiredService<ICacheKeyProvider>(), sp.GetRequiredService<ILogger<CoreEx.Redis.RedisCoordination>>(), options, maxIdempotencyPayloadSize));
        services.AddScoped<ISynchronizer>(sp => sp.GetRequiredService<CoreEx.Redis.RedisCoordination>());
        services.AddScoped<IIdempotencyStore>(sp => sp.GetRequiredService<CoreEx.Redis.RedisCoordination>());
        return services;
    }
}
