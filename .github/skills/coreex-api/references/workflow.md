# coreex-api: Workflow

Full workflow for adding or modifying HTTP API endpoints in a CoreEx `*.Api` host using MVC controllers or Minimal API handlers.

---

## Phase 1 — Clarify Before Writing

| Question | Default | Notes |
|---|---|---|
| Entity / resource being exposed? | Ask | Names the controller, route segment, and OpenApiTag |
| Operations needed? | Ask | GET, query, create, update, delete, custom action — never assume Query |
| Service style: exception-based or `Result<T>`? | Ask | Determines which WebApi helper variant to use |
| Read service (`I{Name}ReadService`) already exists? | Ask | Controllers cannot be written without a service to delegate to |
| MVC controllers or Minimal API? | MVC | MVC for most projects; Minimal API for lighter hosts — see Step 7 |

---

## Step 1 — Scaffold the Controller Pair

Create two files in `Controllers/` of the `*.Api` host. Both share the **same route** and the **same `[OpenApiTag]`** so that OpenAPI/Swagger presents them as a single logical group — the CQRS split is internal, not exposed to API consumers.

```csharp
// {Name}Controller.cs — mutations: POST, PUT, PATCH, DELETE
[ApiController, Route("/api/{resources}"), OpenApiTag("{Name}s")]
public class {Name}Controller(WebApi webApi, I{Name}Service service) : ControllerBase
{
    private readonly WebApi _webApi = webApi.ThrowIfNull();
    private readonly I{Name}Service _service = service.ThrowIfNull();
}

// {Name}ReadController.cs — reads: GET, Query, $query schema
[ApiController, Route("/api/{resources}"), OpenApiTag("{Name}s")]
public class {Name}ReadController(WebApi webApi, I{Name}ReadService service) : ControllerBase
{
    private readonly WebApi _webApi = webApi.ThrowIfNull();
    private readonly I{Name}ReadService _service = service.ThrowIfNull();
}
```

**Only one controller needed?** If the entity has no mutation operations (read-only resource), skip `{Name}Controller` entirely and create only `{Name}ReadController`.

---

## Step 2 — GET Endpoints (Read Controller)

### GET by id (+ HEAD for caching support)

**Exception-based:**
```csharp
[HttpGet("{id}"), HttpHead("{id}")]
[ProducesResponseType(typeof({Name}), 200)]
[ProducesNotFoundProblem()]
public Task<IActionResult> GetAsync(string id) =>
    _webApi.GetAsync(Request, (_, _) => _service.GetAsync(id.Required()));
```

**Result&lt;T&gt;:**
```csharp
[HttpGet("{id}"), HttpHead("{id}")]
[ProducesResponseType(typeof({Name}), 200)]
[ProducesNotFoundProblem()]
public Task<IActionResult> GetAsync(string id) =>
    _webApi.GetWithResultAsync(Request, (_, _) => _service.GetAsync(id.Required()));
```

### Query — filtered and paged collection

```csharp
[HttpGet]
[ProducesResponseType(typeof({Name}Lite[]), 200)]
[Query(supportsOrderBy: true), Paging(supportsCount: true)]
public Task<IActionResult> QueryAsync() =>
    _webApi.GetAsync(Request, (ro, _) => _service.QueryAsync(ro.QueryArgs, ro.PagingArgs));
```

Use `_webApi.GetWithResultAsync` when the service returns `Result<ItemsResult<{Name}Lite>>`.

### $query schema endpoint

Always pair with `QueryAsync` — returns the JSON schema for supported filter parameters:

```csharp
[HttpGet("$query")]
[ProducesResponseType(typeof(JsonElement), 200)]
public Task<IActionResult> QuerySchemaAsync() =>
    _webApi.GetAsync(Request, (ro, _) => _service.QuerySchemaAsync());
```

---

## Step 3 — POST — Create

**Ask the user whether this POST should be idempotent before writing it.** Create-style POSTs almost always are — a retried request with the same `[IdempotencyKey]` returns the original result instead of creating a duplicate. Omit `[IdempotencyKey]` only when the user explicitly confirms the operation is intentionally non-idempotent (e.g. a deliberately non-repeatable command).

### Exception-based

