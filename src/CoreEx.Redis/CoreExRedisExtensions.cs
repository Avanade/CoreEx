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

    /// <summary>
    /// Adds the singleton <see cref="CoreEx.Redis.RedisWorkProvider"/> as the <see cref="CoreEx.Hosting.Work.IWorkProvider"/> over the host-owned connection, and a scoped <see cref="CoreEx.Hosting.Work.WorkOrchestrator"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="keyPrefix">The key prefix enabling separation within a shared Redis; defaults to <c>CoreEx:work:v1:</c>.</param>
    /// <param name="maxDataSize">The maximum result data size in bytes. Defaults to 512 KiB; use <see langword="null"/> for no configured limit.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddRedisWorkProvider(this IServiceCollection services, string? keyPrefix = null, int? maxDataSize = 512 * 1024)
    {
        if (maxDataSize is <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxDataSize));

        services.TryAddSingleton<CoreEx.Hosting.Work.IWorkProvider>(sp => new CoreEx.Redis.RedisWorkProvider(sp.GetRequiredService<IConnectionMultiplexer>(), keyPrefix: keyPrefix, maxDataSize: maxDataSize));
        services.TryAddScoped(sp => new CoreEx.Hosting.Work.WorkOrchestrator(sp.GetRequiredService<CoreEx.Hosting.Work.IWorkProvider>()));
        return services;
    }
}
