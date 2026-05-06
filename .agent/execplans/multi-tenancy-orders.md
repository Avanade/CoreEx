# Add header-driven multi-tenancy to Contoso.Orders using tenant-specific SQL schemas

This ExecPlan is a living document. The sections `Progress`, `Surprises & Discoveries`, `Decision Log`, and `Outcomes & Retrospective` must be kept up to date as work proceeds. Updates to this plan should be reflected in `.agent/PLANS.md` as implementation progresses.

## Purpose / Big Picture

After this change, the sample Orders system will behave like a multi-tenant service. A caller will send a tenant identifier in an HTTP header, the API will place that tenant identifier into `CoreEx.ExecutionContext`, and all order reads, writes, workflow starts, and related test data will resolve to that tenant's own SQL schema instead of a shared `Orders` schema. A user will be able to create an order under tenant `tenanta`, then make the same read under tenant `tenantb` and observe a `404 Not Found` because the second tenant's schema does not contain that order.

The most important user-visible proof is simple. Start the Orders API, send a `POST /api/orders` request with header `X-Tenant-Id: tenanta`, then send `GET /api/orders/{id}` with the same header and observe the created order. Repeat the `GET` with `X-Tenant-Id: tenantb` and observe `404 Not Found`. That proves tenant context is flowing from HTTP into execution state and that data isolation is real, not cosmetic.

## Progress

- [x] (2026-05-06 00:00Z) Captured the implementation direction with the requester: separate SQL schema per tenant, tenant flows from an HTTP header into `ExecutionContext`, cross-tenant access must be invisible as `404`, and scope includes CRUD, workflow, and database seed data.
- [x] (2026-05-06 00:00Z) Inspected the current Orders API, application, repository, workflow, database, and API tests to identify the controlling files and current single-schema assumptions.
- [ ] (2026-05-06 00:00Z) Add request-level tenant header handling for the Orders API and reject missing or invalid tenant headers before business logic runs.
- [ ] (2026-05-06 00:00Z) Introduce tenant-aware schema resolution for Orders aggregate tables and migrate test data into multiple tenant schemas.
- [ ] (2026-05-06 00:00Z) Propagate tenant context through order workflow start and worker execution.
- [ ] (2026-05-06 00:00Z) Add and update API tests that prove same-tenant success and cross-tenant `404` behavior.
- [ ] (2026-05-06 00:00Z) Run focused validation for Orders unit tests, Orders API tests, and manual HTTP scenarios.

## Surprises & Discoveries

- Observation: The Orders sample already creates an `ExecutionContext` per request through `UseExecutionContext()` in `samples/src/Contoso.Orders.Api/Program.cs`, but the default middleware does not populate `ExecutionContext.TenantId`.
  Evidence:
    `builder.Services.AddExecutionContext()` and `app.UseExecutionContext()` already exist in `samples/src/Contoso.Orders.Api/Program.cs`.

- Observation: The Orders aggregate tables are hard-wired to schema `Orders`, so current data isolation is impossible without changing schema resolution.
  Evidence:
    `samples/src/Contoso.Orders.Infrastructure/Repositories/OrdersDbContext.cs` uses `e.ToTable("Order", "Orders")` and `e.ToTable("OrderItem", "Orders")`.

- Observation: The Orders outbox already stores a `TenantId`, so event publication is closer to multi-tenant-ready than the aggregate data itself.
  Evidence:
    `samples/src/Contoso.Orders.Database/Migrations/20260101-000301-create-orders-outbox-tables.sql` defines `[TenantId]` in outbox tables and outbox stored procedures accept `@TenantId`.

- Observation: The current workflow contracts do not carry any tenant identifier.
  Evidence:
    `samples/src/Contoso.Order.Workflow.Workflow/Contracts/OrderWorkflowContracts.cs` defines `OrderWorkflowRequest(string OrderId, decimal Amount, string Currency, string? RequestedBy = null)` with no tenant field.

## Decision Log

- Decision: Use the HTTP request header `X-Tenant-Id` as the tenant source for the Orders API.
  Rationale: The requester specified header-based tenant flow, and a dedicated header is the least invasive fit for the current sample because the routes are currently `/api/orders` and no authentication-based tenant claims are wired here.
  Date/Author: 2026-05-06 / GitHub Copilot