```csharp
[HttpPost]
[Accepts<{Name}>]
[ProducesResponseType<{Name}>(201)]
[IdempotencyKey]
public Task<IActionResult> PostAsync() => _webApi.PostAsync<{Name}, {Name}>(Request, (ro, _) =>
{
    ro.WithLocationUri(v => new Uri($"/api/{resources}/{v.Id}", UriKind.Relative));
    return _service.CreateAsync(ro.Value);
});
```

### Result&lt;T&gt;

```csharp
[HttpPost]
[Accepts<{Name}>]
[ProducesResponseType<{Name}>(201)]
[IdempotencyKey]
public Task<IActionResult> PostAsync() => _webApi.PostWithResultAsync<{Name}, {Name}>(Request, async (ro, _) =>
{
    ro.WithLocationUri(v => new Uri($"/api/{resources}/{v.Id}", UriKind.Relative));
    return await _service.CreateAsync(ro.Value).ConfigureAwait(false);
});
```

`ro.WithLocationUri` sets the `Location` response header. The lambda receives the **created entity** (return value of `CreateAsync`) and constructs the URI. Set it before the service call when using a block body; the `WebApi` helper captures it after the service returns.

---

## Step 4 — PUT + PATCH — Full-Entity Update

Expose **both** by default whenever a full-entity update is needed. `PUT` replaces the resource; `PATCH` applies a JSON Merge-Patch (RFC 7396) over the current entity. Only implement specialised or partial-update endpoints on explicit user request.

`PATCH` fetches the current entity via `get:`, merges the patch document, then calls `put:`. The `get:` fetch goes through the **write** service (`I{Name}Service.GetAsync`) — not the read service — so the mutating controller needs only one service dependency.

### Exception-based

```csharp
[HttpPut("{id}")]
[Accepts<{Name}>]
[ProducesResponseType(typeof({Name}), 200)]
[ProducesNotFoundProblem()]
public Task<IActionResult> PutAsync(string id) => _webApi.PutAsync<{Name}, {Name}>(Request, (ro, _)
    => _service.UpdateAsync(ro.Value.Adjust(v => v.Id = id.Required())));

[HttpPatch("{id}")]
[Accepts<{Name}>(HttpNames.MergePatchJsonMediaTypeName)]
[ProducesResponseType(typeof({Name}), 200)]
[ProducesNotFoundProblem()]
public Task<IActionResult> PatchAsync(string id) => _webApi.PatchAsync<{Name}>(Request,
    get: (ro, _) => _service.GetAsync(id.Required()),
    put: (ro, _) => _service.UpdateAsync(ro.Value.Adjust(v => v.Id = id)));
```

### Result&lt;T&gt;

```csharp
[HttpPut("{id}")]
[Accepts<{Name}>]
[ProducesResponseType(typeof({Name}), 200)]
[ProducesNotFoundProblem()]
public Task<IActionResult> PutAsync(string id) => _webApi.PutWithResultAsync<{Name}, {Name}>(Request, (ro, _)
    => _service.UpdateAsync(ro.Value.Adjust(v => v.Id = id.Required())));

[HttpPatch("{id}")]
[Accepts<{Name}>(HttpNames.MergePatchJsonMediaTypeName)]
[ProducesResponseType(typeof({Name}), 200)]
[ProducesNotFoundProblem()]
public Task<IActionResult> PatchAsync(string id) => _webApi.PatchWithResultAsync<{Name}>(Request,
    get: (ro, _) => _service.GetAsync(id.Required()),
    put: (ro, _) => _service.UpdateAsync(ro.Value.Adjust(v => v.Id = id)));
```

`ro.Value.Adjust(v => v.Id = id)` — binds the route id into the deserialized body so the service receives a fully populated entity.

---

## Step 5 — DELETE

### Exception-based

```csharp
[HttpDelete("{id}")]
[ProducesResponseType(204)]
public Task<IActionResult> DeleteAsync(string id) => _webApi.DeleteAsync(Request, (_, _)
    => _service.DeleteAsync(id.Required()));
```

### Result&lt;T&gt; — void delete (204 No Content)

```csharp
[HttpDelete("{id}")]
[ProducesResponseType(204)]
public Task<IActionResult> DeleteAsync(string id) => _webApi.DeleteWithResultAsync(Request, (_, _)
    => _service.DeleteAsync(id.Required()));
```

### Result&lt;T&gt; — delete returning the deleted resource

