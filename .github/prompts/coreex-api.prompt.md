---
description: Add or modify a CoreEx API controller (MVC or Minimal API) — CQRS controller pair, CRUD endpoints, custom business actions, Result<T> variants
---

Guide this workspace through adding or modifying a CoreEx API controller or Minimal API endpoint.

Use `.github/skills/coreex-api/SKILL.md` and its referenced workflow as the authoritative workflow when they exist.

Operational contract:
- Ask upfront: entity/resource name, operations needed (get/query/create/update/delete/custom), service style (exception-based or Result<T>), read service present, MVC or Minimal API.
- Scaffold the CQRS pair: `{Name}Controller` (mutations, `I{Name}Service`) + `{Name}ReadController` (reads, `I{Name}ReadService`). Same route, same `[OpenApiTag]`.
- Inherit from `ControllerBase` — never `Controller`.
- All action methods return `Task<IActionResult>` via the WebApi helper — never `ActionResult<T>` directly.
- Route parameters: use `.Required()` — never `.ThrowIfNull()` (wrong exception type → 500 not 400).
- `ro.Value.Adjust(v => v.Id = id)` — bind route id into request body before passing to service.
- `ro.WithLocationUri(...)` — set Location header in POST 201 responses.
- `[IdempotencyKey]` on every create-style POST — ask the user; omit only when explicitly non-idempotent.
- Always offer PUT + PATCH together for full-entity updates; specialised/partial endpoints only on explicit request.
- Exception-based service → standard helpers (`GetAsync`, `PostAsync`, `PutAsync`, `PatchAsync`, `DeleteAsync`).
- Result<T> service → `WithResult` variants (`GetWithResultAsync`, `PostWithResultAsync`, etc.). Never mix.
- Query endpoints: `[Query(supportsOrderBy: true), Paging(supportsCount: true)]` + pair with `[HttpGet("$query")]` schema endpoint.
- No business logic in controllers — delegate immediately to the application service.
- No `IUnitOfWork`, `HttpClient`, adapters, or policies injected into controllers.
- If any prompt text conflicts with the skill, the skill wins.

Outcome:
- Controllers are placed correctly in the `*.Api` host, use the WebApi helper consistently, expose a unified OpenAPI surface, and build without warnings.
