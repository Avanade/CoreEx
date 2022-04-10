using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus.Administration;
using CoreEx.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CoreEx.Messaging.Azure.Health
{

    /// <summary> Health check for Azure Service Bus Queue. </summary>
    /// <remarks> Check doesn't verify permissions to Send/Receive. <br/>
    /// To use this health check, add the following to the HealthCheckBuilder.
    /// Make sure to use **NAME** of the settings, not values themselves.
    /// <code>
    /// builder.Services
    ///     .AddHealthChecks()
    ///     .AddTypeActivatedCheck{AzureServiceBusQueueHealthCheck}(
    ///        "Check", 
    ///        HealthStatus.Unhealthy, 
    ///        nameof(Settings.ServiceBusConnection__fullyQualifiedNamespace), 
    ///        nameof(Settings.QueueName)
    ///     );
    /// </code>
    /// </remarks>
    public class AzureServiceBusQueueHealthCheck : AzureServiceHealthCheckBase
    {
        private readonly string _queueName;
        private readonly string _queueSettingName;

        /// <summary> constructor. </summary>
        /// <remarks> Note that constructor takes setting NAMES not values, values are looked up from <paramref name="settings"/>. </remarks>
        public AzureServiceBusQueueHealthCheck(SettingsBase settings, string connectionName, string queueSettingName)
        : base(settings, connectionName)
        {
            _queueName = settings.GetSettingValue<string>(queueSettingName);
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