```csharp
[HttpDelete("{id}")]
[ProducesResponseType(typeof({Name}), 200)]
[ProducesNotFoundProblem()]
public Task<IActionResult> DeleteAsync(string id) => _webApi.DeleteWithResultAsync<{Name}>(Request, (_, _)
    => _service.DeleteAsync(id.Required()));
```

---

## Step 6 — Custom Business Actions

Non-CRUD operations — state transitions, orchestration triggers, sub-resource mutations — belong in `{Name}Controller`. They are always `POST` (triggers with or without a body) or `PUT` (state transitions with extra parameters).

### POST — no request body, returns the updated entity

```csharp
[HttpPost("{id}/activate")]
[ProducesResponseType(typeof({Name}), 200)]
[ProducesNotFoundProblem()]
public Task<IActionResult> ActivateAsync(string id) => _webApi.PostAsync<{Name}>(Request, (_, _)
    => _service.ActivateAsync(id.Required()), HttpStatusCode.OK);
```

Use `_webApi.PostWithResultAsync<{Name}>` when the service returns `Result<{Name}>`.

### POST — with request body, returns the aggregate

```csharp
[HttpPost("{id}/action")]
[IdempotencyKey]
[Accepts<{ActionRequest}>]
[ProducesResponseType(typeof({Name}), 200)]
public Task<IActionResult> ActionAsync(string id) => _webApi.PostWithResultAsync<{ActionRequest}, {Name}>(Request, (ro, _)
    => _service.ActionAsync(id.Required(), ro.Value), HttpStatusCode.OK);
```

### PUT — state transition with extra route param

```csharp
[HttpPut("{id}/apply-{thing}")]
[ProducesResponseType(typeof({Name}), 200)]
[ProducesNotFoundProblem()]
public Task<IActionResult> ApplyThingAsync(string id, string thing) => _webApi.PutWithResultAsync<{Name}>(Request, (_, _)
    => _service.ApplyThingAsync(id.Required(), thing.Required()));
```

### Cross-tagged nested resource (advanced)

When an action belongs semantically to a different OpenAPI group or lives under a different parent resource, override the route and add an extra `[OpenApiTag]`:

```csharp
// Defined in {Name}Controller but tagged as "ParentResources" and routed under /api/parents/
[OpenApiTag("ParentResources")]
[HttpPost("/api/parents/{parentId}/{resources}")]
[IdempotencyKey]
[ProducesResponseType<{Name}>(201)]
public Task<IActionResult> CreateForParentAsync(string parentId) => _webApi.PostWithResultAsync<{Name}>(Request, async (ro, _) =>
{
    ro.WithLocationUri(v => new Uri($"/api/{resources}/{v.Id}", UriKind.Relative));
    return await _service.CreateForParentAsync(parentId.Required()).ConfigureAwait(false);
});
```

An absolute route path (`/api/parents/{parentId}/...`) overrides the controller's base `[Route]`.

---

## Step 7 — Minimal API (Alternative to MVC)

Register `AddHttpWebApi()` in `Program.cs` instead of (or alongside) `AddMvcWebApi()`. Map endpoints directly — no controller class required. MVC attributes have direct `RouteHandlerBuilder` extension equivalents.

| MVC attribute | Minimal API extension |
|---|---|
| `[Query(supportsOrderBy: true)]` | `.WithQuery(supportsOrderBy: true)` |
| `[Paging(supportsCount: true)]` | `.WithPaging(supportsCount: true)` |
| `[Accepts<T>]` | `.Accepts<T>()` |
| `[ProducesNotFoundProblem]` | `.ProducesNotFoundProblem()` |
| `[IdempotencyKey]` | `.WithIdempotencyKey()` |

