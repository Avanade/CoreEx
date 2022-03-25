using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus.Administration;
using CoreEx.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CoreEx.Messaging.Azure.Health
{

    /// <summary> Health check for Azure Service Bus Topic. </summary>
    /// <remarks> Check doesn't verify permissions to Send/Receive. <br/>
    /// To use this health check, add the following to the <see cref="HealthCheckBuilder"/>.
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
    public class AzureServiceBusTopicHealthCheck : AzureServiceHealthCheckBase
    {
        private readonly string _topicName;
        private readonly string _subscriptionName;
        private readonly string _topicSettingName;
        private readonly string _subscriptionSettingName;

        /// <summary> constructor. </summary>
        /// <remarks> Note that constructor takes setting NAMES not values, values are looked up from <paramref name="settings"/>. </remarks>
        public AzureServiceBusTopicHealthCheck(SettingsBase settings, string connectionName, string topicSettingName, string subscriptionSettingName)
        : base(settings, connectionName)
        {
            _topicName = settings.GetValue<string>(topicSettingName);
            _subscriptionName = settings.GetValue<string>(subscriptionSettingName);
            _topicSettingName = topicSettingName;
            _subscriptionSettingName = subscriptionSettingName;
        }

        /// <inheritdoc/>
        protected override async Task<HealthCheckResult> CheckServiceBusHealthAsync(ServiceBusAdministrationClient managementClient, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(_topicName))
            {
                return HealthCheckResult.Unhealthy($"Topic name is not configured under '{_topicSettingName}' in settings");
            }

            if (string.IsNullOrEmpty(_subscriptionName))
            {
                return HealthCheckResult.Unhealthy($"Subscription name is not configured under '{_subscriptionSettingName}' in settings");
            }

            _ = await managementClient.GetSubscriptionRuntimePropertiesAsync(_topicName, _subscriptionName, cancellationToken);

            return HealthCheckResult.Healthy();
        }
    }
}