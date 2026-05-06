---
description: "Project-wide guidelines and conventions for CoreEx development"
tags: ["guidelines", "conventions", "comments"]
---

# Copilot Instructions

## Purpose
CoreEx is a modular .NET framework for enterprise APIs and distributed services. Favor CoreEx-native primitives, patterns, and extensions over ad-hoc implementations.

## Repository Shape
- `CoreEx.sln`: main solution for framework + samples.
- `src\`: reusable CoreEx libraries (AspNetCore, Database, EntityFrameworkCore, Events, Validation, DomainDriven, RefData, Caching, etc.).
- `gen\CoreEx.Gen\`: Roslyn source generator for contracts.
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

## Architecture
- **Two roles**: framework packages (`src\`) + sample reference implementations (`samples\`).
- **Domain layers**: `*.Contracts` → `*.Application` → `*.Infrastructure` → `*.Api`, plus `*.Database`, `*.Outbox.Relay`, `*.Subscribe` (messaging).
- **Sample flow**: Controllers → `WebApi` helpers → Application services (validate + `IUnitOfWork`) → Infrastructure repositories (EF + explicit mappers) → outbox events → relay publishes to Service Bus → subscribers consume.
- **Primary domains**: Products and Shopping complete; Orders WIP. See `samples\README.md` for topology.
- **Aspire**: orchestrates sample hosts in `samples\aspire\Contoso.Aspire\AppHost.cs`.

## Key Conventions That Matter in This Repo

### CoreEx-First Patterns
- Prefer CoreEx primitives before introducing external libraries that overlap with framework capabilities.
- Prefer CoreEx exception types (`NotFoundException`, `ValidationException`, `BusinessException`, `ConcurrencyException`, etc.) and CoreEx `Result`/`Result<T>` flows over custom error wrappers.
- Do not introduce AutoMapper unless the user explicitly requests it. Repositories and services use explicit mapping helpers/classes.

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
  - application interfaces live in `Application\Interfaces\` or `Application\Repositories\`;
  - infrastructure implementations live in `Infrastructure\`.

### Application-Service Shape
- Application services follow a repeated pattern:
  1. guard/normalize inputs;
  2. validate with CoreEx validators;
  3. load current state where needed;
  4. run mutations inside `_unitOfWork.ExecuteAsync(...)`;
  5. add `EventData` within the same unit-of-work scope.
- Use exception-based flows for straightforward CRUD-style services.
- Use `Result<T>` pipelines for aggregate-oriented flows and multi-step orchestration, especially in Shopping.
- When working in application or infrastructure code, follow `.github\instructions\application-services.instructions.md`, `.github\instructions\repositories.instructions.md`, and related scoped instruction files.

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
- SQL Server + outbox + Azure Service Bus are first-class patterns.
- Shopping: synchronous HTTP reservation + transactional outbox + async event publishing. Preserve this split.

### Testing
- Framework: NUnit + FluentAssertions.
- Sample: `WithGenericTester<EntryPoint>` (unit) or `WithApiTester<Program>` (API/Subscribe/Relay).
- Integration tests: `Data\data.yaml` (Test.Common) + `Resources\` JSON expectations + `ExpectSqlServerOutboxEvents(...)`.
- Mock downstream HTTP calls; do not assume live APIs.

### House Rules
- Code comments end with a period/full stop.
- Use `GlobalUsing.cs` per project; do not scatter `using` directives.
- Always use `.ConfigureAwait(false)` in service/repository code.

## Key Docs to Read Before Large Changes
- `README.md` for repo-level positioning and top-level commands.
- `samples\README.md` for the runnable Contoso architecture and local setup.
- `docs\capabilities.md` for the deeper CoreEx capability/pattern explanations.
- `.github\instructions\*.instructions.md` for area-specific rules when editing `Program.cs`, contracts, application services, repositories, validators, subscribers, or tests.

## Agent Customizations (Prompts and Skills)

The following prompts and skills are available in this repository. Type `/` in chat to invoke them.

| Command | Type | When to use |
|---------|------|-------------|
| `/generate-domain` | Skill | Guided scaffolding of a new CoreEx domain across all 5 layers. Use when your entity has custom fields, business rules, or you want the agent to reason about conventions, validation, and event naming. The agent will ask for inputs and generate code tailored to your domain model. |
| `/add-capability` | Skill | Retrofit an existing CoreEx domain with additional capabilities. Use when a domain already exists and you want to add messaging/integration features such as `Outbox.Relay`, `Subscribe`, Azure Service Bus wiring, or initial subscriber scaffolding without regenerating the domain. |
| `/coreex.plan` | Prompt | Create a codex-style ExecPlan in `.agent/execplans/` before implementation starts. Use when you want clarifying questions, a self-contained plan, explicit validation, and a user approval checkpoint before coding begins. Plans are indexed in `.agent/PLANS.md`. |
| `/scaffold-domain-from-templates` | Prompt | Fast, deterministic domain scaffolding by cloning and materializing the canonical templates in `.github\templates\domain\` with placeholder substitution. Use when your entity fits the standard template shape and you want exact output with no creative generation. |
| `/init` | Prompt | Initialize a new CoreEx solution or workspace. |
| `/setup` | Prompt | Configure an existing CoreEx solution with standard tooling and settings. |

## Guidance for Authoring Instructions and Skills

When creating or maintaining Copilot instruction files and skills:

- **Instruction files** (`.instructions.md`) — see [INSTRUCTION_AUTHORING.md](.github/INSTRUCTION_AUTHORING.md) for standards on YAML frontmatter, section order, and content rules.
- **Skill files** (`SKILL.md`) — see [SKILL_AUTHORING.md](.github/SKILL_AUTHORING.md) for the directory structure pattern (`references/`, `assets/`), lean main file rules (<300 lines), and cross-referencing guidelines.

Both documents define durable patterns for creating guidance that is discoverable, maintainable, and context-efficient.