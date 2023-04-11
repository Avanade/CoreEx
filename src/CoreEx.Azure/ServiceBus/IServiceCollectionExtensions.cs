﻿// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using Azure.Identity;
using CoreEx;
using CoreEx.Azure.ServiceBus;
using CoreEx.Configuration;
using CoreEx.Events;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;
using Asb = Azure.Messaging.ServiceBus;
using Ace = Azure.Core.Extensions;
using CoreEx.Events.Subscribing;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Provides <see cref="IServiceCollection"/> extensions.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the Azure <see cref="Asb.ServiceBusClient"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <param name="connectionName">The connection <see cref="SettingsBase">configuration setting</see> name.</param>
        /// <param name="configure">Adds a delegate to configure the client options.</param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddAzureServiceBusClient(this IServiceCollection services, string connectionName = "ServiceBusConnection", Action<Asb.ServiceBusClientOptions, IServiceProvider>? configure = null)
        {
            services.AddAzureClients(cb =>
            {
                var config = services.BuildServiceProvider().GetRequiredService<SettingsBase>();
                var sbcs = config.GetValue<string>(connectionName);

                if (string.IsNullOrEmpty(sbcs))
                    throw new InvalidOperationException(@$"The Azure Service Bus connection string is not configured; the configuration setting '{connectionName}' is either not specified or does not have a value.");

                Ace.IAzureClientBuilder<Asb.ServiceBusClient, Asb.ServiceBusClientOptions> acb;
                if (sbcs.Contains("SharedAccessKey=", StringComparison.OrdinalIgnoreCase))
                    acb = cb.AddServiceBusClient(sbcs); // Connect to Azure Service Bus with secret.
                else
                    acb = cb.AddServiceBusClientWithNamespace(sbcs).WithCredential(new DefaultAzureCredential()); // Connect to Azure Service Bus with managed identity.

                if (configure != null)
                    acb.ConfigureOptions(configure);
            });

            return services;
        }

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
            var sbs = new ServiceBusSender(sp.GetRequiredService<Asb.ServiceBusClient>(), sp.GetRequiredService<ExecutionContext>(), sp.GetRequiredService<SettingsBase>(), sp.GetRequiredService<ILogger<ServiceBusSender>>());
            var sbp = new ServiceBusPurger(sp.GetRequiredService<Asb.ServiceBusClient>(), sp.GetRequiredService<SettingsBase>(), sp.GetRequiredService<ILogger<ServiceBusPurger>>());
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
    }
}