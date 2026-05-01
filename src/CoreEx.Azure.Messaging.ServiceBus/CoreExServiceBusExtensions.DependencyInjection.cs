#pragma warning disable IDE0130 // Namespace does not match folder structure; by design.
namespace Microsoft.Extensions.DependencyInjection;
#pragma warning restore IDE0130 // Namespace does not match folder structure

/// <summary>
/// Provides standard extensions.
/// </summary>
public static class CoreExServiceBusExtensions
{
    /// <summary>
    /// Adds a keyed <b>scoped</b> Azure <see cref="ServiceBusPublisher"/> service.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="configure">An optional action to configure the <see cref="ServiceBusPublisher"/> instance.</param>
    /// <param name="addAsDefaultIEventPublisher">Indicates whether to also register as the default (non-keyed) <see cref="IEventPublisher"/> service.</param>
    /// <param name="serviceKey">The service key to use for the keyed registration.</param>
    /// <returns>The <see cref="IServiceCollection"/> for fluent-style method-chaining.</returns>
    /// <remarks>See <see cref="Microsoft.Extensions.DependencyInjection.CoreExEventsExtensions.AddEventPublisher(IServiceCollection, string, Func{IServiceProvider, IEventPublisher}, bool)"/> for more information
    /// related to the underlying registration implementation.</remarks>
    public static IServiceCollection AddAzureServiceBusPublisher(this IServiceCollection services, Action<IServiceProvider, ServiceBusPublisher>? configure = null, bool addAsDefaultIEventPublisher = true, string serviceKey = ServiceBusPublisher.DefaultServiceKey)
        => services.ThrowIfNull().AddEventPublisher(serviceKey, sp =>
        {
            var sbp = ActivatorUtilities.CreateInstance<ServiceBusPublisher>(sp);
            configure?.Invoke(sp, sbp);
            return sbp;
        }, addAsDefaultIEventPublisher);

    /// <summary>
    /// Adds a <b>singleton</b> Azure <see cref="ServiceBusSubscribedSubscriber"/> service.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="configure">An optional action to configure the <see cref="ServiceBusSubscribedSubscriber"/> instance.</param>
    /// <returns>The <see cref="IServiceCollection"/> for fluent-style method-chaining.</returns>
    public static IServiceCollection AddAzureServiceBusSubscribedSubscriber(this IServiceCollection services, Action<IServiceProvider, ServiceBusSubscribedSubscriber>? configure = null)
    {
        return services.ThrowIfNull().AddSingleton(sp =>
        {
            var sbss = ActivatorUtilities.CreateInstance<ServiceBusSubscribedSubscriber>(sp);
            configure?.Invoke(sp, sbss);
            return sbss;
        });
    }

    /// <summary>
    /// Provides a builder to register the Azure Service Bus receiving services.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <returns>The <see cref="AzureServiceBusReceiveServiceBuilder"/>.</returns>
    /// <remarks>Provides a fluent-style builder for configuring and registering the related Azure Service Bus receiver services to simplify usage and minimize challenges with the configuration hierarchy.</remarks>
    public static AzureServiceBusReceiveServiceBuilder AzureServiceBusReceiving(this IServiceCollection services) => new(services);

    /// <summary>
    /// Provides a builder for configuring and registering Azure Service Bus receiver services .
    /// </summary>
    public sealed class AzureServiceBusReceiveServiceBuilder
    {
        private readonly IServiceCollection _services;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureServiceBusReceiveServiceBuilder"/> class.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        internal AzureServiceBusReceiveServiceBuilder(IServiceCollection services) => _services = services.ThrowIfNull();

        /// <summary>
        /// Adds a <b>singleton</b> Azure <see cref="ServiceBusReceiver"/> service enabling ongoing fluent-style method-chaining registration.
        /// </summary>
        /// <param name="optionsFactory">The factory to create the required <see cref="CoreEx.Azure.Messaging.ServiceBus.ServiceBusReceiverOptions"/>.</param>
        /// <returns>The <see cref="AzureServiceBusReceiverService"/> for fluent-style method-chaining.</returns>
        public AzureServiceBusReceiverService WithReceiver(Func<IServiceProvider, CoreEx.Azure.Messaging.ServiceBus.ServiceBusReceiverOptions> optionsFactory) => new(_services, null, optionsFactory);

