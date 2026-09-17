#pragma warning disable IDE0130 // Namespace does not match folder structure - this is by design.
namespace Microsoft.Extensions.DependencyInjection;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public static partial class CoreExCosmosExtensions
{
    /// <summary>
    /// Adds a keyed <b>scoped</b> <see cref="CosmosDbEventPublisher"/> service.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="configure">An optional action to configure the <see cref="CosmosDbEventPublisher"/> instance.</param>
    /// <param name="addAsDefaultIEventPublisher">Indicates whether to also register as the default (non-keyed) <see cref="IEventPublisher"/> service.</param>
    /// <param name="serviceKey">The service key to use for the keyed registration.</param>
    /// <returns>The <see cref="IServiceCollection"/> for fluent-style method-chaining.</returns>
    /// <remarks>See <see cref="CoreExEventsExtensions.AddEventPublisher(IServiceCollection, string, Func{IServiceProvider, IEventPublisher}, bool)"/> for more information related to the underlying
    /// registration implementation - matches the same keyed-root-plus-resolvable-key convention <c>AddPostgresOutboxPublisher</c>/<c>AddSqlServerOutboxPublisher</c> already use, so the same
    /// <c>UseExpectedEventPublisher</c>-based test spy/expectation machinery (<c>CoreEx.UnitTesting</c>) works unchanged for a Cosmos-backed outbox too.</remarks>
    public static IServiceCollection AddCosmosDbEventPublisher(this IServiceCollection services, Action<IServiceProvider, CosmosDbEventPublisher>? configure = null, bool addAsDefaultIEventPublisher = true, string serviceKey = CosmosDbEventPublisher.DefaultServiceKey)
        => services.AddCosmosDbEventPublisher<CosmosDbEventPublisher>(configure, addAsDefaultIEventPublisher, serviceKey);

    /// <summary>
    /// Adds a keyed <b>scoped</b> <typeparamref name="TOutbox"/> <see cref="CosmosDbEventPublisher"/> service.
    /// </summary>
    /// <typeparam name="TOutbox">The <see cref="CosmosDbEventPublisher"/> <see cref="Type"/>.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="configure">An optional action to configure the <typeparamref name="TOutbox"/> instance.</param>
    /// <param name="addAsDefaultIEventPublisher">Indicates whether to also register as the default (non-keyed) <see cref="IEventPublisher"/> service.</param>
    /// <param name="serviceKey">The service key to use for the keyed registration.</param>
    /// <returns>The <see cref="IServiceCollection"/> for fluent-style method-chaining.</returns>
    /// <remarks>See <see cref="CoreExEventsExtensions.AddEventPublisher(IServiceCollection, string, Func{IServiceProvider, IEventPublisher}, bool)"/> for more information related to the underlying
    /// registration implementation.</remarks>
    public static IServiceCollection AddCosmosDbEventPublisher<TOutbox>(this IServiceCollection services, Action<IServiceProvider, TOutbox>? configure = null, bool addAsDefaultIEventPublisher = true, string serviceKey = CosmosDbEventPublisher.DefaultServiceKey) where TOutbox : CosmosDbEventPublisher
        => services.ThrowIfNull().AddEventPublisher(serviceKey, sp =>
        {
            var outbox = ActivatorUtilities.CreateInstance<TOutbox>(sp);
            configure?.Invoke(sp, outbox);
            return outbox;
        }, addAsDefaultIEventPublisher);
}
