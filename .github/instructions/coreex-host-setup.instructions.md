---
applyTo: "**/Program.cs"
description: "Host setup conventions for Program.cs: API host, Subscribe host, Outbox Relay host, middleware, service registration, and distributed caching"
tags: ["program-cs", "host-setup", "middleware", "dependency-registration", "caching"]
---

# Host Setup Conventions (Program.cs)

The host is a **composition root only** — no business logic. There are three host types in a CoreEx solution depending on the capabilities required. Each follows the same opening skeleton, then diverges based on its responsibilities.

> **Further Reading**: [Hosts Layer Guide](https://github.com/Avanade/CoreEx/blob/main/samples/docs/hosts-layer.md) · [Layer Dependencies](https://github.com/Avanade/CoreEx/blob/main/samples/docs/layers.md) · [Pattern Catalog](https://github.com/Avanade/CoreEx/blob/main/samples/docs/patterns.md)

---

## Key Registrations by Host Type

### API Host

| Package | Key registrations |
|---|---|
| `CoreEx.AspNetCore` | `AddMvcWebApi()`, `AddHttpWebApi()`, `AddExecutionContext()`, `UseCoreExExceptionHandler()`, `UseExecutionContext()`, `UseIdempotencyKey()`, `MapHealthChecks()` |
| `CoreEx.AspNetCore.NSwag` | `AddOpenApiDocument()`, `AddCoreExConfiguration()`, `UseOpenApi()`, `UseSwaggerUi()` |
| `CoreEx.Caching.FusionCache` | `AddFusionCache()`, `AddFusionHybridCache()`, `AddDefaultCacheKeyProvider()`, `AddHybridCacheIdempotencyProvider()` |
| `CoreEx.Database.SqlServer` | `AddSqlServerDatabase()`, `AddSqlServerUnitOfWork()`, `AddSqlServerOutboxPublisher()`, `AddSqlServerClient("SqlServer")` |
| `CoreEx.Database.Postgres` | `AddPostgresDatabase()`, `AddPostgresUnitOfWork()`, `AddPostgresOutboxPublisher()`, `AddAzureNpgsqlDataSource("Postgres")` |
| `CoreEx.EntityFrameworkCore` | `AddDbContext<T>()`, `AddEfDb<T>()` |
| `CoreEx.Events` | `AddEventFormatter()` |
| `CoreEx.RefData` | `AddReferenceDataOrchestrator<T>()` |
| `Aspire.StackExchange.Redis.DistributedCaching` | `AddRedisDistributedCache("redis")` |
| `FusionCache.Backplane.StackExchangeRedis` | `RedisBackplane`, `RedisBackplaneOptions` |
| `OpenTelemetry.*` | `WithCoreExTelemetry()`, `WithCoreExSqlServerTelemetry()` / `WithCoreExPostgresTelemetry()`, `UseOtlpExporter()` |

### Subscribe Host

| Package | Key registrations |
|---|---|
| `CoreEx.AspNetCore` | `AddMvcWebApi()`, `AddHttpWebApi()`, `AddExecutionContext()`, `AddHostedServiceManager()`, `UseCoreExExceptionHandler()`, `UseExecutionContext()`, `MapHealthChecks()`, `MapHostedServices()` |
| `CoreEx.Caching.FusionCache` | `AddFusionCache()`, `AddFusionHybridCache()`, `AddDefaultCacheKeyProvider()`, `AddHybridCacheIdempotencyProvider()` |
| `CoreEx.Events` | `AddEventFormatter()`, `AddSubscribedManager()` |
| `CoreEx.Database.SqlServer` | `AddSqlServerDatabase()`, `AddSqlServerUnitOfWork()`, `AddSqlServerOutboxPublisher()`, `AddSqlServerClient("SqlServer")` |
| `CoreEx.Database.Postgres` | `AddPostgresDatabase()`, `AddPostgresUnitOfWork()`, `AddPostgresOutboxPublisher()`, `AddAzureNpgsqlDataSource("Postgres")` |
| `CoreEx.EntityFrameworkCore` | `AddDbContext<T>()`, `AddEfDb<T>()` |
| `CoreEx.RefData` | `AddReferenceDataOrchestrator<T>()` |
| `CoreEx.Azure.Messaging.ServiceBus` | `AddAzureServiceBusClient("ServiceBus")`, `AddAzureServiceBusPublisher(..., addAsDefaultIEventPublisher: false)`, `AzureServiceBusReceiving()`, `WithCoreExServiceBusTelemetry()` |
| `Aspire.StackExchange.Redis.DistributedCaching` | `AddRedisDistributedCache("redis")` |
| `FusionCache.Backplane.StackExchangeRedis` | `RedisBackplane`, `RedisBackplaneOptions` |
| `OpenTelemetry.*` | `WithCoreExTelemetry()`, `WithCoreExServiceBusTelemetry()`, `WithCoreExSqlServerTelemetry()` / `WithCoreExPostgresTelemetry()`, `UseOtlpExporter()` |

### Outbox Relay Host

| Package | Key registrations |
|---|---|
| `CoreEx.AspNetCore` | `AddMvcWebApi()`, `AddHttpWebApi()`, `AddExecutionContext()`, `AddHostedServiceManager()`, `UseCoreExExceptionHandler()`, `UseExecutionContext()`, `MapHealthChecks()`, `MapHostedServices()` |
| `CoreEx.Database.SqlServer` | `AddSqlServerDatabase()`, `AddSqlServerUnitOfWork()`, `AddSqlServerOutboxRelay()`, `AddSqlServerOutboxRelayHostedService()` |
| `CoreEx.Database.Postgres` | `AddPostgresDatabase()`, `AddPostgresUnitOfWork()`, `AddPostgresOutboxRelay()`, `AddPostgresOutboxRelayHostedService()` |
| `CoreEx.Azure.Messaging.ServiceBus` | `AddAzureServiceBusClient("ServiceBus")`, `AddAzureServiceBusPublisher(...)`, `ServiceBusSessionStrategy` |
| `OpenTelemetry.*` | `WithCoreExTelemetry()`, `WithCoreExSqlServerTelemetry()` / `WithCoreExPostgresTelemetry()`, `WithCoreExServiceBusTelemetry()`, `UseOtlpExporter()` |

---

## API Host

The API host is the primary HTTP composition root. It exposes controllers, OpenAPI docs, reference-data endpoints, and idempotency support.

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.AddHostSettings();

builder.Services
    .AddPrecisionTimeProvider()
    .AddExecutionContext()
    .AddReferenceDataOrchestrator<ReferenceDataService>()
    .AddMvcWebApi()
    .AddHttpWebApi();

builder.Services.AddDynamicServicesUsing<ReferenceDataService, ReferenceDataRepository>();

// L1/L2 caching with FusionCache + Redis backplane.
builder.Services.AddMemoryCache();
builder.AddRedisDistributedCache("redis");
builder.Services.AddFusionCache()
    .WithRegisteredMemoryCache()
    .WithRegisteredDistributedCache()
    .WithBackplane(sp => new RedisBackplane(new RedisBackplaneOptions { Configuration = ... }))
    .WithSystemTextJsonSerializer(JsonDefaults.SerializerOptions);
builder.Services
    .AddFusionHybridCache()
    .AddDefaultCacheKeyProvider()
    .AddHybridCacheIdempotencyProvider();

// Database, EF, outbox publisher.
// SQL Server variant:
builder.AddSqlServerClient("SqlServer");
builder.Services
    .AddSqlServerDatabase()
    .AddSqlServerUnitOfWork()
    .AddEventFormatter()
    .AddSqlServerOutboxPublisher()
    .AddDbContext<MyDbContext>()
    .AddEfDb<MyEfDb>();

// PostgreSQL variant (use instead of SQL Server):
// builder.AddAzureNpgsqlDataSource("Postgres");
// builder.Services
//     .AddPostgresDatabase()
//     .AddPostgresUnitOfWork()
//     .AddEventFormatter()
//     .AddPostgresOutboxPublisher()
//     .AddDbContext<MyDbContext>()
//     .AddEfDb<MyEfDb>();

builder.Services.PostConfigureAllHealthChecks();
builder.Services.AddControllers();
builder.Services.AddOpenApiDocument(s => { s.Title = builder.Environment.ApplicationName; s.AddCoreExConfiguration(); });

builder.WithCoreExTelemetry().WithCoreExSqlServerTelemetry().UseOtlpExporter();

var app = builder.Build();
app.UseCoreExExceptionHandler();
app.UseHttpsRedirection();
app.UseAuthorization();
app.UseExecutionContext();
app.UseIdempotencyKey();       // After UseExecutionContext.
app.MapControllers();
app.UseOpenApi();
app.UseSwaggerUi();
app.MapHealthChecks();
app.Run();
```

Key points:
- `AddReferenceDataOrchestrator<T>()` and `AddDynamicServicesUsing<...>()` are shared with Subscribe hosts — both API and Subscribe hosts are full application-layer consumers.
- FusionCache (L1/L2) and `AddHybridCacheIdempotencyProvider()` are shared with Subscribe hosts — both need caching for reference data and idempotency for safe duplicate handling.
- `AddEventFormatter()` is required wherever events are published or parsed.
- `AddSqlServerOutboxPublisher()` / `AddPostgresOutboxPublisher()` take no generic type parameter.
- `UseIdempotencyKey()` must come **after** `UseExecutionContext()`.
- If the domain also publishes directly to Service Bus (e.g. for cross-domain adapters), add `AddAzureServiceBusPublisher(..., addAsDefaultIEventPublisher: false)` so the outbox publisher remains the default `IEventPublisher`.

---

## Subscribe Host

The Subscribe host receives broker messages and delegates to Application-layer services. Subscribers are **full application-layer consumers** — they invoke application services that may validate, persist data, and publish outbound events. Therefore, Subscribe hosts include reference data, caching, database, and idempotency support.

```csharp
builder.Services
    .AddPrecisionTimeProvider()
    .AddExecutionContext()
    .AddReferenceDataOrchestrator<ReferenceDataService>()
    .AddMvcWebApi()
    .AddHttpWebApi()
    .AddHostedServiceManager();

builder.Services.AddDynamicServicesUsing<MySubscriber, ReferenceDataService, ReferenceDataRepository>();

// L1/L2 caching with FusionCache + Redis backplane.
builder.Services.AddMemoryCache();
builder.AddRedisDistributedCache("redis");
builder.Services.AddFusionCache()
    .WithRegisteredMemoryCache()
    .WithRegisteredDistributedCache()
    .WithBackplane(sp => new RedisBackplane(new RedisBackplaneOptions { Configuration = ... }))
    .WithSystemTextJsonSerializer(JsonDefaults.SerializerOptions);
builder.Services
    .AddFusionHybridCache()
    .AddDefaultCacheKeyProvider()
    .AddHybridCacheIdempotencyProvider();

// Domain database + outbox publisher.
// SQL Server variant:
builder.AddSqlServerClient("SqlServer");
builder.Services
    .AddSqlServerDatabase()
    .AddSqlServerUnitOfWork()
    .AddSqlServerOutboxPublisher()
    .AddDbContext<MyDbContext>()
    .AddEfDb<MyEfDb>();

// PostgreSQL variant (use instead of SQL Server):
// builder.AddAzureNpgsqlDataSource("Postgres");
// builder.Services
//     .AddPostgresDatabase()
//     .AddPostgresUnitOfWork()
//     .AddPostgresOutboxPublisher()
//     .AddDbContext<MyDbContext>()
//     .AddEfDb<MyEfDb>();

// Service Bus: keep outbox publisher as the default IEventPublisher.
builder.AddAzureServiceBusClient("ServiceBus");
builder.Services.AddAzureServiceBusPublisher((_, c) =>
{
    c.SessionIdStrategy = ServiceBusSessionStrategy.UsePartitionKeyConvertedToAnId;
}, addAsDefaultIEventPublisher: false);

// Subscriber wiring.
builder.Services
    .AddEventFormatter()
    .AddSubscribedManager((_, c) => c.AddSubscribersUsing<MySubscriber>());

builder.Services.AzureServiceBusReceiving()
    .WithSessionReceiver(_ =>
    {
        var o = ServiceBusSessionReceiverOptions.CreateForTopicSubscription();
        o.SessionProcessorOptions.MaxConcurrentSessions = 4;
        return o;
    })
    .WithSubscribedSubscriber()
    .WithHostedService()
    .Build();

builder.Services.PostConfigureAllHealthChecks();
builder.Services.AddControllers();
builder.Services.AddOpenApiDocument(s => { s.Title = builder.Environment.ApplicationName; s.AddCoreExConfiguration(); });

builder.WithCoreExTelemetry()
    .WithCoreExServiceBusTelemetry()
    .WithCoreExSqlServerTelemetry()   // or .WithCoreExPostgresTelemetry() for PostgreSQL
    .UseOtlpExporter();

var app = builder.Build();
app.UseCoreExExceptionHandler();
app.UseHttpsRedirection();
app.UseAuthorization();
app.UseExecutionContext();
app.MapControllers();
app.UseOpenApi();
app.UseSwaggerUi();
app.MapHealthChecks();
app.MapHostedServices();   // Exposes pause/resume management endpoints — must follow MapHealthChecks.
app.Run();
```

Key points:
- Subscribe hosts **do** include `AddReferenceDataOrchestrator<T>()` and `AddDynamicServicesUsing<...>()` — subscribers call application services that need reference data for validation and business logic.
- Subscribe hosts **do** include FusionCache (L1/L2) and `AddHybridCacheIdempotencyProvider()` — caching is required for reference data; idempotency is required to safely handle duplicate message delivery.
- Subscribe hosts **do** include database/EF Core and outbox publisher — subscribers persist domain data and publish outbound events.
- `AddHostedServiceManager()` must be registered before `AzureServiceBusReceiving()`.
- `AddSubscribersUsing<T>()` scans the assembly of `T` and auto-registers all `[Subscribe]`-decorated classes — no manual registration per subscriber.
- `AddAzureServiceBusPublisher(..., addAsDefaultIEventPublisher: false)` keeps the outbox publisher as the default `IEventPublisher` for transactional writes.
- `MapHostedServices()` must come **after** `MapHealthChecks()`.

---

## Outbox Relay Host

The Outbox Relay host is minimal: it polls the outbox table and forwards committed events to Azure Service Bus. It has **no application logic** — no controllers, no OpenAPI, no FusionCache, no reference data, no EF Core DbContext. It only needs database connectivity to read the outbox table and Service Bus connectivity to publish.

```csharp
builder.Services
    .AddPrecisionTimeProvider()
    .AddExecutionContext()
    .AddMvcWebApi()
    .AddHttpWebApi()
    .AddHostedServiceManager();

// SQL Server example; use Postgres equivalents for PostgreSQL domains.
builder.AddSqlServerClient("SqlServer");
builder.Services
    .AddSqlServerDatabase()
    .AddSqlServerUnitOfWork()
    .AddSqlServerOutboxRelay();   // No configuration lambda required.

builder.AddSqlServerOutboxRelayHostedService();

// PostgreSQL variant:
// builder.AddAzureNpgsqlDataSource("Postgres");
// builder.Services
//     .AddPostgresDatabase()
//     .AddPostgresUnitOfWork()
//     .AddPostgresOutboxRelay();
// builder.AddPostgresOutboxRelayHostedService();

// Service Bus publisher — this IS the default IEventPublisher for the relay.
builder.AddAzureServiceBusClient("ServiceBus");
builder.Services.AddAzureServiceBusPublisher((_, c) =>
{
    c.SessionIdStrategy = ServiceBusSessionStrategy.UsePartitionKeyConvertedToAnId;
});

builder.Services.PostConfigureAllHealthChecks();

builder.WithCoreExTelemetry().WithCoreExSqlServerTelemetry().WithCoreExServiceBusTelemetry().UseOtlpExporter();

var app = builder.Build();
app.UseCoreExExceptionHandler();
app.UseHttpsRedirection();
app.UseExecutionContext();
app.MapHealthChecks();
app.MapHostedServices();
app.Run();
```

Key points:
- The Relay host has **no application-layer dependencies** — no `AddReferenceDataOrchestrator`, no `AddDynamicServicesUsing`, no FusionCache, no EF Core DbContext, no domain services.
- `AddSqlServerOutboxRelay()` / `AddPostgresOutboxRelay()` take no configuration lambda.
- `AddSqlServerOutboxRelayHostedService()` / `AddPostgresOutboxRelayHostedService()` register the background relay pump — call these on `builder`, not `builder.Services`.
- No `AddControllers()`, no `AddOpenApiDocument()`, no `UseOpenApi()`, no `UseSwaggerUi()`, no `UseIdempotencyKey()`, no `UseAuthorization()`.
