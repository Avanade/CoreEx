# Host Patterns

CoreEx is largely agnostic to the choice of host technology. A domain can be exposed through any combination of host types — API, Outbox Relay, and Subscribe — each as a separate deployable process. All three are implemented here as ASP.NET Core hosts, which provides a consistent model for health checks, OpenTelemetry, and runtime management endpoints, but any .NET host type (Worker Service, console, etc.) would work equally well.

The host is the **composition root** of the application. It sits above all layers — wiring Contracts, Application, Domain, and Infrastructure together via dependency injection — but it does not reach into those layers directly. Where business operations are needed (API request handling, event subscription processing), the host delegates to Application-layer services; it never bypasses the Application layer to call Infrastructure directly. The host itself contains no business logic.

**Example projects**

| Host type | Products | Shopping |
|---|---|---|
| API | [`Contoso.Products.Api`](../src/Contoso.Products.Api) | [`Contoso.Shopping.Api`](../src/Contoso.Shopping.Api) |
| Outbox Relay | [`Contoso.Products.Relay`](../src/Contoso.Products.Relay) | [`Contoso.Shopping.Relay`](../src/Contoso.Shopping.Relay) |
| Subscribe | [`Contoso.Products.Subscribe`](../src/Contoso.Products.Subscribe) | [`Contoso.Shopping.Subscribe`](../src/Contoso.Shopping.Subscribe) |

---

## Choosing a Host Topology: Split vs. Consolidate

The samples run API, Outbox Relay, and Subscribe as three separate deployable processes per domain, but **this is a workload-isolation convention, not a technical requirement.** `AddHostedServiceManager()`/`MapHostedServices()`, the outbox-relay hosted service (`Add{Provider}OutboxRelayHostedService()`), and the Service Bus receiver (`AzureServiceBusReceiving()...WithHostedService()`) are all plain ASP.NET Core hosted-service registrations and minimal-API endpoint mappings — none of them require a dedicated process, and all three would work identically if registered inside the API host's own `Program.cs` alongside its controllers.

**Keep them split when:**
- Request-serving and background processing need to **scale independently** (e.g. API replicas driven by request load, relay/subscriber instances driven by message/partition volume).
- **Fault isolation** matters — a stuck outbox drain, a poison-message loop in a subscriber, or a slow downstream broker shouldn't be able to degrade or crash the process serving live HTTP traffic.
- Each workload has a **different deployment cadence** or resource profile (CPU/memory sizing, restart policy) worth tuning independently.

**Consider consolidating into a single host when:**
- The solution is small and low-traffic, and the operational overhead of running/monitoring/deploying three processes outweighs the isolation benefit.
- Cost matters more than blast-radius containment (one process/container instead of three).

