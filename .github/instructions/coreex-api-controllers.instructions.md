---
applyTo: "**/Controllers/**/*.cs"
description: "API conventions for CoreEx: MVC ControllerBase and Minimal API approaches, WebApi integration, routing, CQRS separation"
tags: ["controllers", "api", "routing", "cqrs", "dependency-injection", "minimal-api"]
---

# API Conventions

> **Precondition — the Api host must exist.** Controllers live in the `*.Api` host, which is **not** part of the base `coreex` solution. Before authoring a controller, confirm an Api host is present (`**/*.Api/*.Api.csproj`); if it is absent, run the scaffolding workflow first (see `coreex-host-setup.instructions.md` → "Scaffolding an API host") — which confirms creation, generates it via the `coreex-api` template using the recorded solution options, and hands off the `.slnx` wiring as a manual step. Also ensure the entity's application service exists (create per *Service Operations — Confirm Scope* in `coreex-application-services.instructions.md` if not).

CoreEx.AspNetCore supports two approaches for exposing HTTP endpoints. Choose one per host — they can coexist in the same application when needed.

| Approach | Registration | Returns | Best for |
|---|---|---|---|
| **MVC Controllers** | `AddMvcWebApi()` | `IActionResult` | Familiar controller model; NSwag/OpenAPI attributes |
| **Minimal APIs** | `AddHttpWebApi()` | `IResult` | Lightweight; less ceremony; endpoint groups in `Program.cs` |

Both use the same `WebApi` helper — method names, `WithResult` variants, `ro.WithLocationUri`, `.Required()`, and `.Adjust(...)` are identical in both approaches.

## NuGet / Project References

| Package | Key types provided |
|---|---|
| `CoreEx.AspNetCore` | `WebApi`, `[IdempotencyKey]`, `[Accepts<T>]`, `[ProducesNotFoundProblem]`, `[Query]`, `[Paging]`, `HttpNames`; Minimal API: `.WithQuery()`, `.WithPaging()`, `.Accepts<T>()`, `.ProducesNotFoundProblem()`, `.ProducesNoContent()`, `.ProducesCreated<T>()`, `.WithIdempotencyKey()` |
| `CoreEx.AspNetCore.NSwag` | `[OpenApiTag]` |
| `CoreEx` | `.Required()`, `.Adjust(...)` |

---

## MVC Controllers

### Structure

- Inherit from `ControllerBase`. Never inherit from `Controller` (that brings View support).
- Decorate with `[ApiController]` and `[Route("...")]` on the class.
- Inject `WebApi` and the relevant service interface via primary constructor. Guard with `.ThrowIfNull()`.
- **Mirror the service CQRS split:** a **`XxxController`** exposes the **mutating** endpoints (POST/PUT/PATCH/DELETE) and injects `IXxxService`; a **`XxxReadController`** exposes the **read** endpoints (GET/query) and injects `IXxxReadService`.
- **Unify them in OpenAPI with a shared `[OpenApiTag("Xxx")]`.** Put the **same** tag on both controllers so Swagger/OpenAPI presents one logical "Xxx" group — CQRS is an **internal** structuring concern, not something the external API surface should expose. (A tag may also be placed on an individual action to cross-tag it.)

```csharp
// Mutating endpoints
[ApiController, Route("/api/products"), OpenApiTag("Products")]
public class ProductController(WebApi webApi, IProductService service) : ControllerBase
{
    private readonly WebApi _webApi = webApi.ThrowIfNull();
    private readonly IProductService _service = service.ThrowIfNull();
}

// Read endpoints — same route base and same OpenApiTag so they appear as one "Products" group
[ApiController, Route("/api/products"), OpenApiTag("Products")]
public class ProductReadController(WebApi webApi, IProductReadService service) : ControllerBase
{
    private readonly WebApi _webApi = webApi.ThrowIfNull();
    private readonly IProductReadService _service = service.ThrowIfNull();
}
```

### Method Signatures

All action methods return `Task<IActionResult>` using the `WebApi` helper. Do not return typed `ActionResult<T>` directly.

