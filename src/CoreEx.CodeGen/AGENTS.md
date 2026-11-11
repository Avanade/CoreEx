# CoreEx.CodeGen — AI Usage Guide

`CoreEx.CodeGen` is a **development-time** code generation tool — it is never deployed at runtime. Its default RefData command reads `ref-data.yaml` and generates the complete reference-data layer. Its explicit File command generates host-neutral CoreEx.IO CSV and nested JSONL integrations from top-level `*-file.yaml` definitions; Count reports generated versus handwritten code.

## Setup

Create a console project (e.g. `MyApp.CodeGen`) and add a `Program.cs` with one line:

```csharp
return await CoreEx.CodeGen.CodeGenConsole.Create().RunAsync(args);
```

For an independently scaffolded console, use `dotnet new coreex-codegen -n MyApp.CodeGen` through the scaffold workflow. The non-packable tool follows central/inline repository package policy and needs neither a database nor business-layer projects. Place `ref-data.yaml` beside the entry point only for RefData; File uses top-level `*-file.yaml` instead. The tool has no runtime-host ProjectReference.

## File processing

Run `dotnet run -- file` for all top-level `*-file.yaml` definitions, or supply explicit filenames with the same suffix. Run from the CodeGen project directory: `dotnet run --project <path>` selects the project to run but does not change the process working directory used for top-level configuration discovery. `--validate` checks the whole batch without writing; `--simulation` and `--expect-no-changes` retain OnRamp behavior. See [File usage](https://github.com/Avanade/CoreEx/blob/main/src/CoreEx.CodeGen/Files/README.md) and its [configuration reference](https://github.com/Avanade/CoreEx/blob/main/src/CoreEx.CodeGen/Files/docs/CodeGeneration.md); installed AI bundles also include `/.github/docs/coreex/files/` and `/.github/docs/coreex/schema/coreex-files.json`.

Required File metadata is `name`, opaque `version`, `runtimeProjectPath`, and typed `records`. Format defaults to Csv; grouped `csv.discriminatorIndex` enables exact unordered routing, and `csv.structure` declares an ordered hierarchy. JSONL selects `format: Jsonl`, grouped `json` nesting-depth/unknown-member options, optional `jsonl.discriminator`, `unit` selection/cardinality, and nested `record`/`collection` occurrences. Reusable nested types are not CSV assembled groups. JSONL supports one business type plus opt-in bounded metadata routes. All three modes use the shared lifecycle. JSON document/XML readers and selectors are not implemented. Keep artifacts together in the configured runtime project/subfolder; override `runtimeNamespace` rather than guessing evaluated MSBuild namespace values.

The generation-first Functions example uses `tests/CoreEx.Azure.Functions.TestApp.CodeGen/inventory-file.yaml` (`name: Inventory`, one `Adjustment` record) and outputs `InventoryAdjustment`, `InventoryAdjustmentMapping`, `InventoryAdjustmentValidator`, `InventoryContext`, `InventoryProcessor`, and `InventoryHandler` under `InventoryImports/Files`. Processors inherit protected `Services` from `FileProcessor<TUnit>` for coordinator options/report adapters; this is the construction-time provider, not an automatically invocation-scoped or fresh unit provider. Unit business services remain in the handler's fresh child scope. Keep application execution and Functions-specific source/report handling in handwritten extensions outside the generator; do not hand-author duplicate wire, mapping, or validator types.

The generated processor rejects mismatched context name/version/type before opening input. Default processing is validation-only; inherited execution fails until a handwritten handler implements it. Handler starters are create-only and never overwritten. The host owns execution-context registration, immutable stream factories, reporting, and application transactions/idempotency. Optional String `referenceData: { type: Fully.Qualified.Type, allowInactive: false }` emits CoreEx's typed code-validation rule, not reference-object conversion. Initialize the ambient orchestrator/collections before processing; unknown/inactive codes are validation messages, missing setup is a fault, and replay does not rerun lookup validation or normalize codes.

## ref-data.yaml Structure

```yaml
collectionSortOrder: Code     # default sort for all reference data collections

entities:
  - name: Status
    idType: Int32              # Id property type; defaults to String. Must be one of: String, Guid, Int32, Int64.
    properties:
      - name: IsExternal
        type: bool
  - name: Country
  - name: Currency
```

Run the project (`dotnet run`) to regenerate all `.g.cs` files after changing the YAML.

## Generated Outputs

| Output | Layer | What changes it |
|---|---|---|
| `{Name}.g.cs` (Contracts project root) | Contracts | `ref-data.yaml` entity/property config |
| `Controllers/ReferenceDataController.g.cs` | Api | `ref-data.yaml` route/entity config |
| `ReferenceDataProvider.g.cs` (Application project root) — `IReferenceDataProvider` for the orchestrator | Application | `ref-data.yaml` entity config |
| `Controllers/{Name}Controller.g.cs` — **mutable entities only** | Api | `mutability` / `mutableAttribute` |
| `Interfaces/IReferenceDataService.g.cs` + `ReferenceDataService.g.cs` — **only if any entity is mutable** | Application | `mutability` / `validator` |
| `Repositories/IReferenceDataRepository.g.cs` | Application | `ref-data.yaml` entity/repository config |
| `Repositories/ReferenceDataRepository.g.cs` | Infrastructure | `ref-data.yaml` repository config |
| `Mapping/{Name}Mapper.g.cs` | Infrastructure | `ref-data.yaml` property/mapping config |

`Repositories`/`Mapping` are the default `dataRepositoriesPath`/`dataMappingPath` folder names — both configurable in `ref-data.yaml`.

## Readonly vs Mutation

Entities are **read-only by default** (`mutability: None`). Set `mutability` per entity to opt in:

```yaml
repository: EntityFramework
getNamed: true                       # Root; default false — emits GetNamedAsync on ReferenceDataController.
entities:
- name: Brand
  mutability: CreateUpdateDelete     # None (default) | CreateUpdate | CreateUpdateDelete
  validator: BrandValidator          # Optional; default ReferenceDataValidator<Brand>; needs a default constructor.
  mutableAttribute: '[Authorize(Policy = "ReferenceDataAdmin")]' # Replaces the default [Authorize].
- name: Category                     # Read-only.
```

| | `None` | `CreateUpdate` | `CreateUpdateDelete` |
|---|:-:|:-:|:-:|
| List via `ReferenceDataController` (cached orchestrator) | ✅ | ✅ | ✅ |
| `{Name}Controller`: GET `{id}`, POST (inactive), PUT, PATCH, POST `{id}/activate`, POST `{id}/deactivate` | — | ✅ | ✅ |
| DELETE `{id}` (204; active values rejected with 400 — deactivate first) | — | — | ✅ |
| `IReferenceDataService`/`ReferenceDataService` methods, repository write ops, bidirectional mapper | — | ✅ | ✅ (+ delete) |
| Events (`created/updated/activated/deactivated` `.v1`, `deleted`) + orchestrator cache invalidation | — | ✅ | ✅ |

- Service and service interface are generated **only when at least one entity is mutable**.
- A mutable entity **requires `repository: EntityFramework` or `Cosmos`** — codegen throws otherwise (`None` is read-only only). `Cosmos` uses `CosmosDbReferenceData` (duplicate codes rely on a `/typeDiscriminator` + `/code` unique key on the container, which must also be registered with `CosmosDbContainerOptions.WithReferenceDataOutboxEvent()` so the co-located outbox events — which carry neither path — do not collide with each other) and the generated service re-gets the item after the transaction so the returned `ETag` is final.
- `code` is immutable after create; create always yields an inactive item; activate/deactivate are no-ops (no event) if already in that state.
- Mutable controllers default to `[Authorize]`. Set `mutableAttribute` to a project-specific authorization policy/role, or to `[AllowAnonymous]` only when the write API is intentionally public; this replaces the default attribute. Ensure the API project's `GlobalUsing.cs` imports `Microsoft.AspNetCore.Authorization`.
- `attribute` (root/entity) decorates only the read-only `ReferenceDataController`; use `mutableAttribute` for the write endpoints.
- The host needs `IUnitOfWork` registered; the generated service wraps each write and its outbox event in one transaction.
- **No usage/cascade check on delete or deactivate.** Other tables usually store the code (e.g. Product.BrandCode), so deleting or deactivating a value still in use leaves that data silently invalid. This is the consumer's responsibility — use the PreCheckAsync hook on the generated ReferenceDataService to veto the operation.
- PreCheckAsync is a private Func<IReferenceData, EventAction, CancellationToken, Task<Result>> set from a hand-written partial class ReferenceDataService via partial void OnInitialization(). It is a lightweight allow/deny veto only — no side effects, cascades or other writes; if more is needed, hand-write the data/repository logic instead of stretching the hook. It runs for Activated/Deactivated/Deleted only (not create/update), before the transaction (so not atomic, by design), and one delegate serves all mutable entities — switch on type/action. See the README for an example.
- If the table lacks `Description`/`StartsOn`/`EndsOn` columns (EF `Ignore`d), a non-null value for them is rejected on create/update with a 400 (`not-supported`) — add the columns if needed.

## Do Not

- Do not edit `*.g.cs` files — they are overwritten on every generation run. Edit `ref-data.yaml` or the Handlebars templates in the `CoreEx.CodeGen` package instead.
- Do not set `mutability` on an entity whose `repository` is not `EntityFramework` or `Cosmos`, and do not hand-write mutation endpoints/services that duplicate the generated ones — turn on `mutability` instead.
- Do not assume delete/deactivate protects referenced data — add the checks yourself in `PreCheckAsync` (never in the `.g.cs`).
- Do not add `CoreEx.CodeGen` as a runtime dependency — it is a development tool only.

## Further Reading

- [README](./README.md) — full YAML schema, script structure, and template customisation reference.
- [Tooling](../../samples/docs/tooling.md) — how `*.CodeGen` and `*.Database` projects are used together in the sample solution, including run order and generated-file ownership.
- [Contracts layer](../../samples/docs/contracts-layer.md) — shows generated reference-data contracts (`[ReferenceData]`) and how `ref-data.yaml` drives the controller/service/repository layer.
