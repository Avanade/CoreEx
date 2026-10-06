# CoreEx.CodeGen

> Provides the CoreEx development-time code-generation tooling: a deterministic, schema-driven pipeline that scaffolds the full reference-data implementation — contract, controller, service, repository interface, repository, and mapper — from a single YAML configuration file — read-only by default, with opt-in per-entity mutation (create, update, activate/deactivate, delete).

## Overview

`CoreEx.CodeGen` is a console-executable tooling package used **during development**, not at runtime. It reads a developer-authored `ref-data.yaml` file (validated against [`schema/coreex-refdata.json`](../../schema/coreex-refdata.json)) as its sole configuration source, then produces the `.g.cs` generated files that implement the complete reference-data layer across the Contracts, Api, Application, and Infrastructure projects of a CoreEx solution.

Generation is orchestrated by [OnRamp](https://github.com/Avanade/OnRamp), which loads `ref-data-script.yaml` (embedded in the package) to discover the ordered sequence of generation steps. Each step binds a generator class to a Handlebars template and an output path; OnRamp resolves the template, evaluates it against the typed configuration model, and writes the result. Templates reside in `RefData/Templates/` and are also embedded so the package is entirely self-contained.

The package exposes `CodeGenConsole`, a thin wrapper around `OnRamp.Console.CodeGenConsole`, as its entry point. A consuming project needs only a single `Program.cs` line to wire up the generator. A secondary `Count` command is available to report generated vs. hand-authored file and line counts across the solution directories.

## Motivation

- Reference data (lookup tables) follows a well-understood, deterministic pattern: each entity needs a contract class, a controller route, a service method, a repository interface, a repository implementation, and a mapper. Writing these by hand is repetitive and error-prone.
- Centralising the pattern in code-generation ensures every entity is consistent — identical structure, naming conventions, route patterns, and EF mapping — with no room for per-entity drift.
- The YAML input and JSON schema act as a concise, reviewable declaration of the domain's reference data catalogue; the generated output is an artefact, not something developers need to maintain.
- Embedding the script and templates inside the NuGet package means consuming projects carry no additional files — only a `ref-data.yaml` and a one-line `Program.cs`.

## Key capabilities

- 📄 **Schema-validated YAML input**: `ref-data.yaml` is validated against `schema/coreex-refdata.json`; the schema covers root settings (`domain`, `idType`, `collectionSortOrder`, `route`, `routeConvention`, `repository`) and per-entity / per-property overrides, including `getNamed`, `attribute`, `mutability`, `validator` and `mutableAttribute`.
- 🔧 **Script-driven generation**: `ref-data-script.yaml` (embedded) defines the ordered generation steps — contract, controller, provider, repository interface, repository, and mapper, plus service interface, service and per-entity controller for mutable entities — each bound to a generator class and a Handlebars template.
- 📐 **Handlebars templates**: nine `.hbs` templates (embedded in `RefData/Templates/`) produce idiomatic CoreEx C# across all target layers; output files carry a `.g.cs` suffix to clearly distinguish generated from hand-authored code.
- 🏗️ **Multi-layer output**: a single run generates artefacts across four project directories — Contracts, Api, Application, and Infrastructure — all resolved automatically from the CodeGen project's location by convention.
- 🗺️ **EF Core mapper generation**: the `MapperGenerator` emits a typed mapper per entity, with per-property mapping directives derived from the `PropertyConfig`; entities can opt out via `excludeMapper: true`.
- ✏️ **Opt-in mutation**: per-entity `mutability` (`None` / `CreateUpdate` / `CreateUpdateDelete`) additionally generates a service, service interface, per-entity controller and repository write operations — see [Readonly vs Mutation](#readonly-vs-mutation).
- 📊 **Code-count reporting**: the `Count` command walks the solution output directories and reports total vs. generated file and line counts per directory, helping track the proportion of the codebase that is generated.

## Usage

A CodeGen project is a minimal console executable that references this package. Create a project at e.g. `My.App.Sales.CodeGen`, add the project reference, then write:

**`Program.cs`**
```csharp
await CoreEx.CodeGen.CodeGenConsole.Create().RunAsync(args);
```

**`ref-data.yaml`** — place alongside `Program.cs`:
```yaml
collectionSortOrder: Code
repository: EntityFramework
entities:
- name: Status
- name: Region
  properties:
  - name: CountryCode
    type: ^Country
- name: Currency
  plural: Currencies
  idType: Guid
```

Run from the project directory:
```
dotnet run
```

The generator resolves the Contracts, Api, Application, and Infrastructure project directories relative to the CodeGen project's location by convention (e.g. `../My.App.Sales.Contracts`, `../My.App.Sales.Api`, etc.), then writes `.g.cs` files into the appropriate sub-folders.

To report code counts instead of generating:
```
dotnet run -- count
```

## Schema

The [`schema/coreex-refdata.json`](../../schema/coreex-refdata.json) JSON Schema defines the structure and validation rules for `ref-data.yaml`. 
The hierarchy is as follows:

```
CodeGeneration
└── Entity(s)
  └── Property(s)
```

Configuration details for each of the above are as follows:

- [`CodeGeneration`](./docs/CodeGeneration.md)
- [`Entity`](./docs/Entity.md)
- [`Property`](./docs/Property.md)

## Readonly vs Mutation

Reference data is **read-only by default**: entities are loaded through the `ReferenceDataOrchestrator` (cached) and exposed through the single `ReferenceDataController`. Mutation is **opt-in per entity** via `mutability`, and adds a transactional write path (service → repository → database, with events and cache invalidation) alongside the read path. Nothing about the read path changes for a mutable entity.

### Options

| Option | Level | Default | Description |
|---|---|---|---|
| `mutability` | Entity | `None` | `None` (read-only), `CreateUpdate` (create, update, activate, deactivate) or `CreateUpdateDelete` (adds delete). |
| `validator` | Entity | `ReferenceDataValidator<{Name}>` | Validator type used on create/update. Must have a default constructor. The default enforces `code` (mandatory, max 50), `text` (mandatory, max 250), `description` (max 1000) and `endsOn >= startsOn`. |
| `mutableAttribute` | Entity | _none_ | Attribute applied as-is to the generated `{Name}Controller` class, e.g. `'[Authorize(Roles = "Admin")]'`. |
| `attribute` | Root / Entity | _none_ | Attribute applied to the read-only `ReferenceDataController` (root) or that entity's read operation (entity). Does **not** apply to mutable endpoints. |
| `getNamed` | Root | `false` | Emits the read-only `GetNamedAsync` endpoint. Applies to read-only and mutable entities alike. |
| `repository` | Root / Entity | root value | **Must be `EntityFramework` or `Cosmos` for a mutable entity** — generation fails fast otherwise. `None` is read-only only. `Cosmos` uses `CosmosDbReferenceData`; duplicate codes are rejected by a `/typeDiscriminator` + `/code` unique key on the container, which must also use `CosmosDbContainerOptions.WithReferenceDataOutboxEvent()` so co-located outbox events do not collide. |

```yaml
collectionSortOrder: Code
repository: EntityFramework
entities:
- name: Brand
  mutability: CreateUpdateDelete
  mutableAttribute: '[Authorize(Roles = "Admin")]'
- name: Category                    # Read-only (default).
```

### Outputs by mode

| Output | Layer | Read-only | Mutable |
|---|---|:-:|:-:|
| `{Name}.g.cs` contract | Contracts | ✅ | ✅ |
| `Controllers/ReferenceDataController.g.cs` — list (and optional `GetNamed`) | Api | ✅ | ✅ |
| `Controllers/{Name}Controller.g.cs` — per-entity write endpoints | Api | — | ✅ |
| `ReferenceDataProvider.g.cs` — `IReferenceDataProvider` for the orchestrator | Application | ✅ | ✅ |
| `Interfaces/IReferenceDataService.g.cs` | Application | — | ✅ |
| `ReferenceDataService.g.cs` — validation, unit of work, events, cache invalidation | Application | — | ✅ |
| `Repositories/IReferenceDataRepository.g.cs` | Application | read ops | + write ops |
| `Repositories/ReferenceDataRepository.g.cs` | Infrastructure | read ops | + write ops via `EfDbReferenceData` |
| `Mapping/{Name}Mapper.g.cs` | Infrastructure | forward only (reverse throws `NotSupportedException`) | bidirectional |

The service interface and service are generated **only if at least one entity is mutable**; a read-only solution produces neither. `ReferenceDataProvider` replaced the provider role formerly held by `ReferenceDataService` — a mutable solution's `ReferenceDataService` is now a different class entirely (the write service).

### Endpoints

Generated per mutable entity into `{Name}Controller`, routed at the root `route` (default `/api/refdata`) plus the entity `route` (default: pluralised name per `routeConvention`) — e.g. `/api/refdata/brands`. The `{id}` below is that entity's `idType` (`String`, `Guid`, `Int32` or `Int64`).

| Verb & route | Mutability | Success | Notes |
|---|---|---|---|
| `GET {id}` | any mutable | 200 | Includes inactive items. |
| `POST` | any mutable | 201 + `Location` | `[IdempotencyKey]`; generated `id`, always created **inactive**. |
| `PUT {id}` | any mutable | 200 | Full replace; `If-Match` required (428 if absent); `id`, `code` and `isActive` in the body are ignored. |
| `PATCH {id}` | any mutable | 200 | `application/merge-patch+json`; ETag-checked. |
| `POST {id}/activate` | any mutable | 200 | No-op (no event, no cache invalidation) if already active. |
| `POST {id}/deactivate` | any mutable | 200 | No-op if already inactive. |
| `DELETE {id}` | `CreateUpdateDelete` | 204 | Idempotent (missing → 204). Deleting an **active** value is a 400 (`cannot-delete-active`) — deactivate first. |

The list endpoint continues to exclude inactive items unless `$inactive=true` is supplied.

### Runtime behaviour

- **`code` is immutable** — update (PUT or PATCH) keeps the existing `id`, `code` and active state; use activate/deactivate to change the latter.
- **Transactional** — each operation runs inside `IUnitOfWork.TransactionAsync`; the change and its outbox event commit atomically.
- **Events** — added to the unit of work's outbox only when the operation actually changed something: `{domain}.{entity}.created.v1`, `.updated.v1`, `.activated.v1`, `.deactivated.v1` (carrying the value) and `.deleted` (key only).
- **Cache** — after a successful mutation the service calls `ReferenceDataOrchestrator.Current.TryInvalidateAsync<T>()` (post-commit), so the next read reloads the collection (and, with a distributed cache, the shared entry is removed too). The try variant is awaited but best-effort: a cache failure is logged as a warning and does not fail the already-committed write; the stale entry then expires per its TTL. Use the strict `InvalidateAsync<T>()` only where an eviction failure must surface, and never invoke either inside the transaction.
- **Validation** — failures surface as a standard 400 validation `ProblemDetails`.

### Referential integrity and pre-checks

**CodeGen does not check whether a reference-data value is in use.** Delete, deactivate (and the `code` that other tables store) carry no cascade or usage check, and consumers typically hold the *code* (e.g. `Product.BrandCode`), usually without a database foreign key. Deleting a value, or deactivating one that is still referenced, therefore leaves existing data pointing at a missing or inactive value — it does not fail, and the data is now quietly invalid. Whether that is acceptable, and what to do about it, is **the consumer's responsibility**. The only built-in guard is that an *active* value cannot be deleted (`cannot-delete-active`), which merely forces deactivate-then-delete; it does nothing about usage.

Where a rule is needed, the generated `ReferenceDataService` provides a `PreCheckAsync` hook. **Its intent is deliberately narrow: a lightweight veto (allow/deny) before the mutation — nothing more.** It is not a place for side effects, cascades or other data changes. If the operation needs more than a veto (e.g. reassigning or deactivating dependents atomically with the change, locking reads, or any extra writes), do not stretch the hook: implement that data/repository logic yourself (a hand-written service or repository method inside your own unit of work) rather than using the generated mutation for that entity. The hook is a `Func<IReferenceData, EventAction, CancellationToken, Task<Result>>` (default is a no-op success) exposed as a **private** property, so set it from the hand-written side of the `partial` class via the `OnInitialization()` partial method:

```csharp
public partial class ReferenceDataService
{
    partial void OnInitialization() => PreCheckAsync = (value, action, cancellationToken) =>
    {
        if (value is Brand b && b.Code == "YETI" && action == EventAction.Deactivated)
            return Result.BusinessError("YETI brand cannot be deactivated as it is awesome.", c => c.WithErrorCode("yeti-cannot-be-deactivated")).AsTask();

        return Result.SuccessTask;
    };
}
```

(This is the Products sample, `ReferenceDataService.cs`.) For a real in-use check, add a method to `IReferenceDataRepository` and the matching `partial` repository class (both are `partial`) that queries the referencing table by code, and call it from the hook when `action` is `Deactivated` or `Deleted`; return a `Result` failure (e.g. `Result.BusinessError(...)`) to veto the operation.

How the hook behaves:

| | |
|---|---|
| **Invoked for** | `Activated`, `Deactivated`, `Deleted` only — **not** create or update. |
| **Single hook** | One delegate for all mutable entities; switch on the value's type (and `action`) to target specific entities. |
| **Ordering** | Runs after the value is loaded and **before** the transaction opens. Deactivate/activate run it even when the value is already in that state. Delete runs it only if the value exists (a missing value is an idempotent 204), and before the `cannot-delete-active` check. |
| **Not atomic (by design)** | The check and the write are not in the same transaction, so a concurrent insert can still slip in between. Back it with a database constraint if you need a hard guarantee; if you need the check and the change to be atomic, hand-write that logic instead. |
| **Failure** | The returned failure surfaces as a normal error response (a `BusinessError` becomes a 400) and no event or cache invalidation occurs. |
### Prerequisites and gotchas

- The host must register `IUnitOfWork` (and the outbox, where events are required) — the generated service takes `IUnitOfWork` and `IReferenceDataRepository`.
- The generated reverse mapper writes `Code`, `Text`, `Description`, `SortOrder`, `IsActive`, `StartsOn`, `EndsOn`, `ETag` and any extra properties. If the table has no `Description`/`StartsOn`/`EndsOn` columns (the DbEx generated `DbContext` then `Ignore`s them, as for the sample `Brand`), create/update **rejects** any non-null value for them with a 400 (`not-supported`, e.g. "Starts on is not currently supported and as such cannot be set.") — add the columns if you need them.
- Per-entity authorization is **not** generated beyond `mutableAttribute`; apply a policy there or in a hand-written partial.
- Never edit the `.g.cs` outputs — change `ref-data.yaml` or the templates and regenerate.

## Key types

| Type | Description |
|------|-------------|
| **[`CodeGenConsole`](./CodeGenConsole.cs)** | Entry point for the code-generation tool; wraps `OnRamp.Console.CodeGenConsole`, loads `ref-data.yaml` and `ref-data-script.yaml`, and exposes the `RefData` and `Count` commands. |
| **[`CommandType`](./CommandType.cs)** | Enum selecting the command to execute: `RefData` (default — runs code generation) or `Count` (reports file and line statistics). |
| **[`CodeGenConfig`](./RefData/Config/CodeGenConfig.cs)** | Root configuration model for reference-data generation; maps directly to the top-level YAML and resolves all project directory paths, namespace defaults, and entity collection preparation. |
| **[`EntityConfig`](./RefData/Config/EntityConfig.cs)** | Per-entity configuration: name, plural, text, `idType`, `collectionSortOrder`, route, repository mode, model name, mapper name, `excludeMapper`, and the property collection. |
| **[`PropertyConfig`](./RefData/Config/PropertyConfig.cs)** | Per-property configuration: name, type (with `^`-prefix for reference-data and `?`-suffix for nullable), text, data model property name, and exclude flags for contract and mapping generation. |
| **[`RootGenerator`](./RefData/Generators/RootGenerator.cs)** | `CodeGeneratorBase` implementation that selects `CodeGenConfig` as its single generation target; used for the provider, repository interface, and repository templates. |
| **[`RootMutableGenerator`](./RefData/Generators/RootMutableGenerator.cs)** | Selects `CodeGenConfig` only when at least one entity is mutable; used for the service interface and service templates (skipped entirely for read-only solutions). |
| **[`MutableApiGenerator`](./RefData/Generators/MutableApiGenerator.cs)** | Selects each mutable `EntityConfig` (only when the Api project directory exists); produces one `{Name}Controller.g.cs` per mutable entity. |
| **[`ApiGenerator`](./RefData/Generators/ApiGenerator.cs)** | `CodeGeneratorBase` implementation that selects `CodeGenConfig` only when the Api project directory exists; used for the controller template (API generation is skipped entirely when the Api project isn't found). |
| **[`ContractGenerator`](./RefData/Generators/ContractGenerator.cs)** | `CodeGeneratorBase` implementation that iterates all `EntityConfig` entries to produce one contract file per entity. |
| **[`MapperGenerator`](./RefData/Generators/MapperGenerator.cs)** | `CodeGeneratorBase` implementation that iterates `EntityConfig` entries where `excludeMapper` is not `true`, producing one mapper file per entity. |
| **[`CodeGenCounter`](./Counting/CodeGenCounter.cs)** | Walks the solution output directories and counts total vs. generated files and lines; drives the `Count` command output. |
| **[`DirectoryCountStatistics`](./Counting/DirectoryCountStatistics.cs)** | Aggregates file and line counts (total and generated) for a single directory and its children; renders a formatted hierarchy table to the logger. |

## Namespaces

| Namespace | Description | Documentation |
|-----------|-------------|---------------|
| **`CoreEx.CodeGen.RefData`** | Reference-data generation pipeline: typed configuration models (`Config`), generator classes (`Generators`), and the embedded Handlebars templates (`Templates`). | [📖 README](./RefData/README.md) |
| **`CoreEx.CodeGen.Counting`** | File and line counting utilities for reporting generated vs. hand-authored code proportions across solution directories. | [📖 README](./Counting/README.md) |

## Additional Resources

- [OnRamp](https://github.com/Avanade/OnRamp) — the underlying code-generation orchestration framework used to load the script, resolve templates, and manage file output.
- [CoreEx ref-data schema](../../schema/coreex-refdata.json) — JSON Schema for `ref-data.yaml`; use it with IDE YAML language-server support for validation and auto-complete while authoring configuration.

## AI Usage Guide

An [`AGENTS.md`](./AGENTS.md) file is included with this package. AI coding assistants (GitHub Copilot, Claude, Cursor, etc.) that support workspace-injected package documentation will automatically surface concise usage guidance, code examples, and `Do Not` rules for this package without requiring a local CoreEx checkout.