- Decision: Return `404 Not Found` for cross-tenant reads and mutations instead of exposing a tenant mismatch error.
  Rationale: The requester explicitly chose invisible cross-tenant behavior. The simplest way to achieve that is to resolve all data access within the current tenant's schema so another tenant's records are naturally absent.
  Date/Author: 2026-05-06 / GitHub Copilot

- Decision: Treat mutable aggregate tables `Order` and `OrderItem` as tenant-schema-scoped, while keeping shared reference data and the shared outbox in schema `Orders`.
  Rationale: Reference data such as order status codes is global sample metadata, not customer-owned data, and the outbox is already tenant-partitioned by row with existing stored procedure support. Moving the outbox into per-tenant schemas would add substantial infrastructure churn without improving the user-visible tenant isolation required here.
  Date/Author: 2026-05-06 / GitHub Copilot

- Decision: Require `X-Tenant-Id` on all Orders API endpoints and treat a missing or invalid tenant header as a client error.
  Rationale: Separate-schema lookup cannot proceed safely without a tenant identifier. A fallback default tenant would hide configuration mistakes and weaken the demonstration of explicit tenant isolation.
  Date/Author: 2026-05-06 / GitHub Copilot

- Decision: Use SQL-safe tenant identifiers directly as schema names for the sample, and validate them before schema resolution.
  Rationale: A mapping table adds another subsystem that the sample does not currently need. Limiting the sample tenants to SQL-safe identifiers such as `tenanta` and `tenantb` keeps the implementation observable and beginner-friendly.
  Date/Author: 2026-05-06 / GitHub Copilot

## Outcomes & Retrospective

- Outcome: Not implemented yet.
  Evidence: This document was produced before implementation began.
  Remaining gap: All implementation and validation work remains.

## Context and Orientation

The Orders sample is split across several projects under `samples/src`. The HTTP API host is `samples/src/Contoso.Orders.Api`. The application services that hold validation and unit-of-work behavior are in `samples/src/Contoso.Orders.Application`. The Entity Framework Core repository and persistence model live in `samples/src/Contoso.Orders.Infrastructure`. The SQL migration console project is `samples/src/Contoso.Orders.Database`. The API integration tests are in `samples/tests/Contoso.Orders.Test.Api`, and shared seed data for those tests is in `samples/tests/Contoso.Orders.Test.Common/Data/data.yaml`.

A "schema" in SQL Server is a named namespace inside one database. Right now the Orders sample stores all order rows in the `Orders` schema because `samples/src/Contoso.Orders.Infrastructure/Repositories/OrdersDbContext.cs` maps `Persistence.Order` and `Persistence.OrderItem` to `ToTable(..., "Orders")`. This means all tenants would currently share the same physical tables. The required change is to use a different schema per tenant, such as `tenanta` and `tenantb`, while keeping the same table names inside each schema.

`ExecutionContext` is a CoreEx per-request state object. It already exists in the Orders API because `samples/src/Contoso.Orders.Api/Program.cs` calls `AddExecutionContext()` and `UseExecutionContext()`. The missing behavior is populating `ExecutionContext.TenantId` from the incoming HTTP header and then using that value everywhere data access or workflow execution needs tenant identity.

The current Orders API routes are defined in `samples/src/Contoso.Orders.Api/Controllers/OrderController.cs` and `samples/src/Contoso.Orders.Api/Controllers/OrderReadController.cs`. They use `/api/orders` and do not currently require a tenant header. The workflow start endpoint is `POST /api/orders/orchestrate` in the same controller file, and it delegates to `samples/src/Contoso.Order.Workflow.Client/OrderWorkflowClient.cs`, which starts the durable orchestration defined in `samples/src/Contoso.Order.Workflow.Workflow/OrderWorkflowOrchestration.cs`.

The current test seed loads one order into the shared `Orders` schema through `samples/tests/Contoso.Orders.Test.Common/Data/data.yaml`. The API tests in `samples/tests/Contoso.Orders.Test.Api` currently call endpoints without any tenant header and therefore must be updated to set tenant headers explicitly and to seed at least two tenant schemas.

## Plan of Work

Begin with a narrow infrastructure slice that makes tenant identity explicit at the API boundary. Update `samples/src/Contoso.Orders.Api/Program.cs` so `UseExecutionContext(...)` reads the `X-Tenant-Id` request header, validates that it is present and SQL-schema-safe, and stores it in `ExecutionContext.TenantId`. The validation must be strict enough to reject values that would produce unsafe or invalid schema names. The API should fail fast with a `400 Bad Request` when the header is missing or malformed because the system cannot resolve a tenant schema without it.

