namespace CoreEx.Caching.FusionCache;

/// <summary>
/// Provides standard extensions for <i>FusionCache</i>.
/// </summary>
public static partial class FusionCacheExtensions
{
    /// <summary>
    /// Converts the <see cref="HybridCacheEntryOptions"/> into a <see cref="FusionCacheEntryOptions"/> equivalent.
    /// </summary>
    /// <param name="options">The <see cref="HybridCacheEntryOptions"/>.</param>
    /// <returns>The resulting <see cref="FusionCacheEntryOptions"/>.</returns>
    public static FusionCacheEntryOptions ToFusionCacheEntryOptions(this HybridCacheEntryOptions? options) => new()
    {
        Duration = options?.LocalExpiration ?? HybridCacheEntryOptions.DefaultLocalExpiration,
        DistributedCacheDuration = options?.DistributedExpiration ?? HybridCacheEntryOptions.DefaultDistributedExpiration,
        SkipDistributedCacheRead = (options?.Strategy ?? HybridCacheEntryOptions.DefaultStrategy) == CacheStrategy.Local,
        SkipDistributedCacheWrite = (options?.Strategy ?? HybridCacheEntryOptions.DefaultStrategy) == CacheStrategy.Local,
        SkipMemoryCacheRead = (options?.Strategy ?? HybridCacheEntryOptions.DefaultStrategy) == CacheStrategy.Distributed,
        SkipMemoryCacheWrite = (options?.Strategy ?? HybridCacheEntryOptions.DefaultStrategy) == CacheStrategy.Distributed
    };
}