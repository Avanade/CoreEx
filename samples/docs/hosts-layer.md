# Host Patterns

CoreEx is largely agnostic to the choice of host technology. A domain can be exposed through any combination of host types — API, Outbox Relay, and Subscribe — each as a separate deployable process. All three are implemented here as ASP.NET Core hosts, which provides a consistent model for health checks, OpenTelemetry, and runtime management endpoints, but any .NET host type (Worker Service, console, etc.) would work equally well.

The host is the **composition root** of the application. It sits above all layers — wiring Contracts, Application, Domain, and Infrastructure together via dependency injection — but it does not reach into those layers directly. Where business operations are needed (API request handling, event subscription processing), the host delegates to Application-layer services; it never bypasses the Application layer to call Infrastructure directly. The host itself contains no business logic.

**Example projects**

| Host type | Products | Shopping |
|---|---|---|
| API | [`Contoso.Products.Api`](../src/Contoso.Products.Api) | [`Contoso.Shopping.Api`](../src/Contoso.Shopping.Api) |
| Outbox Relay | [`Contoso.Products.Outbox.Relay`](../src/Contoso.Products.Outbox.Relay) | [`Contoso.Shopping.Outbox.Relay`](../src/Contoso.Shopping.Outbox.Relay) |
| Subscribe | [`Contoso.Products.Subscribe`](../src/Contoso.Products.Subscribe) | [`Contoso.Shopping.Subscribe`](../src/Contoso.Shopping.Subscribe) |

---

## API host

The API host exposes the domain's capabilities as HTTP endpoints. Controllers delegate immediately to Application-layer services via the CoreEx `WebApi` helper — they contain no business logic.

**Example projects**
- [`samples/src/Contoso.Products.Api`](../src/Contoso.Products.Api)
- [`samples/src/Contoso.Shopping.Api`](../src/Contoso.Shopping.Api)

### Controllers

MVC controllers are the chosen style in the samples, but CoreEx supports minimal APIs equally — the developer chooses. Each controller is a thin routing shell: it declares the route, HTTP verb, OpenAPI metadata, and delegates to the `WebApi` helper which handles request deserialisation, response serialisation, status-code mapping, and error translation.

```csharp
// samples/src/Contoso.Products.Api/Controllers/ProductController.cs
[ApiController, Route("/api/products"), OpenApiTag("Products")]
public class ProductController(WebApi webApi, IProductService service) : ControllerBase
{
    [HttpPost]
    [IdempotencyKey]
    public Task<IActionResult> PostAsync() => _webApi.PostAsync<Product, Product>(Request, (ro, _) =>
    {
        ro.WithLocationUri(p => new Uri($"/api/products/{p.Id}", UriKind.Relative));
        return _service.CreateAsync(ro.Value);
    });

    [HttpPatch("{id}")]
    public Task<IActionResult> PatchAsync(string id) => _webApi.PatchAsync<Product>(Request,
        get: (ro, _) => _service.GetAsync(id.Required()),
        put: (ro, _) => _service.UpdateAsync(ro.Value.Adjust(p => p.Id = id)));
}
```

Where a service uses `Result<T>` pipelines (Shopping), the `WithResult` variants of the helpers are used (`PostWithResultAsync`, `PutWithResultAsync`, etc.) — the controller code remains equally thin.

Responsibilities that are deliberately offloaded to the `WebApi` helper include:

- Deserialising the request body and binding route/query parameters.
- Mapping CoreEx exception types (`NotFoundException`, `ValidationException`, `BusinessException`, etc.) to the appropriate HTTP problem-detail responses.
- Enforcing idempotency via the `[IdempotencyKey]` attribute and the `UseIdempotencyKey()` middleware.
- Generating `Location` headers on POST responses via `WithLocationUri`.
- Supporting `HTTP PATCH` with `application/merge-patch+json` semantics.

> **Read vs write split**: Products separates read and write operations into distinct controllers (`ProductController` / `ProductReadController`, `MovementController` / `MovementReadController`). This keeps each controller focused and mirrors the CQRS-style split at the service level. There is no framework requirement to do this — it is a developer organisational choice.

> **See also**: [`WebApi`](../../src/CoreEx.AspNetCore/WebApis/WebApi.cs) · [`[IdempotencyKey]`](../../src/CoreEx.AspNetCore/Http/IdempotencyKeyAttribute.cs) · [Minimal APIs](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis)

### Program.cs composition

`Program.cs` follows a predictable CoreEx shape and is the only file in the API host:

1. `builder.AddHostSettings()` — loads CoreEx host configuration.
2. Core services — `AddExecutionContext()`, `AddReferenceDataOrchestrator<T>()`, `AddMvcWebApi()`, `AddHttpWebApi()`.
3. Dynamic service registration — `AddDynamicServicesUsing<T…>()` auto-discovers all `[ScopedService]`-decorated types.
4. Infrastructure wiring — database, EF DbContext, outbox publisher, caching (L1 in-memory + L2 Redis + FusionCache backplane).
5. `PostConfigureAllHealthChecks()` — adds standard health-check tags.
6. OpenAPI — NSwag document with `AddCoreExConfiguration()`.
7. OpenTelemetry — `WithCoreExTelemetry()` and provider-specific extensions.
8. Middleware pipeline — `UseCoreExExceptionHandler()` → `UseExecutionContext()` → `UseIdempotencyKey()` → `MapControllers()` → health checks.

```csharp
// samples/src/Contoso.Products.Api/Program.cs  (abridged)
builder.Services
    .AddExecutionContext()
    .AddReferenceDataOrchestrator<ReferenceDataService>()
    .AddMvcWebApi()
    .AddHttpWebApi();

builder.Services.AddDynamicServicesUsing<ReferenceDataService, ReferenceDataRepository>();

// ...database, caching, outbox...

app.UseCoreExExceptionHandler();
app.UseExecutionContext();
app.UseIdempotencyKey();
app.MapControllers();
app.MapHealthChecks();
```

> **See also**: [`AddMvcWebApi`](../../src/CoreEx.AspNetCore/WebApis/WebApiServiceCollectionExtensions.cs) · [`AddDynamicServicesUsing`](../../src/CoreEx/DependencyInjection/ServiceCollectionExtensions.cs)

---

## Outbox Relay host

The Outbox Relay host performs the **transactional outbox relay function** — it polls the outbox table, reads committed event records, and forwards them to the configured message broker (Azure Service Bus in the samples, but the broker is swappable: RabbitMQ, Kafka, etc. are equally supported). There is no business logic in this host; it is a relay only.

**Example projects**
- [`samples/src/Contoso.Products.Outbox.Relay`](../src/Contoso.Products.Outbox.Relay)
- [`samples/src/Contoso.Shopping.Outbox.Relay`](../src/Contoso.Shopping.Outbox.Relay)

The relay runs as a `BackgroundService` (hosted service) registered via `AddPostgresOutboxRelayHostedService()` / `AddSqlServerOutboxRelayHostedService()`. It uses **partitioning** to improve throughput and scalability — multiple relay instances can each own a subset of partitions and process them independently without coordination.

Because it is implemented as an ASP.NET Core host, it can expose additional HTTP endpoints alongside the relay worker:

- **`MapHealthChecks()`** — liveness and readiness probes for container orchestrators.
- **`MapHostedServices()`** — runtime management endpoints that allow the relay to be **paused and resumed** per partition/tenant ID without restarting the process.

```csharp
// samples/src/Contoso.Products.Outbox.Relay/Program.cs  (abridged)
builder.Services
    .AddPostgresDatabase()
    .AddPostgresUnitOfWork()
    .AddPostgresOutboxRelay();

builder.AddPostgresOutboxRelayHostedService();

builder.Services.AddAzureServiceBusPublisher((_, c) =>
{
    c.SessionIdStrategy = ServiceBusSessionStrategy.UsePartitionKeyConvertedToAnId;
});

app.MapHealthChecks();
app.MapHostedServices();  // Exposes pause/resume management endpoints.
```

> The `Program.cs` for the Outbox Relay is intentionally minimal — no controllers, no OpenAPI document, no application-layer services. Its sole concern is shuttling committed outbox records to the broker reliably.