        /// <summary>
        /// Adds a <b>singleton</b> Azure <see cref="ServiceBusSessionReceiver"/> service enabling ongoing fluent-style method-chaining registration.
        /// </summary>
        /// <param name="optionsFactory">The factory to create the required <see cref="CoreEx.Azure.Messaging.ServiceBus.ServiceBusReceiverOptions"/>.</param>
        /// <returns>The <see cref="AzureServiceBusSessionReceiverService"/> for fluent-style method-chaining.</returns>
        public AzureServiceBusSessionReceiverService WithSessionReceiver(Func<IServiceProvider, CoreEx.Azure.Messaging.ServiceBus.ServiceBusSessionReceiverOptions> optionsFactory) => new(_services, null, optionsFactory);

        /// <summary>
        /// Provides the <see cref="ServiceBusReceiver{TSubscriber}"/> service registration.
        /// </summary>
        public sealed class AzureServiceBusReceiverService
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="AzureServiceBusReceiverService"/> class.
            /// </summary>
            /// <param name="services">The <see cref="IServiceCollection"/>.</param>
            /// <param name="serviceKey">The service key.</param>
            /// <param name="optionsFactory">The factory to create the required <see cref="CoreEx.Azure.Messaging.ServiceBus.ServiceBusReceiverOptions"/>.</param>
            internal AzureServiceBusReceiverService(IServiceCollection services, object? serviceKey, Func<IServiceProvider, CoreEx.Azure.Messaging.ServiceBus.ServiceBusReceiverOptions> optionsFactory)
            {
                Services = services.ThrowIfNull();
                ServiceKey = serviceKey;
                OptionsFactory = optionsFactory.ThrowIfNull();
            }

            /// <summary>
            /// Gets the <see cref="IServiceCollection"/>.
            /// </summary>
            internal IServiceCollection Services { get; }

            /// <summary>
            /// Gets the service key.
            /// </summary>
            internal object? ServiceKey { get; }

            /// <summary>
            /// Gets the options factory.
            /// </summary>
            internal Func<IServiceProvider, CoreEx.Azure.Messaging.ServiceBus.ServiceBusReceiverOptions> OptionsFactory { get; }

            /// <summary>
            /// Adds a <b>singleton</b> Azure Service Bus <typeparamref name="TSubscriber"/> (see <see cref="CoreEx.Azure.Messaging.ServiceBus.Abstractions.ServiceBusReceiverBase"/>).
            /// </summary>
            /// <typeparam name="TSubscriber">The Azure <see cref="ServiceBusSubscriberBase"/> <see cref="Type"/>.</typeparam>
            /// <param name="configure">An optional action to configure the <typeparamref name="TSubscriber"/> instance.</param>
            /// <returns>The <see cref="WithSubscriberService{TSubscriber}"/> for fluent-style method-chaining.</returns>
            public WithSubscriberService<TSubscriber> WithSubscriber<TSubscriber>(Action<IServiceProvider, TSubscriber>? configure = null) where TSubscriber : ServiceBusSubscriberBase
                => new(this, null, configure);

            /// <summary>
            /// Adds a <b>singleton</b> Azure Service Bus <typeparamref name="TSubscriber"/> (see <see cref="CoreEx.Azure.Messaging.ServiceBus.Abstractions.ServiceBusReceiverBase"/>).
            /// </summary>
            /// <typeparam name="TSubscriber">The Azure <see cref="ServiceBusSubscriberBase"/> <see cref="Type"/>.</typeparam>
            /// <param name="serviceKey">The service key.</param>
            /// <param name="configure">An optional action to configure the <typeparamref name="TSubscriber"/> instance.</param>
            /// <returns>The <see cref="WithSubscriberService{TSubscriber}"/> for fluent-style method-chaining.</returns>
            public WithSubscriberService<TSubscriber> WithKeyedSubscriber<TSubscriber>(object serviceKey, Action<IServiceProvider, TSubscriber>? configure = null) where TSubscriber : ServiceBusSubscriberBase
                => new(this, serviceKey, configure);

