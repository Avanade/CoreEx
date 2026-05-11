# Generate Domain Detailed Workflow

## Phase 1: Load Context

Before generating any files:

1. Read all `.github/instructions/*.instructions.md` — especially api-controllers, application-services, contracts, database-project, repositories, tests, validators, host-setup
2. Load all templates in `/.github/templates/domain/**`
3. Load `DomainScaffold.checklist.md` to track completion gates

## Phase 2: Gather and Confirm Inputs

Ask user for any values not supplied. Confirm all before creating files.

| Input | Example |
|-------|---------|
| Solution prefix | `Contoso` |
| Domain name | `Orders` |
| Root entity name | `Order` |
| Root entity fields | Names, types, ref-data codes, read-only flags |
| Child entity (optional) | `OrderItem` with fields |
| Operations | Create / Read / Update / Patch / Delete |
| Event subjects | Confirm: `{solution}.{domain}.{entity}.{action}.v1` |

## Phase 3: Generate Contracts Layer

`{Solution}.{Domain}.Contracts`

In order:
1. `GlobalUsing.cs` — usings: `CoreEx.Entities`, `CoreEx.Localization`, `CoreEx.RefData`, `System.ComponentModel`, `System.Text.Json.Serialization`
2. `{Entity}Base.cs` — `[Contract] partial class` with `IIdentifier<string?>`. Use `[ReadOnly(true)]` for server fields. Use `[ReferenceData<T>]` for ref-data codes. Add `[Localization("...")]` for poor property names.
3. `{Entity}.cs` — extends `{Entity}Base`, implements `IETag, IChangeLog`. Mark `ETag` and `ChangeLog` as `[ReadOnly(true)]`.
4. `{Entity}Lite.cs` (optional) — trimmed projection for query responses
5. Reference data types — inherit `ReferenceData<TSelf>`, use `[ReferenceData]`, pair with `{Type}Collection`
6. `{Solution}.{Domain}.Contracts.csproj`

## Phase 4: Generate Application Layer

`{Solution}.{Domain}.Application`

In order:
1. `GlobalUsing.cs`
2. `Interfaces/I{Entity}Service.cs` + `I{Entity}ReadService.cs` (if CQRS)
3. `Repositories/I{Entity}Repository.cs` — return types: `Task<{Entity}?>` (Get), `Task<DataResult<{Entity}>>` (Create/Update), `Task<DataResult>` (Delete), `Task<ItemsResult<{Entity}Lite>>` (Query)
4. `Validators/{Entity}Validator.cs` — `Validator<{Entity}, {Entity}Validator>` with `.Mandatory()`, `.MaximumLength()`, `.IsValid()`, `.PrecisionScale()`
5. `{Entity}Service.cs` — `[ScopedService<I{Entity}Service>]`. Guard inputs. Validate. Wrap mutations in `_unitOfWork.ExecuteAsync(...)`. Emit events inside `WhereMutated(...)`
6. `{Entity}ReadService.cs` (if CQRS) — read-only, no UoW or events
7. `{Solution}.{Domain}.Application.csproj`

## Phase 5: Generate Infrastructure Layer

`{Solution}.{Domain}.Infrastructure`

In order:
1. `{Domain}EfDb.cs` — EF database class with `EfDbSet<{Entity}>`
2. `{Domain}DbContext.cs` — DbContext with entity model config
3. `Repositories/{Entity}Repository.cs` — `[ScopedService<I{Entity}Repository>]`. Static `QueryArgsConfig`. Apply via `.Where(parsed).OrderBy(parsed).ToMappedItemsResultAsync(...)`
4. `{Domain}OutboxPublisher.cs`
5. `{Solution}.{Domain}.Infrastructure.csproj`

## Phase 6: Generate API Host

`{Solution}.{Domain}.Api`

In order:
1. `Controllers/{Entity}Controller.cs` — mutations (POST, PUT, PATCH, DELETE). `[ApiController, Route("/api/{entities}"), OpenApiTag(...)]`. `[IdempotencyKey]` on POST
2. `Controllers/{Entity}ReadController.cs` — reads (GET single, GET query). `[Query(supportsOrderBy: true), Paging(supportsCount: true)]`
3. `Program.cs` — AddExecutionContext, AddReferenceDataOrchestrator (if needed), AddMvcWebApi, AddHttpWebApi, AddDynamicServicesUsing, FusionCache, SQL Server + EF + Outbox, OpenAPI, telemetry, middleware order
4. `appsettings.json` — connection string placeholders: `SqlServer`, `redis`
5. `GlobalUsing.cs`
6. `{Solution}.{Domain}.Api.csproj`

## Phase 7: Generate Database

`{Solution}.{Domain}.Database`

In order:
1. `Program.cs` — `SqlServerMigrationConsole` with `DataResetFilterPredicate` scoped to `{Domain}` schema
2. `dbex.yaml` — outbox enabled; full table list
3. `Migrations/*.sql` — schema, ref-data, aggregate, child, and outbox tables
4. `Schema/Stored Procedures/*.g.sql` — six outbox stored procedures
5. `Data/ref-data.yaml` — seed data
6. `{Solution}.{Domain}.Database.csproj`

## Phase 8: Generate Test Projects

`{Solution}.{Domain}.Test.*`

In order:
1. Create `{Solution}.{Domain}.Test.Unit` — validator/service-focused tests with `WithGenericTester<EntryPoint>`
2. Create `{Solution}.{Domain}.Test.Api` — `WithApiTester<{Solution}.{Domain}.Api.Program>`
3. Ensure both use AwesomeAssertions (not FluentAssertions)
4. Add both to solution under `{Domain}` solution folder
5. Group all domain projects together under `{Domain}` folder

## Quality Gates

Check before finishing:
- Every injected dependency guarded with `.ThrowIfNull()`
- Every `await` uses `.ConfigureAwait(false)`
- All mutations wrapped in `_unitOfWork.ExecuteAsync(...)`
- Events added inside `WhereMutated(...)` only
- POST endpoints carry `[IdempotencyKey]`
- Both Unit and Api test projects scaffolded
- `dotnet test` passes for Unit and Api projects
- `DomainScaffold.checklist.md` fully checked

## Naming Conventions

| Artefact | Pattern |
|----------|---------|
| Namespace root | `{Solution}.{Domain}.{Layer}` |
| Event subjects | `{solution}.{domain}.{entity}.{action}.v1` (lowercase) |
| Write controller | `{Entity}Controller` |
| Read controller | `{Entity}ReadController` |
| Write service | `{Entity}Service` / `I{Entity}Service` |
| Read service | `{Entity}ReadService` / `I{Entity}ReadService` |
| Repository | `{Entity}Repository` / `I{Entity}Repository` |
| Ref-data collection | `{Type}Collection` |
