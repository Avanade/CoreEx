# CoreEx — AI Coding Context

## What CoreEx Is

CoreEx is a modular .NET framework for enterprise back-end services. It provides opinionated primitives, patterns, and extensions for HTTP API development, data access, event-driven messaging, validation, reference data, caching, and testing. Add it via NuGet — favor CoreEx-native primitives over ad-hoc implementations wherever a CoreEx type or extension exists.

## Package Map

| NuGet Package | Capability |
|---|---|
| `CoreEx` | Core: exceptions, `Result<T>`, execution context, DI helpers, entity markers, JSON, mapping, and Roslyn source generator for `[Contract]`-decorated and `[ReferenceData]`-decorated types |
| `CoreEx.AspNetCore` | HTTP API: `WebApi` helpers, idempotency, health checks, middleware |
| `CoreEx.AspNetCore.NSwag` | OpenAPI / NSwag integration |
| `CoreEx.Validation` | Fluent validation: `Validator<T>`, `AbstractValidator<T>`, rules |
| `CoreEx.EntityFrameworkCore` | EF Core: `EfDb`, `EfDbModel`, `EfDbMappedModel` |
| `CoreEx.Database` | Core database abstractions: `IDatabase`, `IUnitOfWork` |
| `CoreEx.Database.SqlServer` | SQL Server: unit of work, outbox publisher, outbox relay |
| `CoreEx.Database.Postgres` | PostgreSQL: unit of work, outbox publisher, outbox relay |
| `CoreEx.Events` | Event publishing/subscribing: `EventData`, `IEventPublisher` |
| `CoreEx.Azure.Messaging.ServiceBus` | Azure Service Bus: subscriber base classes, publisher, receiver wiring |
| `CoreEx.RefData` | Reference data: `ReferenceData<T>`, `ReferenceDataOrchestrator` |
| `CoreEx.Caching.FusionCache` | Hybrid L1/L2 cache: FusionCache + Redis backplane |
| `CoreEx.DomainDriven` | DDD: `Aggregate<TId,TSelf>`, `Entity<TId,TSelf>`, `PersistenceState` |
| `CoreEx.UnitTesting` | Test helpers: `WithApiTester`, `WithGenericTester`, `Test.Http()` |
| `CoreEx.CodeGen` | Design-time: generates reference-data C# artefacts from `ref-data.yaml` |
| `CoreEx.Generator` | Roslyn source generator: emits members for `[Contract]`-decorated types |

## Recommended Layer Structure

CoreEx does not impose a specific folder or project structure. The following layers are a recommended pattern, not a requirement. Introduce only the layers your domain needs.

| Layer | Typical project suffix | Notes |
|---|---|---|
| Contracts | `*.Contracts` | DTOs, entity marker interfaces, reference-data types |
| Application | `*.Application` | Services, validators, repository interfaces, adapters, policies |
| Domain | `*.Domain` | Aggregates, entities, value objects — optional; introduce only when needed |
| Infrastructure | `*.Infrastructure` | EF repositories, persistence models, typed HTTP clients, adapter impls |
| API host | `*.Api` | HTTP composition root — controllers and `Program.cs` |
| Subscribe host | `*.Subscribe` | Message consumer composition root |
| Outbox Relay host | `*.Outbox.Relay` | Relay background service composition root |
| Database tooling | `*.Database` | Design-time: schema, migrations, outbox provisioning (no runtime presence) |
| CodeGen tooling | `*.CodeGen` | Design-time: reference-data C# generation (no runtime presence) |

The Domain layer is **optional**. Introduce it only when the domain has aggregates with non-trivial invariants enforced at the model level. Simple CRUD-oriented domains can skip it entirely.

## Universal Rules

### Before Generating Any Code

1. Run `Get-ChildItem .github/instructions -File` to enumerate all instruction files.
2. Identify which instruction files match the target layer (contracts, domain, application, etc.).
3. Read each matching file in full with `get_file` before writing any code.

### Using Statements

Every project has a single `GlobalUsing.cs` at its root where all namespace imports are declared. When emitting code, do **not** add `using` statements to individual files. If a referenced namespace is missing, add the `global using` to that project's `GlobalUsing.cs` instead. If unsure whether an import already exists, check the project's `GlobalUsing.cs` and amend it — unless the user has explicitly instructed otherwise.

### Always Prefer CoreEx Primitives

