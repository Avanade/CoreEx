---
applyTo: "**/Subscribe/**/*.cs"
description: "Event subscriber conventions: SubscribedBase, SubscribedBase<T>, ValueValidator, ErrorHandler, subject naming, and Subscribe host Program.cs composition"
tags: ["subscribers", "messaging", "service-bus", "event-handling", "integration", "subscribe-host"]
---

# Event Subscriber Conventions

## NuGet / Project References

| Package | Key types provided |
|---|---|
| `CoreEx.Azure.Messaging.ServiceBus` | `SubscribedBase`, `SubscribedBase<TValue>`, `[Subscribe(...)]`, `EventSubscriberArgs`, `ErrorHandler`, `ErrorHandling`, `ServiceBusSessionReceiverOptions`, `.AzureServiceBusReceiving()`, `.WithSessionReceiver()`, `.WithSubscribedSubscriber()`, `.WithHostedService()` |
| `CoreEx.Events` | `EventData`, `.Key`, `.Required()`, `.ToData<T>()`, `IValidator<T>` |
| `CoreEx.Results` | `Result`, `Result.Success` |
| `CoreEx` | `[ScopedService]`, `.ThrowIfNull()` |

## Subscriber Structure

Each subscriber is a small, focused class that:

1. Opts in to one or more message subjects via `[Subscribe("subject")]` attributes.
2. Extends `SubscribedBase` (untyped) or `SubscribedBase<TValue>` (typed payload with optional validation).
3. Delegates immediately to an Application-layer service or adapter — no business logic in the subscriber.
4. Returns `Result` or `Result<T>` so that error handling and dead-lettering decisions can be expressed declaratively.

All subscribers are decorated with `[ScopedService]` for automatic DI discovery. Dependencies are injected via primary constructor and guarded with `.ThrowIfNull()`.

### Untyped subscriber — `SubscribedBase`

Use when the relevant data is carried in the message key, not the payload:

```csharp
[ScopedService, Subscribe("contoso.products.reservation.confirm")]
public class ReservationConfirmSubscriber : SubscribedBase
{
    internal static readonly ErrorHandler DefaultErrorHandler = new ErrorHandler()
        .Add<NotFoundException>(ex => ex.ErrorCode == "pending-reservation-not-found"
            ? ErrorHandling.CompleteAsInformation
            : null);

    private readonly IMovementService _service;

    public ReservationConfirmSubscriber(IMovementService service)
    {
        _service = service.ThrowIfNull();
        ErrorHandler = DefaultErrorHandler;
    }

    protected async override Task<Result> OnReceiveAsync(
        EventData @event, EventSubscriberArgs args, CancellationToken cancellationToken = default)
    {
        var referenceId = @event.Key.Required();
        await _service.ConfirmReservationAsync(referenceId).ConfigureAwait(false);
        return Result.Success;
    }
}
```

### Typed subscriber — `SubscribedBase<TValue>`

Use when the message carries a typed payload that should be deserialized and optionally validated before `OnReceiveAsync` is called. Wire a `ValueValidator` to validate the deserialized value:

```csharp
[ScopedService]
[Subscribe("contoso.products.product.created.v1")]
[Subscribe("contoso.products.product.updated.v1")]
public class ProductModifySubscriber(IProductSyncAdapter adapter) : SubscribedBase<Product>
{
    private readonly IProductSyncAdapter _adapter = adapter.ThrowIfNull();

    public override IValidator<Product>? ValueValidator => ProductValidator.Default;

    protected override Task<Result> OnReceiveAsync(
        Product value, EventData @event, EventSubscriberArgs args, CancellationToken cancellationToken = default)
        => _adapter.ModifyAsync(value);
}
```

Multiple `[Subscribe]` attributes on a single class handle multiple subjects with the same logic — no duplication required.

### Key-only untyped subscriber

When only the key is needed (no payload), use `SubscribedBase` and extract the key directly:

