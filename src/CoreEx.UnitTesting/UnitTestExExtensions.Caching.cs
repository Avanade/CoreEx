#pragma warning disable IDE0130 // Namespace does not match folder structure; by design.
namespace UnitTestEx;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public static partial class UnitTestExExtensions
{
    /// <summary>
    /// Clears the underlying L1/L2 cache by executing the <see cref="ZiggyCreatures.Caching.Fusion.IFusionCache.ClearAsync(bool, ZiggyCreatures.Caching.Fusion.FusionCacheEntryOptions?, CancellationToken)"/>.
    /// </summary>
    /// <param name="tester">The <see cref="TesterBase"/>.</param>
    /// <remarks>The <see cref="ZiggyCreatures.Caching.Fusion.IFusionCache"/> service must be registered within the underlying test host.</remarks>
    public static async Task ClearFusionCacheAsync(this TesterBase tester) => await tester.ThrowIfNull().Services.GetRequiredService<ZiggyCreatures.Caching.Fusion.IFusionCache>().ClearAsync(false);
}