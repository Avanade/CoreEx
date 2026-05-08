# Generate {Solution}.{Domain} Domain

This ExecPlan documents the scaffolding of the `{Solution}.{Domain}` domain including all layers: Contracts, Application, Infrastructure, API, Database, and Tests.

## Purpose / Big Picture

The `{Domain}` domain handles [brief description of domain responsibility, e.g., "order lifecycle management"]. It will be accessible via REST API at `/api/{entities}` with full CRUD operations, event publication via outbox, and comprehensive test coverage. Users will be able to [user scenario], and operators will receive domain events for downstream integration.

## Progress

Use this checklist to track completion. Update timestamps as each phase completes.

- [ ] (YYYY-MM-DD HH:MMZ) Phase 0: Plan approved
- [ ] (YYYY-MM-DD HH:MMZ) Phase 2: Contracts layer generated
- [ ] (YYYY-MM-DD HH:MMZ) Phase 3: Application layer generated
- [ ] (YYYY-MM-DD HH:MMZ) Phase 4: Infrastructure layer generated
- [ ] (YYYY-MM-DD HH:MMZ) Phase 5: API host generated
- [ ] (YYYY-MM-DD HH:MMZ) Phase 6: Database project generated
- [ ] (YYYY-MM-DD HH:MMZ) Phase 7: Test projects generated
- [ ] (YYYY-MM-DD HH:MMZ) Phase 8: Quality gates passed
- [ ] (YYYY-MM-DD HH:MMZ) Phase 9: Plan validated and complete

## Surprises & Discoveries

Record unexpected behavior, missing assumptions, or blocking issues discovered while researching or implementing.

- Observation: [Replace with discovery]
  Evidence: [Short excerpt or command output]

## Decision Log

Record design or scope decisions made during execution.

- Decision: [What was decided]
  Rationale: [Why this path was chosen]
  Date/Author: YYYY-MM-DD / [Name]

## Outcomes & Retrospective

Summarize what was achieved. Compare against purpose, note remaining gaps, and capture lessons.

- Outcome: [What now works]
  Evidence: [Proof of success]
  Remaining gap: [Any incomplete work or `None`]

## Context and Orientation

### Repository Structure

This domain will be scaffolded following the Contoso sample architecture:

- **Solution prefix:** `{Solution}`
- **Domain name:** `{Domain}`
- **Root entity:** `{Entity}`
- **Namespace root:** `{Solution}.{Domain}.{Layer}`

All projects will be created under `samples/src/Contoso.{Domain}/` with the following layer structure:

- `{Solution}.{Domain}.Contracts/` — Data transfer objects, reference-data types
- `{Solution}.{Domain}.Application/` — Business logic, services, validators, repositories
- `{Solution}.{Domain}.Infrastructure/` — EF mappings, repository implementations, outbox publisher
- `{Solution}.{Domain}.Api/` — REST controllers, HTTP host
- `{Solution}.{Domain}.Database/` — SQL Server migrations, stored procedures, seed data
- `{Solution}.{Domain}.Test.Unit/` — Service and validator tests
- `{Solution}.{Domain}.Test.Api/` — API integration tests

### Key Fields and Attributes

Root entity `{Entity}` will have these confirmed fields:

[List confirmed fields with types and special attributes here, e.g., ETag, ChangeLog, reference-data codes]

### Event Subjects

Integration events will use these subjects (lowercase, event-driven naming):

- `{solution}.{domain}.{entity}.created.v1`
- `{solution}.{domain}.{entity}.updated.v1`
- `{solution}.{domain}.{entity}.deleted.v1`
- [Add any additional event subjects confirmed with user]

### Entry Points

After generation, the domain will be accessible and testable via:

- **API endpoint:** `GET/POST/PUT/PATCH/DELETE /api/{entities}`
- **Service interface:** `I{Entity}Service` and `I{Entity}ReadService` in Application layer
- **Database:** `{Solution}DbContext` with `DbSet<{Entity}>` and `DbSet<{Entity}Outbox>`
- **Tests:** `{Solution}.{Domain}.Test.Unit` and `{Solution}.{Domain}.Test.Api`

## Plan of Work

### Phase 2: Contracts Layer

