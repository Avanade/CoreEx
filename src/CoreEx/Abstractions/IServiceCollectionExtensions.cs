// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx;
using CoreEx.Caching;
using CoreEx.Configuration;
using CoreEx.Entities;
using CoreEx.Events;
using CoreEx.Events.Subscribing;
using CoreEx.Hosting;
using CoreEx.Http;
using CoreEx.Json;
using CoreEx.Json.Merge;
using CoreEx.Mapping;
using CoreEx.RefData;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net.Http;
using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Provides <see cref="IServiceCollection"/> extensions.
    /// </summary>
    public static class IServiceCollectionExtensions
    {
        /// <summary>
        /// Checks that the <see cref="IServiceCollection"/> is not null.
        /// </summary>
        private static IServiceCollection CheckServices(IServiceCollection services) => services ?? throw new ArgumentNullException(nameof(services));

        /// <summary>
        /// Removes all items from the <see cref="IServiceCollection"/> for the specified <typeparamref name="TService"/>.
        /// </summary>
        /// <typeparam name="TService">The service <see cref="Type"/>.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <returns><c>true</c> if item was successfully removed; otherwise, <c>false</c>. Also returns <c>false</c> where item was not found.</returns>
        public static bool Remove<TService>(this IServiceCollection services) where TService : class
        {
            var descriptor = CheckServices(services).FirstOrDefault(d => d.ServiceType == typeof(TService));
            return descriptor != null && services.Remove(descriptor);
        }

        /// <summary>
        /// Adds the <see cref="SystemTime.Default"/> as the <see cref="ISystemTime"/> singleton service.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/> for fluent-style method-chaining.</returns>
        public static IServiceCollection AddSystemTime(this IServiceCollection services) => CheckServices(services).AddSingleton<ISystemTime>(sp => SystemTime.Default);

        /// <summary>
        /// Adds a scoped service to instantiate a new <see cref="ExecutionContext"/> instance.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <param name="executionContextFactory">The function to override the creation of the <see cref="ExecutionContext"/> instance.</param>
        /// <returns>The <see cref="IServiceCollection"/> for fluent-style method-chaining.</returns>
        /// <remarks>Where the <paramref name="executionContextFactory"/> is <c>null</c>, then the <see cref="ExecutionContext.Create"/> is used to create.</remarks>
        public static IServiceCollection AddExecutionContext(this IServiceCollection services, Func<IServiceProvider, ExecutionContext>? executionContextFactory = null)
        {
            return CheckServices(services).AddScoped(sp =>
            {
                var ec = executionContextFactory?.Invoke(sp) ?? ExecutionContext.Create?.Invoke() ??
                    throw new InvalidOperationException("Unable to create 'ExecutionContext' instance; either (in order) 'executionContextFactory' resulted in null, or 'ExecutionContext.Create' resulted in null.");

                ec.ServiceProvider = sp;

                ExecutionContext.Reset();
                ExecutionContext.SetCurrent(ec);

                return ec;
            });
        }

        /// <summary>
        /// Adds a <see cref="TypedHttpClientBase"/> using the underlying <see cref="HttpClientFactoryServiceCollectionExtensions.AddHttpClient{TClient}(IServiceCollection, string, Action{IServiceProvider, HttpClient})"/>.
        /// </summary>
        /// <typeparam name="TClient">The client <see cref="TypedHttpClientBase"/> <see cref="Type"/>.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <param name="name">The logical name of the <see cref="HttpClient"/> to configure.</param>
        /// <param name="configure">The delegate to configure the underlying <see cref="HttpClient"/>.</param>
        /// <returns>An <see cref="IHttpClientBuilder"/> that can be used to configure the client.</returns>
        public static IHttpClientBuilder AddTypedHttpClient<TClient>(this IServiceCollection services, string name, Action<IServiceProvider, HttpClient>? configure = null) where TClient : TypedHttpClientBase
            => configure == null ? services.AddHttpClient<TClient>(name) : services.AddHttpClient<TClient>(name, configure);

        /// <summary>
        /// Adds a <see cref="TypedHttpClientBase"/> using the underlying <see cref="HttpClientFactoryServiceCollectionExtensions.AddHttpClient{TClient}(IServiceCollection, string, Action{IServiceProvider, HttpClient})"/>.
        /// </summary>
        /// <typeparam name="TClient">The client <see cref="Type"/>.</typeparam>
        /// <typeparam name="TImplementation">The client <see cref="TypedHttpClientBase"/> implementation <see cref="Type"/>.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <param name="name">The logical name of the <see cref="HttpClient"/> to configure.</param>
        /// <param name="configure">The delegate to configure the underlying <see cref="HttpClient"/>.</param>
        /// <returns>An <see cref="IHttpClientBuilder"/> that can be used to configure the client.</returns>
        public static IHttpClientBuilder AddTypedHttpClient<TClient, TImplementation>(this IServiceCollection services, string name, Action<IServiceProvider, HttpClient>? configure = null) where TClient : class where TImplementation : TypedHttpClientBase, TClient
            => configure == null ? services.AddHttpClient<TClient, TImplementation>(name) : services.AddHttpClient<TClient, TImplementation>(name, configure);

        /// <summary>
        /// Adds the <see cref="DefaultSettings"/> as the <see cref="SettingsBase"/> singleton service.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddDefaultSettings(this IServiceCollection services) => CheckServices(services).AddSettings<DefaultSettings>();

        /// <summary>
        /// Adds the <typeparamref name="TSettings"/> <see cref="Type"/> as the singleton service.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        /// <remarks>Where the <see cref="SettingsBase"/> has not been registered then this will be registered automatically also.</remarks>
        public static IServiceCollection AddSettings<TSettings>(this IServiceCollection services) where TSettings : SettingsBase
        {
            CheckServices(services).AddSingleton<TSettings>();

            if (services.FirstOrDefault(d => d.ServiceType == typeof(SettingsBase)) == null)
                CheckServices(services).AddSingleton<SettingsBase, TSettings>();

            return services;
        }

        /// <summary>
        /// Adds the <see cref="IdentifierGenerator"/> as the <see cref="string"/> <see cref="IIdentifierGenerator{T}"/> singleton service.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddStringIdentifierGenerator(this IServiceCollection services) => CheckServices(services).AddSingleton<IIdentifierGenerator<string>, IdentifierGenerator>();

        /// <summary>
        /// Adds the <see cref="IdentifierGenerator"/> as the <see cref="Guid"/> <see cref="IIdentifierGenerator{T}"/> singleton service.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddGuidIdentifierGenerator(this IServiceCollection services) => CheckServices(services).AddSingleton<IIdentifierGenerator<Guid>, IdentifierGenerator>();

        /// <summary>
        /// Adds the <see cref="LoggerEventPublisher"/> as the <see cref="IEventPublisher"/> scoped service.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddLoggerEventPublisher(this IServiceCollection services) => CheckServices(services).AddEventPublisher<LoggerEventPublisher>();

        /// <summary>
        /// Adds the <see cref="NullEventPublisher"/> as the <see cref="IEventPublisher"/> scoped service.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddNullEventPublisher(this IServiceCollection services) => CheckServices(services).AddEventPublisher<NullEventPublisher>();

        /// <summary>
        /// Adds the <see cref="EventPublisher"/> as the <see cref="IEventPublisher"/> scoped service.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddEventPublisher(this IServiceCollection services) => CheckServices(services).AddEventPublisher<EventPublisher>();

        /// <summary>
        /// Adds the <typeparamref name="TEventPublisher"/> as the <see cref="IEventPublisher"/> scoped service.
        /// </summary>
        /// <typeparam name="TEventPublisher">The <see cref="IEventPublisher"/> <see cref="Type"/>.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddEventPublisher<TEventPublisher>(this IServiceCollection services) where TEventPublisher : class, IEventPublisher => CheckServices(services).AddScoped<IEventPublisher, TEventPublisher>();

        /// <summary>
        /// Adds the <see cref="LoggerEventSender"/> as the <see cref="IEventSender"/> scoped service.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddLoggerEventSender(this IServiceCollection services) => CheckServices(services).AddEventSender<LoggerEventSender>();

        /// <summary>
        /// Adds the <see cref="NullEventSender"/> as the <see cref="IEventSender"/> scoped service.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddNullEventSender(this IServiceCollection services) => CheckServices(services).AddEventSender<NullEventSender>();

        /// <summary>
        /// Adds the <typeparamref name="TEventSender"/> as the <see cref="IEventSender"/> scoped service.
        /// </summary>
        /// <typeparam name="TEventSender">The <see cref="IEventSender"/> <see cref="Type"/>.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddEventSender<TEventSender>(this IServiceCollection services) where TEventSender : class, IEventSender => CheckServices(services).AddScoped<IEventSender, TEventSender>();

        /// <summary>
        /// Adds the <see cref="EventSubscriberOrchestrator"/> as a singleton service.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <param name="configure">The action to enable the <see cref="EventSubscriberOrchestrator"/> to be further configured.</param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddEventSubscriberOrchestrator(this IServiceCollection services, Action<IServiceProvider, EventSubscriberOrchestrator>? configure = null) => CheckServices(services).AddSingleton(sp =>
        {
            var eso = new EventSubscriberOrchestrator(sp);
            configure?.Invoke(sp, eso);
            return eso;
        });

        /// <summary>
        /// Adds all the <see cref="SubscriberBase"/> types for a given <typeparamref name="TAssembly"/> that have at least one <see cref="EventSubscriberAttribute"/> (see also <seealso cref="EventSubscriberOrchestrator.GetSubscribers{TAssembly}(bool)"/>) as scoped services.
        /// </summary>
        /// <typeparam name="TAssembly">The <see cref="Type"/> to infer the underlying <see cref="Type.Assembly"/>.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <param name="includeInternalTypes">Indicates whether to include internally defined types.</param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddEventSubscribers<TAssembly>(this IServiceCollection services, bool includeInternalTypes = false)
        {
            foreach (var type in EventSubscriberOrchestrator.GetSubscribers<TAssembly>(includeInternalTypes))
            {
                CheckServices(services).TryAddScoped(type);
            }

            return services;
        }

        /// <summary>
        /// Adds the <see cref="CoreEx.Text.Json.JsonSerializer"/> as the <see cref="IJsonSerializer"/> and <see cref="CoreEx.Text.Json.ReferenceDataContentJsonSerializer"/> as the <see cref="IReferenceDataContentJsonSerializer"/> singleton services.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddJsonSerializer(this IServiceCollection services)
            => CheckServices(services).AddSingleton<IJsonSerializer, CoreEx.Text.Json.JsonSerializer>()
                                      .AddSingleton<IReferenceDataContentJsonSerializer, CoreEx.Text.Json.ReferenceDataContentJsonSerializer>();

        /// <summary>
        /// Adds the <see cref="CoreEx.Json.Merge.JsonMergePatch"/> as the singleton service.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <param name="configure">The action to enable the <see cref="CoreEx.Json.Merge.JsonMergePatchOptions"/> to be further configured.</param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddJsonMergePatch(this IServiceCollection services, Action<CoreEx.Json.Merge.JsonMergePatchOptions>? configure = null) => CheckServices(services).AddSingleton<IJsonMergePatch>(sp =>
        {
            var jmpo = new CoreEx.Json.Merge.JsonMergePatchOptions(sp.GetService<IJsonSerializer>());
            configure?.Invoke(jmpo);
            return new CoreEx.Json.Merge.JsonMergePatch(jmpo);
        });

        /// <summary>
        /// Adds the <see cref="CoreEx.Text.Json.CloudEventSerializer"/> as the <see cref="IEventSerializer"/> scoped service.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <param name="configure">The action to enable the <see cref="CoreEx.Text.Json.CloudEventSerializer"/> to be further configured.</param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddCloudEventSerializer(this IServiceCollection services, Action<IServiceProvider, CoreEx.Text.Json.CloudEventSerializer>? configure = null) => CheckServices(services).AddScoped<IEventSerializer>(sp =>
        {
            var ces = new CoreEx.Text.Json.CloudEventSerializer(sp.GetService<EventDataFormatter>());
            configure?.Invoke(sp, ces);
            return ces;
        });

        /// <summary>
        /// Adds the <see cref="CoreEx.Text.Json.EventDataSerializer"/> as the <see cref="IEventSerializer"/> scoped service.
        /// </summary>
        /// <param name="configure">The action to enable the <see cref="CoreEx.Text.Json.EventDataSerializer"/> to be further configured.</param>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddEventDataSerializer(this IServiceCollection services, Action<IServiceProvider, CoreEx.Text.Json.EventDataSerializer>? configure = null) => CheckServices(services).AddScoped<IEventSerializer>(sp =>
        {
            var eds = new CoreEx.Text.Json.EventDataSerializer(sp.GetService<IJsonSerializer>(), sp.GetService<EventDataFormatter>());
            configure?.Invoke(sp, eds);
            return eds;
        });

        /// <summary>
        /// Adds the <see cref="EventDataFormatter"/> as the scoped service.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <param name="formatter">The optional <see cref="EventDataFormatter"/>; will default where not specified.</param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddEventDataFormatter(this IServiceCollection services, EventDataFormatter? formatter = null) => CheckServices(services).AddScoped(_ => formatter ?? new EventDataFormatter());

        /// <summary>
        /// Adds the <see cref="FileLockSynchronizer"/> as the <see cref="IServiceSynchronizer"/> singleton service.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddFileLockSynchronizer(this IServiceCollection services) => CheckServices(services).AddSingleton<IServiceSynchronizer, FileLockSynchronizer>();

        /// <summary>
        /// Adds the <see cref="ReferenceDataOrchestrator"/> created by <paramref name="createOrchestrator"/> as a singleton service.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <param name="createOrchestrator">The function to create the <see cref="ReferenceDataOrchestrator"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddReferenceDataOrchestrator(this IServiceCollection services, Func<IServiceProvider, ReferenceDataOrchestrator> createOrchestrator)
            => CheckServices(services).AddSingleton(sp => createOrchestrator(sp));

        /// <summary>
        /// Adds the <see cref="ReferenceDataOrchestrator"/> using a <see cref="MemoryCache"/> as a singleton service automatically registering the <see cref="IReferenceDataProvider"/> (see <see cref="ReferenceDataOrchestrator.Register"/>).
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddReferenceDataOrchestrator(this IServiceCollection services)
            => AddReferenceDataOrchestrator(services, sp => new ReferenceDataOrchestrator(sp).Register());

        /// <summary>
        /// Adds the <see cref="ReferenceDataOrchestrator"/> using a <see cref="MemoryCache"/> as a singleton service automatically registering the specified <typeparamref name="TProvider"/> (see <see cref="ReferenceDataOrchestrator.Register"/>).
        /// </summary>
        /// <typeparam name="TProvider">The <see cref="IReferenceDataProvider"/> to register.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddReferenceDataOrchestrator<TProvider>(this IServiceCollection services) where TProvider : IReferenceDataProvider
            => AddReferenceDataOrchestrator(services, sp => new ReferenceDataOrchestrator(sp).Register<TProvider>());

        /// <summary>
        /// Adds the <see cref="RequestCache"/> as the <see cref="IRequestCache"/> scoped service.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddRequestCache(this IServiceCollection services) => CheckServices(services).AddScoped<IRequestCache>(_ => new RequestCache());

        /// <summary>
        /// Registers all the <see cref="IMapper{TSource, TDestination}"/>(s) from the specified <typeparamref name="TAssembly"/> into a new <see cref="Mapper"/> that is then registered as a singleton service.
        /// </summary>
        /// <typeparam name="TAssembly">The <see cref="Type"/> to infer the underlying <see cref="Type.Assembly"/>.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/> for fluent-style method-chaining.</returns>
        public static IServiceCollection AddMappers<TAssembly>(this IServiceCollection services)
            => AddMappers(services, new Assembly[] { typeof(TAssembly).Assembly });

        /// <summary>
        /// Registers all the <see cref="IMapper{TSource, TDestination}"/>(s) from the specified <paramref name="assemblies"/> into a new <see cref="Mapper"/> that is then registered as a singleton service.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <param name="assemblies">The assemblies to probe for mappers.</param>
        /// <returns>The <see cref="IServiceCollection"/> for fluent-style method-chaining.</returns>
        public static IServiceCollection AddMappers(this IServiceCollection services, params Assembly[] assemblies)
        {
            var mapper = new Mapper();
            var mi = typeof(Mapper).GetMethod(nameof(Mapper.Register), System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)!;

            foreach (var assembly in assemblies.Distinct())
            {
                foreach (var match in from type in assembly.GetTypes()
                                      where !type.IsAbstract && !type.IsGenericTypeDefinition
                                      let interfaces = type.GetInterfaces()
                                      let genericInterfaces = interfaces.Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IMapper<,>))
                                      let @interface = genericInterfaces.FirstOrDefault()
                                      let sourceType = @interface?.GetGenericArguments().Length == 2 ? @interface?.GetGenericArguments()[0] : null
                                      let destinationType = @interface?.GetGenericArguments().Length == 2 ? @interface?.GetGenericArguments()[1] : null
                                      where @interface != null
                                      select new { type, sourceType, destinationType })
                {
                    mi.MakeGenericMethod(match.sourceType, match.destinationType).Invoke(mapper, new object[] { Activator.CreateInstance(match.type)! });
                }
            }

            return services.AddSingleton<IMapper>(mapper);
        }
    }
}