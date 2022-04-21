// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx;
using CoreEx.Configuration;
using CoreEx.Entities;
using CoreEx.Events;
using CoreEx.Hosting;
using CoreEx.Http;
using CoreEx.Json;
using CoreEx.Json.Merge;
using CoreEx.WebApis;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net.Http;

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
        /// Adds a <see cref="TypedHttpClientBase"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="TypedHttpClientBase"/> <see cref="Type"/>.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <param name="name">The logical name of the <see cref="HttpClient"/> to configure.</param>
        /// <param name="configure">The delegate to configure the underlying <see cref="HttpClient"/>.</param>
        public static IServiceCollection AddTypedHttpClient<T>(this IServiceCollection services, string name, Action<IServiceProvider, HttpClient>? configure = null) where T : TypedHttpClientBase
        {
            if (configure == null)
                services.AddHttpClient<T>(name);
            else
                services.AddHttpClient<T>(name, configure);

            return services;
        }

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
        /// Adds the <see cref="CoreEx.Text.Json.JsonSerializer"/> as the <see cref="IJsonSerializer"/> singleton service.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddJsonSerializer(this IServiceCollection services) => CheckServices(services).AddSingleton<IJsonSerializer, CoreEx.Text.Json.JsonSerializer>();

        /// <summary>
        /// Adds the <see cref="System.Text.Json.JsonSerializerOptions"/> as the singleton service.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <param name="options">The <see cref="System.Text.Json.JsonSerializerOptions"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddJsonSerializerOptions(this IServiceCollection services, System.Text.Json.JsonSerializerOptions options) => CheckServices(services).AddSingleton(_ => options ?? throw new ArgumentNullException(nameof(options)));

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
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddCloudEventSerializer(this IServiceCollection services) => CheckServices(services).AddScoped<IEventSerializer, CoreEx.Text.Json.CloudEventSerializer>();

        /// <summary>
        /// Adds the <see cref="CoreEx.Text.Json.EventDataSerializer"/> as the <see cref="IEventSerializer"/> scoped service.
        /// </summary>
        /// <param name="configure">The action to enable the <see cref="CoreEx.Text.Json.EventDataSerializer"/> to be further configured.</param>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddEventDataSerializer(this IServiceCollection services, Action<CoreEx.Text.Json.EventDataSerializer>? configure = null) => CheckServices(services).AddScoped<IEventSerializer>(sp =>
        {
            var eds = new CoreEx.Text.Json.EventDataSerializer(sp.GetService<IJsonSerializer>(), sp.GetService<EventDataFormatter>());
            configure?.Invoke(eds);
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
        /// Adds the <see cref="WebApi"/> as a scoped service.
        /// </summary>
        /// <param name="configure">The action to enable the <see cref="WebApi"/> to be further configured.</param>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddWebApi(this IServiceCollection services, Action<WebApi>? configure = null) => CheckServices(services).AddScoped(sp =>
        {
            var wa = new WebApi(sp.GetRequiredService<ExecutionContext>(), sp.GetRequiredService<SettingsBase>(), sp.GetRequiredService<IJsonSerializer>(), sp.GetRequiredService<ILogger<WebApi>>(), sp.GetService<IJsonMergePatch>());
            configure?.Invoke(wa);
            return wa;
        });

        /// <summary>
        /// Adds the <see cref="WebApiPublisher"/> as a scoped service.
        /// </summary>
        /// <param name="configure">The action to enable the <see cref="WebApi"/> to be further configured.</param>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddWebApiPublisher(this IServiceCollection services, Action<WebApiPublisher>? configure = null) => CheckServices(services).AddScoped(sp =>
        {
            var wap = new WebApiPublisher(sp.GetRequiredService<IEventPublisher>(), sp.GetRequiredService<ExecutionContext>(), sp.GetRequiredService<SettingsBase>(), sp.GetRequiredService<IJsonSerializer>(), sp.GetRequiredService<ILogger<WebApiPublisher>>());
            configure?.Invoke(wap);
            return wap;
        });
    }
}