Generate immutable data transfer objects (`{Entity}`, `{Entity}Lite`) and reference-data types with CoreEx attributes (`[Contract]`, `[ReadOnly]`, `[ReferenceData]`). These define the shape of data flowing in/out of the API and the type system for the domain.

**Expected files:**
- `Contracts/GlobalUsing.cs`
- `Contracts/{Entity}Base.cs`, `Contracts/{Entity}.cs`, `Contracts/{Entity}Lite.cs`
- `Contracts/{Entity}Contracts.csproj`

**Validation:** Generated types compile, have correct namespace roots, carry expected attributes.

### Phase 3: Application Layer

Generate service interfaces and implementations (`I{Entity}Service`, `{Entity}Service`) with dependency injection markers (`[ScopedService<...>]`). Validators use fluent patterns. Repositories define data-access contracts. All follow unit-of-work and event-emission patterns.

**Expected files:**
- `Application/GlobalUsing.cs`
- `Application/Interfaces/I{Entity}Service.cs`, `I{Entity}ReadService.cs`
- `Application/Repositories/I{Entity}Repository.cs`
- `Application/Validators/{Entity}Validator.cs`
- `Application/{Entity}Service.cs`, `{Entity}ReadService.cs`
- `Application/{Solution}.{Domain}.Application.csproj`

**Validation:** All services have `[ScopedService<...>]`. Mutations use `_unitOfWork.ExecuteAsync(...)`. Events are emitted via `WhereMutated(...)`. No external HTTP calls or async I/O outside repository.

### Phase 4: Infrastructure Layer

Generate EF mappings, `DbContext`, repository implementations, and outbox publisher. The repository applies parsed query arguments (filter, order, paging) via CoreEx fluent helpers.

**Expected files:**
- `Infrastructure/{Domain}EfDb.cs`
- `Infrastructure/{Domain}DbContext.cs`
- `Infrastructure/Repositories/{Entity}Repository.cs`
- `Infrastructure/{Domain}OutboxPublisher.cs`
- `Infrastructure/{Solution}.{Domain}.Infrastructure.csproj`

**Validation:** `DbContext` fluently configures entities. Repository uses `QueryArgsConfig` and `.ToMappedItemsResultAsync()`. Outbox publisher follows `IOutboxPublisher` convention.

### Phase 5: API Host

Generate HTTP controllers (one for mutations, one for reads if CQRS), `Program.cs` with host setup, and `appsettings.json`. Controllers use CoreEx `WebApi` helpers. POST carries `[IdempotencyKey]`. NSwag/OpenAPI registered.

**Expected files:**
- `Api/Controllers/{Entity}Controller.cs`, `{Entity}ReadController.cs`
- `Api/Program.cs`
- `Api/appsettings.json`
- `Api/GlobalUsing.cs`
- `Api/{Solution}.{Domain}.Api.csproj`

**Validation:** Controllers use `PostAsync`, `PutAsync`, `PatchAsync`, `DeleteAsync`. POST has `[IdempotencyKey]`. `Program.cs` follows host setup conventions (context, DI, caching, telemetry, middleware order). API launches and serves requests.

### Phase 6: Database

Generate SQL Server migrations, stored procedures (outbox operations), reference-data seed, and `Program.cs` migration console.

**Expected files:**
- `Database/Program.cs`
- `Database/dbex.yaml` (outbox enabled)
- `Database/Migrations/001_initial.sql` (schema, tables, indexes)
- `Database/Schema/Stored Procedures/spOutbox_*.g.sql` (six outbox SPs)
- `Database/Data/ref-data.yaml` (seed data if applicable)
- `Database/{Solution}.{Domain}.Database.csproj`

**Validation:** Migrations apply cleanly. Outbox table and procedures exist and are correct. Reference data loads. Data reset works.

### Phase 7: Test Projects

Generate two test projects: `Test.Unit` (service/validator logic) and `Test.Api` (REST endpoint behavior). Both follow AwesomeAssertions conventions.

**Expected files:**
- `Test.Unit/{Entity}ServiceTests.cs`, `{Entity}ValidatorTests.cs`
- `Test.Api/{Entity}ApiTests.cs`
- `Test.Unit/{Solution}.{Domain}.Test.Unit.csproj`
- `Test.Api/{Solution}.{Domain}.Test.Api.csproj`

