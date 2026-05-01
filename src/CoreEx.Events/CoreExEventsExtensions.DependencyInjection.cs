#pragma warning disable IDE0130 // Namespace does not match folder structure - this is by design.
namespace Microsoft.Extensions.DependencyInjection;
#pragma warning restore IDE0130 // Namespace does not match folder structure

/// <summary>
/// Provides <see cref="CoreEx.Events"/> and related extensions.
/// </summary>
public static class CoreExEventsExtensions
{
    /// <summary>
    /// Adds a <b>singleton</b> service for the <see cref="EventFormatter"/> as the <see cref="IEventFormatter"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="configure">The action to configure the <see cref="EventFormatter"/> instance.</param>
    /// <returns>The <see cref="IServiceCollection"/> for fluent-style method-chaining.</returns>
    public static IServiceCollection AddEventFormatter(this IServiceCollection services, Action<EventFormatter>? configure = null)
    {
        return services.AddSingleton<IEventFormatter>(sp =>
        {
            var ef = ActivatorUtilities.CreateInstance<EventFormatter>(sp);
            configure?.Invoke(ef);
            return ef;
        });
    }

    /// <summary>
    /// Adds a <b>singleton</b> service for the <see cref="FixedDestinationProvider"/> as the <see cref="IDestinationProvider"/>
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="destination">The fixed destination name.</param>
    /// <returns>The <see cref="IServiceCollection"/> for fluent-style method-chaining.</returns>
    public static IServiceCollection AddFixedDestinationProvider(this IServiceCollection services, string destination)
        => services.ThrowIfNull().AddSingleton<IDestinationProvider>(new FixedDestinationProvider { Destination = destination.ThrowIfNullOrEmpty() });

    /// <summary>
    /// Adds a <b>singleton</b> <see cref="SubscribedManager"/> service.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="configure">The action to configure the <see cref="SubscribedManager"/> instance.</param>
    /// <returns>The <see cref="IServiceCollection"/> for fluent-style method-chaining.</returns>
    public static IServiceCollection AddSubscribedManager(this IServiceCollection services, Action<IServiceProvider, SubscribedManager>? configure = null)
    {
        return services.AddSingleton<SubscribedManager>(sp =>
        {
            var manager = new SubscribedManager(sp.GetService<SubscribedInvoker>());
            configure?.Invoke(sp, manager);
            return manager;
        });
    }

    /// <summary>
    /// Adds a keyed <b>scoped</b> <see cref="IEventPublisher"/> service.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="serviceKey">The service key to use for the keyed registration.</param>
    /// <param name="serviceFactory">The factory function to create the <see cref="IEventPublisher"/> instance.</param>
    /// <param name="addAsDefaultIEventPublisher">Indicates whether to also register as the default (non-keyed) <see cref="IEventPublisher"/> service.</param>
    /// <returns>The <see cref="IServiceCollection"/> for fluent-style method-chaining.</returns>
    /// <remarks>This method firstly registers the service with a root key (derived from the provided service key) and then registers the provided service key to resolve to the root service.
    /// This ensures that the root service is always registered, even if the non-root is re-registered the root can be resolved - useful for testing scenarios where the service is re-registered with a test double (the root remains and is accessible).
    /// <para>Where <paramref name="addAsDefaultIEventPublisher"/> is <see langword="true"/>, it also registers the provided service key as the default (primary / non-keyed) <see cref="IEventPublisher"/> service, allowing it to be resolved
    /// without specifying a key simplifying usage in most scenarios. Note that only a single default <see cref="IEventPublisher"/> can be registered at a time.</para></remarks>
    public static IServiceCollection AddEventPublisher(this IServiceCollection services, string serviceKey, Func<IServiceProvider, IEventPublisher> serviceFactory, bool addAsDefaultIEventPublisher = true)
    {
        serviceFactory.ThrowIfNull();
        var rootKey = $"{serviceKey.ThrowIfNullOrEmpty()}_Root"; 

        services.ThrowIfNull().AddKeyedScoped<IEventPublisher>(rootKey, (sp, _) => serviceFactory(sp) ?? throw new InvalidOperationException("The service factory must not return null."))
                              .AddKeyedScoped<IEventPublisher>(serviceKey, (sp, _) => sp.GetRequiredKeyedService<IEventPublisher>(rootKey));

        if (addAsDefaultIEventPublisher)
            services.AddScoped<IEventPublisher>(sp => sp.GetRequiredKeyedService<IEventPublisher>(serviceKey));

        return services;
    }

    /// <summary>
    /// Adds a keyed <b>scoped</b> <see cref="NoOpEventPublisher"/> service for the specified service key.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="serviceKey">The service key to use for the keyed registration.</param>
    /// <param name="addAsDefaultIEventPublisher">Indicates whether to also register as the default (non-keyed) <see cref="IEventPublisher"/> service.</param>
    /// <returns>The <see cref="IServiceCollection"/> for fluent-style method-chaining.</returns>
    public static IServiceCollection AddNoOpEventPublisher(this IServiceCollection services, string serviceKey, bool addAsDefaultIEventPublisher = true)
        => AddEventPublisher(services, serviceKey, sp => ActivatorUtilities.CreateInstance<NoOpEventPublisher>(sp), addAsDefaultIEventPublisher);
}