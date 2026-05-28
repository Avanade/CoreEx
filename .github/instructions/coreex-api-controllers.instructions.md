---
applyTo: "**/Controllers/**/*.cs"
description: "API controller conventions for CoreEx: inheritance, routing, dependency injection, CQRS separation, and WebApi integration"
tags: ["controllers", "api", "routing", "cqrs", "dependency-injection"]
---

# API Controller Conventions

## NuGet / Project References

| Package | Key types provided |
|---|---|
| `CoreEx.AspNetCore` | `WebApi`, `[IdempotencyKey]`, `[Accepts<T>]`, `[ProducesNotFoundProblem]`, `[Query]`, `[Paging]`, `HttpNames`, `.Required()`, `.Adjust(...)` |
| `CoreEx.AspNetCore.NSwag` | `[OpenApiTag]` |

## Structure

- Inherit from `ControllerBase`. Never inherit from `Controller` (that brings View support).
- Decorate with `[ApiController]` and `[Route("...")]` on the class.
- Add `[OpenApiTag("TagName")]` to group endpoints in the generated OpenAPI document. Can also be placed on an individual action method to cross-tag it into a different OpenAPI group.
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

### Standard (exception-based services — Products style)

| HTTP Verb | WebApi helper | Notes |
|---|---|---|
| `GET` / `HEAD` | `_webApi.GetAsync(...)` | Use both attributes together |
| `POST` | `_webApi.PostAsync<TIn, TOut>(...)` | Add `[IdempotencyKey]` for safe POST |
| `PUT` | `_webApi.PutAsync<TIn, TOut>(...)` | Include ETag via `IF-MATCH` header |
| `PATCH` | `_webApi.PatchAsync<T>(...)` | Requires `get:` and `put:` lambdas |
| `DELETE` | `_webApi.DeleteAsync(...)` | Returns 204 No Content |

### Result-based (`Result<T>` pipeline services — Shopping style)

When the service returns `Result<T>`, use the `WithResult` variants. The controller code is equally thin.

| HTTP Verb | WebApi helper | Notes |
|---|---|---|
| `GET` | `_webApi.GetWithResultAsync(...)` | |
| `POST` (single out) | `_webApi.PostWithResultAsync<TOut>(...)` | |
| `POST` (in + out) | `_webApi.PostWithResultAsync<TIn, TOut>(...)` | Use when body maps to a different output type |
| `PUT` (single out) | `_webApi.PutWithResultAsync<TOut>(...)` | |
| `PUT` (in + out) | `_webApi.PutWithResultAsync<TIn, TOut>(...)` | |
| `DELETE` (typed) | `_webApi.DeleteWithResultAsync<T>(...)` | Use when delete returns the deleted resource |

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

- `[ProducesResponseType<T>(statusCode)]` — preferred generic form for new code.
- `[ProducesResponseType(typeof(T), statusCode)]` — equivalent non-generic form; either is acceptable.
- `[ProducesNotFoundProblem()]` — shorthand for `[ProducesResponseType(typeof(ProblemDetails), 404)]`; use on GET/PUT/PATCH/DELETE where not-found is expected.
- `[Accepts<T>]` — documents the consumed media type.

## Query Schema Endpoint

Read controllers that expose a `QueryAsync` should also expose a `$query` schema endpoint. This returns the JSON schema for the supported query/filter parameters:

```csharp
[HttpGet("$query")]
[ProducesResponseType(typeof(JsonElement), 200)]
public Task<IActionResult> QuerySchemaAsync() =>
    _webApi.GetAsync(Request, (ro, _) => _service.QuerySchemaAsync());
```

## Result-Based Services

When the service returns `Result<T>` (Shopping-style domain services), use the `WithResult` variants. See the Method Signatures table above for the full variant list.

```csharp
[HttpPost("{basketId}/checkout")]
[ProducesResponseType(typeof(Basket), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
public Task<IActionResult> CheckoutAsync(string basketId) =>
    _webApi.PostWithResultAsync<Basket>(Request, (_, _) =>
        _service.CheckoutAsync(basketId.Required()), HttpStatusCode.OK);

[HttpPost("{basketId}/items")]
[IdempotencyKey]
[Accepts<BasketItemAddRequest>]
[ProducesResponseType(typeof(Basket), StatusCodes.Status200OK)]
public Task<IActionResult> ItemAddAsync(string basketId) =>
    _webApi.PostWithResultAsync<BasketItemAddRequest, Basket>(Request, (ro, _) =>
        _service.ItemAddAsync(basketId.Required(), ro.Value), HttpStatusCode.OK);
```

## Do Not

- Do not inherit from `Controller` — that pulls in View support; use `ControllerBase`.
- Do not return `ActionResult<T>` directly — use the `WebApi` helper for consistent error translation and status-code mapping.
- Do not inject `IUnitOfWork` into controllers — it belongs in the application service.
- Do not put business logic in controllers — delegate immediately to the application service.
- Do not call `HttpClient` or adapters directly from controllers — go through the application service.

## Further Reading

- [`samples/docs/hosts-layer.md`](../../samples/docs/hosts-layer.md) — API host composition, controller patterns, and `Program.cs` shape.
- [`src/CoreEx.AspNetCore/README.md`](../../src/CoreEx.AspNetCore/README.md) — `WebApi` helper API reference.