#### Standard (exception-based services)

| HTTP Verb | WebApi helper | Notes |
|---|---|---|
| `GET` / `HEAD` | `_webApi.GetAsync(...)` | Use both attributes together |
| `POST` | `_webApi.PostAsync<TIn, TOut>(...)` | Add `[IdempotencyKey]` for safe POST |
| `PUT` | `_webApi.PutAsync<TIn, TOut>(...)` | Include ETag via `IF-MATCH` header |
| `PATCH` | `_webApi.PatchAsync<T>(...)` | Requires `get:` and `put:` lambdas |
| `DELETE` | `_webApi.DeleteAsync(...)` | Returns 204 No Content |

#### Result-based (`Result<T>` pipeline services)

When the service returns `Result<T>`, use the `WithResult` variants. The controller code is equally thin.

| HTTP Verb | WebApi helper | Notes |
|---|---|---|
| `GET` | `_webApi.GetWithResultAsync(...)` | |
| `POST` (single out) | `_webApi.PostWithResultAsync<TOut>(...)` | |
| `POST` (in + out) | `_webApi.PostWithResultAsync<TIn, TOut>(...)` | Use when body maps to a different output type |
| `PUT` (single out) | `_webApi.PutWithResultAsync<TOut>(...)` | |
| `PUT` (in + out) | `_webApi.PutWithResultAsync<TIn, TOut>(...)` | |
| `DELETE` (typed) | `_webApi.DeleteWithResultAsync<T>(...)` | Use when delete returns the deleted resource |

### Route Parameters

Use `.Required()` to validate route parameters at the point of first use. It **returns the value** when non-default, or throws a `ValidationException` when the value is null/default — which the `WebApi` error handler translates to a **400 validation response** (not a 500). This is the correct treatment: a missing or empty route parameter is a caller error, not a programming error.

```csharp
[HttpGet("{id}"), HttpHead("{id}")]
public Task<IActionResult> GetAsync(string id) =>
    _webApi.GetAsync(Request, (_, _) => _service.GetAsync(id.Required()));
```

Do not use `.ThrowIfNull()` / `.ThrowIfNullOrEmpty()` on route parameters — those throw `ArgumentNullException`, which results in a 500 rather than a 400.

### POST — Create with Location Header

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

> **Agent instruction — confirm idempotency for every POST.** When adding a `POST`, ask whether the operation should be **idempotent** (safe to retry without creating a duplicate). A general **create-style** POST is a strong candidate — default to offering it. If confirmed, decorate the action with **`[IdempotencyKey]`** (MVC) or `.WithIdempotencyKey()` (Minimal API): a retried request carrying the same key then returns the original result instead of creating a second resource. Omit it only when the user confirms the POST is **not** idempotent (e.g. a deliberately non-repeatable command). This applies to `POST` specifically; `PUT`/`PATCH`/`DELETE` are inherently idempotent and do not take the attribute.

For a **full-entity update**, expose **both** endpoints by default — they share the same write `UpdateAsync`:
- **`PUT`** — full replace.
- **`PATCH`** — merge-patch (RFC 7396) over the current entity.

Only implement **specialized/partial** update or patch endpoints when the user **explicitly** asks; the default is the PUT + PATCH pair.

```csharp
[HttpPut("{id}")]
[Accepts<Product>]
public Task<IActionResult> UpdateAsync(string id) => _webApi.PutAsync<Product, Product>(Request,
    (ro, _) => _service.UpdateAsync(ro.Value.Adjust(p => p.Id = id)));
```

`PATCH` always supplies both `get:` and `put:` delegates: it **fetches** the current entity, merges the patch document over it, then calls `put`. The fetch uses the write service's own primary `GetAsync` (`XxxService` exposes a by-id `GetAsync` for exactly this), so the mutating controller depends on a **single** service — no need to also inject `IXxxReadService`:

```csharp
[HttpPatch("{id}")]
[Accepts<Product>(HttpNames.MergePatchJsonMediaTypeName)]
public Task<IActionResult> PatchAsync(string id) => _webApi.PatchAsync<Product>(Request,
    get: (ro, _) => _service.GetAsync(id.Required()),
    put: (ro, _) => _service.UpdateAsync(ro.Value.Adjust(p => p.Id = id)));
```

### Query Endpoints

Expose `QueryArgs` and `PagingArgs` via `[Query]` and `[Paging]` action attributes. Access them via the request options object (`ro`):

```csharp
[HttpGet]
[Query(supportsOrderBy: true), Paging(supportsCount: true)]
public Task<IActionResult> QueryAsync() =>
    _webApi.GetAsync(Request, (ro, _) => _service.QueryAsync(ro.QueryArgs, ro.PagingArgs));
```

### Reference Data Endpoints

Delegate to `ReferenceDataOrchestrator.Current.GetWithFilterAsync<T>()`. Support `codes`, `text`, and `isIncludeInactive` filter parameters:

```csharp
[HttpGet("categories")]
public Task<IActionResult> GetCategoriesAsync([FromQuery] IEnumerable<string>? codes = default, string? text = default)
    => _webApi.GetAsync(Request, (ro, ct) => ReferenceDataOrchestrator.Current.GetWithFilterAsync<Category>(codes, text, ro.IsIncludeInactive, ct));
```

### Response Metadata Attributes

Decorate actions with standard response metadata attributes:

- `[ProducesResponseType<T>(statusCode)]` — preferred generic form for new code.
- `[ProducesResponseType(typeof(T), statusCode)]` — equivalent non-generic form; either is acceptable.
- `[ProducesNotFoundProblem()]` — shorthand for `[ProducesResponseType(typeof(ProblemDetails), 404)]`; use on GET/PUT/PATCH/DELETE where not-found is expected.
- `[Accepts<T>]` — documents the consumed media type.

### Query Schema Endpoint

Read controllers that expose a `QueryAsync` should also expose a `$query` schema endpoint. This returns the JSON schema for the supported query/filter parameters:

```csharp
[HttpGet("$query")]
[ProducesResponseType(typeof(JsonElement), 200)]
public Task<IActionResult> QuerySchemaAsync() =>
    _webApi.GetAsync(Request, (ro, _) => _service.QuerySchemaAsync());
```

### Result-Based Services

When the service returns `Result<T>`, use the `WithResult` variants:

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

---

## Minimal APIs

Register the HTTP variant in `Program.cs` and map endpoints directly — no controller class required. `WebApi` is injected into the handler lambda alongside the service:

```csharp
// Program.cs
builder.Services.AddHttpWebApi(); // or alongside AddMvcWebApi() if both are needed
```

### Attribute → RouteHandlerBuilder Equivalents

MVC action attributes have direct `RouteHandlerBuilder` extension equivalents — chain them after `app.MapGet/Post/etc.`:

| MVC attribute | Minimal API equivalent |
|---|---|
| `[Query(supportsOrderBy: true)]` | `.WithQuery(supportsOrderBy: true)` |
| `[Paging(supportsCount: true)]` | `.WithPaging(supportsCount: true)` |
| `[Accepts<T>]` | `.Accepts<T>()` |
| `[ProducesNotFoundProblem]` | `.ProducesNotFoundProblem()` |
| `[IdempotencyKey]` | `.WithIdempotencyKey()` |

### Examples

**GET by id:**
```csharp
app.MapGet("api/products/{id}",
    (HttpRequest request, WebApi webApi, IProductReadService service, string id)
        => webApi.GetWithResultAsync(request, (_, _) => service.GetAsync(id.Required())))
    .Produces<Product>().ProducesNotFoundProblem();
```

**POST — create with Location header:**
```csharp
app.MapPost("api/products",
    (HttpRequest request, WebApi webApi, IProductService service)
        => webApi.PostWithResultAsync<Product, Product>(request, async (ro, _) =>
        {
            ro.WithLocationUri(p => new Uri($"api/products/{p.Id}", UriKind.Relative));
            return await service.CreateAsync(ro.Value).ConfigureAwait(false);
        }))
    .Accepts<Product>().ProducesCreated<Product>().WithIdempotencyKey();
```

