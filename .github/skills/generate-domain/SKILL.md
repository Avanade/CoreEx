---
name: generate-domain
description: "Generate a new CoreEx domain or microservice. Use when: scaffolding a new domain, creating a new microservice, adding a new bounded context, generating sample domain code like shopping or product, creating contracts/application/infrastructure/API/database layers from scratch following CoreEx conventions."
argument-hint: "Optional: solution prefix, domain name, and root entity — e.g. 'Contoso Orders Order'"
tags: ["scaffolding", "microservice", "bounded-context", "code-generation", "layering"]
---

# Generate Domain

Scaffolds all layers of a new CoreEx domain — Contracts, Application, Infrastructure, API, Database, and baseline Unit/Api tests — aligned to the Contoso sample architecture (shopping, product).

## When to Use

- Scaffolding a new microservice or bounded context from scratch.
- Generating domain code that follows CoreEx conventions (ETag, ChangeLog, Outbox, FusionCache, NSwag).
- Producing code that mirrors the Shopping or Product sample domains.

## Pre-flight: Load Context

Before generating any files, load all of the following:

1. All instruction files in `/.github/instructions/*.instructions.md`.
2. All template files in `/.github/templates/domain/**` as canonical patterns.
3. The checklist at [DomainScaffold.checklist.md](../../templates/domain/DomainScaffold.checklist.md) — use it to track completion gates.

## Step 1 — Gather Inputs

Ask the user for any values not already supplied:

| Input | Example |
|-------|---------|
| Solution prefix | `Contoso` |
| Domain name | `Orders` |
| Root entity name | `Order` |
| Root entity fields | Names, types, reference-data codes, read-only flags |
| Child entity (optional) | `OrderItem` with its fields |
| Operations | Create / Read / Update / Patch / Delete |
| Event subjects | Confirm or derive: `{solution}.{domain}.{entity}.{action}.v1` |

Confirm all inputs before creating any files. Use a todo list to track layer progress.

## Step 2 — Contracts Layer (`{Solution}.{Domain}.Contracts`)

Generate in this order:

1. `GlobalUsing.cs` — usings for `CoreEx.Entities`, `CoreEx.Localization`, `CoreEx.RefData`, `System.ComponentModel`, `System.Text.Json.Serialization`.
2. `{Entity}Base.cs` — `[Contract] partial class` implementing `IIdentifier<string?>`. Mark server-assigned fields `[ReadOnly(true)]`. Use `[ReferenceData<T>]` on ref-data code properties. Add `[Localization("...")]` where property names produce poor error messages.
3. `{Entity}.cs` — extends `{Entity}Base`, implements `IETag, IChangeLog`. Mark `ETag` and `ChangeLog` as `[ReadOnly(true)]`.
4. `{Entity}Lite.cs` (optional) — trimmed projection for query responses.
5. Reference data types — inherit `ReferenceData<TSelf>`, add `[ReferenceData]`, pair each with a `{Type}Collection`.
6. `{Solution}.{Domain}.Contracts.csproj`.

## Step 3 — Application Layer (`{Solution}.{Domain}.Application`)

1. `GlobalUsing.cs`.
2. `Interfaces/I{Entity}Service.cs` and `I{Entity}ReadService.cs` (if CQRS split needed).
3. `Repositories/I{Entity}Repository.cs` — `Task<{Entity}?>` for Get, `Task<DataResult<{Entity}>>` for Create/Update, `Task<DataResult>` for Delete, `Task<ItemsResult<{Entity}Lite>>` for Query.
4. `Validators/{Entity}Validator.cs` — `Validator<{Entity}, {Entity}Validator>` with `.Mandatory()`, `.MaximumLength()`, `.IsValid()`, `.PrecisionScale()` rules.
5. `{Entity}Service.cs` — `[ScopedService<I{Entity}Service>]`. Guard inputs with `.ThrowIfNull()` / `.ThrowIfNullOrEmpty()`. Validate with `{Entity}Validator.Default.ValidateAndThrowAsync(...)`. Wrap mutations in `_unitOfWork.ExecuteAsync(...)`. Emit events inside `WhereMutated(...)`.
6. `{Entity}ReadService.cs` (if CQRS) — read-only, no UoW or events.
7. `{Solution}.{Domain}.Application.csproj`.

