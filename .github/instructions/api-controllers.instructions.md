---
applyTo: "**/Controllers/**/*.cs"
---

# API Controller Conventions

## NuGet / Project References

| Package | Key types provided |
|---|---|
| `CoreEx.AspNetCore` | `WebApi`, `[IdempotencyKey]`, `[Accepts<T>]`, `[ProducesNotFoundProblem]`, `[Query]`, `[Paging]`, `HttpNames`, `app.UseCoreExExceptionHandler()`, `app.UseExecutionContext()`, `app.UseIdempotencyKey()`, `app.MapHealthChecks()` |
| `CoreEx.AspNetCore.NSwag` | `[OpenApiTag]`, `app.UseOpenApi()`, `app.UseSwaggerUi()`, `s.AddCoreExConfiguration()` |
| `CoreEx` | `WebApplicationBuilderExtensions.AddHostSettings()`, `AddExecutionContext()` |

## Structure

- Inherit from `ControllerBase`. Never inherit from `Controller` (that brings View support).
- Decorate with `[ApiController]` and `[Route("...")]` on the class.
- Add `[OpenApiTag("TagName")]` to group endpoints in the generated OpenAPI document.
- Inject `WebApi` and the relevant service interface via primary constructor. Guard with `.ThrowIfNull()`.
- Split read operations and write operations into separate controller classes (`ProductController` for mutations, `ProductReadController` for queries) following CQRS conventions.

```csharp
[ApiController, Route("/api/products"), OpenApiTag("Products")]
public class ProductController(WebApi webApi, IProductService service) : ControllerBase
{
    private readonly WebApi _webApi = webApi.ThrowIfNull();
    private readonly IProductService _service = service.ThrowIfNull();
}
```

## Method Signatures

All action methods return `Task<IActionResult>` using the `WebApi` helper. Do not return typed `ActionResult<T>` directly.

| HTTP Verb | WebApi helper | Notes |
|---|---|---|
| `GET` / `HEAD` | `_webApi.GetAsync(...)` | Use both attributes together |
| `POST` | `_webApi.PostAsync<TIn, TOut>(...)` or `PostWithResultAsync` | Add `[IdempotencyKey]` for safe POST |
| `PUT` | `_webApi.PutAsync<TIn, TOut>(...)` | Include ETag via `IF-MATCH` header |
| `PATCH` | `_webApi.PatchAsync<T>(...)` | Requires `get:` and `put:` lambdas |
| `DELETE` | `_webApi.DeleteAsync(...)` | Returns 204 No Content |

## Route Parameters

Validate route parameters inline using `.Required()`:

```csharp
[HttpGet("{id}"), HttpHead("{id}")]
public Task<IActionResult> GetAsync(string id) =>
    _webApi.GetAsync(Request, (_, _) => _service.GetAsync(id.Required()));
```

## POST — Create with Location Header

Use `ro.WithLocationUri(...)` to set the `Location` response header:

```csharp
[HttpPost]
[Accepts<Product>]
[ProducesResponseType<Product>(201)]
[IdempotencyKey]
public Task<IActionResult> PostAsync() => _webApi.PostAsync<Product, Product>(Request, (ro, _) =>
{
    ro.WithLocationUri(p => new Uri($"/api/products/{p.Id}", UriKind.Relative));
    return _service.CreateAsync(ro.Value);
});
```

## PATCH — Merge-Patch

Always supply both `get:` and `put:` delegates. PATCH merges the incoming patch document over the fetched entity and calls `put`:

```csharp
[HttpPatch("{id}")]
[Accepts<Product>(HttpNames.MergePatchJsonMediaTypeName)]
public Task<IActionResult> PatchAsync(string id) => _webApi.PatchAsync<Product>(Request,
    get: (ro, _) => _service.GetAsync(id.Required()),
    put: (ro, _) => _service.UpdateAsync(ro.Value.Adjust(p => p.Id = id)));
```

## Query Endpoints

Expose `QueryArgs` and `PagingArgs` via `[Query]` and `[Paging]` action attributes. Access them via the request options object (`ro`):

```csharp
[HttpGet]
[Query(supportsOrderBy: true), Paging(supportsCount: true)]
public Task<IActionResult> QueryAsync() =>
    _webApi.GetAsync(Request, (ro, _) => _service.QueryAsync(ro.QueryArgs, ro.PagingArgs));
```

## Reference Data Endpoints

Delegate to `ReferenceDataOrchestrator.Current.GetWithFilterAsync<T>()`. Support `codes`, `text`, and `isIncludeInactive` filter parameters:

```csharp
[HttpGet("categories")]
public Task<IActionResult> GetCategoriesAsync([FromQuery] IEnumerable<string>? codes = default, string? text = default)
    => _webApi.GetAsync(Request, (ro, ct) => ReferenceDataOrchestrator.Current.GetWithFilterAsync<Category>(codes, text, ro.IsIncludeInactive, ct));
```

## Response Metadata Attributes

Decorate actions with standard response metadata attributes:

- `[ProducesResponseType<T>(StatusCodes.Status201Created)]`
- `[ProducesNotFoundProblem()]` on GET/PUT/PATCH/DELETE where not-found is expected.
- `[Accepts<T>]` to document the consumed media type.

## Result-Based Services

When the service returns `Result<T>` (Shopping-style domain services), use the `PostWithResultAsync` / `GetWithResultAsync` variants:

```csharp
[HttpPost("{basketId}/checkout")]
public Task<IActionResult> CheckoutAsync(string basketId) =>
    _webApi.PostWithResultAsync<Basket>(Request, (_, _) =>
        _service.CheckoutAsync(basketId.Required()), HttpStatusCode.OK);
```