Once tenant identity is available in `ExecutionContext`, make repository access tenant-aware by introducing a schema resolver abstraction in `samples/src/Contoso.Orders.Infrastructure`. The simplest beginner-friendly design is a small service such as `ITenantSchemaResolver` plus an implementation that reads `ExecutionContext.TenantId`, validates it again defensively, and returns the schema name to use for aggregate tables. Update `samples/src/Contoso.Orders.Infrastructure/Repositories/OrdersDbContext.cs` so `Persistence.Order` and `Persistence.OrderItem` map to the current tenant schema instead of hard-coded `"Orders"`. Keep `OrderStatus` in shared schema `Orders` because it is global reference data, not tenant-owned order data.

Add a short proof-of-concept milestone before broadening the change. The proof should demonstrate that the same repository code can read tenant `tenanta.Order` and tenant `tenantb.Order` based only on `ExecutionContext.TenantId`. This can be done with a focused API test first, because the API test harness already migrates SQL data and clears cache. If the proof fails because EF model caching freezes the first schema name it sees, add an EF model cache key customization tied to the resolved tenant schema so each tenant schema gets its own cached model shape.

After tenant-aware table resolution works, update the database utility and seed data. Extend `samples/src/Contoso.Orders.Database/Program.cs` and the migration scripts in `samples/src/Contoso.Orders.Database/Migrations` so known tenant schemas for local development and tests are created idempotently. Add creation scripts for at least `tenanta` and `tenantb`, and create `Order` and `OrderItem` tables in both schemas. Keep `OrderStatus` and outbox objects in `Orders`. Update `samples/tests/Contoso.Orders.Test.Common/Data/data.yaml` so it seeds representative order rows into `tenanta` and `tenantb`, not just the shared `Orders` schema. The seed data must make cross-tenant `404` verification obvious by storing different order identifiers or by ensuring `ORD-1001` exists in only one tenant.

Then update the workflow path. Extend `samples/src/Contoso.Order.Workflow.Workflow/Contracts/OrderWorkflowContracts.cs` so `OrderWorkflowRequest` and any activity inputs that need tenant identity include `TenantId`. Update `samples/src/Contoso.Orders.Api/Controllers/OrderController.cs` so the orchestration request sent to `OrderWorkflowClient.StartAsync(...)` is stamped with the current `ExecutionContext.TenantId` even if the client body does not provide one. Update the worker in `samples/src/Contoso.Order.Workflow.Worker/Program.cs` or the activity layer so a fresh `ExecutionContext` is created and populated from the workflow payload when activities execute. That keeps telemetry, future data access, and any emitted events tenant-correct. Even though the current `SubmitOrderActivity` only returns a message, this step is necessary because the scope explicitly includes workflow and because future workflow activities would otherwise silently lose tenant context.

Finally, update the API tests so tenant behavior is proven end-to-end. Extend the existing read and mutate test files under `samples/tests/Contoso.Orders.Test.Api` to send `X-Tenant-Id` headers. Add tests that create an order under one tenant and verify `404` under another. Add tests that missing headers fail cleanly with `400`. Add a workflow API test that starts orchestration with a tenant header and verifies the payload or metadata contains the expected tenant. Keep the tests scoped to the Orders sample so the rest of the solution is not disturbed.

## Concrete Steps

All paths are relative to the repository root.

1. Inspect the API host and current request pipeline.

   Command: `dotnet build samples/src/Contoso.Orders.Api/Contoso.Orders.Api.csproj`

   Expected result: the Orders API project builds cleanly before changes, confirming the starting point is stable.

   Files to inspect while implementing:
   `samples/src/Contoso.Orders.Api/Program.cs`
   `samples/src/Contoso.Orders.Api/Controllers/OrderController.cs`
   `samples/src/Contoso.Orders.Api/Controllers/OrderReadController.cs`