- Use CoreEx exception types — `NotFoundException`, `ValidationException`, `BusinessException`, `ConcurrencyException`, `AuthenticationException`, `AuthorizationException`. These auto-map to correct HTTP status codes via `UseCoreExExceptionHandler()`.
- Use `Result<T>` pipelines for multi-step orchestration, aggregate-oriented flows, and subscriber handlers.
- Use exception-based flow for simpler CRUD-oriented services where pipeline composition adds no value.
- Use `WebApi` helpers in controllers — never return typed `ActionResult<T>` directly.
- Use `[ScopedService<TInterface>]` on service and repository classes for automatic DI discovery — avoid manual `services.AddScoped<>()` registration.
- Use `AddDynamicServicesUsing<T1, T2, ...>()` in `Program.cs` to discover and register all `[ScopedService]`-decorated types from the specified assemblies.

### Mapping

- Never use AutoMapper. Use explicit mappers:
  - `BiDirectionMapper<TFrom, TTo, TSelf>` — Contract ↔ Persistence model (Infrastructure layer).
  - `Mapper<TSource, TDest, TSelf>` — Domain aggregate ↔ Contract (Application layer, only when a Domain layer exists).
- Do not conflate the two mapping layers. Infrastructure mapping is a persistence concern; Application mapping is a domain surface concern.

### Async

- Always call `.ConfigureAwait(false)` on every `await` in service, repository, adapter, and validator methods.

### Dependency Direction (strict — enforce at all times)

- Application layer depends inward only: on Contracts and its own interfaces. It must never reference the Infrastructure project.
- Infrastructure implements Application interfaces — never the reverse.
- Do not call `HttpClient` directly from services or adapters — use typed HTTP client classes in `Infrastructure/Clients/`.
- Do not put business logic in controllers or subscribers — delegate immediately to Application-layer services.
- Do not inject `IUnitOfWork` into controllers — it belongs in application services.

### Events and Unit of Work

- Always wrap database writes **and** event publication together inside `_unitOfWork.TransactionAsync(...)`. Both are committed atomically or not at all.
- Never publish events outside of `_unitOfWork.TransactionAsync(...)`.

### Generated Code

Never create or edit the following file types directly — they are owned by tooling:

| Pattern | Owner |
|---|---|
| `*.g.cs` | `CoreEx.Generator` (contracts) or `*.CodeGen` / `*.Database` tooling |
| `*.g.sql` / `*.g.pgsql` | `*.Database` tooling (outbox schema objects) |

Regenerate by re-running the relevant tooling project (`dotnet run` in `*.CodeGen` or `*.Database`).

## Layer Dependency Summary

```
Host (Api / Subscribe / Relay)   ← composition root only, no business logic
  └─ Application
       ├─ Contracts
       └─ Domain (optional)
  Infrastructure                 ← implements Application interfaces
       └─ Contracts
```

Hosts depend on all layers to compose the application. They contain no business logic.

## Capability-Specific Guidance

File-scoped instruction files provide detailed guidance when you edit matching file types. Copy the relevant files from the CoreEx repo's `.github/instructions/` into your project's `.github/instructions/` folder.

| When editing | Guidance provided by |
|---|---|
| `**/Controllers/**/*.cs` | `coreex-api-controllers.instructions.md` |
| `**/Contracts/**/*.cs` | `coreex-contracts.instructions.md` |
| `**/Application/**/*.cs` | `coreex-application-services.instructions.md` |
| `**/Infrastructure/**/*.cs` | `coreex-repositories.instructions.md` |
| `**/*Validator*.cs` | `coreex-validators.instructions.md` |
| `**/Program.cs` | `coreex-host-setup.instructions.md` |
| `**/Subscribe/**/*.cs` | `coreex-event-subscribers.instructions.md` |
| `**/Domain/**/*.cs` | `coreex-domain.instructions.md` |
| `**/*.Test*/**/*.cs` | `coreex-tests.instructions.md` |
| `**/*.CodeGen/**` or `**/*.Database/**` | `coreex-tooling.instructions.md` |

## Further Reading

- [CoreEx on GitHub](https://github.com/Avanade/CoreEx)
- [Capabilities Guide](https://github.com/Avanade/CoreEx/blob/main/docs/capabilities.md) — deep capability and pattern explanations
- [Application Scaffolding Guide](https://github.com/Avanade/CoreEx/blob/main/docs/application-scaffolding-guide.md) — deciding what to scaffold for a new service
- [Sample Reference Architecture](https://github.com/Avanade/CoreEx/tree/main/samples) — complete Contoso domains demonstrating all patterns end-to-end
