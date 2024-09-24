// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx;
using CoreEx.Azure.ServiceBus;
using CoreEx.Azure.ServiceBus.HealthChecks;
using CoreEx.Configuration;
using CoreEx.Events;
using CoreEx.Events.Subscribing;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using Asb = Azure.Messaging.ServiceBus;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Provides <see cref="IServiceCollection"/> extensions.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the Azure <see cref="ServiceBusSubscriber"/> as a scoped service.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <param name="configure">The action to enable the <see cref="ServiceBusSubscriber"/> to be further configured.</param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddAzureServiceBusSubscriber(this IServiceCollection services, Action<IServiceProvider, ServiceBusSubscriber>? configure = null) => services.AddScoped<ServiceBusSubscriber>(sp =>
        {
            var sbs = new ServiceBusSubscriber(sp.GetRequiredService<ExecutionContext>(), sp.GetRequiredService<SettingsBase>(), sp.GetRequiredService<ILogger<ServiceBusSubscriber>>(), sp.GetService<EventSubscriberInvoker>(), sp.GetService<ServiceBusSubscriberInvoker>(), sp.GetService<IEventDataConverter<Asb.ServiceBusReceivedMessage>>(), sp.GetService<IEventSerializer>());
            configure?.Invoke(sp, sbs);
            return sbs;
        });

        /// <summary>
        /// Adds the Azure <see cref="ServiceBusOrchestratedSubscriber"/> as a scoped service.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <param name="configure">The action to enable the <see cref="ServiceBusOrchestratedSubscriber"/> to be further configured.</param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddAzureServiceBusOrchestratedSubscriber(this IServiceCollection services, Action<IServiceProvider, ServiceBusOrchestratedSubscriber>? configure = null) => services.AddScoped<ServiceBusOrchestratedSubscriber>(sp =>
        {
            var sbos = new ServiceBusOrchestratedSubscriber(sp.GetRequiredService<EventSubscriberOrchestrator>(), sp.GetRequiredService<ExecutionContext>(), sp.GetRequiredService<SettingsBase>(), sp.GetRequiredService<ILogger<ServiceBusSubscriber>>(), sp.GetService<EventSubscriberInvoker>(), sp.GetService<ServiceBusSubscriberInvoker>(), sp.GetService<IEventDataConverter<Asb.ServiceBusReceivedMessage>>(), sp.GetService<IEventSerializer>());
            configure?.Invoke(sp, sbos);
            return sbos;
        });

        /// <summary>
        /// Adds the Azure <see cref="ServiceBusReceivedMessageEventDataConverter"/> as the <see cref="IEventDataConverter"/> scoped service.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddAzureServiceBusReceivedMessageConverter(this IServiceCollection services) => services.AddScoped<IEventDataConverter, ServiceBusReceivedMessageEventDataConverter>();

        /// <summary>
        /// Adds the <see cref="ServiceBusSender"/> as the <see cref="IEventSender"/> scoped service.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <param name="configure">The action to enable the <see cref="ServiceBusSender"/> to be further configured.</param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddAzureServiceBusSender(this IServiceCollection services, Action<IServiceProvider, ServiceBusSender>? configure = null) => services.AddScoped<IEventSender>(sp =>
        {
            var sbs = new ServiceBusSender(sp.GetRequiredService<Asb.ServiceBusClient>(), sp.GetRequiredService<SettingsBase>(), sp.GetRequiredService<ILogger<ServiceBusSender>>());
            configure?.Invoke(sp, sbs);
            return sbs;
        });

        /// <summary>
        /// Adds the <see cref="ServiceBusPurger"/> as the <see cref="IEventSender"/> scoped service.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <param name="configure">The action to enable the <see cref="ServiceBusPurger"/> to be further configured.</param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddAzureServiceBusPurger(this IServiceCollection services, Action<IServiceProvider, ServiceBusPurger>? configure = null) => services.AddScoped<IEventPurger>(sp =>
        {
            var sbp = new ServiceBusPurger(sp.GetRequiredService<Asb.ServiceBusClient>(), sp.GetRequiredService<SettingsBase>(), sp.GetRequiredService<ILogger<ServiceBusPurger>>());
            configure?.Invoke(sp, sbp);
            return sbp;
        });

        /// <summary>
        /// Adds a <see cref="ServiceBusReceiverHealthCheck"/> that will peek a message from the Azure Service Bus receiver to confirm health.
        /// </summary>
        /// <param name="builder">The <see cref="IHealthChecksBuilder"/>.</param>
        /// <param name="name">The health check name. Defaults to '<c>azure-service-bus-receiver</c>'.</param>
        /// <param name="serviceBusReceiverFactory">The <see cref="Asb.ServiceBusReceiver"/> factory.</param>
        /// <param name="failureStatus">The <see cref="HealthStatus"/> that should be reported when the health check reports a failure. If the provided value is <c>null</c>, then <see cref="HealthStatus.Unhealthy"/> will be reported.</param>
        /// <param name="tags">A list of tags that can be used for filtering health checks.</param>
        /// <param name="timeout">An optional <see cref="TimeSpan"/> representing the timeout of the check.</param>
        public static IHealthChecksBuilder AddServiceBusReceiverHealthCheck(this IHealthChecksBuilder builder, Func<IServiceProvider, Asb.ServiceBusReceiver> serviceBusReceiverFactory, string? name = null, HealthStatus? failureStatus = default, IEnumerable<string>? tags = default, TimeSpan? timeout = default)
        {
            serviceBusReceiverFactory.ThrowIfNull(nameof(serviceBusReceiverFactory));

            return builder.Add(new HealthCheckRegistration(name ?? "azure-service-bus-receiver", sp =>
            {                 
                return new ServiceBusReceiverHealthCheck(() => serviceBusReceiverFactory(sp));
            }, failureStatus, tags, timeout));
        }
    }
}