To consolidate, register the Relay's and/or Subscribe's `Program.cs` wiring directly in the API host instead of scaffolding separate `coreex-relay`/`coreex-subscribe` projects — see the [Outbox Relay Host](#outbox-relay-host) and [Subscribe Host](#subscribe-host) sections below for the exact registrations to fold in. There is nothing CoreEx-specific to unwind if a consolidated solution later needs to split a workload back out — the registrations move to their own host verbatim.

---

## API host

The API host exposes the domain's capabilities as HTTP endpoints. Controllers delegate immediately to Application-layer services via the CoreEx `WebApi` helper — they contain no business logic.

**Example projects**
- [`samples/src/Contoso.Products.Api`](../src/Contoso.Products.Api)
- [`samples/src/Contoso.Shopping.Api`](../src/Contoso.Shopping.Api)

### Controllers

MVC controllers are the chosen style in the samples, but CoreEx supports minimal APIs equally — the developer chooses. Each controller is a thin routing shell: it declares the route, HTTP verb, OpenAPI metadata, and delegates to the `WebApi` helper which handles request deserialization, response serialization, status-code mapping, and error translation.

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

- Deserializing the request body and binding route/query parameters.
- Mapping CoreEx exception types (`NotFoundException`, `ValidationException`, `BusinessException`, etc.) to the appropriate HTTP problem-detail responses.
- Enforcing idempotency via the `[IdempotencyKey]` attribute and the `UseIdempotencyKey()` middleware.
- Generating `Location` headers on POST responses via `WithLocationUri`.
- Supporting `HTTP PATCH` with `application/merge-patch+json` semantics.

> **Read vs write split**: Products separates read and write operations into distinct controllers (`ProductController` / `ProductReadController`, `MovementController` / `MovementReadController`). This keeps each controller focused and mirrors the CQRS-style split at the service level. There is no framework requirement to do this — it is a developer organizational choice.

> **See also**: [`WebApi`](../../src/CoreEx.AspNetCore/WebApis/WebApi.cs) · [`[IdempotencyKey]`](../../src/CoreEx.AspNetCore/Http/IdempotencyKeyAttribute.cs) · [Minimal APIs](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis)

### GraphQL-lite query bridge

Alongside its REST controllers, a domain's API host can additionally expose a single `/query` endpoint that
bridges a minimal GraphQL-over-HTTP request to the *same* `QueryArgs`/`PagingArgs` → `QueryAsync` pipeline
and `JsonFilter` field-projection already used by the REST `$query` endpoint (`CoreEx.Data.GraphQL`). This is
additive, not a replacement — the REST endpoints and the `/query` bridge share identical filter, order-by,
paging, and field-selection behavior because they drive the exact same underlying repository/service call.

```csharp
// samples/src/Contoso.Products.Api/Program.cs
builder.Services.AddCoreExGraphQLLite((o, sp) =>
{
    o.AddQuery<ProductLite>("products", ProductQueryArgsConfig.Default, async (qa, pa, ct) => await CoreEx.ExecutionContext.GetRequiredService<IProductReadService>().QueryAsync(qa, pa, ct).ConfigureAwait(false))
     // GetIdentifier<TId> validates the named argument (default "id") for presence and type (it casts to TId, it does not convert) and throws an ArgumentException, mapped by the engine to ARGUMENT_ERROR, if missing/empty/wrong-typed.
     .AddGet<Product>("product", (args, ct) => CoreEx.ExecutionContext.GetRequiredService<IProductReadService>().GetAsync(args.GetIdentifier<string>(), ct));
});

// ...

app.MapCoreExGraphQLLite("/api/query");
```

Each `AddQuery`/`AddGet` root registration is a single line referencing the entity's *existing*
`QueryArgsConfig<TSelf>.Default` and application-service method — no new per-entity resolver code is
authored. Because the `IGraphQLEngine` is registered as a singleton, root resolvers that need scoped
dependencies (repositories, application services) must resolve them per-invocation rather than capturing
an instance resolved from the root `IServiceProvider` at registration time — as shown above via
`CoreEx.ExecutionContext.GetRequiredService<T>()`, which reads from the ambient `ExecutionContext`'s
scoped service provider (set by the `UseExecutionContext()` middleware every CoreEx host already
registers), so no extra `IHttpContextAccessor` registration is needed.

`MapCoreExGraphQLLite` executes through `WebApi.PostAsync<GraphQLLiteResponse>(...)` — the same
response-shaping pipeline the REST controllers use — so `ProblemDetails`/exception-handling middleware still
applies as a safety net for anything the engine's own exception mapping doesn't catch. Add
`.WithCoreExGraphQLTelemetry()` alongside a host's other OpenTelemetry tracing extensions (see
[Program.cs composition](#programcs-composition) below) to trace `GraphQLEngine.ExecuteAsync` calls the same
way as any other CoreEx invoker.

> **v1 scope**: read-only (queries only, no mutations); selection sets may traverse arbitrarily nested
> properties already present on a single resolved DTO (e.g. `person { address { street city } }`), but
> cannot request a field that would require invoking a *different* registered root (no cross-repository
> dataloader/N+1 resolution). See [`CoreEx.Data.GraphQL`](../../src/CoreEx.Data.GraphQL/README.md) for the
> full capability and non-goal list.

> **Secure defaults**: `GraphQLLiteOptions.EnableIntrospection` defaults to `false` — the sample above opts
> in explicitly so Postman/GraphiQL-style tooling can introspect the schema in development. `MapCoreExGraphQLLite`
> also applies no authorization by default; pass `configure: rb => rb.RequireAuthorization()` (or an
> equivalent policy) in hosts where this endpoint should require the same access control as REST controllers.

### Program.cs composition

`Program.cs` follows a predictable CoreEx shape and is the only file in the API host:

1. `builder.AddHostSettings()` — loads CoreEx host configuration.
2. Core services — `AddExecutionContext()`, `AddReferenceDataOrchestrator<T>()`, `AddMvcWebApi()`, `AddHttpWebApi()`.
3. Dynamic service registration — `AddDynamicServicesUsing<T…>()` auto-discovers all `[ScopedService]`-decorated types.
4. Infrastructure wiring — database, EF DbContext, outbox publisher, caching (L1 in-memory + L2 Redis + FusionCache backplane).
5. `PostConfigureAllHealthChecks()` — adds standard health-check tags.
6. OpenAPI — NSwag document with `AddCoreExConfiguration()`.
7. OpenTelemetry — `WithCoreExTelemetry()`, provider-specific extensions (e.g. `WithCoreExPostgresTelemetry()`), and `WithCoreExGraphQLTelemetry()` where the GraphQL-lite bridge is mapped.
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
- [`samples/src/Contoso.Products.Relay`](../src/Contoso.Products.Relay)
- [`samples/src/Contoso.Shopping.Relay`](../src/Contoso.Shopping.Relay)

The relay runs as a `BackgroundService` (hosted service) registered via `AddPostgresOutboxRelayHostedService()` / `AddSqlServerOutboxRelayHostedService()`. It uses **partitioning** to improve throughput and scalability — multiple relay instances can each own a subset of partitions and process them independently without coordination.

Because it is implemented as an ASP.NET Core host, it can expose additional HTTP endpoints alongside the relay worker:

- **`MapHealthChecks()`** — liveness and readiness probes for container orchestrators.
- **`MapHostedServices()`** — runtime management endpoints that allow the relay to be **paused and resumed** per partition/tenant ID without restarting the process.

```csharp
// samples/src/Contoso.Products.Relay/Program.cs  (abridged)
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

### Distributed tracing: why the relay's own span is not the originating trace's parent/child

A relayed event's outgoing message keeps the **original producer's** W3C `traceparent`/`tracestate` untouched — `IEventFormatter.AddTracing` is idempotent and skips an event that already carries trace context, so a Subscriber's span correlates directly back to the request that raised the event (e.g. an API `PUT`), never to the relay. This is intentional, not a gap: a single relay poll can pull a batch of events raised by many causally-unrelated originating traces, so there is no one valid "parent" for the relay's own batch-level span — reparenting it into a single trace only when a batch happens to contain one trace would make the relay's visibility a runtime accident (present for batch-of-one in dev, silently gone for real multi-event batches in production).

Instead, every relay (SQL Server/Postgres via `DatabaseOutboxRelayBase`, and Cosmos DB via `CosmosDbOutboxRelayProcessor`) emits **two** complementary signals per batch, both from [`CloudEventTracingExtensions`](../../src/CoreEx.Events/CloudEventTracingExtensions.cs):

- **`LinkTraceContext`** — adds one [`ActivityLink`](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/distributed-tracing-instrumentation-walkthroughs#activity-and-activitylink) per distinct originating trace onto the relay's own batch-level span — the correct W3C/OTel mechanism for a many-to-one, causally-related-but-not-nested fan-in operation.
- **`EmitRelayMarkers`** — in addition, starts and immediately ends a small `outbox.relay.publish` marker `Activity` **per relayed event**, parented directly to that event's own originating `ActivityContext` (via `ActivitySource.StartActivity(name, kind, parentContext)`, not the relay's ambient activity) and tagged with `outbox.destination`/`outbox.event.id`/`outbox.event.type`. Each marker also carries a back-link to the batch-level relay span. This is what makes the relay hop deterministically visible from *within* the originating trace regardless of batch size — the piece that was previously missing from a PUT's own trace view. Markers are emitted on the dedicated `CoreEx.Events.Outbox.Relay` `ActivitySource`, registered via `WithCoreExEventsSources()` - named to sit alongside the batch-span sources `CoreEx.Database.Outbox.Relay` and `CoreEx.Cosmos.Outbox.Relay` as the `*.Outbox.Relay` family.

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

Where a subscriber expects a typed payload, `SubscribedBase<TValue>` is used and a `ValueValidator` can be wired in to validate the deserialized value before `OnReceiveAsync` is called:

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

### Filtering Azure SDK background-polling telemetry noise

The Azure SDK raises several of its own background/infrastructure activities that carry no business signal and are pure volume in a trace view:

- `ServiceBusReceiver.RenewMessageLock`/`ServiceBusSessionReceiver.RenewSessionLock` — for session-enabled subscriptions (as above, via `WithSessionReceiver`), and for any long-running message
  processing under the standard auto-lock-renewal window, these fire on a timer for as long as a lock is held — one pair per lock-renewal interval, for every concurrently held lock.
- `ServiceBusReceiver.Receive` — a `CLIENT`-kind span the `ServiceBusProcessor`/`ServiceBusSessionProcessor` background pump creates on *every* underlying receive poll, whether or not a message
  comes back. It is not correlated to any specific message's trace context (that correlation is carried separately by `ServiceBusProcessor.ProcessMessage`/
  `ServiceBusSessionProcessor.ProcessSessionMessage`, which remain visible), so it typically shows up as several detail-less, single-span traces per delivered message.

`WithCoreExServiceBusTelemetry()` (called from `Program.cs` wherever CoreEx OpenTelemetry tracing is configured, e.g. `builder.WithCoreExTelemetry().WithCoreExServiceBusTelemetry()`) drops all
three activities **by default** using a custom `Sampler`, so they never reach an exporter (Aspire dashboard, OTLP, etc.), while leaving every other activity's sampling behaviour untouched. Pass
`includeBackgroundPollingTelemetry: true` to restore them — useful when actively diagnosing lock-expiry/session-timeout behaviour, or receive-call latency/batch-size:

```csharp
builder.WithCoreExTelemetry()
    .WithCoreExServiceBusTelemetry(includeBackgroundPollingTelemetry: true)  // Opt back in only while diagnosing lock-expiry/session-timeout or receive-poll behaviour.
    .UseOtlpExporter();
```

> **See also**: [`CoreExServiceBusExtensions.WithCoreExServiceBusTelemetry`](../../src/CoreEx.Azure.Messaging.ServiceBus/CoreExServiceBusExtensions.OpenTelemetry.cs) · [OpenTelemetry `Sampler`](https://learn.microsoft.com/en-us/dotnet/api/opentelemetry.trace.sampler)
