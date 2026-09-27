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

    /// <summary>
    /// Clears the underlying Redis cache by executing the <see cref="StackExchange.Redis.IServer.FlushDatabaseAsync"/> operation.
    /// </summary>
    /// <param name="tester">The <see cref="AspireTesterBase"/>.</param>
    /// <param name="aspireResourceName">The name of the Aspire resource to retrieve the connection string for.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public static async Task ClearRedisCacheAsync(this AspireTesterBase tester, string aspireResourceName)
    {
        var app = await tester.GetDistributedApplicationAsync();
        var cs = (await app.GetConnectionStringAsync(aspireResourceName.ThrowIfNullOrEmpty())) ?? throw new InvalidOperationException($"The '{aspireResourceName}' connection string not found.");

        var options = ConfigurationOptions.Parse(cs);
        options.AllowAdmin = true;

        await using var mux = await ConnectionMultiplexer.ConnectAsync(options);

        foreach (var ep in mux.GetEndPoints())
            await mux.GetServer(ep).FlushDatabaseAsync();
    }
}