**PUT:**
```csharp
app.MapPut("api/products/{id}",
    (HttpRequest request, WebApi webApi, IProductService service, string id)
        => webApi.PutWithResultAsync<Product, Product>(request, (ro, _) =>
            service.UpdateAsync(ro.Value.Adjust(p => p.Id = id))))
    .Accepts<Product>().Produces<Product>().ProducesNotFoundProblem();
```

**PATCH — JSON Merge-Patch:**
```csharp
app.MapPatch("api/products/{id}",
    (HttpRequest request, WebApi webApi, IProductService service, string id)
        => webApi.PatchWithResultAsync<Product>(request,
            get: (_, _) => service.GetAsync(id.Required()),
            put: (ro, _) => service.UpdateAsync(ro.Value.Adjust(p => p.Id = id))))
    .Accepts<Product>(HttpNames.MergePatchJsonMediaTypeName).Produces<Product>().ProducesNotFoundProblem();
```

**DELETE:**
```csharp
app.MapDelete("api/products/{id}",
    (HttpRequest request, WebApi webApi, IProductService service, string id)
        => webApi.DeleteWithResultAsync(request, (_, _) => service.DeleteAsync(id.Required())))
    .ProducesNoContent();
```

**Query with filtering and paging:**
```csharp
app.MapGet("api/products",
    (HttpRequest request, WebApi webApi, IProductReadService service)
        => webApi.GetWithResultAsync(request, (ro, _) => service.QueryAsync(ro.QueryArgs, ro.PagingArgs)))
    .Produces<ProductLite[]>().WithQuery(supportsOrderBy: true).WithPaging(supportsCount: true);
```

All the same rules apply as for MVC controllers: no business logic in the handler, delegate immediately to the application service, use `.Required()` on route parameters.

---

## Do Not

- Do not inherit from `Controller` — that pulls in View support; use `ControllerBase`.
- Do not return `ActionResult<T>` directly — use the `WebApi` helper for consistent error translation and status-code mapping.
- Do not inject `IUnitOfWork` into controllers or endpoint handlers — it belongs in the application service.
- Do not put business logic in controllers or endpoint handlers — delegate immediately to the application service.
- Do not call `HttpClient` or adapters directly from controllers — go through the application service.
- Do not put mutating and read endpoints in one controller — split into `XxxController` (mutations, `IXxxService`) and `XxxReadController` (reads, `IXxxReadService`).
- Do not expose the CQRS split externally — give both controllers the **same** `[OpenApiTag("Xxx")]` so they surface as one OpenAPI group; do not use distinct tags/route bases per controller.
- Do not omit the `PATCH` when exposing a full `PUT` update — offer both by default; add specialized/partial update or patch endpoints only on explicit request.
- Do not add a `POST` without confirming idempotency — apply `[IdempotencyKey]` (`.WithIdempotencyKey()` for Minimal APIs) when it is idempotent (create-style POSTs almost always are); omit only when the user confirms it is not.

## Further Reading

- [Hosts Layer Guide](https://github.com/Avanade/CoreEx/blob/main/samples/docs/hosts-layer.md) — API host composition, controller patterns, and `Program.cs` shape.
- [CoreEx.AspNetCore README](https://github.com/Avanade/CoreEx/blob/main/src/CoreEx.AspNetCore/README.md) — `WebApi` helper API reference.
- [CoreEx.AspNetCore Mvc README](https://github.com/Avanade/CoreEx/blob/main/src/CoreEx.AspNetCore/Mvc/README.md) — MVC `WebApi` (`IActionResult`-returning), action attributes, and controller patterns.
- [CoreEx.AspNetCore Http README](https://github.com/Avanade/CoreEx/blob/main/src/CoreEx.AspNetCore/Http/README.md) — Minimal API `WebApi` (`IResult`-returning) and `RouteHandlerBuilder` extensions.
