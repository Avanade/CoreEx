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
    /// <param name="configure">An optional action to configure the <see cref="CoreEx.Caching.FusionCache.FusionHybridCache"/> instance (e.g. to set up its <c>ConfigureEntryOptions</c> escape hatch).</param>
    /// <returns>The <see cref="IServiceCollection"/> for fluent-style method-chaining.</returns>
    public static IServiceCollection AddFusionHybridCache(this IServiceCollection services, Action<IServiceProvider, CoreEx.Caching.FusionCache.FusionHybridCache>? configure = null)
        => services.ThrowIfNull().AddScoped<IHybridCache>(sp =>
        {
            var fhc = ActivatorUtilities.CreateInstance<CoreEx.Caching.FusionCache.FusionHybridCache>(sp);
            configure?.Invoke(sp, fhc);
            return fhc;
        });
}