            /// <summary>
            /// Adds a <b>singleton</b> Azure <see cref="ServiceBusSubscribedSubscriber"/> as the subscriber.
            /// </summary>
            /// <param name="configure">An optional action to configure the <see cref="ServiceBusSubscribedSubscriber"/> instance.</param>
            /// <returns>The <see cref="WithSubscriberService{TSubscriber}"/> for fluent-style method-chaining.</returns>
            public WithSubscriberService<ServiceBusSubscribedSubscriber> WithSubscribedSubscriber(Action<IServiceProvider, ServiceBusSubscribedSubscriber>? configure = null) => WithSubscriber(configure);

            /// <summary>
            /// Adds a <b>singleton</b> Azure <see cref="ServiceBusSubscribedSubscriber"/> as the subscriber.
            /// </summary>
            /// <param name="serviceKey">The service key.</param>
            /// <param name="configure">An optional action to configure the <see cref="ServiceBusSubscribedSubscriber"/> instance.</param>
            /// <returns>The <see cref="WithSubscriberService{TSubscriber}"/> for fluent-style method-chaining.</returns>
            public WithSubscriberService<ServiceBusSubscribedSubscriber> WithKeyedSubscribedSubscriber(object serviceKey, Action<IServiceProvider, ServiceBusSubscribedSubscriber>? configure = null) => WithKeyedSubscriber(serviceKey, configure);
        }

        /// <summary>
        /// Provides the <typeparamref name="TSubscriber"/> service registration.
        /// </summary>
        /// <typeparam name="TSubscriber"></typeparam>
        public sealed class WithSubscriberService<TSubscriber> where TSubscriber : ServiceBusSubscriberBase
        {
            private readonly AzureServiceBusReceiverService _owner;
            private readonly object? _serviceKey;
            private readonly Action<IServiceProvider, TSubscriber>? _configure;

            /// <summary>
            /// Initializes a new instance of the <see cref="WithSubscriberService{TSubscriber}"/> class.
            /// </summary>
            /// <param name="owner">The owner <see cref="AzureServiceBusReceiverService"/> instance.</param>
            /// <param name="serviceKey">The service key.</param>
            /// <param name="configure">An optional action to configure the <typeparamref name="TSubscriber"/> instance.</param>
            internal WithSubscriberService(AzureServiceBusReceiverService owner, object? serviceKey, Action<IServiceProvider, TSubscriber>? configure)
            {
                _owner = owner.ThrowIfNull();
                _serviceKey = serviceKey;
                _configure = configure;
            }

            /// <summary>
            /// Builds and registers all of the chained services.
            /// </summary>
            /// <remarks>Where a hosted service is also required then the chained <see cref="WithHostedService"/> should be used instead.</remarks>
            public void Build()
            {
                // Add the subscriber service.
                if (_serviceKey is null)
                    _owner.Services.AddSingleton(sp =>
                    {
                        var subscriber = ActivatorUtilities.CreateInstance<TSubscriber>(sp);
                        _configure?.Invoke(sp, subscriber);
                        return subscriber;
                    });
                else
                    _owner.Services.AddKeyedSingleton(_serviceKey, (sp, _) =>
                    {
                        var subscriber = ActivatorUtilities.CreateInstance<TSubscriber>(sp);
                        _configure?.Invoke(sp, subscriber);
                        return subscriber;
                    });

                // Add the receiver service.
                if (_owner.ServiceKey is null)
                    _owner.Services.AddSingleton(sp =>
                    {
                        var options = _owner.OptionsFactory(sp) ?? throw new InvalidOperationException("The options factory must return a non-null.");
                        options.SubscriberServiceKey = _serviceKey;
                        var receiver = ActivatorUtilities.CreateInstance<ServiceBusReceiver<TSubscriber>>(sp, options);
                        return receiver;
                    });
                else
                    _owner.Services.AddKeyedSingleton(_owner.ServiceKey, (sp, _) =>
                    {
                        var options = _owner.OptionsFactory(sp) ?? throw new InvalidOperationException("The options factory must return a non-null.");
                        options.SubscriberServiceKey = _serviceKey;
                        var receiver = ActivatorUtilities.CreateInstance<ServiceBusReceiver<TSubscriber>>(sp, options);
                        return receiver;
                    });
            }

            /// <summary>
            /// Create the receiver instance.
            /// </summary>
            private ServiceBusReceiver<TSubscriber> GetReceiverInstance(IServiceProvider serviceProvider) => _serviceKey is null
                ? serviceProvider.GetRequiredService<ServiceBusReceiver<TSubscriber>>()
                : serviceProvider.GetRequiredKeyedService<ServiceBusReceiver<TSubscriber>>(_serviceKey);

