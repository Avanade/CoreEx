# Health checks

The `CoreEx.HealthChecks` namespace provides standardized health check capabilities.

<br/>

## Motivation

To enable, and provide a standardized pattern, for the production of health checks for an underlying application.
 
<br/>

## Usage

The _CoreEx_ framework supports building health check endpoints, that validate all function dependencies in a safe way, that is safe to execute within any environment.

Feature utilizes built-in health check capabilities provided by Microsoft in `Microsoft.Extensions.Diagnostics.HealthChecks`.

To add a health check, add following implementation of HTTP triggered function:

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

In order for health checks to be reliable, it is recommended for checks and functions to use the _same_ configuration entries. This can be enforced by avoiding so-called magic strings in trigger definitions and using references to settings instead, e.g.

``` csharp
[ServiceBusTrigger("%" + nameof(SampleSettings.QueueName) + "%", Connection = "ServiceBusConnection")]` 

...

_executor.RunBatchPublishAsync<List<Product>, Product, ProductsValidator>(req, _settings.QueueName, _publisher, (m, _) => m.Subject = "TEST", maxBatchSize: 5);
```

It's important to note, that both common-provided registrations and native `IHealthCheck` implementations are equally supported.

Once health checks are added, the following information can be read from `/api/health` endpoint:

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

<br/>

## Open Source Health Check implementations

There are open-source implementations for popular health checks; for example, [AspNetCore.Diagnostics.HealthChecks](https://github.com/Xabaril/AspNetCore.Diagnostics.HealthChecks) which can be utilized in CoreEx projects.

<br/>

## HttpClient considerations

By default `HttpClient` will use **`HEAD`** request for health checks. It is important that the HTTP call does not affect state; hence, `Task<HttpResult> HealthCheckAsync()` can be overridden and customized for specific scenarios (different HTTP methods, parameters, etc.).

```csharp
public override Task<HttpResult> HealthCheckAsync()
{
    return base.HeadAsync("/health", null, null, new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token);
}
```
