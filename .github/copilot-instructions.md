---
# applyTo is intentionally omitted — this file is applied globally by VS Copilot convention for copilot-instructions.md.
description: "Project-wide guidelines and conventions for CoreEx development"
tags: ["guidelines", "conventions", "comments"]
---

# Copilot Instructions

## Purpose
CoreEx is a modular .NET framework for enterprise APIs and distributed services. Favor CoreEx-native primitives, patterns, and extensions over ad-hoc implementations.

## Repository Shape
- `CoreEx.sln`: main solution for framework + samples.
- `src\`: reusable CoreEx libraries (AspNetCore, Database, EntityFrameworkCore, Events, Validation, DomainDriven, RefData, Caching, etc.).
- `gen\CoreEx.Generator\`: Roslyn source generator for contracts.
- `tests\`: framework-level tests.
- `samples\src\Contoso.*\`: sample domains split by layer/host.
- `samples\aspire\AppHost.cs`: orchestration entrypoint.
- `coreex-starter\`: separate starter template repo — ignore unless user wants starter changes.

## Build, Test, and Run
- **Build**: `dotnet build CoreEx.sln`
- **Test**: `dotnet test CoreEx.sln` or target specific projects.
- **Single test**: `dotnet test <proj> --filter "FullyQualifiedName~<name>"`
- **Samples**: docker-compose infrastructure + dotnet run for Database projects + Aspire AppHost.
- **Linting**: No separate `dotnet format`. Build is the lint pass (nullable, LangVersion=preview, TreatWarningsAsErrors in `src\Directory.Build.props`).
- **Formatting**: 4 spaces for `*.cs`, 2 spaces for `*.json|*.xml|*.yaml|*.props|*.csproj|*.sln|*.sql` per `.editorconfig`.

## Local Development Infrastructure

All sample hosts depend on containerised infrastructure. Start it before running any host or integration test:

```bash
podman compose -f docker-compose.yml up -d   # Podman preferred; `docker compose` also works
```

| Service | Port(s) | Purpose |
|---|---|---|
| `db-sql-server` | 1433 | Shopping domain database; Service Bus emulator backing store |
| `db-postgres` | 5432 | Products domain database |
| `redis-cache` | 6379 | FusionCache Redis backplane (all domains) |
| `servicebus-emulator` | 5672 AMQP, 5300 mgmt | Azure Service Bus emulator; namespace `sbemulatorns`; topic `contoso` with subscriptions `products` and `shopping` (both session-enabled); config at `servicebus/Config.json` |
| `dts-emulator` | 8080, 8082 | Azure Durable Task Scheduler emulator; task hubs `default` and `order` |
| `aspire-dashboard` | 18888 UI, 4317 OTLP | Standalone OpenTelemetry dashboard; usable without running the full Aspire AppHost |

Connection strings for each service in development are in each host's `appsettings.Development.json` under the `Aspire:` configuration key hierarchy. See [`samples/docs/local-dev.md`](../samples/docs/local-dev.md) for full detail, connection string patterns, and startup sequences.

## Architecture
- **Two roles**: framework packages (`src\`) + sample reference implementations (`samples\`).
- **Business layers** (strict inward dependency — inner layers have no knowledge of outer): `*.Contracts` → `*.Application` → `*.Domain` (optional) → `*.Infrastructure`.
- **Host layers** (composition roots, no business logic): `*.Api`, `*.Outbox.Relay`, `*.Subscribe`.
- **Design-time tooling** (no runtime presence): `*.CodeGen` (generates reference-data layer from `ref-data.yaml`) and `*.Database` (schema, seeding, outbox infrastructure via DbEx).
- **Sample flow**: Controllers → `WebApi` helpers → Application services (validate + `IUnitOfWork`) → Infrastructure repositories (EF + explicit mappers) → transactional outbox → relay publishes to Service Bus → subscribers consume.
- **Polyglot data**: Products uses PostgreSQL (`CoreEx.Database.Postgres` + `CoreEx.EntityFrameworkCore`); Shopping uses SQL Server (`CoreEx.Database.SqlServer` + `CoreEx.EntityFrameworkCore`). Layers above Infrastructure are database-agnostic.
- **Primary domains**: Products and Shopping complete; Orders WIP. See `samples\README.md` for topology.
- **Aspire**: orchestrates all sample hosts in `samples\aspire\Contoso.Aspire\AppHost.cs` for local distributed development and E2E testing.

## Key Conventions That Matter in This Repo

### CoreEx-First Patterns
- Prefer CoreEx primitives before introducing external libraries that overlap with framework capabilities.
- Prefer CoreEx exception types (`NotFoundException`, `ValidationException`, `BusinessException`, `ConcurrencyException`, etc.) and CoreEx `Result`/`Result<T>` flows over custom error wrappers.
- Do not introduce AutoMapper in any repository unless the repository maintainer explicitly requests it. Repositories and services use explicit mapping helpers/classes.

### Contracts and Source Generation
- Contracts are commonly declared as `[Contract] public partial class ...`.
- Mutable contracts often implement `IIdentifier<T>`, `IETag`, and `IChangeLog`.
- Use `[ReadOnly(true)]` for server-managed fields and `[ReferenceData<T>]` for reference-data-backed code properties.
- Canonical casing transformations belong in property setters when already established by the model (for example `Sku` uppercasing in `ProductBase`).
- Favor the existing source-generation approach; do not hand-write members that are meant to be generated.

### Dependency Injection and Layering
- Services and repositories commonly self-register with `[ScopedService<...>]`.
- Hosts use `AddDynamicServicesUsing<T1, T2, ...>()` to discover and register services instead of manually wiring every type.
- Keep interface/implementation layering intact:
  - application interfaces live in `Application\Interfaces\`, `Application\Repositories\`, `Application\Adapters\`, or `Application\Policies\`;
  - infrastructure implementations live in `Infrastructure\`.
- There are two distinct mapping layers — do not conflate them:
  - **Application-level** (`Application\Mapping\`): Domain aggregate → Contract, using `Mapper<TSource, TDest, TSelf>`; present only in domains with a Domain layer (e.g. Shopping).
  - **Infrastructure-level** (`Infrastructure\Mapping\`): Contract ↔ Persistence model, using `BiDirectionMapper<TFrom, TTo, TSelf>`; present in all domains.

### Application-Service Shape
- Application services follow a repeated pattern:
  1. guard/normalize inputs;
  2. validate with CoreEx validators;
  3. load current state where needed;
  4. wrap mutations **and** event publication together inside `_unitOfWork.TransactionAsync(...)` — both the database write and the outbox event are committed atomically or not at all;
  5. add `EventData` to `_unitOfWork.Events` inside that same transactional scope.
- Use exception-based flows for straightforward CRUD-style services.
- Use `Result<T>` pipelines for aggregate-oriented flows and multi-step orchestration, especially in Shopping.

### Adapters (Anti-Corruption Layer)
- When a domain needs to interact with another domain or external service, define an **adapter interface** in `Application\Adapters\`. The Application layer depends on this domain-idiomatic abstraction — never on the remote API's schema or transport directly.
- Infrastructure implements the adapter using a **typed HTTP client** (`Infrastructure\Clients\`) for the transport concern, keeping client and orchestration in separate focused classes.
- Two adapter roles appear in Shopping:
  - **Synchronous adapter** (`IProductAdapter`) — real-time calls (e.g. inventory reservation at checkout); the HTTP client is called live inside the unit of work.
  - **Sync/replication adapter** (`IProductSyncAdapter`) — event-driven data replication; receives published domain events and maintains a local eventually-consistent copy in the domain's own store.
- Do not call `HttpClient` directly from services — always go through the adapter interface.

### Policies
- Policies (`Application\Policies\`) encapsulate **domain-level guard logic** that requires I/O — adapter or repository calls. A policy provides a named, independently testable home for rules that depend on external state and cannot be expressed in a validator alone (synchronous) or in the domain model (no async I/O). Policies return `Result` or `Result<T>` and can be called from any point in service orchestration where the condition needs to be verified.
- Use a policy when an invariant cannot be expressed in a validator alone (e.g. confirming a referenced entity exists before allowing a mutation).
- Policies return `Result` or `Result<T>` and compose naturally into `Result<T>` service pipelines via `.GoAsync()` / `.ThenAsAsync()`.

### Host Composition
- `Program.cs` files follow a predictable CoreEx host shape:
  - `builder.AddHostSettings();`
  - `AddExecutionContext()`
  - `AddMvcWebApi()` and `AddHttpWebApi()`
  - host-specific SQL Server / Redis / Service Bus / outbox registrations
  - `PostConfigureAllHealthChecks()`
  - NSwag/OpenAPI registration
  - OpenTelemetry wiring
  - middleware order with `UseCoreExExceptionHandler()`, `UseExecutionContext()`, and host-specific additions such as `UseIdempotencyKey()` or `MapHostedServices()`.
- API hosts, subscriber hosts, and outbox relay hosts intentionally have different startup shapes. Do not collapse them into one generic startup unless the user explicitly asks for that refactor.

### Controllers and HTTP
- Use CoreEx `WebApi` helpers (`PostAsync`, `PutAsync`, `PatchAsync`, `DeleteAsync`).
- PATCH: `application/merge-patch+json`.
- POST: use `[IdempotencyKey]`.
- OpenAPI/health endpoints standard in hosts.

### Data and Messaging
- Transactional outbox + Azure Service Bus are first-class messaging patterns across all domains.
- **Products** uses PostgreSQL; **Shopping** uses SQL Server. Do not assume SQL Server when working on Products.
- Shopping: synchronous HTTP inventory reservation + transactional outbox + async event publishing. Preserve this split.
- Both domains use `CoreEx.Caching.FusionCache` (hybrid in-process + Redis backplane cache) for reference data and idempotency. Register via `AddFusionCache()` / `AddFusionHybridCache()` in `Program.cs`; clear via `Test.ClearFusionCacheAsync()` in test `[OneTimeSetUp]`.

### Testing
- Framework: UnitTestEx + NUnit + AwesomeAssertions (the `AwesomeAssertions` NuGet package — not FluentAssertions).
- Sample: `WithGenericTester<EntryPoint>` (unit) or `WithApiTester<Program>` (API/Subscribe/Relay).
- Integration tests: per-class named seed files `Data\read-data.seed.yaml` / `Data\mutate-data.seed.yaml` (Products), or a single `Data\data.yaml` (Orders/Shopping), in Test.Common + `Resources\` JSON expectations.
- **Intra-domain dependencies are real; inter-domain dependencies are always mocked.** Own database, cache, and outbox are started and seeded in `[OneTimeSetUp]`. Cross-domain HTTP calls and direct broker publishes are replaced with `MockHttpClientFactory` / `UseExpectedAzureServiceBusPublisher()`.
- Outbox assertion helpers are database-specific: `UseExpectedPostgresOutboxPublisher()` for Products; `UseExpectedSqlServerOutboxPublisher()` for Shopping. Do not use the SQL Server helper in Products tests.
- Mock downstream HTTP calls; do not assume live APIs.

### House Rules
- Code comments end with a period/full stop.
- Always use `.ConfigureAwait(false)` in service/repository code.
- `<Nullable>enable</Nullable>` and `<ImplicitUsings>enable</ImplicitUsings>` are set in `Directory.Build.props` — treat nullable warnings as errors, never suppress them with `!` without justification.
- Every project has a single `GlobalUsing.cs` at the project root. All `using` statements go there — never in individual source files. The code generator (`*.CodeGen`) emits no `using` statements and depends on this.
- File-scoped namespace declarations only: `namespace Foo.Bar;` — never block-scoped.
- Single-line `if` bodies do not need braces: `if (x) return;`
- Use expression-bodied syntax (`=>`) when the entire method or property body is a single expression.
- Private instance fields are always prefixed with `_`.

### Generated Code
Never create or edit `*.g.cs`, `*.g.sql`, or `*.g.pgsql` files directly. Each generator owns its outputs:

| File pattern | Generator | Change instead |
|---|---|---|
| `*.g.cs` (contracts, ref-data) | Roslyn source generator (`CoreEx.Generator`) | The `[Contract]`- or `[ReferenceData]`-decorated partial class |
| `*.g.cs` (ref-data layer — controller, service, repository, mapper) | `*.CodeGen` project (CoreEx.CodeGen + `ref-data.yaml`) | `ref-data.yaml` config or the Handlebars templates in `CoreEx.CodeGen/RefData/Templates/` |
| `*.g.sql`, `*.g.pgsql`, `*DbContext.g.cs`, `Persistence/*.g.cs` | `*.Database` project (DbEx) | DbEx YAML config or SQL migration scripts |

See [INSTRUCTION_AUTHORING.md](.github/INSTRUCTION_AUTHORING.md#generated-code) for full generator ownership detail.

## Key Docs to Read Before Large Changes
- `README.md` — repo-level positioning and top-level commands.
- `samples\README.md` — runnable Contoso architecture and local setup.
- `docs\capabilities.md` — deeper CoreEx capability and pattern explanations.
- `samples\docs\layers.md` — full layer diagram, dependency rules, and design-time tooling overview.
- `samples\docs\patterns.md` — pattern catalog with links to layer-specific detail for every architectural, application, messaging, and testing pattern used in the samples.
- `samples\docs\<layer>.md` — detailed walkthrough for each layer: `contracts-layer.md`, `application-layer.md`, `domain-layer.md`, `infrastructure-layer.md`, `hosts-layer.md`, `testing.md`, `tooling.md`.
- `.github\instructions\*.instructions.md` — area-specific rules auto-injected when editing matching files (`Program.cs`, contracts, application services, repositories, validators, subscribers, tests).

## Agent Customizations (Prompts, Skills, and Templates)

The following prompts, skills, and templates are available in this repository. Type `/` in chat to invoke prompts and skills. Use `dotnet new` in a terminal for templates.

| Command | Type | When to use |
|---------|------|-------------|
| `CoreEx.Template` | Template pack | Deterministic `dotnet new` scaffolding for solution, API, relay, and subscriber shapes. Use `dotnet new install CoreEx.Template` and then run `dotnet new coreex`, `coreex-api`, `coreex-relay`, or `coreex-subscribe` as needed. |
| `CoreEx Expert` | Agent | Architecture guidance, pattern recommendations, and design review aligned to the samples and repo instructions. |
| `/init` | Prompt | Initialize a new CoreEx solution or workspace. |
| `/setup` | Prompt | Configure an existing CoreEx solution with standard tooling and settings. |

## Guidance for Authoring Instructions and Skills

When creating or maintaining Copilot instruction files and skills:

- **Instruction files** (`.instructions.md`) — see [INSTRUCTION_AUTHORING.md](./INSTRUCTION_AUTHORING.md) for standards on YAML frontmatter, section order, and content rules.
- **Skill files** (`SKILL.md`) — see [SKILL_AUTHORING.md](./SKILL_AUTHORING.md) for the directory structure pattern (`references/`, `assets/`), lean main file rules (<300 lines), and cross-referencing guidelines.

Both documents define durable patterns for creating guidance that is discoverable, maintainable, and context-efficient.