> **See also**: [`PostgresOutboxRelay`](../../src/CoreEx.Database.Postgres/PostgresOutboxRelay.cs) · [`SqlServerOutboxRelay`](../../src/CoreEx.Database.SqlServer/SqlServerOutboxRelay.cs) · [Transactional Outbox pattern](https://learn.microsoft.com/en-us/azure/architecture/best-practices/transactional-outbox-cosmos) · [`MapHostedServices`](../../src/CoreEx.AspNetCore/WebApis/WebApiServiceCollectionExtensions.cs)

---

## Subscribe host

The Subscribe host processes inbound events/messages from a message broker. Like the Relay, it is implemented as an ASP.NET Core host so that health check and runtime management endpoints can sit alongside the message-processing workers. Individual `Subscriber` classes encapsulate the opt-in subscription to a specific message type.

**Example projects**
- [`samples/src/Contoso.Products.Subscribe`](../src/Contoso.Products.Subscribe)
- [`samples/src/Contoso.Shopping.Subscribe`](../src/Contoso.Shopping.Subscribe)

### Subscribers

Each subscriber is a small, focused class that:

1. Opts in to one or more message subjects via `[Subscribe("subject")]` attributes.
2. Extends `SubscribedBase` (untyped) or `SubscribedBase<TValue>` (strongly typed, with optional value validation).
3. Delegates immediately to an Application-layer service or adapter — keeping all business logic out of the subscriber.
4. Returns `Result` / `Result<T>` so that error handling and dead-lettering decisions can be expressed declaratively.

```csharp
// samples/src/Contoso.Products.Subscribe/Subscribers/ReservationConfirmSubscriber.cs
[ScopedService, Subscribe("contoso.products.reservation.confirm")]
public class ReservationConfirmSubscriber : SubscribedBase
{
    protected async override Task<Result> OnReceiveAsync(EventData @event, EventSubscriberArgs args, CancellationToken cancellationToken = default)
    {
        var referenceId = @event.Key.Required();
        await _service.ConfirmReservationAsync(referenceId).ConfigureAwait(false);
        return Result.Success;
    }
}
```

Where a subscriber expects a typed payload, `SubscribedBase<TValue>` is used and a `ValueValidator` can be wired in to validate the deserialised value before `OnReceiveAsync` is called:

```csharp
// samples/src/Contoso.Shopping.Subscribe/Subscribers/ProductModifySubscriber.cs
[ScopedService]
[Subscribe("contoso.products.product.created.v1")]
[Subscribe("contoso.products.product.updated.v1")]
public class ProductModifySubscriber(IProductSyncAdapter adapter) : SubscribedBase<Product>
{
    public override IValidator<Product>? ValueValidator => ProductValidator.Default;

    protected override Task<Result> OnReceiveAsync(Product value, EventData @event, EventSubscriberArgs args, CancellationToken cancellationToken = default)
        => _adapter.ModifyAsync(value);
}
```

A subscriber can also declare an `ErrorHandler` to control how specific exceptions are treated — for example, converting a `NotFoundException` with a known error code to an informational completion (rather than dead-lettering) when the referenced entity has already been removed:

```csharp
internal static readonly ErrorHandler DefaultErrorHandler = new ErrorHandler()
    .Add<NotFoundException>(ex => ex.ErrorCode == "pending-reservation-not-found"
        ? ErrorHandling.CompleteAsInformation
        : null);
```

### Program.cs composition

The Subscribe host `Program.cs` follows a similar shape to the API host but replaces the controller/OpenAPI section with broker-receiver wiring:

```csharp
// samples/src/Contoso.Products.Subscribe/Program.cs  (abridged)
builder.Services.AddSubscribedManager((_, c) =>
    c.AddSubscribersUsing<ReservationConfirmSubscriber>());  // Discovers all subscribers in the assembly.

builder.Services.AzureServiceBusReceiving()
    .WithSessionReceiver(_ =>
    {
        var o = ServiceBusSessionReceiverOptions.CreateForTopicSubscription();
        o.SessionProcessorOptions.MaxConcurrentSessions = 4;
        return o;
    })
    .WithSubscribedSubscriber()   // Routes received messages through the SubscribedManager.
    .WithHostedService()          // Runs the receiver as a BackgroundService.
    .Build();

app.MapHealthChecks();
app.MapHostedServices();  // Exposes pause/resume management endpoints.
```

`AddSubscribersUsing<T>()` scans the assembly containing `T` and auto-registers every class decorated with `[Subscribe]`, so adding a new subscriber requires only creating the class — no `Program.cs` edits are needed.

> **See also**: [`SubscribedBase`](../../src/CoreEx.Events/Subscribing/SubscribedBase.cs) · [`SubscribedBase<T>`](../../src/CoreEx.Events/Subscribing/SubscribedBase.cs) · [`ErrorHandler`](../../src/CoreEx.Events/Subscribing/ErrorHandler.cs) · [`AddSubscribedManager`](../../src/CoreEx.Azure.Messaging.ServiceBus) · [Competing Consumers pattern](https://learn.microsoft.com/en-us/azure/architecture/patterns/competing-consumers)
