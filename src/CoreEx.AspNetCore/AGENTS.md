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

## Further Reading

- [README](./README.md) — full API surface for `WebApi`, middleware, and health checks.
- [CoreEx](../CoreEx/README.md) — semantic exceptions and `Result<T>` translated by this package.
- [CoreEx.AspNetCore.NSwag](../CoreEx.AspNetCore.NSwag/README.md) — OpenAPI spec generation for CoreEx attributes.
- [Hosts layer](../../samples/docs/hosts-layer.md) — real-world `Program.cs` shape, middleware ordering, and host-specific wiring patterns.
- [Patterns](../../samples/docs/patterns.md) — pattern catalogue for HTTP endpoints, idempotency, paging, and PATCH.
- [Layers overview](../../samples/docs/layers.md) — full layer dependency diagram and host composition rules.
