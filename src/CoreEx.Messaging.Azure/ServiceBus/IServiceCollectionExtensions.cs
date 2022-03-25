// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using Azure.Identity;
using CoreEx.Configuration;
using Microsoft.Extensions.Azure;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Provides <see cref="IServiceCollection"/> extensions.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the Azure Service Bus Client.
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
    }
}