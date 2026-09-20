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
    /// <c>builder.AddAzureCosmosClient("Cosmos")</c> called on the host <c>builder</c>, which also provides connection-string resolution and telemetry - <b>but not a health check</b>, unlike its
    /// Npgsql/SqlClient counterparts; see <see cref="AddCosmosDbHealthCheck(IServiceCollection, string)"/>).</remarks>
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
    /// <c>builder.AddAzureCosmosClient("Cosmos")</c> called on the host <c>builder</c>, which also provides connection-string resolution and telemetry - <b>but not a health check</b>, unlike its
    /// Npgsql/SqlClient counterparts; see <see cref="AddCosmosDbHealthCheck(IServiceCollection, string)"/>). No custom health check is registered by this method itself either, for the same reason
    /// (it needs no <typeparamref name="TCosmosDb"/>-specific behavior).</remarks>
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

    /// <summary>
    /// Adds a <b>scoped</b> <see cref="CosmosDbUnitOfWork"/> service.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="addAsIUnitOfWork">Indicates whether to also register as the <see cref="IUnitOfWork"/> service.</param>
    /// <returns>The <see cref="IServiceCollection"/> for fluent-style method-chaining.</returns>
    /// <remarks>Mirrors <c>AddPostgresUnitOfWork</c>/<c>AddSqlServerUnitOfWork</c>'s multi-register shape (concrete type plus, optionally, the <see cref="IUnitOfWork"/> abstraction) - but unlike those,
    /// takes no generic <c>TCosmosDb</c>/<c>TDatabase</c> type parameter: <see cref="CosmosDbUnitOfWork"/>'s constructor already depends on the tech-agnostic <see cref="ICosmosDb"/> interface (not a
    /// concrete <see cref="CosmosDb"/>-derived type), and <see cref="AddCosmosDb(IServiceCollection, string, Action{IServiceProvider, CosmosDb}?)"/>/<see cref="AddCosmosDb{TCosmosDb}(IServiceCollection, string, Action{IServiceProvider, TCosmosDb}?)"/>
    /// already register whichever concrete type was used as <see cref="ICosmosDb"/>, so resolving that directly here is sufficient.
    /// <para>The optional <see cref="CosmosDbUnitOfWork"/> <c>outbox</c> constructor parameter is left to <see cref="ActivatorUtilities.CreateInstance{T}(IServiceProvider, object[])"/> to auto-resolve -
    /// where an <see cref="IEventPublisher"/> is registered (e.g. via <see cref="AddCosmosDbEventPublisher(IServiceCollection, Action{IServiceProvider, CosmosDbEventPublisher}?, bool, string)"/>), it is
    /// wired in automatically; otherwise the unit-of-work simply has no outbox support (<see cref="IUnitOfWork.AreEventsSupported"/> is <see langword="false"/>).</para></remarks>
    public static IServiceCollection AddCosmosDbUnitOfWork(this IServiceCollection services, bool addAsIUnitOfWork = true)
    {
        services.ThrowIfNull().AddScoped(sp =>
        {
            var cosmosDb = sp.GetRequiredService<ICosmosDb>();
            return ActivatorUtilities.CreateInstance<CosmosDbUnitOfWork>(sp, cosmosDb);
        });

        if (addAsIUnitOfWork)
            services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<CosmosDbUnitOfWork>());

        return services;
    }

    /// <summary>
    /// Adds a <see cref="CosmosDbHealthCheck"/> for the registered <see cref="ICosmosDb"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="name">The health check name.</param>
    /// <returns>The <see cref="IServiceCollection"/> for fluent-style method-chaining.</returns>
    /// <remarks>Aspire's own Cosmos DB client integration (<c>Aspire.Microsoft.Azure.Cosmos</c>) does not register a health check of its own, unlike its Npgsql/SqlClient counterparts - this fills that
    /// gap; see <see cref="CosmosDbHealthCheck"/>. Registered against <c>HealthCheckTags.StartUpAndReadyOnly</c> (matching <c>AddHostedService</c>'s own health-check registration convention), not
    /// <see cref="HealthCheckTags.Live"/> - liveness should reflect whether the process itself is running, not the reachability of a downstream dependency.</remarks>
    public static IServiceCollection AddCosmosDbHealthCheck(this IServiceCollection services, string name = "cosmos-database")
    {
        services.ThrowIfNull().AddHealthChecks().AddCheck<CosmosDbHealthCheck>(name.ThrowIfNullOrEmpty(), tags: HealthCheckTags.StartUpAndReadyOnly);
        return services;
    }
}
