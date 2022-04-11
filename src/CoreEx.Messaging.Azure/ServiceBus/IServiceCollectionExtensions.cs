// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using Azure.Identity;
using CoreEx;
using CoreEx.Configuration;
using CoreEx.Events;
using CoreEx.Messaging.Azure.ServiceBus;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;
using System;
using Asb = Azure.Messaging.ServiceBus;

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
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddAzureServiceBusClient(this IServiceCollection services, string connectionName = "ServiceBusConnection")
        {
            services.AddAzureClients(cb =>
            {
                var config = services.BuildServiceProvider().GetRequiredService<SettingsBase>();
                var sbcs = config.GetValue<string>(connectionName);

                if (string.IsNullOrEmpty(sbcs))
                    throw new InvalidOperationException(@$"The Azure Service Bus connection string is not configured; the configuration setting '{connectionName}' is either not specified or does not have a value.");

                if (sbcs.Contains("SharedAccessKey=", StringComparison.OrdinalIgnoreCase))
                    cb.AddServiceBusClient(sbcs); // Connect to Azure Service Bus with secret.
                else
                    cb.AddServiceBusClientWithNamespace(sbcs).WithCredential(new DefaultAzureCredential()); // Connect to Azure Service Bus with managed identity.
            });

            return services;
        }

        /// <summary>
        /// Adds the Azure <see cref="ServiceBusSubscriber"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddAzureServiceBusSubscriber(this IServiceCollection services) => services.AddScoped<ServiceBusSubscriber>();

        /// <summary>
        /// Adds the <see cref="ServiceBusSender"/> as the <see cref="IEventSender"/> scoped service.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <param name="configure">The action to enable the <see cref="ServiceBusSender"/> to be further configured.</param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddAzureServiceBusSender(this IServiceCollection services, Action<IServiceProvider, ServiceBusSender>? configure = null) => services.AddScoped<IEventSender>(sp =>
        {
            var sbs = new ServiceBusSender(sp.GetRequiredService<Asb.ServiceBusClient>(), sp.GetRequiredService<ExecutionContext>(), sp.GetRequiredService<SettingsBase>(), sp.GetRequiredService<ILogger<ServiceBusSender>>());
            configure?.Invoke(sp, sbs);
            return sbs;
        });
    }
}