## Step 4 — Infrastructure Layer (`{Solution}.{Domain}.Infrastructure`)

1. `{Domain}EfDb.cs` — EF database class with `EfDbSet<{Entity}>`.
2. `{Domain}DbContext.cs` — `DbContext` with entity model configuration.
3. `Repositories/{Entity}Repository.cs` — `[ScopedService<I{Entity}Repository>]`. Static `QueryArgsConfig` for filterable/sortable fields. Apply via `.Where(parsed).OrderBy(parsed).ToMappedItemsResultAsync(...)`.
4. `{Domain}OutboxPublisher.cs`.
5. `{Solution}.{Domain}.Infrastructure.csproj`.

## Step 5 — API Host (`{Solution}.{Domain}.Api`)

1. `Controllers/{Entity}Controller.cs` — mutations (POST, PUT, PATCH, DELETE). `[ApiController, Route("/api/{entities}"), OpenApiTag(...)]`. `[IdempotencyKey]` on POST.
2. `Controllers/{Entity}ReadController.cs` — reads (GET single, GET query). `[Query(supportsOrderBy: true), Paging(supportsCount: true)]` on query endpoint.
3. `Program.cs` — `AddExecutionContext`, `AddReferenceDataOrchestrator` (if ref data), `AddMvcWebApi`, `AddHttpWebApi`, `AddDynamicServicesUsing<...>`, FusionCache, SQL Server + EF + Outbox, OpenAPI, telemetry, middleware in correct order.
4. `appsettings.json` — connection string placeholders for `SqlServer` and `redis`.
5. `GlobalUsing.cs`.
6. `{Solution}.{Domain}.Api.csproj`.

## Step 6 — Database (`{Solution}.{Domain}.Database`)

1. `Program.cs` — `SqlServerMigrationConsole` with `DataResetFilterPredicate` scoped to `{Domain}` schema.
2. `dbex.yaml` — outbox enabled; full table list.
3. `Migrations/*.sql` — schema, reference data, aggregate, child, and outbox tables.
4. `Schema/Stored Procedures/*.g.sql` — six outbox stored procedures.
5. `Data/ref-data.yaml` — seed data.
6. `{Solution}.{Domain}.Database.csproj`.

## Step 7 — Test Projects (`{Solution}.{Domain}.Test.*`)

1. Create `{Solution}.{Domain}.Test.Unit` using CoreEx test conventions (e.g., validator/service-focused tests with `WithGenericTester<EntryPoint>`).
2. Create `{Solution}.{Domain}.Test.Api` using CoreEx API integration conventions (e.g., `WithApiTester<{Solution}.{Domain}.Api.Program>`).
3. Ensure both test projects reference AwesomeAssertions, not FluentAssertions.
4. Ensure both test projects are added to the solution and grouped under the `{Domain}` solution folder.
5. Ensure all generated domain projects are also grouped with the test projects under the `{Domain}` solution folder.

## Quality Gates (check before finishing)

- Every injected dependency guarded with `.ThrowIfNull()`.
- Every `await` uses `.ConfigureAwait(false)`.
- All mutations wrapped in `_unitOfWork.ExecuteAsync(...)`.
- Events added inside `WhereMutated(...)` only.
- POST endpoints carry `[IdempotencyKey]`.
- `{Solution}.{Domain}.Test.Unit` and `{Solution}.{Domain}.Test.Api` are scaffolded.
- `dotnet test` passes for both Unit and Api test projects.
- Run the [DomainScaffold.checklist.md](../../templates/domain/DomainScaffold.checklist.md) in full and report any unchecked items.

## Naming Conventions

| Artefact | Pattern |
|----------|---------|
| Namespace root | `{Solution}.{Domain}.{Layer}` |
| Event subjects | `{solution}.{domain}.{entity}.{action}.v1` (all lowercase) |
| Write controller | `{Entity}Controller` |
| Read controller | `{Entity}ReadController` |
| Write service | `{Entity}Service` / `I{Entity}Service` |
| Read service | `{Entity}ReadService` / `I{Entity}ReadService` |
| Repository | `{Entity}Repository` / `I{Entity}Repository` |
| Reference data collection | `{Type}Collection` |
