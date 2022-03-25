using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus.Administration;
using CoreEx.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CoreEx.Messaging.Azure.Health
{

    /// <summary> Health check for Azure Service Bus Queue. </summary>
    public class AzureServiceBusQueueHealthCheck : AzureServiceHealthCheckBase
    {
        private readonly string _queueName;
        private readonly string _queueSettingName;

        /// <summary> constructor. </summary>
        /// <remarks> Note that constructor takes setting NAMES not values, values are looked up from <paramref name="settings"/>. </remarks>
        public AzureServiceBusQueueHealthCheck(SettingsBase settings, string connectionName, string queueSettingName)
        : base(settings, connectionName)
        {
            _queueName = settings.GetValue<string>(queueSettingName);
            _queueSettingName = queueSettingName;
        }

        /// <inheritdoc/>
        protected override async Task<HealthCheckResult> CheckServiceBusHealthAsync(ServiceBusAdministrationClient managementClient, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(_queueName))
            {
                return HealthCheckResult.Unhealthy($"Queue name is not configured under '{_queueSettingName}' in settings");
            }

            _ = await managementClient.GetQueueRuntimePropertiesAsync(_queueName, cancellationToken);

            return HealthCheckResult.Healthy();
        }
    }
}