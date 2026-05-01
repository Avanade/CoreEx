#pragma warning disable IDE0130 // Namespace does not match folder structure - this is by design.
namespace Microsoft.Extensions.DependencyInjection;
#pragma warning restore IDE0130 // Namespace does not match folder structure

/// <summary>
/// Provides <see cref="SqlServerDatabase"/> and related extensions.
/// </summary>
public static partial class CoreExSqlServerExtensions
{
    /// <summary>
    /// Adds a <b>scoped</b> <see cref="SqlServerDatabase"/> service.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="configure">An optional action to configure the database instance.</param>
    /// <returns>The <see cref="IServiceCollection"/> for fluent-style method-chaining.</returns>
    public static IServiceCollection AddSqlServerDatabase(this IServiceCollection services, Action<IServiceProvider, SqlServerDatabase>? configure = null)
        => AddSqlServerDatabase<SqlServerDatabase>(services, configure);

    /// <summary>
    /// Adds a <b>scoped</b> <see cref="SqlServerDatabase"/> <typeparamref name="TDatabase"/> service.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="configure">An optional action to configure the database instance.</param>
    /// <returns>The <see cref="IServiceCollection"/> for fluent-style method-chaining.</returns>
    public static IServiceCollection AddSqlServerDatabase<TDatabase>(this IServiceCollection services, Action<IServiceProvider, TDatabase>? configure = null) where TDatabase : SqlServerDatabase
    {
        return services.ThrowIfNull().AddScoped<TDatabase>(sp =>
        {
            var db = ActivatorUtilities.CreateInstance<TDatabase>(sp);
            configure?.Invoke(sp, db);
            return db;
        });
    }

    /// <summary>
    /// Adds a <b>scoped</b> <see cref="SqlServerUnitOfWork"/> service for the <see cref="SqlServerDatabase"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="addAsIUnitOfWork">Indicates whether to also register as the <see cref="IUnitOfWork"/> service.</param>
    /// <returns>The <see cref="IServiceCollection"/> for fluent-style method-chaining.</returns>
    public static IServiceCollection AddSqlServerUnitOfWork(this IServiceCollection services, bool addAsIUnitOfWork = true)
        => AddSqlServerUnitOfWork<SqlServerDatabase>(services, addAsIUnitOfWork);

    /// <summary>
    /// Adds a <b>scoped</b> <see cref="SqlServerUnitOfWork"/> service for the specified <see cref="SqlServerDatabase"/> <typeparamref name="TDatabase"/>.
    /// </summary>
    /// <typeparam name="TDatabase">The <see cref="SqlServerDatabase"/>.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="addAsIUnitOfWork">Indicates whether to also register as the <see cref="IUnitOfWork"/> service.</param>
    /// <returns>The <see cref="IServiceCollection"/> for fluent-style method-chaining.</returns>
    public static IServiceCollection AddSqlServerUnitOfWork<TDatabase>(this IServiceCollection services, bool addAsIUnitOfWork = true) where TDatabase : SqlServerDatabase
    {
        services.ThrowIfNull().AddScoped<SqlServerUnitOfWork>(sp =>
        {
            var sql = sp.GetRequiredService<TDatabase>();
            return ActivatorUtilities.CreateInstance<SqlServerUnitOfWork>(sp, sql);
        });

        if (addAsIUnitOfWork)
            services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<SqlServerUnitOfWork>());