```csharp
[ScopedService, Subscribe("contoso.products.product.deleted.v1")]
public class ProductDeleteSubscriber(IProductSyncAdapter adapter) : SubscribedBase
{
    private readonly IProductSyncAdapter _adapter = adapter.ThrowIfNull();

    protected override Task<Result> OnReceiveAsync(
        EventData @event, EventSubscriberArgs args, CancellationToken cancellationToken = default)
        => _adapter.DeleteAsync(@event.Key.Required());
}
```

## Subject Naming

Use dot-separated lowercase subject strings:

```
{solution}.{domain}.{entity}.{action}[.v{n}]
```

- **Domain events** (published from the outbox) include a version suffix: `contoso.products.product.created.v1`
- **Command messages** (point-to-point, no versioning semantics): `contoso.products.reservation.confirm`

Examples:
- `contoso.products.product.created.v1`
- `contoso.products.product.updated.v1`
- `contoso.products.product.deleted.v1`
- `contoso.products.reservation.confirm`
- `contoso.products.reservation.cancel`

## Error Handling

Define a static `ErrorHandler` to control how specific exceptions are treated — for example, converting a known `NotFoundException` to an informational completion rather than dead-lettering:

```csharp
internal static readonly ErrorHandler DefaultErrorHandler = new ErrorHandler()
    .Add<NotFoundException>(ex => ex.ErrorCode == "pending-reservation-not-found"
        ? ErrorHandling.CompleteAsInformation   // consume silently; log as informational
        : null);                                // null = fall through to default handling (retry / dead-letter)
```

Assign it in the constructor: `ErrorHandler = DefaultErrorHandler;`

Share the same `ErrorHandler` instance across related subscribers (e.g., both Confirm and Cancel subscribers can reference the same static instance).

## Accessing Event Data

```csharp
var key   = @event.Key.Required();          // Key from the message — throws if missing
var value = @event.ToData<MyPayload>();     // Deserialize typed payload from untyped subscriber
```

In typed subscribers (`SubscribedBase<TValue>`), the deserialized value is passed directly as the first parameter to `OnReceiveAsync` — no manual deserialization needed.

## Program.cs Composition

The Subscribe host `Program.cs` follows a predictable CoreEx shape. Key sections in order:

