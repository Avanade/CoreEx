#pragma warning disable IDE0130 // Namespace does not match folder structure - this is by design.
namespace Microsoft.Extensions.DependencyInjection;
#pragma warning restore IDE0130 // Namespace does not match folder structure

/// <summary>
/// Provides <see cref="PostgresDatabase"/> and related extensions.
/// </summary>
public static partial class CoreExPostgresExtensions
{
    /// <summary>
    /// Adds a <b>scoped</b> <see cref="PostgresDatabase"/> service.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="configure">An optional action to configure the database instance.</param>
    /// <returns>The <see cref="IServiceCollection"/> for fluent-style method-chaining.</returns>
    public static IServiceCollection AddPostgresDatabase(this IServiceCollection services, Action<IServiceProvider, PostgresDatabase>? configure = null)
        => AddPostgresDatabase<PostgresDatabase>(services, configure);

    /// <summary>
    /// Adds a <b>scoped</b> <see cref="PostgresDatabase"/> <typeparamref name="TDatabase"/> service.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="configure">An optional action to configure the database instance.</param>
    /// <returns>The <see cref="IServiceCollection"/> for fluent-style method-chaining.</returns>
    public static IServiceCollection AddPostgresDatabase<TDatabase>(this IServiceCollection services, Action<IServiceProvider, TDatabase>? configure = null) where TDatabase : PostgresDatabase
    {
        return services.ThrowIfNull().AddScoped<TDatabase>(sp =>
        {
            var db = ActivatorUtilities.CreateInstance<TDatabase>(sp);
            configure?.Invoke(sp, db);
            return db;
        });
    }

    /// <summary>
    /// Adds a <b>scoped</b> <see cref="PostgresUnitOfWork"/> service for the <see cref="PostgresDatabase"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="addAsIUnitOfWork">Indicates whether to also register as the <see cref="IUnitOfWork"/> service.</param>
    /// <returns>The <see cref="IServiceCollection"/> for fluent-style method-chaining.</returns>
    public static IServiceCollection AddPostgresUnitOfWork(this IServiceCollection services, bool addAsIUnitOfWork = true)
        => AddPostgresUnitOfWork<PostgresDatabase>(services, addAsIUnitOfWork);

    /// <summary>
    /// Adds a <b>scoped</b> <see cref="PostgresUnitOfWork"/> service for the specified <see cref="PostgresDatabase"/> <typeparamref name="TDatabase"/>.
    /// </summary>
    /// <typeparam name="TDatabase">The <see cref="PostgresDatabase"/>.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="addAsIUnitOfWork">Indicates whether to also register as the <see cref="IUnitOfWork"/> service.</param>
    /// <returns>The <see cref="IServiceCollection"/> for fluent-style method-chaining.</returns>
    public static IServiceCollection AddPostgresUnitOfWork<TDatabase>(this IServiceCollection services, bool addAsIUnitOfWork = true) where TDatabase : PostgresDatabase
    {
        services.ThrowIfNull().AddScoped<PostgresUnitOfWork>(sp =>
        {
            var sql = sp.GetRequiredService<TDatabase>();
            return ActivatorUtilities.CreateInstance<PostgresUnitOfWork>(sp, sql);
        });

        if (addAsIUnitOfWork)
            services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<PostgresUnitOfWork>());

