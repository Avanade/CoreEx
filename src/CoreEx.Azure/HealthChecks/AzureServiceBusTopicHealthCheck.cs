using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus.Administration;
using CoreEx.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CoreEx.Azure.HealthChecks
{
    /// <summary> Health check for Azure Service Bus Topic. </summary>
    /// <remarks> Check doesn't verify permissions to Send/Receive. <br/>
    /// To use this health check, add the following to the HealthCheckBuilder.
    /// Make sure to use **NAME** of the settings, not values themselves.
    /// <code>
    /// builder.Services
    ///     .AddHealthChecks()
    ///     .AddTypeActivatedCheck{AzureServiceBusTopicHealthCheck}(
    ///        "Check", 
    ///        HealthStatus.Unhealthy, 
    ///        nameof(Settings.ServiceBusConnection__fullyQualifiedNamespace), 
    ///        nameof(Settings.TopicName),
    ///        nameof(Settings.SubscriptionName)
    ///     );
    /// </code>
    /// </remarks>
    public class AzureServiceBusTopicHealthCheck(SettingsBase settings, string connectionName, string topicSettingName, string subscriptionSettingName) : AzureServiceHealthCheckBase(settings, connectionName)
    {
        private readonly string _topicName = settings.GetValue<string>(topicSettingName);
        private readonly string _subscriptionName = settings.GetValue<string>(subscriptionSettingName);
        private readonly string _topicSettingName = topicSettingName;
        private readonly string _subscriptionSettingName = subscriptionSettingName;

        /// <inheritdoc/>
        protected override async Task<HealthCheckResult> CheckServiceBusHealthAsync(ServiceBusAdministrationClient managementClient, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(_topicName))
                return HealthCheckResult.Unhealthy($"Topic name is not configured under '{_topicSettingName}' in settings");

            if (string.IsNullOrEmpty(_subscriptionName))
                return HealthCheckResult.Unhealthy($"Subscription name is not configured under '{_subscriptionSettingName}' in settings");

            _ = await managementClient.GetSubscriptionRuntimePropertiesAsync(_topicName, _subscriptionName, cancellationToken).ConfigureAwait(false);

            return HealthCheckResult.Healthy();
        }
    }
}