            /// <summary>
            /// Adds a <b>singleton</b> Azure <see cref="ServiceBusReceiverHostedService{TReceiver}"/> keyed service that will be executed as a hosted service (i.e. in the background).
            /// </summary>
            /// <param name="serviceKey">The keyed singleton and health check key.</param>
            /// <param name="configure">An optional action to configure the <see cref="ServiceBusReceiverHostedService{TReceiver}"/> instance.</param>
            /// <returns>The <see cref="WithHostedServiceService{TReceiver}"/> instance for fluent-style method-chaining.</returns>
            /// <remarks>No services are added until the chained <see cref="WithHostedServiceService{TReceiver}.Build"/> method is called.</remarks>
            public WithHostedServiceService<ServiceBusReceiver<TSubscriber>> WithHostedService(string serviceKey = "azure-service-bus-receiver", Action<IServiceProvider, ServiceBusReceiverHostedService<ServiceBusReceiver<TSubscriber>>>? configure = null)
                => new(_owner.Services, serviceKey.ThrowIfNullOrEmpty(), configure, Build, GetReceiverInstance);
        }

        /// <summary>
        /// Provides the <see cref="ServiceBusSessionReceiver{TSubscriber}"/> service registration.
        /// </summary>
        public sealed class AzureServiceBusSessionReceiverService
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="AzureServiceBusSessionReceiverService"/> class.
            /// </summary>
            /// <param name="services">The <see cref="IServiceCollection"/>.</param>
            /// <param name="serviceKey">The service key.</param>
            /// <param name="optionsFactory">The factory to create the required <see cref="CoreEx.Azure.Messaging.ServiceBus.ServiceBusSessionReceiverOptions"/>.</param>
            internal AzureServiceBusSessionReceiverService(IServiceCollection services, object? serviceKey, Func<IServiceProvider, CoreEx.Azure.Messaging.ServiceBus.ServiceBusSessionReceiverOptions> optionsFactory)
            {
                Services = services.ThrowIfNull();
                ServiceKey = serviceKey;
                OptionsFactory = optionsFactory.ThrowIfNull();
            }

            /// <summary>
            /// Gets the <see cref="IServiceCollection"/>.
            /// </summary>
            internal IServiceCollection Services { get; }

            /// <summary>
            /// Gets the service key.
            /// </summary>
            internal object? ServiceKey { get; }

            /// <summary>
            /// Gets the options factory.
            /// </summary>
            internal Func<IServiceProvider, CoreEx.Azure.Messaging.ServiceBus.ServiceBusSessionReceiverOptions> OptionsFactory { get; }

            /// <summary>
            /// Adds a <b>singleton</b> Azure Service Bus <typeparamref name="TSubscriber"/> (see <see cref="CoreEx.Azure.Messaging.ServiceBus.Abstractions.ServiceBusReceiverBase"/>).
            /// </summary>
            /// <typeparam name="TSubscriber">The Azure <see cref="ServiceBusSubscriberBase"/> <see cref="Type"/>.</typeparam>
            /// <param name="configure">An optional action to configure the <typeparamref name="TSubscriber"/> instance.</param>
            /// <returns>The <see cref="WithSessionSubscriberService{TSubscriber}"/> for fluent-style method-chaining.</returns>
            public WithSessionSubscriberService<TSubscriber> WithSubscriber<TSubscriber>(Action<IServiceProvider, TSubscriber>? configure = null) where TSubscriber : ServiceBusSubscriberBase
                => new(this, null, configure);

            /// <summary>
            /// Adds a <b>singleton</b> Azure Service Bus <typeparamref name="TSubscriber"/> (see <see cref="CoreEx.Azure.Messaging.ServiceBus.Abstractions.ServiceBusReceiverBase"/>).
            /// </summary>
            /// <typeparam name="TSubscriber">The Azure <see cref="ServiceBusSubscriberBase"/> <see cref="Type"/>.</typeparam>
            /// <param name="serviceKey">The service key.</param>
            /// <param name="configure">An optional action to configure the <typeparamref name="TSubscriber"/> instance.</param>
            /// <returns>The <see cref="WithSessionSubscriberService{TSubscriber}"/> for fluent-style method-chaining.</returns>
            public WithSessionSubscriberService<TSubscriber> WithKeyedSubscriber<TSubscriber>(object serviceKey, Action<IServiceProvider, TSubscriber>? configure = null) where TSubscriber : ServiceBusSubscriberBase
                => new(this, serviceKey, configure);

