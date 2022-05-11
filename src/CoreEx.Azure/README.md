# About

tbd

## Health Checks

Popular health check library [AspNetCore.Diagnostics.HealthChecks](https://github.com/Xabaril/AspNetCore.Diagnostics.HealthChecks) provides checks for azure service bus.

CoreEx.Azure has two checks, which use `SettingsBase` class for reading configuration:

* `CoreEx.Azure.Health.AzureServiceBusQueueHealthCheck`
* `CoreEx.Azure.Health.AzureServiceBusTopicHealthCheck`

which can be registered using following code:

```csharp
builder.Services
    .AddScoped<HealthService>()
    .AddHealthChecks()
    .AddServiceBusQueueHealthCheck("Health check for Service Bus trigger (inbound) connection", nameof(SampleSettings.ServiceBusConnection__fullyQualifiedNamespace), nameof(SampleSettings.QueueName))
    .AddServiceBusQueueHealthCheck("Health check for Service Bus publisher (outbound) connection", nameof(SampleSettings.PublisherServiceBusConnection), nameof(SampleSettings.QueueName))
```