        return services;
    }

    /// <summary>
    /// Adds a keyed <b>scoped</b> <see cref="SqlServerOutboxPublisher"/> service.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="configure">An optional action to configure the <see cref="SqlServerOutboxPublisher"/> instance.</param>
    /// <param name="addAsDefaultIEventPublisher">Indicates whether to also register as the default (non-keyed) <see cref="IEventPublisher"/> service.</param>
    /// <param name="serviceKey">The service key to use for the keyed registration.</param>
    /// <returns>The <see cref="IServiceCollection"/> for fluent-style method-chaining.</returns>
    /// <remarks>See <see cref="Microsoft.Extensions.DependencyInjection.CoreExEventsExtensions.AddEventPublisher(IServiceCollection, string, Func{IServiceProvider, IEventPublisher}, bool)"/> for more information
    /// related to the underlying registration implementation.</remarks>
    public static IServiceCollection AddSqlServerOutboxPublisher(this IServiceCollection services, Action<IServiceProvider, SqlServerOutboxPublisher>? configure = null, bool addAsDefaultIEventPublisher = true, string serviceKey = SqlServerOutboxPublisher.DefaultServiceKey)
        => services.AddSqlServerOutboxPublisher<SqlServerOutboxPublisher>(configure, addAsDefaultIEventPublisher, serviceKey);

    /// <summary>
    /// Adds a keyed <b>scoped</b> <typeparamref name="TOutbox"/> <see cref="SqlServerOutboxPublisher"/> service.
    /// </summary>
    /// <typeparam name="TOutbox">The <see cref="SqlServerOutboxPublisher"/> <see cref="Type"/>.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="configure">An optional action to configure the <typeparamref name="TOutbox"/> instance.</param>
    /// <param name="addAsDefaultIEventPublisher">Indicates whether to also register as the default (non-keyed) <see cref="IEventPublisher"/> service.</param>
    /// <param name="serviceKey">The service key to use for the keyed registration.</param>
    /// <returns>The <see cref="IServiceCollection"/> for fluent-style method-chaining.</returns>
    /// <remarks>See <see cref="Microsoft.Extensions.DependencyInjection.CoreExEventsExtensions.AddEventPublisher(IServiceCollection, string, Func{IServiceProvider, IEventPublisher}, bool)"/> for more information
    /// related to the underlying registration implementation.</remarks>
    public static IServiceCollection AddSqlServerOutboxPublisher<TOutbox>(this IServiceCollection services, Action<IServiceProvider, TOutbox>? configure = null, bool addAsDefaultIEventPublisher = true, string serviceKey = SqlServerOutboxPublisher.DefaultServiceKey) where TOutbox : SqlServerOutboxPublisher
        => services.ThrowIfNull().AddEventPublisher(serviceKey, sp =>
        {
            var outbox = ActivatorUtilities.CreateInstance<TOutbox>(sp);
            configure?.Invoke(sp, outbox);
            return outbox;
        }, addAsDefaultIEventPublisher);

    /// <summary>
    /// Adds a <b>scoped</b> <see cref="SqlServerOutboxRelay"/> service for the <see cref="SqlServerDatabase"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="configure">An optional action to configure the <see cref="SqlServerOutboxRelay"/> instance.</param>
    /// <returns>The <see cref="IServiceCollection"/> for fluent-style method-chaining.</returns>
    public static IServiceCollection AddSqlServerOutboxRelay(this IServiceCollection services, Action<IServiceProvider, SqlServerOutboxRelay>? configure = null)
        => services.AddSqlServerOutboxRelay<SqlServerOutboxRelay, SqlServerDatabase>(configure);

    /// <summary>
    /// Adds a <b>scoped</b> <see cref="SqlServerOutboxRelay"/> service for the <see cref="SqlServerDatabase"/>.
    /// </summary>
    /// <typeparam name="TOutboxRelay">The <see cref="SqlServerOutboxRelay"/> <see cref="Type"/>.</typeparam>
    /// <typeparam name="TDatabase">The <see cref="SqlServerDatabase"/> <see cref="Type"/>.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="configure">An optional action to configure the <see cref="SqlServerOutboxRelay"/> instance.</param>
    /// <returns>The <see cref="IServiceCollection"/> for fluent-style method-chaining.</returns>
    public static IServiceCollection AddSqlServerOutboxRelay<TOutboxRelay, TDatabase>(this IServiceCollection services, Action<IServiceProvider, SqlServerOutboxRelay>? configure = null) where TOutboxRelay : SqlServerOutboxRelay where TDatabase : SqlServerDatabase
    {
        return services.ThrowIfNull().AddScoped<SqlServerOutboxRelay>(sp =>
        {
            var sql = sp.GetRequiredService<TDatabase>();
            var relay = ActivatorUtilities.CreateInstance<SqlServerOutboxRelay>(sp, sql);
            configure?.Invoke(sp, relay);
            return relay;
        });
    }
}