        return services;
    }

    /// <summary>
    /// Adds a keyed <b>scoped</b> <see cref="PostgresOutboxPublisher"/> service.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="configure">An optional action to configure the <see cref="PostgresOutboxPublisher"/> instance.</param>
    /// <param name="addAsDefaultIEventPublisher">Indicates whether to also register as the default (non-keyed) <see cref="IEventPublisher"/> service.</param>
    /// <param name="serviceKey">The service key to use for the keyed registration.</param>
    /// <returns>The <see cref="IServiceCollection"/> for fluent-style method-chaining.</returns>
    /// <remarks>See <see cref="Microsoft.Extensions.DependencyInjection.CoreExEventsExtensions.AddEventPublisher(IServiceCollection, string, Func{IServiceProvider, IEventPublisher}, bool)"/> for more information
    /// related to the underlying registration implementation.</remarks>
    public static IServiceCollection AddPostgresOutboxPublisher(this IServiceCollection services, Action<IServiceProvider, PostgresOutboxPublisher>? configure = null, bool addAsDefaultIEventPublisher = true, string serviceKey = PostgresOutboxPublisher.DefaultServiceKey)
        => services.AddPostgresOutboxPublisher<PostgresOutboxPublisher>(configure, addAsDefaultIEventPublisher, serviceKey);

    /// <summary>
    /// Adds a keyed <b>scoped</b> <typeparamref name="TOutbox"/> <see cref="PostgresOutboxPublisher"/> service.
    /// </summary>
    /// <typeparam name="TOutbox">The <see cref="PostgresOutboxPublisher"/> <see cref="Type"/>.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="configure">An optional action to configure the <typeparamref name="TOutbox"/> instance.</param>
    /// <param name="addAsDefaultIEventPublisher">Indicates whether to also register as the default (non-keyed) <see cref="IEventPublisher"/> service.</param>
    /// <param name="serviceKey">The service key to use for the keyed registration.</param>
    /// <returns>The <see cref="IServiceCollection"/> for fluent-style method-chaining.</returns>
    /// <remarks>See <see cref="Microsoft.Extensions.DependencyInjection.CoreExEventsExtensions.AddEventPublisher(IServiceCollection, string, Func{IServiceProvider, IEventPublisher}, bool)"/> for more information
    /// related to the underlying registration implementation.</remarks>
    public static IServiceCollection AddPostgresOutboxPublisher<TOutbox>(this IServiceCollection services, Action<IServiceProvider, TOutbox>? configure = null, bool addAsDefaultIEventPublisher = true, string serviceKey = PostgresOutboxPublisher.DefaultServiceKey) where TOutbox : PostgresOutboxPublisher
        => services.ThrowIfNull().AddEventPublisher(serviceKey, sp =>
        {
            var outbox = ActivatorUtilities.CreateInstance<TOutbox>(sp);
            configure?.Invoke(sp, outbox);
            return outbox;
        }, addAsDefaultIEventPublisher);

    /// <summary>
    /// Adds a <b>scoped</b> <see cref="PostgresOutboxRelay"/> service for the <see cref="PostgresDatabase"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="configure">An optional action to configure the <see cref="PostgresOutboxRelay"/> instance.</param>
    /// <returns>The <see cref="IServiceCollection"/> for fluent-style method-chaining.</returns>
    public static IServiceCollection AddPostgresOutboxRelay(this IServiceCollection services, Action<IServiceProvider, PostgresOutboxRelay>? configure = null)
        => services.AddPostgresOutboxRelay<PostgresOutboxRelay, PostgresDatabase>(configure);

    /// <summary>
    /// Adds a <b>scoped</b> <see cref="PostgresOutboxRelay"/> service for the <see cref="PostgresDatabase"/>.
    /// </summary>
    /// <typeparam name="TOutboxRelay">The <see cref="PostgresOutboxRelay"/> <see cref="Type"/>.</typeparam>
    /// <typeparam name="TDatabase">The <see cref="PostgresDatabase"/> <see cref="Type"/>.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="configure">An optional action to configure the <typeparamref name="TOutboxRelay"/> instance.</param>
    /// <returns>The <see cref="IServiceCollection"/> for fluent-style method-chaining.</returns>
    public static IServiceCollection AddPostgresOutboxRelay<TOutboxRelay, TDatabase>(this IServiceCollection services, Action<IServiceProvider, TOutboxRelay>? configure = null) where TOutboxRelay : PostgresOutboxRelay where TDatabase : PostgresDatabase
    {
        return services.ThrowIfNull().AddScoped<TOutboxRelay>(sp =>
        {
            var db = sp.GetRequiredService<TDatabase>();
            var relay = ActivatorUtilities.CreateInstance<TOutboxRelay>(sp, db);
            configure?.Invoke(sp, relay);
            return relay;
        });
    }
}