            /// <summary>
            /// Adds a <b>singleton</b> Azure <see cref="ServiceBusSubscribedSubscriber"/> as the subscriber.
            /// </summary>
            /// <param name="configure">An optional action to configure the <see cref="ServiceBusSubscribedSubscriber"/> instance.</param>
            /// <returns>The <see cref="WithSessionSubscriberService{TSubscriber}"/> for fluent-style method-chaining.</returns>
            public WithSessionSubscriberService<ServiceBusSubscribedSubscriber> WithSubscribedSubscriber(Action<IServiceProvider, ServiceBusSubscribedSubscriber>? configure = null) => WithSubscriber(configure);

            /// <summary>
            /// Adds a <b>singleton</b> Azure <see cref="ServiceBusSubscribedSubscriber"/> as the subscriber.
            /// </summary>
            /// <param name="serviceKey">The service key.</param>
            /// <param name="configure">An optional action to configure the <see cref="ServiceBusSubscribedSubscriber"/> instance.</param>
            /// <returns>The <see cref="WithSessionSubscriberService{TSubscriber}"/> for fluent-style method-chaining.</returns>
            public WithSessionSubscriberService<ServiceBusSubscribedSubscriber> WithKeyedSubscribedSubscriber(object serviceKey, Action<IServiceProvider, ServiceBusSubscribedSubscriber>? configure = null) => WithKeyedSubscriber(serviceKey, configure);
        }

        /// <summary>
        /// Provides the <typeparamref name="TSubscriber"/> service registration.
        /// </summary>
        /// <typeparam name="TSubscriber">The <see cref="ServiceBusSubscriberBase"/> <see cref="Type"/>.</typeparam>
        public sealed class WithSessionSubscriberService<TSubscriber> where TSubscriber : ServiceBusSubscriberBase
        {
            private readonly AzureServiceBusSessionReceiverService _owner;
            private readonly object? _serviceKey;
            private readonly Action<IServiceProvider, TSubscriber>? _configure;

            /// <summary>
            /// Initializes a new instance of the <see cref="WithSubscriberService{TSubscriber}"/> class.
            /// </summary>
            /// <param name="owner">The owner <see cref="AzureServiceBusSessionReceiverService"/> instance.</param>
            /// <param name="serviceKey">The service key.</param>
            /// <param name="configure">An optional action to configure the <typeparamref name="TSubscriber"/> instance.</param>
            internal WithSessionSubscriberService(AzureServiceBusSessionReceiverService owner, object? serviceKey, Action<IServiceProvider, TSubscriber>? configure)
            {
                _owner = owner.ThrowIfNull();
                _serviceKey = serviceKey;
                _configure = configure;
            }

            /// <summary>
            /// Builds and registers all of the chained services.
            /// </summary>
            /// <remarks>Where a hosted service is also required then the chained <see cref="WithHostedService"/> should be used instead.</remarks>
            public void Build()
            {
                // Add the subscriber service.
                if (_serviceKey is null)
                    _owner.Services.AddSingleton(sp =>
                    {
                        var subscriber = ActivatorUtilities.CreateInstance<TSubscriber>(sp);
                        _configure?.Invoke(sp, subscriber);
                        return subscriber;
                    });
                else
                    _owner.Services.AddKeyedSingleton(_serviceKey, (sp, _) =>
                    {
                        var subscriber = ActivatorUtilities.CreateInstance<TSubscriber>(sp);
                        _configure?.Invoke(sp, subscriber);
                        return subscriber;
                    });

                // Add the receiver service.
                if (_owner.ServiceKey is null)
                    _owner.Services.AddSingleton(sp =>
                    {
                        var options = _owner.OptionsFactory(sp) ?? throw new InvalidOperationException("The options factory must return a non-null.");
                        options.SubscriberServiceKey = _serviceKey;
                        var receiver = ActivatorUtilities.CreateInstance<ServiceBusSessionReceiver<TSubscriber>>(sp, options);
                        return receiver;
                    });
                else
                    _owner.Services.AddKeyedSingleton(_owner.ServiceKey, (sp, _) =>
                    {
                        var options = _owner.OptionsFactory(sp) ?? throw new InvalidOperationException("The options factory must return a non-null.");
                        options.SubscriberServiceKey = _serviceKey;
                        var receiver = ActivatorUtilities.CreateInstance<ServiceBusSessionReceiver<TSubscriber>>(sp, options);
                        return receiver;
                    });
            }

            /// <summary>
            /// Create the receiver instance.
            /// </summary>
            private ServiceBusSessionReceiver<TSubscriber> GetReceiverInstance(IServiceProvider serviceProvider) => _serviceKey is null
                ? serviceProvider.GetRequiredService<ServiceBusSessionReceiver<TSubscriber>>()
                : serviceProvider.GetRequiredKeyedService<ServiceBusSessionReceiver<TSubscriber>>(_serviceKey);

            /// <summary>
            /// Adds a <b>singleton</b> Azure <see cref="ServiceBusReceiverHostedService{TReceiver}"/> keyed service that will be executed as a hosted service (i.e. in the background).
            /// </summary>
            /// <param name="serviceKey">The keyed singleton and health check key.</param>
            /// <param name="configure">An optional action to configure the <see cref="ServiceBusReceiverHostedService{TReceiver}"/> instance.</param>
            /// <returns>The <see cref="WithHostedServiceService{TReceiver}"/> instance for fluent-style method-chaining.</returns>
            /// <remarks>No services are added until the chained <see cref="WithHostedServiceService{TReceiver}.Build"/> method is called.</remarks>
            public WithHostedServiceService<ServiceBusSessionReceiver<TSubscriber>> WithHostedService(string serviceKey = "azure-service-bus-session-receiver", Action<IServiceProvider, ServiceBusReceiverHostedService<ServiceBusSessionReceiver<TSubscriber>>>? configure = null)
                => new(_owner.Services, serviceKey.ThrowIfNullOrEmpty(), configure, Build, GetReceiverInstance);
        }

        /// <summary>
        /// Provides the <see cref="ServiceBusReceiverHostedService{TReceiver}"/> service registration.
        /// </summary>
        /// <typeparam name="TReceiver">The Azure <see cref="ServiceBusReceiverBase"/> <see cref="Type"/>.</typeparam>
        public sealed class WithHostedServiceService<TReceiver> where TReceiver : ServiceBusReceiverBase
        {
            private readonly IServiceCollection _services;
            private readonly string _serviceKey;
            private readonly Action<IServiceProvider, ServiceBusReceiverHostedService<TReceiver>>? _configure;
            private readonly Action _buildParentServices;
            private readonly Func<IServiceProvider, object> _createReceiverInstance;

            /// <summary>
            /// Initializes a new instance of the <see cref="WithHostedServiceService{TReceiver}"/> class.
            /// </summary>
            /// <param name="services">The <see cref="IServiceCollection"/>.</param>
            /// <param name="serviceKey">The service key.</param>
            /// <param name="configure">An optional action to configure the <see cref="ServiceBusReceiverHostedService{TReceiver}"/> instance.</param>
            /// <param name="buildParentServices">The action to build the parent services (i.e. the subscriber and receiver).</param>
            /// <param name="createReceiverInstance">The function to create the receiver instance.</param>
            internal WithHostedServiceService(IServiceCollection services, string serviceKey, Action<IServiceProvider, ServiceBusReceiverHostedService<TReceiver>>? configure, Action buildParentServices, Func<IServiceProvider, object> createReceiverInstance)
            {
                _services = services.ThrowIfNull();
                _serviceKey = serviceKey.ThrowIfNullOrEmpty();
                _configure = configure;
                _buildParentServices = buildParentServices;
                _createReceiverInstance = createReceiverInstance;
            }

            /// <summary>
            /// Builds and registers all of the chained services.
            /// </summary>
            public void Build()
            {
                // Adds the parent service registrations.
                _buildParentServices();

                // Adds the hosted service registration.
                _services.AddHostedService(_serviceKey, sp =>
                {
                    var receiver = _createReceiverInstance(sp);
                    return ActivatorUtilities.CreateInstance<ServiceBusReceiverHostedService<TReceiver>>(sp, receiver);
                });
            }
        }
    }
}