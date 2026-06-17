#pragma warning disable IDE0130 // Namespace does not match folder structure; by design.
namespace Microsoft.Extensions.DependencyInjection;
#pragma warning restore IDE0130 // Namespace does not match folder structure

/// <summary>
/// Provides standard extensions.
/// </summary>
public static partial class CoreExFusionCacheExtensions
{
    /// <summary>
    /// Adds a <b>scoped</b> service for the <see cref="IHybridCache"/> using the <see cref="CoreEx.Caching.FusionCache.FusionHybridCache"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/> for fluent-style method-chaining.</returns>
    public static IServiceCollection AddFusionHybridCache(this IServiceCollection services) => services.ThrowIfNull().AddScoped<IHybridCache, CoreEx.Caching.FusionCache.FusionHybridCache>();
}