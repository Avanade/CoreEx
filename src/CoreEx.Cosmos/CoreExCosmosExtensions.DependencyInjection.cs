#pragma warning disable IDE0130 // Namespace does not match folder structure - this is by design.
namespace Microsoft.Extensions.DependencyInjection;
#pragma warning restore IDE0130 // Namespace does not match folder structure

/// <summary>
/// Provides <see cref="CosmosDb"/> and related extensions.
/// </summary>
public static partial class CoreExCosmosExtensions
{
    /// <summary>
    /// Adds a <b>scoped</b> <see cref="CosmosDb"/> service.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="databaseId">The <see cref="Microsoft.Azure.Cosmos.Database"/> identifier.</param>
    /// <param name="configure">An optional action to configure the database instance.</param>
    /// <returns>The <see cref="IServiceCollection"/> for fluent-style method-chaining.</returns>
    /// <remarks>The underlying <see cref="CosmosClient"/> is <b>not</b> registered by this method; it is expected to already be registered in the <see cref="IServiceCollection"/> (typically via Aspire's
    /// <c>builder.AddAzureCosmosClient("Cosmos")</c> called on the host <c>builder</c>, which also provides connection-string resolution, health checks and telemetry).</remarks>
    public static IServiceCollection AddCosmosDb(this IServiceCollection services, string databaseId, Action<IServiceProvider, CosmosDb>? configure = null)
        => AddCosmosDb<CosmosDb>(services, databaseId, configure);

    /// <summary>
    /// Adds a <b>scoped</b> <see cref="CosmosDb"/> <typeparamref name="TCosmosDb"/> service.
    /// </summary>
    /// <typeparam name="TCosmosDb">The <see cref="ICosmosDb"/> <see cref="Type"/>.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="databaseId">The <see cref="Microsoft.Azure.Cosmos.Database"/> identifier.</param>
    /// <param name="configure">An optional action to configure the database instance.</param>
    /// <returns>The <see cref="IServiceCollection"/> for fluent-style method-chaining.</returns>
    /// <remarks>The underlying <see cref="CosmosClient"/> is <b>not</b> registered by this method; it is expected to already be registered in the <see cref="IServiceCollection"/> (typically via Aspire's
    /// <c>builder.AddAzureCosmosClient("Cosmos")</c> called on the host <c>builder</c>, which also provides connection-string resolution, health checks and telemetry). No custom health check is registered
    /// here either, for the same reason.</remarks>
    public static IServiceCollection AddCosmosDb<TCosmosDb>(this IServiceCollection services, string databaseId, Action<IServiceProvider, TCosmosDb>? configure = null) where TCosmosDb : class, ICosmosDb
    {
        databaseId.ThrowIfNull();

        return services.ThrowIfNull().AddScoped<TCosmosDb>(sp =>
        {
            var db = ActivatorUtilities.CreateInstance<TCosmosDb>(sp, databaseId);
            configure?.Invoke(sp, db);
            return db;
        }).AddScoped<ICosmosDb>(sp => sp.GetRequiredService<TCosmosDb>());
    }
}
