---
name: coreex-api
description: "Add or modify a CoreEx API controller (or Minimal API endpoint) in an *.Api host. USE FOR: scaffolding the MVC controller pair (XxxController + XxxReadController), GET/query/schema endpoints, POST create, PUT + PATCH full-entity update, DELETE, and custom business-action endpoints. Covers both exception-based and Result<T> service styles, and Minimal API as an alternative to MVC. DO NOT USE FOR: Api host setup / Program.cs (use coreex-host-setup), application services (use coreex-app-service), API integration tests (use coreex-test-api)."
argument-hint: "Optional: entity name, operations needed (get/query/create/update/delete/custom), exception-based or Result<T> service style, MVC or Minimal API"
tags: ["api", "controller", "mvc", "minimal-api", "webapi", "routing", "cqrs", "coreex"]
---

# CoreEx: API Controller

Guides you through adding or modifying HTTP API endpoints in an `*.Api` host. Covers the MVC controller pair (mutations + reads), all standard CRUD verbs, custom business-action endpoints, and Minimal API as an alternative.

## When to Use

- Scaffold a new `{Name}Controller` + `{Name}ReadController` pair for an entity
- Add a GET by id, query, or `$query` schema endpoint
- Add a POST create (with Location header and idempotency)
- Add a PUT + PATCH full-entity update pair
- Add a DELETE endpoint
- Add a custom business-action endpoint (POST/PUT that isn't a standard create/replace)
- Convert or add a Minimal API endpoint alternative

## When Not to Use

- Api host setup and `Program.cs` composition — use `coreex-host-setup`
- Application service creation — use `coreex-app-service`
- API integration tests — use `coreex-test-api` (hand off once the endpoint is implemented)
- Subscriber or relay hosts — controllers do not belong there

## Quick Reference

**Clarifying questions before writing any code:**
1. What entity / resource is being exposed? (names the controller and route)
2. Which operations? GET / Query / Create / Update / Delete / custom business action?
3. Is the application service exception-based or `Result<T>` pipeline style? (determines WebApi helper variant)
4. Is a read service (`I{Name}ReadService`) already present, or does it need to be created?
5. MVC controllers or Minimal API?

**Key rules at a glance:**
- Inherit from `ControllerBase` — **never** `Controller` (that adds View support)
- **CQRS split:** `{Name}Controller` (POST/PUT/PATCH/DELETE → `I{Name}Service`) + `{Name}ReadController` (GET/query → `I{Name}ReadService`). Both use the **same route** and **same `[OpenApiTag]`** so they appear as one OpenAPI group
- All action methods return `Task<IActionResult>` via the `WebApi` helper — never `ActionResult<T>` directly
- Route parameter validation: use `.Required()` — **not** `.ThrowIfNull()` (wrong exception type → 500 not 400)
- `ro.Value.Adjust(v => v.Id = id.Required())` — bind route `id` into the deserialized body before passing to the service
- `ro.WithLocationUri(...)` — set the `Location` response header in POST 201 responses
- `[IdempotencyKey]` on every create-style POST — confirm with user; omit only if explicitly non-idempotent
- Always expose **both PUT and PATCH** for full-entity updates; specialised partial-update endpoints only on request
- Every action method takes `CancellationToken cancellationToken = default` — pass to the WebApi helper via `cancellationToken:` and to the service via the lambda's `ct`: `(ro, ct) => _service.XxxAsync(... , ct)`. Never discard with `(ro, _)`
- Exception-based service → standard helpers (`GetAsync`, `PostAsync`, `PutAsync`, …)
- `Result<T>` service → `WithResult` variants (`GetWithResultAsync`, `PostWithResultAsync`, `PutWithResultAsync`, …)
- No business logic in controllers — delegate immediately to the application service
- `[Query(supportsOrderBy: true), Paging(supportsCount: true)]` + `[HttpGet("$query")]` schema endpoint for query operations
- Once the endpoint is implemented, hand off to `coreex-test-api` to add/update its integration test

For full workflow and code examples see [`references/workflow.md`](references/workflow.md).

## Key References

- [`/.github/instructions/coreex-api-controllers.instructions.md`](/.github/instructions/coreex-api-controllers.instructions.md) — authoritative conventions: MVC vs Minimal API, WebApi helpers, attributes, route parameter rules, CQRS split
- [`/samples/src/Contoso.Products.Api/Controllers/`](/samples/src/Contoso.Products.Api/Controllers/) — `ProductController` + `ProductReadController` (exception-based, full CRUD + query + $query)
- [`/samples/src/Contoso.Shopping.Api/Controllers/`](/samples/src/Contoso.Shopping.Api/Controllers/) — `BasketController` + `BasketReadController` (Result&lt;T&gt; style, custom business actions, cross-tagged nested route)
- [`/samples/src/Contoso.Orders.Api/Controllers/`](/samples/src/Contoso.Orders.Api/Controllers/) — `OrderController` (exception-based, custom orchestration action returning 202 Accepted)
- `coreex-test-api` — the integration-test workflow for the endpoints this skill scaffolds
