# domain-name API Host -- AI Agent Guide

This is the **ASP.NET Core API host** for the `domain-name` domain, part of the `solution-name` microservice. It exposes REST endpoints backed by the CoreEx layered application core.

> **Before answering any CoreEx question:** check whether `.github/docs/coreex/` is populated at the solution root. If empty, run `/coreex-docs-sync` first. The package guide at `.github/docs/coreex/agents/CoreEx.AspNetCore.md` is especially relevant to this project.

---

## Host Responsibilities

This project is a **thin host** -- it wires up DI, configures the pipeline, and delegates all business logic to the `solution-name.Application` layer. It should contain:

- `Program.cs` -- startup, DI registration, middleware pipeline
- Controllers -- route handlers that call Application services; no business logic here
- No direct database access; no direct Service Bus calls

Read `.github/docs/coreex/hosts-layer.md` for the full host-layer guide.

---

## Adding a Controller

1. Create a new file in the project, e.g. `Controllers/ProductController.cs`
2. Inherit from `ControllerBase` and apply `[Route("api/[controller]")]`
3. Inject the Application service interface (from `solution-name.Application`)
4. Map HTTP verbs to service methods -- return `IActionResult` using `solution-name.Application` result types
5. CoreEx provides `WebApiPublisher` / `HttpWebApi` helpers for standardised response handling

Consult `.github/docs/coreex/agents/CoreEx.AspNetCore.md` for controller patterns and `WebApiPublisher` usage.

---

## OpenAPI / NSwag

The host registers NSwag via `services.AddOpenApiDocument(...)` with CoreEx defaults applied (`s.AddCoreExConfiguration()`). The Swagger UI is available at `/swagger` in development.

Consult `.github/docs/coreex/agents/CoreEx.AspNetCore.NSwag.md` for NSwag customisation patterns.

---

## Caching

The host wires FusionCache with both in-memory (L1) and Redis distributed (L2) backing. The CoreEx `IHybridCache` abstraction is registered as `AddFusionHybridCache()`.

- Use `IHybridCache` in Application services -- never take direct `IMemoryCache` or `IDistributedCache` dependencies
- Idempotency support is provided via `AddHybridCacheIdempotencyProvider()`

Consult `.github/docs/coreex/agents/CoreEx.Caching.FusionCache.md` for caching patterns.

---

## This Host's Feature Configuration

<!-- #if implement-sqlserver -->
- **Data provider:** SQL Server -- `builder.AddSqlClientConnection("SqlServer")` (Aspire connection name)
<!-- #elif implement-cosmos -->
- **Data provider:** Azure Cosmos DB -- `builder.AddAzureCosmosClient("Cosmos")` (Aspire connection name) plus `AddCosmosDb<domain-nameCosmosDb>("domain-name-lower")`
<!-- #elif implement-postgres -->
- **Data provider:** PostgreSQL -- `builder.AddNpgsqlDataSource("Postgres")` (Aspire connection name)
<!-- #else -->
- **Data provider:** None -- this host uses no database; services call external systems directly
<!-- #endif -->
<!-- #if (outbox-enabled && implement-cosmos) -->
- **Transactional outbox:** Enabled -- events are written to the Cosmos DB outbox by `CosmosDbEventPublisher` (inside `CosmosDbUnitOfWork.TransactionAsync`); the Relay host's Change Feed Processor reads and forwards them
<!-- #elif (outbox-enabled && has-data-provider) -->
- **Transactional outbox:** Enabled -- events are written to the DB outbox by `domain-nameOutboxPublisher`; the Relay host reads and forwards them
<!-- #else -->
- **Transactional outbox:** Disabled -- events are published directly to the message broker
<!-- #endif -->
<!-- #if refdata-enabled -->
- **Reference data:** Enabled -- the non-generic `AddReferenceDataOrchestrator()` is registered and binds the CodeGen-generated `ReferenceDataProvider` (`IReferenceDataProvider`) from DI at runtime; reference data is hydrated via `ReferenceDataRepository`
<!-- #else -->
- **Reference data:** Disabled
<!-- #endif -->

---

## Key Packages

| Package | Purpose |
|---|---|
| `CoreEx.AspNetCore` | Web API base types, middleware, `WebApiPublisher` |
| `CoreEx.AspNetCore.NSwag` | NSwag OpenAPI integration |
| `CoreEx.Caching.FusionCache` | FusionCache `IHybridCache` integration |
<!-- #if implement-sqlserver -->
| `CoreEx.Database.SqlServer` | SQL Server database access |
<!-- #endif -->
<!-- #if implement-postgres -->
| `CoreEx.Database.Postgres` | PostgreSQL database access |
<!-- #endif -->
<!-- #if implement-cosmos -->
| `CoreEx.Cosmos` | Azure Cosmos DB access (`CosmosDb`, typed containers, outbox, unit of work) |
<!-- #endif -->
<!-- #if implement-relational -->
| `CoreEx.EntityFrameworkCore` | EF Core integration (`EfDb`, `IEfDbContext`) |
<!-- #endif -->
<!-- #if refdata-enabled -->
| `CoreEx.RefData` | Reference data orchestration |
<!-- #endif -->

---

## Relevant Docs

- `.github/docs/coreex/hosts-layer.md` -- host startup patterns
- `.github/docs/coreex/patterns.md` -- CoreEx request/response patterns
- `.github/docs/coreex/testing.md` -- integration test setup for the API host
- `.github/docs/coreex/local-dev.md` -- running locally with .NET Aspire
- `.github/docs/coreex/agents/CoreEx.AspNetCore.md` -- Web API patterns
- `.github/docs/coreex/agents/CoreEx.Caching.FusionCache.md` -- caching
<!-- #if implement-relational -->
- `.github/docs/coreex/agents/CoreEx.EntityFrameworkCore.md` -- EF Core patterns
<!-- #endif -->
<!-- #if implement-cosmos -->
- `.github/docs/coreex/agents/CoreEx.Cosmos.md` -- Cosmos DB patterns
<!-- #endif -->
<!-- #if refdata-enabled -->
- `.github/docs/coreex/agents/CoreEx.RefData.md` -- reference data patterns
<!-- #endif -->