```csharp
// GET by id
app.MapGet("api/{resources}/{id}",
    (HttpRequest request, WebApi webApi, I{Name}ReadService service, string id)
        => webApi.GetWithResultAsync(request, (_, _) => service.GetAsync(id.Required())))
    .Produces<{Name}>().ProducesNotFoundProblem();

// POST — create
app.MapPost("api/{resources}",
    (HttpRequest request, WebApi webApi, I{Name}Service service)
        => webApi.PostWithResultAsync<{Name}, {Name}>(request, async (ro, _) =>
        {
            ro.WithLocationUri(v => new Uri($"api/{resources}/{v.Id}", UriKind.Relative));
            return await service.CreateAsync(ro.Value).ConfigureAwait(false);
        }))
    .Accepts<{Name}>().ProducesCreated<{Name}>().WithIdempotencyKey();

// PUT
app.MapPut("api/{resources}/{id}",
    (HttpRequest request, WebApi webApi, I{Name}Service service, string id)
        => webApi.PutWithResultAsync<{Name}, {Name}>(request, (ro, _) =>
            service.UpdateAsync(ro.Value.Adjust(v => v.Id = id))))
    .Accepts<{Name}>().Produces<{Name}>().ProducesNotFoundProblem();

// PATCH
app.MapPatch("api/{resources}/{id}",
    (HttpRequest request, WebApi webApi, I{Name}Service service, string id)
        => webApi.PatchWithResultAsync<{Name}>(request,
            get: (_, _) => service.GetAsync(id.Required()),
            put: (ro, _) => service.UpdateAsync(ro.Value.Adjust(v => v.Id = id))))
    .Accepts<{Name}>(HttpNames.MergePatchJsonMediaTypeName).Produces<{Name}>().ProducesNotFoundProblem();

// DELETE
app.MapDelete("api/{resources}/{id}",
    (HttpRequest request, WebApi webApi, I{Name}Service service, string id)
        => webApi.DeleteWithResultAsync(request, (_, _) => service.DeleteAsync(id.Required())))
    .ProducesNoContent();

// Query
app.MapGet("api/{resources}",
    (HttpRequest request, WebApi webApi, I{Name}ReadService service)
        => webApi.GetWithResultAsync(request, (ro, _) => service.QueryAsync(ro.QueryArgs, ro.PagingArgs)))
    .Produces<{Name}Lite[]>().WithQuery(supportsOrderBy: true).WithPaging(supportsCount: true);
```

All the same rules apply: `.Required()` on route params, no business logic in handlers, `ro.Value.Adjust(...)` for PUT/PATCH id binding.

---

## Phase 2 — Validate

1. `dotnet build` — no errors or warnings.
2. Confirm both `{Name}Controller` and `{Name}ReadController` share the same `[Route]` and `[OpenApiTag]`.
3. Every route parameter uses `.Required()` — search for `.ThrowIfNull()` on route params and replace.
4. POST endpoints have `[IdempotencyKey]` (or user has explicitly declined it).
5. PUT + PATCH are both present for full-entity update (unless partial-update is the explicit ask).
6. No business logic in the controller body — all lambda bodies delegate immediately to the service.
7. No `IUnitOfWork`, `HttpClient`, or adapter types injected into the controller.
8. `WithResult` variants used throughout when the service style is `Result<T>`; standard variants for exception-based.
9. `[Accepts<T>]` present on POST/PUT/PATCH actions; `[ProducesNotFoundProblem()]` on GET/PUT/PATCH/DELETE where applicable.

---

## Guardrails

- **Never inherit from `Controller`** — always `ControllerBase`. `Controller` pulls in View support which is never needed in an API host.
- **Never return `ActionResult<T>` directly** — the `WebApi` helper provides consistent error translation, ETag handling, and status-code mapping; bypassing it breaks these.
- **Never put mutating and read endpoints in the same controller** — split into `{Name}Controller` (mutations) and `{Name}ReadController` (reads) so each has a single service dependency.
- **Never give the mutation and read controllers different `[OpenApiTag]` values** — the CQRS split is internal; the API surface must look unified.
- **Never use `.ThrowIfNull()` on route parameters** — it throws `ArgumentNullException` (→ 500). Use `.Required()` which throws `ValidationException` (→ 400).
- **Never inject `IUnitOfWork`, `HttpClient`, adapters, or policies** into a controller — those belong in the application service.
- **Never skip `[IdempotencyKey]` on a create-style POST without confirmation** — retried POSTs without it create duplicates.
- **Never expose only PUT without PATCH** (or vice versa) for a full-entity update — always provide the pair unless a specialised partial endpoint is explicitly requested.
- **Never mix standard and `WithResult` helpers** in the same controller — the style choice follows the service interface.
- **Never put business logic in the controller** — the lambda body must be a single delegate call to the application service. If you find yourself writing `if`, `try/catch`, or multiple service calls, stop and move the logic to the service.
