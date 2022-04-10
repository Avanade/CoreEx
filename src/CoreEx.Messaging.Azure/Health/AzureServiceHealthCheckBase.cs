using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Messaging.ServiceBus.Administration;
using CoreEx.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CoreEx.Messaging.Azure.Health
{
    /// <summary> Base Health check class for Azure Service Bus health checks. </summary>
    public abstract class AzureServiceHealthCheckBase : IHealthCheck
    {
        /// <summary> Management connections used by health checks. </summary>
        public static readonly ConcurrentDictionary<string, ServiceBusAdministrationClient> ManagementClientConnections = new ConcurrentDictionary<string, ServiceBusAdministrationClient>();
        private readonly string _endPoint;
        private readonly string _connectionName;

        private string ConnectionKey => $"{_endPoint}";

        /// <summary> constructor. </summary>
        /// <remarks> Note that constructor takes setting NAMES not values, values are looked up from <paramref name="settings"/>. </remarks>
        public AzureServiceHealthCheckBase(SettingsBase settings, string connectionName)
        {
            _endPoint = settings.GetSettingValue<string>(connectionName);
            _connectionName = connectionName;
        }

        /// <inheritdoc/>
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(_endPoint))
            {
                return HealthCheckResult.Unhealthy($"Service bus connection is not configured under '{_connectionName}' in settings");
            }

            try
            {
                var managementClient = ManagementClientConnections.GetOrAdd(ConnectionKey, key => CreateManagementClient());

                if (managementClient == null)
                {
                    return new HealthCheckResult(context.Registration.FailureStatus, description: "No service bus administration client connection can't be added into dictionary.");
                }

                return await CheckServiceBusHealthAsync(managementClient, cancellationToken);

            }
            catch (Exception ex)
            {
                return new HealthCheckResult(context.Registration.FailureStatus, exception: ex);
            }
        }

        /// <summary> Check Azure Service Bus health. </summary>
        protected abstract Task<HealthCheckResult> CheckServiceBusHealthAsync(ServiceBusAdministrationClient managementClient, CancellationToken cancellationToken);

        private ServiceBusAdministrationClient CreateManagementClient()
        {
            ServiceBusAdministrationClient managementClient;
            if (_endPoint.Contains("SharedAccessKey=", StringComparison.OrdinalIgnoreCase))
            {
                managementClient = new ServiceBusAdministrationClient(_endPoint);
            }
            else
            {
                managementClient = new ServiceBusAdministrationClient(_endPoint, new DefaultAzureCredential());
            }

            return managementClient;
        }
    }
}