2. Implement tenant header handling and schema resolution.

   Edit `samples/src/Contoso.Orders.Api/Program.cs` to configure `UseExecutionContext(...)` so it reads `X-Tenant-Id` and writes `ExecutionContext.TenantId`.

   Add infrastructure types in `samples/src/Contoso.Orders.Infrastructure`, for example:
   `samples/src/Contoso.Orders.Infrastructure/Repositories/ITenantSchemaResolver.cs`
   `samples/src/Contoso.Orders.Infrastructure/Repositories/TenantSchemaResolver.cs`

   Update `samples/src/Contoso.Orders.Infrastructure/Repositories/OrdersDbContext.cs` so `Order` and `OrderItem` map to the resolved tenant schema.

   If EF model caching prevents per-tenant schema changes from taking effect, add a model-cache-key type and register it from `OrdersDbContext`.

3. Update database migrations and seed data.

   Files to edit:
   `samples/src/Contoso.Orders.Database/Program.cs`
   `samples/src/Contoso.Orders.Database/Migrations/20260101-000001-create-orders-schema.sql`
   `samples/src/Contoso.Orders.Database/Migrations/20260101-000201-create-orders-order.sql`
   `samples/src/Contoso.Orders.Database/Migrations/20260101-000202-create-orders-orderitem.sql`
   `samples/tests/Contoso.Orders.Test.Common/Data/data.yaml`

   The schema-creation scripts must be safe to run more than once. Prefer `IF NOT EXISTS` checks before creating schemas or tables.

4. Propagate tenant context through workflow.

   Files to edit:
   `samples/src/Contoso.Order.Workflow.Workflow/Contracts/OrderWorkflowContracts.cs`
   `samples/src/Contoso.Orders.Api/Controllers/OrderController.cs`
   `samples/src/Contoso.Order.Workflow.Client/OrderWorkflowClient.cs` if helper overloads are needed
   `samples/src/Contoso.Order.Workflow.Workflow/OrderWorkflowOrchestration.cs`
   `samples/src/Contoso.Order.Workflow.Workflow/Activities/SubmitOrderActivity.cs`
   `samples/src/Contoso.Order.Workflow.Worker/Program.cs`

5. Update tests and run focused validation.

   Command: `dotnet test samples/tests/Contoso.Orders.Test.Unit/Contoso.Orders.Test.Unit.csproj`

   Expected result: validator and any new unit tests pass.

   Command: `dotnet test samples/tests/Contoso.Orders.Test.Api/Contoso.Orders.Test.Api.csproj --filter "FullyQualifiedName~Contoso.Orders.Test.Api"`

   Expected result: Orders API tests pass, including the new same-tenant success and cross-tenant `404` scenarios.

6. Run the API manually and verify behavior.

   Command: `dotnet run --project samples/src/Contoso.Orders.Api/Contoso.Orders.Api.csproj`

   Expected result: the server starts on `https://localhost:62023` and `http://localhost:62024` according to `samples/src/Contoso.Orders.Api/Properties/launchSettings.json`.

   After startup, run these manual checks from another shell:

     Request:
       curl -k -H "X-Tenant-Id: tenanta" https://localhost:62023/api/orders/ORD-1001
     Expected result:
       HTTP 200 and a JSON body for the order seeded in `tenanta`.

     Request:
       curl -k -H "X-Tenant-Id: tenantb" https://localhost:62023/api/orders/ORD-1001
     Expected result:
       HTTP 404 because `tenantb` resolves to a different schema.

     Request:
       curl -k https://localhost:62023/api/orders/ORD-1001
     Expected result:
       HTTP 400 because `X-Tenant-Id` is required.

## Validation and Acceptance

Acceptance is behavior, not just compilation. The change is complete only when all of the following are true.

A request with `X-Tenant-Id: tenanta` can create an order through `POST /api/orders`, then read that same order back through `GET /api/orders/{id}` using the same header, and the API returns `201 Created` followed by `200 OK`.

A request for that same order using `X-Tenant-Id: tenantb` returns `404 Not Found`. This proves that tenant isolation is enforced by schema resolution instead of by a visible authorization error.

A request to any Orders endpoint without `X-Tenant-Id` returns `400 Bad Request` with a clear problem response explaining that the tenant header is required.

Running `dotnet test samples/tests/Contoso.Orders.Test.Api/Contoso.Orders.Test.Api.csproj` passes, and at least one new test demonstrates cross-tenant invisibility. A good concrete target is to extend `samples/tests/Contoso.Orders.Test.Api/ReadTests.OrderGet.cs` with a test that reads a seeded `tenanta` order using tenant `tenantb` and expects `404`.

Running `dotnet test samples/tests/Contoso.Orders.Test.Unit/Contoso.Orders.Test.Unit.csproj` still passes.

