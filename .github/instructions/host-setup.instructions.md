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

builder.Services.AddOpenApiDocument(s =>
{
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

Add reference data, SQL Server, FusionCache, and idempotency support:

```csharp
builder.Services
    .AddExecutionContext()
    .AddReferenceDataOrchestrator<ReferenceDataService>()
    .AddMvcWebApi()
    .AddHttpWebApi();

builder.Services.AddDynamicServicesUsing<ReferenceDataService, ReferenceDataRepository>();

// FusionCache (L1 + L2 + Redis backplane)
builder.Services.AddMemoryCache();
builder.AddRedisDistributedCache("redis");
builder.Services.AddFusionCache()
    .WithRegisteredMemoryCache()
    .WithRegisteredDistributedCache()
    .WithBackplane(sp => new RedisBackplane(new RedisBackplaneOptions
    {
        Configuration = sp.GetRequiredService<IOptions<ConfigurationOptions>>().Value.ToString()
    }))
    .WithSystemTextJsonSerializer(JsonDefaults.SerializerOptions);
builder.Services.AddFusionHybridCache()
    .AddDefaultCacheKeyProvider()
    .AddHybridCacheIdempotencyProvider();

// SQL Server + EF + Outbox
builder.AddSqlServerClient("SqlServer");
builder.Services
    .AddSqlServerDatabase()
    .AddSqlServerUnitOfWork()
    .AddEventFormatter()
    .AddSqlServerOutboxPublisher<DomainOutboxPublisher>()
    .AddDbContext<DomainDbContext>()
    .AddEfDb<DomainEfDb>();

// Telemetry
builder.WithCoreExTelemetry()
    .WithCoreExSqlServerTelemetry()
    .UseOtlpExporter();

// Middleware additions
app.UseIdempotencyKey(); // add after UseExecutionContext
```

---

## Subscribe Host

Builds on the API host skeleton and adds:
- `AddHostedServiceManager()` for managed background services.
- Dynamic service discovery for subscriber classes.
- Service Bus receiver configuration.
- `app.MapHostedServices()` in the middleware pipeline.

```csharp
builder.Services
    .AddExecutionContext()
    .AddReferenceDataOrchestrator<ReferenceDataService>()
    .AddMvcWebApi()
    .AddHttpWebApi()
    .AddHostedServiceManager();

builder.Services.AddDynamicServicesUsing<MySubscriber, ReferenceDataService, ReferenceDataRepository>();

// (FusionCache + SQL Server same as API host)

builder.AddAzureServiceBusClient("ServiceBus");

builder.Services.AddSubscribedManager((_, c) => c.AddSubscribersUsing<MySubscriber>());

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

// Telemetry
builder.WithCoreExTelemetry()
    .WithCoreExServiceBusTelemetry()
    .WithCoreExSqlServerTelemetry()
    .UseOtlpExporter();

// Additional middleware
app.MapHostedServices(); // exposes hosted service health
```

---

## Outbox Relay Host

Minimal — no reference data, no FusionCache, no idempotency. Only SQL Server, Service Bus, and the relay background service:

```csharp
builder.Services
    .AddExecutionContext()
    .AddMvcWebApi()
    .AddHttpWebApi()
    .AddHostedServiceManager();

builder.AddSqlServerClient("SqlServer");
builder.Services
    .AddSqlServerDatabase()
    .AddSqlServerUnitOfWork()
    .AddSqlServerOutboxRelay((_, c) =>
    {
        c.ClaimBatchStatement  = SqlStatement.StoredProcedure("[Schema].[spOutboxBatchClaim]");
        c.CompleteBatchStatement = SqlStatement.StoredProcedure("[Schema].[spOutboxBatchComplete]");
        c.CancelBatchStatement = SqlStatement.StoredProcedure("[Schema].[spOutboxBatchCancel]");
    });

builder.AddSqlServerOutboxRelayHostedService();

builder.AddAzureServiceBusClient("ServiceBus");
builder.Services.AddAzureServiceBusPublisher((_, c) =>
{
    c.SessionIdStrategy = ServiceBusSessionStrategy.UsePartitionKeyConvertedToAnId;
});

builder.WithCoreExTelemetry()
    .WithCoreExSqlServerTelemetry()
    .WithCoreExServiceBusTelemetry()
    .UseOtlpExporter();

// Middleware — no controller mapping, no Swagger, no idempotency
app.UseCoreExExceptionHandler();
app.UseHttpsRedirection();
app.UseExecutionContext();
app.MapHealthChecks();
app.MapHostedServices();
```

---

## Naming Conventions for Connection Strings

Use these standard keys in `appsettings.json` / connection string configuration:

| Key | Points to |
|---|---|
| `"SqlServer"` | SQL Server instance |
| `"redis"` | Redis instance |
| `"ServiceBus"` | Azure Service Bus namespace |

These keys are passed to `AddSqlServerClient("SqlServer")`, `AddRedisDistributedCache("redis")`, and `AddAzureServiceBusClient("ServiceBus")` respectively.

---

## Dynamic Service Discovery

Use `AddDynamicServicesUsing<T1, T2, ...>()` to discover and register all `[ScopedService]`-attributed classes from the same assemblies as the type parameters. Pass one representative type from each assembly that contains services:

```csharp
builder.Services.AddDynamicServicesUsing<MySubscriber, ReferenceDataService, ReferenceDataRepository>();
```

This replaces manual `services.AddScoped<IFoo, Foo>()` calls.
