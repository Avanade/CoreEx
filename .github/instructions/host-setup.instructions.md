---
applyTo: "**/Program.cs"
description: "Host setup conventions for Program.cs: API host, Subscribe host, middleware, service registration, and distributed caching"
tags: ["program-cs", "host-setup", "middleware", "dependency-registration", "caching"]
---

# Host Setup Conventions (Program.cs)

## NuGet / Project References by Host Type

### API Host

| Package | Key registrations |
|---|---|
| `CoreEx.AspNetCore` | `AddMvcWebApi()`, `AddHttpWebApi()`, `AddExecutionContext()`, `UseCoreExExceptionHandler()`, `UseExecutionContext()`, `UseIdempotencyKey()`, `MapHealthChecks()` |
| `CoreEx.AspNetCore.NSwag` | `AddOpenApiDocument()`, `AddCoreExConfiguration()`, `UseOpenApi()`, `UseSwaggerUi()` |
| `CoreEx.Caching.FusionCache` | `AddFusionCache()`, `AddFusionHybridCache()`, `AddDefaultCacheKeyProvider()`, `AddHybridCacheIdempotencyProvider()` |
| `CoreEx.Database.SqlServer` | `AddSqlServerDatabase()`, `AddSqlServerUnitOfWork()`, `AddSqlServerOutboxPublisher<T>()`, `AddSqlServerClient("SqlServer")` |
| `CoreEx.EntityFrameworkCore` | `AddDbContext<T>()`, `AddEfDb<T>()` |
| `CoreEx.Events` | `AddEventFormatter()` |
| `CoreEx.RefData` | `AddReferenceDataOrchestrator<T>()` |
| `Aspire.StackExchange.Redis.DistributedCaching` | `AddRedisDistributedCache("redis")` |
| `FusionCache.Backplane.StackExchangeRedis` | `RedisBackplane`, `RedisBackplaneOptions` |
| `OpenTelemetry.*` | `WithCoreExTelemetry()`, `WithCoreExSqlServerTelemetry()`, `UseOtlpExporter()` |

### Subscribe Host

All of the above **plus**:

| Package | Key registrations |
|---|---|
| `CoreEx.Azure.Messaging.ServiceBus` | `AddAzureServiceBusClient("ServiceBus")`, `AddSubscribedManager()`, `AzureServiceBusReceiving()`, `AddHostedServiceManager()`, `MapHostedServices()`, `WithCoreExServiceBusTelemetry()` |

### Outbox Relay Host

| Package | Key registrations |
|---|---|
| `CoreEx.AspNetCore` | `AddMvcWebApi()`, `AddHttpWebApi()`, `AddExecutionContext()`, `UseCoreExExceptionHandler()` |
| `CoreEx.Database.SqlServer` | `AddSqlServerDatabase()`, `AddSqlServerUnitOfWork()`, `AddSqlServerOutboxRelay()`, `AddSqlServerOutboxRelayHostedService()` |
| `CoreEx.Azure.Messaging.ServiceBus` | `AddAzureServiceBusClient()`, `AddAzureServiceBusPublisher()`, `ServiceBusSessionStrategy` |
| `OpenTelemetry.*` | `WithCoreExTelemetry()`, `WithCoreExSqlServerTelemetry()`, `WithCoreExServiceBusTelemetry()`, `UseOtlpExporter()` |

---

There are three host types in a CoreEx solution. Each follows the same skeleton but adds type-specific registrations.

---

## Shared Skeleton (All Host Types)

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddHostSettings();
builder.Services
    .AddExecutionContext()
    .AddMvcWebApi()
    .AddHttpWebApi();

// ... type-specific registrations follow ...

builder.Services.PostConfigureAllHealthChecks();
builder.Services.AddControllers();
builder.Services.AddOpenApiDocument(s => {
    s.Title = builder.Environment.ApplicationName;
    s.AddCoreExConfiguration();
});

var app = builder.Build();
app.UseCoreExExceptionHandler();
app.UseHttpsRedirection();
app.UseAuthorization();
app.UseExecutionContext();
app.MapControllers();
app.UseOpenApi();
app.UseSwaggerUi();
app.MapHealthChecks();
app.Run();
```

---

## API Host

Add: reference data, SQL Server, FusionCache, outbox publisher, idempotency.

Key registrations:
- `.AddReferenceDataOrchestrator<T>()`
- `.AddDynamicServicesUsing<...>()`
- `.AddFusionCache()` + `.WithRegisteredDistributedCache()` + `.WithBackplane(...)`
- `.AddSqlServerDatabase()` + `.AddSqlServerUnitOfWork()` + `.AddSqlServerOutboxPublisher<T>()`
- `.AddEventFormatter()`
- Middleware: `.UseIdempotencyKey()` after `.UseExecutionContext()`

---

## Subscribe Host

All of API host **plus**:

Key registrations:
- `.AddHostedServiceManager()`
- `.AddSubscribedManager((_, c) => c.AddSubscribersUsing<T>())`
- `.AzureServiceBusReceiving()` → `.WithSessionReceiver(...)` → `.WithSubscribedSubscriber()` → `.WithHostedService()` → `.Build()`

Middleware addition:
- `app.MapHostedServices()` (after `.MapHealthChecks()`)

---

## Outbox Relay Host

Minimal: SQL Server, Service Bus publisher, relay background service only.

Key registrations:
- `.AddHostedServiceManager()`
- `.AddSqlServerOutboxRelay((_, c) => { ... })`
- `.AddSqlServerOutboxRelayHostedService()`
- `.AddAzureServiceBusPublisher((_, c) => { c.SessionIdStrategy = ...; })`

No: reference data, FusionCache, idempotency, controllers, Swagger.

Middleware: minimal (no `.MapControllers()`, no `.UseOpenApi()`).