The workflow start path `POST /api/orders/orchestrate` accepts a tenant-scoped request, carries tenant identity into the workflow payload, and does not lose tenant information between API and worker boundaries. The proof can be a focused test or an assertion on stored orchestration input, plus logging that includes the tenant identifier.

## Idempotence and Recovery

The migration and seed steps must be repeatable. Use schema creation guards such as `IF NOT EXISTS` so rerunning the migration does not fail when `tenanta` or `tenantb` already exist. Keep table creation and seed logic deterministic so API tests can rerun without manual cleanup.

If a migration fails halfway, rerun the Orders database migration after correcting the failing script instead of trying to patch the database manually. The database utility in `samples/src/Contoso.Orders.Database/Program.cs` should remain the single source of truth for schema setup. If test data becomes inconsistent across tenant schemas, fix `samples/tests/Contoso.Orders.Test.Common/Data/data.yaml` and rerun the Orders API tests, because the test harness already performs database migration and seed loading in `samples/tests/Contoso.Orders.Test.Api/ReadTests.cs` and `samples/tests/Contoso.Orders.Test.Api/OrderMutateTests.cs`.

If EF Core continues to query the wrong schema after switching tenants during one process lifetime, treat that as a model-cache-key bug, not a data bug. Fix the EF model cache key so schema changes affect model resolution, then rerun the same failing tenant-switch test until it passes.

## Artifacts and Notes

Expected successful cross-tenant behavior after implementation:

  Same-tenant read:
    > curl -k -H "X-Tenant-Id: tenanta" https://localhost:62023/api/orders/ORD-1001
    < HTTP/1.1 200 OK
    < Content-Type: application/json
    {
      "id": "ORD-1001",
      "customerId": "CUST-1001",
      "statusCode": "P"
    }

  Cross-tenant read:
    > curl -k -H "X-Tenant-Id: tenantb" https://localhost:62023/api/orders/ORD-1001
    < HTTP/1.1 404 Not Found

  Missing tenant header:
    > curl -k https://localhost:62023/api/orders/ORD-1001
    < HTTP/1.1 400 Bad Request

Expected focused API test result:

  Passed!  - Failed: 0, Passed: <updated-count>, Skipped: 0, Total: <updated-count>

## Interfaces and Dependencies

In `samples/src/Contoso.Orders.Infrastructure`, define a tenant schema abstraction that the database layer can depend on without leaking HTTP concerns into repositories. A concrete shape such as the following is appropriate:

  public interface ITenantSchemaResolver
  {
      string GetSchema();
  }

The implementation should read `CoreEx.ExecutionContext.TenantId`, validate that it is present and SQL-schema-safe, and return the schema name to use for aggregate tables.

In `samples/src/Contoso.Order.Workflow.Workflow/Contracts/OrderWorkflowContracts.cs`, extend the workflow contracts so tenant identity is explicit:

  public record OrderWorkflowRequest(string OrderId, decimal Amount, string Currency, string TenantId, string? RequestedBy = null);

  public record SubmitOrderActivityInput(string OrderId, decimal Amount, string Currency, string TenantId, string? RequestedBy);

The Orders API host in `samples/src/Contoso.Orders.Api/Program.cs` must use `UseExecutionContext(Func<HttpContext, ExecutionContext, Task>)` from `src/CoreEx.AspNetCore/CoreExAspNetCoreExtensions.ApplicationBuilder.cs` to populate `ExecutionContext.TenantId`.

The Orders persistence layer in `samples/src/Contoso.Orders.Infrastructure/Repositories/OrdersDbContext.cs` must be able to map `Persistence.Order` and `Persistence.OrderItem` to a tenant-specific schema. If EF model caching blocks this, add the EF Core cache-key customization required to include the current schema.

Do not change the Orders application service interfaces in `samples/src/Contoso.Orders.Application/Interfaces/IOrderService.cs` and `samples/src/Contoso.Orders.Application/Interfaces/IOrderReadService.cs` unless a new method is genuinely required. Tenant resolution should be ambient through `ExecutionContext`, not threaded manually through service signatures.

## Revision Note

Initial plan authored on 2026-05-06 from the current working tree and the requester's confirmed decisions: separate schema per tenant, `X-Tenant-Id`-style header flow into `ExecutionContext`, invisible cross-tenant `404`, and scope covering CRUD, workflow, and database seed data.
