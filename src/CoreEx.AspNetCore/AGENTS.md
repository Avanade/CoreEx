# CoreEx.AspNetCore — AI Usage Guide

Provides the `WebApi` HTTP execution helper, exception-to-ProblemDetails middleware, idempotency, and health checks for ASP.NET Core hosts.

## Controllers

Inherit `ControllerBase`. Inject `WebApi` and the application service interface. Use `WebApi` helper methods for all action methods — never return `ActionResult<T>` directly.

```csharp
[ApiController, Route("/api/products"), OpenApiTag("Products")]
public class ProductController(WebApi webApi, IProductService service) : ControllerBase
{
    private readonly WebApi _webApi = webApi.ThrowIfNull();
    private readonly IProductService _service = service.ThrowIfNull();

    [HttpGet("{id}"), ProducesNotFoundProblem]
    public Task<IActionResult> GetAsync(Guid id) =>
        _webApi.GetAsync(Request, () => _service.GetAsync(id));

    [HttpPost, IdempotencyKey]
    public Task<IActionResult> CreateAsync([FromBody] Product product) =>
        _webApi.PostAsync(Request, () => _service.CreateAsync(product),
            statusCode: HttpStatusCode.Created,
            locationUri: r => new Uri($"/api/products/{r!.Id}", UriKind.Relative));

    [HttpPut("{id}")]
    public Task<IActionResult> UpdateAsync(Guid id, [FromBody] Product product) =>
        _webApi.PutAsync(Request, () => _service.UpdateAsync(id, product));

    [HttpDelete("{id}")]
    public Task<IActionResult> DeleteAsync(Guid id) =>
        _webApi.DeleteAsync(Request, () => _service.DeleteAsync(id));
}
```

## PATCH (JSON Merge Patch)

Use `PatchAsync` with a function that loads the current entity for merging.

```csharp
[HttpPatch("{id}")]
public Task<IActionResult> PatchAsync(Guid id) =>
    _webApi.PatchAsync(Request,
        get: _ => _service.GetAsync(id),
        put: product => _service.UpdateAsync(id, product));
```

## ETag / If-Match Concurrency

`PutAsync`/`PatchAsync` automatically require `If-Match` when the request type implements `IETag` (missing → `428`; stale/mismatched → `412`). `PostAsync`/`DeleteAsync` do not auto-require it — they typically have no prior state of their own to match. Where a specific POST/DELETE **is** conditional on a related resource's state (e.g. mutating a sub-resource that must not have changed since it was read), chain `ro.WithIfMatchRequired()` inline at the point of use: it asserts the header immediately and throws `ConcurrencyException` (→ `428`) if missing, regardless of method or whether the request implements `IETag`.

```csharp
// POST with a body — chain into .Value.
[HttpPost("{id}/segments")]
public Task<IActionResult> AddSegmentAsync(string id, CancellationToken cancellationToken = default) =>
    _webApi.PostAsync<BookingSegmentAddRequest, Booking>(Request, (ro, ct)
        => _service.AddSegmentAsync(id.Required(), ro.WithIfMatchRequired().Value, ct), HttpStatusCode.OK, cancellationToken: cancellationToken);

// DELETE with no body — no request DTO exists, so chain into .ETag instead.
[HttpDelete("{id}/segments/{segmentId}")]
public Task<IActionResult> RemoveSegmentAsync(string id, string segmentId, CancellationToken cancellationToken = default) =>
    _webApi.DeleteAsync<Booking>(Request, (ro, ct)
        => _service.RemoveSegmentAsync(id.Required(), segmentId.Required(), ro.WithIfMatchRequired().ETag, ct), cancellationToken: cancellationToken);
```

Request DTOs for the POST-with-body shape implement `IETag` with `[JsonIgnore]` on `ETag` — not the entity `[ReadOnly(true)]` convention — so the value can only arrive via the header, never the body.

## Query / Paged List Endpoints

Use `[Query]` and `[Paging]` attributes; the `WebApi` helper reads them from the request automatically.

```csharp
[HttpGet, Query, Paging]
public Task<IActionResult> GetAllAsync() =>
    _webApi.GetAsync(Request, q => _service.GetAllAsync(q.QueryArgs, q.PagingArgs));
```

## Middleware Registration Order

Order matters — follow this sequence in `Program.cs`:

```csharp
app.UseCoreExExceptionHandler();   // translates IExtendedException → ProblemDetails
app.UseHttpsRedirection();
app.UseAuthorization();
app.UseExecutionContext();         // scopes ExecutionContext per request
app.UseIdempotencyKey();           // must come AFTER UseExecutionContext
app.MapControllers();
app.MapHealthChecks();
```

## Service Registration

```csharp
builder.Services
    .AddExecutionContext()
    .AddMvcWebApi()       // registers Mvc.WebApi + invoker
    .AddHttpWebApi();     // registers Http.WebApi for Minimal API
```

## Do Not

- Do not inherit from `Controller` — use `ControllerBase`.
- Do not return `ActionResult<T>` directly — always delegate to the `WebApi` helper.
- Do not inject `IUnitOfWork` into controllers — it belongs in the application service.
- Do not put business logic in controllers — delegate immediately to the application service.
- Do not call `UseIdempotencyKey()` before `UseExecutionContext()`.
- Do not assume detailed health-check endpoints (`/health/*/detailed`) are enabled — `HealthCheckOptions.AreDetailedEndpointsEnabled` defaults to `false` (secure by default); pass `new HealthCheckOptions { AreDetailedEndpointsEnabled = true }` to `MapHealthChecks(...)` to opt in, and secure it via `detailedGroupConfigure` once an auth scheme is registered.

## Further Reading

- [README](./README.md) — full API surface for `WebApi`, middleware, and health checks.
- [CoreEx](../CoreEx/README.md) — semantic exceptions and `Result<T>` translated by this package.
- [CoreEx.AspNetCore.NSwag](../CoreEx.AspNetCore.NSwag/README.md) — OpenAPI spec generation for CoreEx attributes.
- [Hosts layer](../../samples/docs/hosts-layer.md) — real-world `Program.cs` shape, middleware ordering, and host-specific wiring patterns.
- [Patterns](../../samples/docs/patterns.md) — pattern catalogue for HTTP endpoints, idempotency, paging, and PATCH.
- [Layers overview](../../samples/docs/layers.md) — full layer dependency diagram and host composition rules.