```csharp
// 1. Execution context and dynamic service discovery
builder.Services
    .AddExecutionContext()
    .AddReferenceDataOrchestrator<ReferenceDataService>()
    .AddMvcWebApi()
    .AddHttpWebApi()
    .AddHostedServiceManager();

// Discover all [ScopedService] types in the subscriber, ref-data, and repository assemblies
builder.Services.AddDynamicServicesUsing<ProductModifySubscriber, ReferenceDataService, ReferenceDataRepository>();

// 2. Caching — L1 memory cache + L2 Redis + FusionCache hybrid + idempotency provider
builder.Services.AddMemoryCache();
builder.AddRedisDistributedCache("redis");
builder.Services.AddFusionCache()
    .WithRegisteredMemoryCache()
    .WithRegisteredDistributedCache()
    .WithBackplane(sp => new RedisBackplane(new RedisBackplaneOptions { Configuration = sp.GetRequiredService<IOptions<ConfigurationOptions>>().Value.ToString() }))
    .WithSystemTextJsonSerializer(JsonDefaults.SerializerOptions);

builder.Services
    .AddFusionHybridCache()
    .AddDefaultCacheKeyProvider()
    .AddHybridCacheIdempotencyProvider();

// 3. Infrastructure — database, EF, outbox publisher (for transactional writes inside subscribers)
// SQL Server (Shopping) variant:
builder.AddSqlServerClient("SqlServer");
builder.Services
    .AddSqlServerDatabase()
    .AddSqlServerUnitOfWork()
    .AddSqlServerOutboxPublisher()              // <-- outbox publisher becomes the default IEventPublisher
    .AddDbContext<ShoppingDbContext>()
    .AddEfDb<ShoppingEfDb>();

// PostgreSQL (Products) variant:
builder.AddAzureNpgsqlDataSource("Postgres");
builder.Services
    .AddPostgresDatabase()
    .AddPostgresUnitOfWork()
    .AddEventFormatter()                        // <-- required for message formatting for publishing
    .AddPostgresOutboxPublisher()               // <-- outbox publisher becomes the default IEventPublisher
    .AddDbContext<ProductsDbContext>()
    .AddEfDb<ProductsEfDb>();

// 4. Azure Service Bus publisher — direct publish capability (not the default IEventPublisher)
builder.AddAzureServiceBusClient("ServiceBus");
builder.Services.AddAzureServiceBusPublisher((_, c) =>
{
    c.SessionIdStrategy = ServiceBusSessionStrategy.UsePartitionKeyConvertedToAnId;
}, addAsDefaultIEventPublisher: false);  // false because outbox publisher is already the default

// 5. Event formatter + subscriber manager (Shopping only — Products included AddEventFormatter earlier)
builder.Services
    .AddEventFormatter()                                                               // Adds the EventFormatter to enable message parsing.
    .AddSubscribedManager((_, c) => c.AddSubscribersUsing<ProductModifySubscriber>()); // Adds the SubscribedManager and dynamically links to the individual Subscribers.

// Products variant (AddEventFormatter already called):
builder.Services.AddSubscribedManager((_, c) => c.AddSubscribersUsing<ReservationConfirmSubscriber>());

// 6. Azure Service Bus receiver wiring
builder.Services.AzureServiceBusReceiving()
    .WithSessionReceiver(_ =>
    {
        var o = ServiceBusSessionReceiverOptions.CreateForTopicSubscription();
        o.SessionProcessorOptions.MaxConcurrentSessions = 4;
        return o;
    })
    .WithSubscribedSubscriber()    // routes received messages through the SubscribedManager
    .WithHostedService()           // runs the receiver as a BackgroundService
    .Build();

// 7. External API clients (if needed — Shopping only)
builder.AddTypedHttpClient<ProductsHttpClient>("ProductsApi");

// 8. Health checks, OpenTelemetry
builder.Services.PostConfigureAllHealthChecks();
builder.Services.AddControllers();
builder.Services.AddOpenApiDocument(s =>
{
    s.Title = builder.Environment.ApplicationName;
    s.AddCoreExConfiguration();
});

builder.WithCoreExTelemetry()
    .WithCoreExServiceBusTelemetry()
    .WithCoreExSqlServerTelemetry()  // or .WithCoreExPostgresTelemetry() for Products
    .UseOtlpExporter();

// 9. Build and middleware pipeline
var app = builder.Build();

app.UseCoreExExceptionHandler();
app.UseHttpsRedirection();
app.UseAuthorization();
app.UseExecutionContext();
app.MapControllers();

app.UseOpenApi();
app.UseSwaggerUi();
app.MapHealthChecks();
app.MapHostedServices();   // exposes pause/resume management endpoints per partition

app.Run();
```

`AddSubscribersUsing<T>()` scans the assembly containing `T` and auto-registers every `[Subscribe]`-decorated class — adding a new subscriber requires only creating the class, no `Program.cs` edits needed.

`MapHostedServices()` exposes runtime management endpoints to **pause and resume** the receiver per partition without restarting the process.

## Do Not

- Do not embed business logic in subscriber classes — delegate immediately to an Application-layer service or adapter.
- Do not use MediatR or in-process event dispatchers — subscribers react to integration events from the broker only.
- Do not manually register subscriber classes in DI — `AddSubscribersUsing<T>()` discovers them automatically via `[ScopedService]`.
- Do not omit `AddEventFormatter()` from `Program.cs` — it is required for message parsing and deserialization.
- Do not set `addAsDefaultIEventPublisher: true` for the Service Bus publisher when the outbox publisher is the intended default `IEventPublisher`.

## Further Reading

- [`samples/docs/hosts-layer.md`](../../../samples/docs/hosts-layer.md#subscribe-host) — Subscribe host architecture, Program.cs shape, and subscriber patterns.
- [`samples/docs/patterns.md`](../../../samples/docs/patterns.md) — Subscribe, Publish, Transactional Outbox, and Event-Driven Replication pattern entries.
- [`src/CoreEx.Azure.Messaging.ServiceBus/README.md`](../../../src/CoreEx.Azure.Messaging.ServiceBus/README.md) — `SubscribedBase`, `ErrorHandler`, and Service Bus receiver configuration.
