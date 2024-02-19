using System;
using Azure.Identity;
using Microsoft.Extensions.Configuration;

namespace CoreEx.Azure.AppConfig;

/// <summary>
/// Extensions for using Azure App Configuration Service
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Adds the Azure App Configuration provider for services using configuration builder
    /// </summary>
    /// <param name="builder">The <see cref="IConfigurationBuilder"/>.</param>
    /// <param name="connectionName">Name of the connection to use for Azure App Configuration in local configuration</param>
    /// <param name="labelFilter">The label filter to use for Azure App Configuration in local configuration</param>
    /// <param name="keyPrefixes">The application configuration key filters.</param>
    /// <returns>The <see cref="IConfigurationBuilder"/> instance to support fluent-style method-chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown when configuration value for provided key <param ref="AppConfigConnectionString"/> doesn't exist.</exception>
    /// <remarks>Use "UseLocal=true" value for app configuration connection string to skip connecting to Azure Service and use local values only.</remarks>
    /// <remarks>To use with Azure functions call it with <code>IFunctionsConfigurationBuilder.ConfigurationBuilder</code></remarks>
    public static IConfigurationBuilder AddAzureAppConfiguration(this IConfigurationBuilder builder, string connectionName = "AppConfigConnectionString", string? labelFilter = null, params string[] keyPrefixes)
    {
        // build configuration and get connection string to Azure App Configuration (this most likely will be environment variable set in app service)
        var config = builder.ThrowIfNull(nameof(builder)).Build();
        var accs = config.GetValue<string?>(connectionName);

        if (string.IsNullOrEmpty(accs))
            throw new InvalidOperationException(@$"{nameof(AddAzureAppConfiguration)}: {connectionName} setting not found. If Azure App Service Configuration is not needed - 
                    do not call {nameof(AddAzureAppConfiguration)}. For local development or unit testing, use ""UseLocal=true"" in the configuration.
                    Custom connection string name can be provided by calling {nameof(AddAzureAppConfiguration)}(connectionName: ""AzureAppConfigurationConnection"").");

        if (accs.Equals("UseLocal=true", StringComparison.OrdinalIgnoreCase))
        {
            System.Diagnostics.Debug.WriteLine("Using local configuration because UseLocal=true was set in Azure App Configuration connection string.");
            return builder;
        }

        builder.AddAzureAppConfiguration(o =>
        {
            if (accs.Contains(";Secret=", StringComparison.OrdinalIgnoreCase))
            {
                // connect to Azure App Configuration with secret
                o.Connect(accs);
            }
            else
            {
                // connect to Azure App Configuration with managed identity
                o.Connect(new Uri(accs), new DefaultAzureCredential())
                // since managed identity is used - let it resolve keyvault secrets
                .ConfigureKeyVault(kv =>
                {
                    kv.SetCredential(new DefaultAzureCredential());
                });
            }

            foreach (var prefix in keyPrefixes)
            {
                // note: azure app config doesn't like null/empty
                o.Select(prefix + "*");

                // if label filter is provided - use it, after default keys are loaded
                // the idea is that labeled values override some (or all) values without label
                if (!string.IsNullOrEmpty(labelFilter))
                    o.Select(prefix + "*", labelFilter);
            }
        });

        return builder;
    }
}