**Validation:** Tests exercise CRUD operations, validation rules, and edge cases. All tests pass. Both projects appear in solution under `{Domain}` folder.

### Phase 8: Quality Gates

Before marking complete, verify:
- Every injected dependency guarded with `.ThrowIfNull()`
- Every `await` uses `.ConfigureAwait(false)`
- All mutations wrapped in `_unitOfWork.ExecuteAsync(...)`
- Events added inside `WhereMutated(...)` only
- POST endpoints carry `[IdempotencyKey]`
- Both Unit and Api test projects present and passing
- `dotnet build` clean
- `dotnet test` all pass

## Concrete Steps

Use these commands to validate each phase:

   Command: `dotnet build CoreEx.sln`
   Expected result: Clean build, no warnings.

   Command: `dotnet test tests/{Solution}.{Domain}.Test.Unit`
   Expected result: All tests pass.

   Command: `dotnet test tests/{Solution}.{Domain}.Test.Api`
   Expected result: All tests pass.

   Command: `dotnet run --project samples/src/Contoso.{Domain}.Database`
   Expected result: Migrations apply successfully, schema and outbox assets created.

## Validation and Acceptance

Proof of successful domain generation:

1. **All layers compile cleanly:**
   `dotnet build CoreEx.sln` produces no warnings.

2. **Services and validators work:**
   Run unit tests: `dotnet test tests/{Solution}.{Domain}.Test.Unit`. All tests pass.

3. **API serves requests:**
   Run API host and invoke endpoints:
   - `POST /api/{entities}` creates an entity
   - `GET /api/{entities}` lists entities
   - `GET /api/{entities}/{id}` retrieves one entity
   - `PUT /api/{entities}/{id}` updates with concurrency check
   - `PATCH /api/{entities}/{id}` applies partial update
   - `DELETE /api/{entities}/{id}` soft-deletes
   All return appropriate HTTP status codes.

4. **Integration tests pass:**
   Run API tests: `dotnet test tests/{Solution}.{Domain}.Test.Api`. All tests pass.

5. **Database migrations work:**
   Run: `dotnet run --project samples/src/Contoso.{Domain}.Database`
   Expected: Schema created, outbox table and six stored procedures exist, reference data seeded.

## Idempotence and Recovery

All phases (2–9) are safe to repeat:
- If contract generation is re-run, inspect the diffs and accept or reject them.
- If a layer is partially generated, complete it by re-running the phase.
- If a test fails, fix the generation template (not hand-edits) and re-run the phase.
- To clean up and start over, delete all generated files under `samples/src/Contoso.{Domain}/` and restore the solution file to its prior state.

If a build fails mid-generation:
1. Inspect the error.
2. Fix the template or inputs if needed.
3. Delete the partially generated project folder.
4. Re-run the generation phase.

## Artifacts and Notes

Record important artifacts here as the plan progresses:

Example test output (when Phase 7 completes):

   Passed!  - Failed:     0, Passed:   24, Skipped:     0, Total:    24

Example API response (when Phase 5 completes):

   POST /api/{entities}
   {
     "id": "ORD-001",
     "createdDate": "2026-05-07T12:00:00Z",
     "etag": "v1"
   }

## Interfaces and Dependencies

After generation, these interfaces and dependencies must exist:

- **Service interface:** `I{Entity}Service` (write) and `I{Entity}ReadService` (read) in `Application/Interfaces/`
- **Repository interface:** `I{Entity}Repository` in `Application/Repositories/`
- **Validator:** `{Entity}Validator : Validator<{Entity}, {Entity}Validator>` in `Application/Validators/`
- **EF context:** `{Domain}DbContext : CoreEx...DbContext` with entity mappings
- **API controllers:** `{Entity}Controller` and `{Entity}ReadController` in `Api/Controllers/`
- **Test base classes:** Both `Test.Unit` and `Test.Api` inherit CoreEx test bases

All follow CoreEx naming, layering, and scoped-service patterns.

## Revision Note

When revising this plan, note changes here:

- [Date/Initials]: [What changed and why]
