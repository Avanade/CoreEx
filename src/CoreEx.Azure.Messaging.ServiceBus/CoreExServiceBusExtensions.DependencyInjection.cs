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
        public AzureServiceBusReceiverService WithReceiver(Func<IServiceProvider, CoreEx.Azure.Messaging.ServiceBus.ServiceBusReceiverOptions> optionsFactory) => new(new(_services, null, optionsFactory));

        /// <summary>
        /// Adds a <b>singleton</b> Azure <see cref="ServiceBusSessionReceiver"/> service enabling ongoing fluent-style method-chaining registration.
        /// </summary>
        /// <param name="optionsFactory">The factory to create the required <see cref="CoreEx.Azure.Messaging.ServiceBus.ServiceBusSessionReceiverOptions"/>.</param>
        /// <returns>The <see cref="AzureServiceBusSessionReceiverService"/> for fluent-style method-chaining.</returns>
        public AzureServiceBusSessionReceiverService WithSessionReceiver(Func<IServiceProvider, CoreEx.Azure.Messaging.ServiceBus.ServiceBusSessionReceiverOptions> optionsFactory) => new(new(_services, null, optionsFactory));

        /// <summary>
        /// Holds the state (<see cref="IServiceCollection"/>, service key and options factory) shared by both the queue/topic and session receiver-service registration builders, so it is captured once rather
        /// than duplicated per receiver family.
        /// </summary>
        /// <typeparam name="TOptions">The <see cref="ServiceBusReceiverOptionsBase"/> <see cref="Type"/>.</typeparam>
        internal sealed class ReceiverOwner<TOptions> where TOptions : ServiceBusReceiverOptionsBase
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="ReceiverOwner{TOptions}"/> class.
            /// </summary>
            /// <param name="services">The <see cref="IServiceCollection"/>.</param>
            /// <param name="serviceKey">The service key.</param>
            /// <param name="optionsFactory">The factory to create the required <typeparamref name="TOptions"/>.</param>
            internal ReceiverOwner(IServiceCollection services, object? serviceKey, Func<IServiceProvider, TOptions> optionsFactory)
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
            internal Func<IServiceProvider, TOptions> OptionsFactory { get; }
        }

        /// <summary>
        /// Provides the shared registration logic for a <typeparamref name="TSubscriber"/> and its corresponding <typeparamref name="TReceiver"/>, common to both the queue/topic and session receiver families
        /// (which otherwise differ only in the concrete <typeparamref name="TOptions"/>/<typeparamref name="TReceiver"/> types involved).
        /// </summary>
        /// <typeparam name="TOptions">The <see cref="ServiceBusReceiverOptionsBase"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="TReceiver">The <see cref="ServiceBusReceiverBase{TSubscriber}"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="TSubscriber">The <see cref="ServiceBusSubscriberBase"/> <see cref="Type"/>.</typeparam>
        private sealed class ReceiverRegistrar<TOptions, TReceiver, TSubscriber>
            where TOptions : ServiceBusReceiverOptionsBase
            where TReceiver : ServiceBusReceiverBase<TSubscriber>
            where TSubscriber : ServiceBusSubscriberBase
        {
            private readonly ReceiverOwner<TOptions> _owner;
            private readonly object? _serviceKey;
            private readonly Action<IServiceProvider, TSubscriber>? _configure;

            /// <summary>
            /// Initializes a new instance of the <see cref="ReceiverRegistrar{TOptions, TReceiver, TSubscriber}"/> class.
            /// </summary>
            /// <param name="owner">The owner <see cref="ReceiverOwner{TOptions}"/> instance.</param>
            /// <param name="serviceKey">The service key.</param>
            /// <param name="configure">An optional action to configure the <typeparamref name="TSubscriber"/> instance.</param>
            internal ReceiverRegistrar(ReceiverOwner<TOptions> owner, object? serviceKey, Action<IServiceProvider, TSubscriber>? configure)
            {
                _owner = owner.ThrowIfNull();
                _serviceKey = serviceKey;
                _configure = configure;
            }

            /// <summary>
            /// Gets the <see cref="IServiceCollection"/>.
            /// </summary>
            internal IServiceCollection Services => _owner.Services;

            /// <summary>
            /// Builds and registers all of the chained services.
            /// </summary>
            internal void Build()
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
                        return ActivatorUtilities.CreateInstance<TReceiver>(sp, options);
                    });
                else
                    _owner.Services.AddKeyedSingleton(_owner.ServiceKey, (sp, _) =>
                    {
                        var options = _owner.OptionsFactory(sp) ?? throw new InvalidOperationException("The options factory must return a non-null.");
                        options.SubscriberServiceKey = _serviceKey;
                        return ActivatorUtilities.CreateInstance<TReceiver>(sp, options);
                    });
            }

            /// <summary>
            /// Create the receiver instance.
            /// </summary>
            /// <remarks>Keys off <see cref="ReceiverOwner{TOptions}.ServiceKey"/> (the receiver's own key) to match how <see cref="Build"/> actually registers the receiver - not <c>_serviceKey</c> (the subscriber's key),
            /// which is an unrelated, independently-keyed registration.</remarks>
            internal TReceiver GetReceiverInstance(IServiceProvider serviceProvider) => _owner.ServiceKey is null
                ? serviceProvider.GetRequiredService<TReceiver>()
                : serviceProvider.GetRequiredKeyedService<TReceiver>(_owner.ServiceKey);
        }

        /// <summary>
        /// Provides the <see cref="ServiceBusReceiver{TSubscriber}"/> service registration.
        /// </summary>
        public sealed class AzureServiceBusReceiverService
        {
            private readonly ReceiverOwner<CoreEx.Azure.Messaging.ServiceBus.ServiceBusReceiverOptions> _owner;

            /// <summary>
            /// Initializes a new instance of the <see cref="AzureServiceBusReceiverService"/> class.
            /// </summary>
            /// <param name="owner">The owner state.</param>
            internal AzureServiceBusReceiverService(ReceiverOwner<CoreEx.Azure.Messaging.ServiceBus.ServiceBusReceiverOptions> owner) => _owner = owner.ThrowIfNull();

            /// <summary>
            /// Adds a <b>singleton</b> Azure Service Bus <typeparamref name="TSubscriber"/> (see <see cref="CoreEx.Azure.Messaging.ServiceBus.Abstractions.ServiceBusReceiverBase"/>).
            /// </summary>
            /// <typeparam name="TSubscriber">The Azure <see cref="ServiceBusSubscriberBase"/> <see cref="Type"/>.</typeparam>
            /// <param name="configure">An optional action to configure the <typeparamref name="TSubscriber"/> instance.</param>
            /// <returns>The <see cref="WithSubscriberService{TSubscriber}"/> for fluent-style method-chaining.</returns>
            public WithSubscriberService<TSubscriber> WithSubscriber<TSubscriber>(Action<IServiceProvider, TSubscriber>? configure = null) where TSubscriber : ServiceBusSubscriberBase
                => new(_owner, null, configure);

            /// <summary>
            /// Adds a <b>singleton</b> Azure Service Bus <typeparamref name="TSubscriber"/> (see <see cref="CoreEx.Azure.Messaging.ServiceBus.Abstractions.ServiceBusReceiverBase"/>).
            /// </summary>
            /// <typeparam name="TSubscriber">The Azure <see cref="ServiceBusSubscriberBase"/> <see cref="Type"/>.</typeparam>
            /// <param name="serviceKey">The service key.</param>
            /// <param name="configure">An optional action to configure the <typeparamref name="TSubscriber"/> instance.</param>
            /// <returns>The <see cref="WithSubscriberService{TSubscriber}"/> for fluent-style method-chaining.</returns>
            public WithSubscriberService<TSubscriber> WithKeyedSubscriber<TSubscriber>(object serviceKey, Action<IServiceProvider, TSubscriber>? configure = null) where TSubscriber : ServiceBusSubscriberBase
                => new(_owner, serviceKey, configure);

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
            private readonly ReceiverRegistrar<CoreEx.Azure.Messaging.ServiceBus.ServiceBusReceiverOptions, ServiceBusReceiver<TSubscriber>, TSubscriber> _registrar;

            /// <summary>
            /// Initializes a new instance of the <see cref="WithSubscriberService{TSubscriber}"/> class.
            /// </summary>
            /// <param name="owner">The owner state.</param>
            /// <param name="serviceKey">The service key.</param>
            /// <param name="configure">An optional action to configure the <typeparamref name="TSubscriber"/> instance.</param>
            internal WithSubscriberService(ReceiverOwner<CoreEx.Azure.Messaging.ServiceBus.ServiceBusReceiverOptions> owner, object? serviceKey, Action<IServiceProvider, TSubscriber>? configure)
                => _registrar = new(owner, serviceKey, configure);

            /// <summary>
            /// Builds and registers all of the chained services.
            /// </summary>
            /// <remarks>Where a hosted service is also required then the chained <see cref="WithHostedService"/> should be used instead.</remarks>
            public void Build() => _registrar.Build();

            /// <summary>
            /// Adds a <b>singleton</b> Azure <see cref="ServiceBusReceiverHostedService{TReceiver}"/> keyed service that will be executed as a hosted service (i.e. in the background).
            /// </summary>
            /// <param name="serviceKey">The keyed singleton and health check key.</param>
            /// <param name="configure">An optional action to configure the <see cref="ServiceBusReceiverHostedService{TReceiver}"/> instance.</param>
            /// <returns>The <see cref="WithHostedServiceService{TReceiver}"/> instance for fluent-style method-chaining.</returns>
            /// <remarks>No services are added until the chained <see cref="WithHostedServiceService{TReceiver}.Build"/> method is called.</remarks>
            public WithHostedServiceService<ServiceBusReceiver<TSubscriber>> WithHostedService(string serviceKey = "azure-service-bus-receiver", Action<IServiceProvider, ServiceBusReceiverHostedService<ServiceBusReceiver<TSubscriber>>>? configure = null)
                => new(_registrar.Services, serviceKey.ThrowIfNullOrEmpty(), configure, _registrar.Build, _registrar.GetReceiverInstance);
        }

        /// <summary>
        /// Provides the <see cref="ServiceBusSessionReceiver{TSubscriber}"/> service registration.
        /// </summary>
        public sealed class AzureServiceBusSessionReceiverService
        {
            private readonly ReceiverOwner<CoreEx.Azure.Messaging.ServiceBus.ServiceBusSessionReceiverOptions> _owner;

            /// <summary>
            /// Initializes a new instance of the <see cref="AzureServiceBusSessionReceiverService"/> class.
            /// </summary>
            /// <param name="owner">The owner state.</param>
            internal AzureServiceBusSessionReceiverService(ReceiverOwner<CoreEx.Azure.Messaging.ServiceBus.ServiceBusSessionReceiverOptions> owner) => _owner = owner.ThrowIfNull();

            /// <summary>
            /// Adds a <b>singleton</b> Azure Service Bus <typeparamref name="TSubscriber"/> (see <see cref="CoreEx.Azure.Messaging.ServiceBus.Abstractions.ServiceBusReceiverBase"/>).
            /// </summary>
            /// <typeparam name="TSubscriber">The Azure <see cref="ServiceBusSubscriberBase"/> <see cref="Type"/>.</typeparam>
            /// <param name="configure">An optional action to configure the <typeparamref name="TSubscriber"/> instance.</param>
            /// <returns>The <see cref="WithSessionSubscriberService{TSubscriber}"/> for fluent-style method-chaining.</returns>
            public WithSessionSubscriberService<TSubscriber> WithSubscriber<TSubscriber>(Action<IServiceProvider, TSubscriber>? configure = null) where TSubscriber : ServiceBusSubscriberBase
                => new(_owner, null, configure);

            /// <summary>
            /// Adds a <b>singleton</b> Azure Service Bus <typeparamref name="TSubscriber"/> (see <see cref="CoreEx.Azure.Messaging.ServiceBus.Abstractions.ServiceBusReceiverBase"/>).
            /// </summary>
            /// <typeparam name="TSubscriber">The Azure <see cref="ServiceBusSubscriberBase"/> <see cref="Type"/>.</typeparam>
            /// <param name="serviceKey">The service key.</param>
            /// <param name="configure">An optional action to configure the <typeparamref name="TSubscriber"/> instance.</param>
            /// <returns>The <see cref="WithSessionSubscriberService{TSubscriber}"/> for fluent-style method-chaining.</returns>
            public WithSessionSubscriberService<TSubscriber> WithKeyedSubscriber<TSubscriber>(object serviceKey, Action<IServiceProvider, TSubscriber>? configure = null) where TSubscriber : ServiceBusSubscriberBase
                => new(_owner, serviceKey, configure);

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
            private readonly ReceiverRegistrar<CoreEx.Azure.Messaging.ServiceBus.ServiceBusSessionReceiverOptions, ServiceBusSessionReceiver<TSubscriber>, TSubscriber> _registrar;

            /// <summary>
            /// Initializes a new instance of the <see cref="WithSessionSubscriberService{TSubscriber}"/> class.
            /// </summary>
            /// <param name="owner">The owner state.</param>
            /// <param name="serviceKey">The service key.</param>
            /// <param name="configure">An optional action to configure the <typeparamref name="TSubscriber"/> instance.</param>
            internal WithSessionSubscriberService(ReceiverOwner<CoreEx.Azure.Messaging.ServiceBus.ServiceBusSessionReceiverOptions> owner, object? serviceKey, Action<IServiceProvider, TSubscriber>? configure)
                => _registrar = new(owner, serviceKey, configure);

            /// <summary>
            /// Builds and registers all of the chained services.
            /// </summary>
            /// <remarks>Where a hosted service is also required then the chained <see cref="WithHostedService"/> should be used instead.</remarks>
            public void Build() => _registrar.Build();

            /// <summary>
            /// Adds a <b>singleton</b> Azure <see cref="ServiceBusReceiverHostedService{TReceiver}"/> keyed service that will be executed as a hosted service (i.e. in the background).
            /// </summary>
            /// <param name="serviceKey">The keyed singleton and health check key.</param>
            /// <param name="configure">An optional action to configure the <see cref="ServiceBusReceiverHostedService{TReceiver}"/> instance.</param>
            /// <returns>The <see cref="WithHostedServiceService{TReceiver}"/> instance for fluent-style method-chaining.</returns>
            /// <remarks>No services are added until the chained <see cref="WithHostedServiceService{TReceiver}.Build"/> method is called.</remarks>
            public WithHostedServiceService<ServiceBusSessionReceiver<TSubscriber>> WithHostedService(string serviceKey = "azure-service-bus-session-receiver", Action<IServiceProvider, ServiceBusReceiverHostedService<ServiceBusSessionReceiver<TSubscriber>>>? configure = null)
                => new(_registrar.Services, serviceKey.ThrowIfNullOrEmpty(), configure, _registrar.Build, _registrar.GetReceiverInstance);
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
