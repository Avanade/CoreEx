# Health checks

CoreEx library supports building health check endpoints, that validate all function dependencies in a safe way, that is safe to execute on any environment.

Feature utilizes built-in health check capabilities provided by Microsoft in `Microsoft.Extensions.Diagnostics.HealthChecks`.

To add a health check add following implementation of HTTP triggered function:

```csharp
public class HttpHealthFunction
{
    private readonly HealthService _health;

    public HttpHealthFunction(HealthService health)
    {
        _health = health;
    }

    [FunctionName("HealthInfo")]
    public async Task<IActionResult> RunAsync([HttpTrigger(AuthorizationLevel.Function, "get", Route = "health")] HttpRequest req)
        => await _health.RunAsync().ConfigureAwait(false);
}
```

Health checks are added to dependency injection container during startup:

```csharp
builder.Services
    .AddScoped<HealthService>()
    .AddHealthChecks()
    .AddHttpHealthCheck<SampleApiClient>("SampleApi")
    .AddSqlServerHealthCheck($"SQL Health check for Azure SQL Server", "SqlConnection");
```

In order for health checks to be reliable, it's recommended for checks and functions to use SAME configuration entries. This can be enforced by avoiding so-called magic strings in trigger definitions and using references to settings instead, e.g. `[ServiceBusTrigger("%" + nameof(SampleSettings.QueueName) + "%", Connection = "ServiceBusConnection")]` and `_executor.RunBatchPublishAsync<List<Product>, Product, ProductsValidator>(req, _settings.QueueName, _publisher, (m, _) => m.Subject = "TEST", maxBatchSize: 5);`

It's important to note, that both common-provided registrations and native `IHealthCheck` implementations are equally supported.

Once health checks are added, following information can be read from `/api/health` endpoint:

```json
{
  "healthReport": {
    "Entries": {
      "Health check for Service Bus trigger (inbound) connection": {
        "Data": {},
        "Description": null,
        "Duration": "00:00:00.5175402",
        "Exception": null,
        "Status": "Healthy",
        "Tags": []
      },
      "Health check for Service Bus publisher (outbound) connection": {
        "Data": {},
        "Description": null,
        "Duration": "00:00:00.6361042",
        "Exception": null,
        "Status": "Healthy",
        "Tags": []
      },
      "D365": {
        "Data": {},
        "Description": null,
        "Duration": "00:00:00.0870336",
        "Exception": null,
        "Status": "Healthy",
        "Tags": []
      },
      "SampleApi": {
        "Data": {},
        "Description": null,
        "Duration": "00:00:00.0526695",
        "Exception": null,
        "Status": "Healthy",
        "Tags": []
      },
      "SQL Health check for Azure SQL Server": {
        "Data": {},
        "Description": null,
        "Duration": "00:00:00.8335928",
        "Exception": null,
        "Status": "Healthy",
        "Tags": []
      }
    },
    "Status": "Unhealthy",
    "TotalDuration": "00:00:01.8493026"
  },
  "Deployment": {
    "By": "me",
    "Build": "build no",
    "Name": "my deployment",
    "Version": "1.0.0",
    "Date": "today"
  }
}
```
