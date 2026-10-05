# CoreEx.CodeGen — AI Usage Guide

`CoreEx.CodeGen` is a **development-time** code generation tool — it is never deployed at runtime. It reads a `ref-data.yaml` file and generates the complete reference-data layer (contract, controller, provider, repository, mapper — plus service and write endpoints for mutable entities) as `.g.cs` files.

## Setup

Create a console project (e.g. `MyApp.CodeGen`) and add a `Program.cs` with one line:

```csharp
await CoreEx.CodeGen.CodeGenConsole.Create().RunAsync(args);
```

Place `ref-data.yaml` alongside `Program.cs`.

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
  mutableAttribute: '[Authorize]'    # Optional; applied as-is to the generated BrandController class.
- name: Category                     # Read-only.
```

| | `None` | `CreateUpdate` | `CreateUpdateDelete` |
|---|:-:|:-:|:-:|
| List via `ReferenceDataController` (cached orchestrator) | ✅ | ✅ | ✅ |
| `{Name}Controller`: GET `{id}`, POST (inactive), PATCH, POST `{id}/activate`, POST `{id}/deactivate` | — | ✅ | ✅ |
| DELETE `{id}` (204; active values rejected with 400 — deactivate first) | — | — | ✅ |
| `IReferenceDataService`/`ReferenceDataService` methods, repository write ops, bidirectional mapper | — | ✅ | ✅ (+ delete) |
| Events (`created/updated/activated/deactivated` `.v1`, `deleted`) + orchestrator cache invalidation | — | ✅ | ✅ |

- Service and service interface are generated **only when at least one entity is mutable**.
- A mutable entity **requires `repository: EntityFramework` or `Cosmos`** — codegen throws otherwise (`None` is read-only only). `Cosmos` uses `CosmosDbReferenceData` (duplicate codes rely on a `/typeDiscriminator` + `/code` unique key on the container, which must also be registered with `CosmosDbContainerOptions.WithReferenceDataOutboxEvent()` so the co-located outbox events — which carry neither path — do not collide with each other) and the generated service re-gets the item after the transaction so the returned `ETag` is final.
- `code` is immutable after create; create always yields an inactive item; activate/deactivate are no-ops (no event) if already in that state.
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
