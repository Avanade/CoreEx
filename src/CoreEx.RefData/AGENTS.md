# CoreEx.RefData — AI Usage Guide

Provides the reference data (lookup table) framework: typed base classes, thread-safe collections, a hybrid-cache-backed orchestrator, and code-serialization support.

## Preferred Approach — Code Generation

The canonical way to introduce reference data is through the **`*.CodeGen` project** (`ref-data.yaml` + `CoreEx.CodeGen`). This is the deterministic, preferred pattern used across all sample domains. Code generation produces:

| Generated output | Description |
|---|---|
| `*.g.cs` — `ReferenceData<TSelf>` partial class | The typed reference data contract |
| `*.g.cs` — `ReferenceDataCollection<TSelf>` class | Thread-safe, cache-friendly collection |
| `*.g.cs` — `IReferenceDataRepository` | Repository interface for loading from the database |
| `*.g.cs` — `ReferenceDataRepository` | EF Core implementation of the repository |
| `*.g.cs` — `IReferenceDataProvider` / `ReferenceDataService` | Orchestrator provider wiring all types together |
| `*.g.cs` — `ReferenceDataController` | API controller exposing all reference data types |

### `ref-data.yaml` — the single source of truth

Declare every reference data type in `ref-data.yaml` inside the `*.CodeGen` project:

```yaml
collectionSortOrder: Code
repository: EntityFramework
entities:
- name: MovementStatus
- name: UnitOfMeasure
  plural: UnitsOfMeasure
  properties:
  - name: Scale
    type: int
```

Run the `*.CodeGen` project to regenerate all `*.g.cs` outputs. Never edit generated files directly.

### Hand-authored partial — `const string` code values

After generation, add a hand-authored `partial class` alongside the generated one to declare the known code values as `const string` fields:

```csharp
// MovementStatus.cs — hand-authored alongside MovementStatus.g.cs
public partial class MovementStatus
{
    public const string Pending   = "P";
    public const string Confirmed = "C";
    public const string Canceled  = "X";
}
```

Use these constants directly in business logic and validators — no runtime lookup required:

```csharp
if (movement.Status == MovementStatus.Pending) { ... }
```

### Registration

Register the generated `IReferenceDataProvider` with the orchestrator, then dynamically register all generated services and repositories:

```csharp
// Program.cs
builder.Services
    .AddReferenceDataOrchestrator<ReferenceDataService>()   // generated IReferenceDataProvider
    ...

// Dynamic registration discovers ReferenceDataService and ReferenceDataRepository via [ScopedService]
builder.Services.AddDynamicServicesUsing<ReferenceDataService, ReferenceDataRepository>();
```

The orchestrator resolves `IHybridCache` from DI to cache loaded collections. Register FusionCache separately — see [CoreEx.Caching.FusionCache](../CoreEx.Caching.FusionCache/README.md).

---

## ReferenceDataCodeCollection — Wire Serialization

For properties that serialise as a list of string codes on the wire but expose typed reference data objects in code:

```csharp
public class Order : IIdentifier<Guid>
{
    // Serialized as ["E","A"] on the wire; exposes ICollection<BasketStatus> in code
    public ReferenceDataCodeCollection<BasketStatus> AllowedStatuses { get; set; } = [];
}
```

## Date Validity

Reference data items with `StartsOn`/`EndsOn` control `IsValid` at runtime. The validation date defaults to `Runtime.UtcNow` but can be overridden per-request by injecting `IReferenceDataContext` (registered as a scoped service) and setting its `Date` property.

```csharp
// Override the validation date for the current request scope
public class MyService(IReferenceDataContext refDataContext)
{
    public void SetValidationDate(DateTimeOffset date)
        => refDataContext.Date = date;

    // Override for a specific type only
    public void SetValidationDateForType(DateTimeOffset date)
        => refDataContext[typeof(DiscountCoupon)] = date;
}
```

## Do Not

- Do not hand-write the `ReferenceData<TSelf>` class, collection, repository, service, or controller — use `ref-data.yaml` and the `*.CodeGen` project to generate them.
- Do not edit `*.g.cs` files — they are overwritten on every generation run.
- Do not load reference data collections on every request — the orchestrator caches via `IHybridCache`; load functions are called only on a cache miss.
- Do not access reference data in `static` constructors — the orchestrator must be resolved from DI at runtime.
- Do not use `ReferenceDataCodeCollection<T>` for single-value fields — use a plain `Code` string property on the contract instead.

## Further Reading

- [README](./README.md) — full `ReferenceData`, `ReferenceDataCollection`, `ReferenceDataHybridCache`, and `ReferenceDataOrchestrator` API reference.
- [CoreEx.Caching.FusionCache](../CoreEx.Caching.FusionCache/README.md) — recommended `IHybridCache` implementation for `ReferenceDataHybridCache`.
- [CoreEx](../CoreEx/README.md) — defines `IReferenceData`, `IReferenceDataCollection`, and `ReferenceDataOrchestrator`.
- [Contracts layer](../../samples/docs/contracts-layer.md) — how reference-data contracts are declared with `[ReferenceData]` and consumed via code properties in entity contracts.
- [Infrastructure layer](../../samples/docs/infrastructure-layer.md) — reference-data repository implementation and cache registration in real sample code.
- [Tooling](../../samples/docs/tooling.md) — how `ref-data.yaml` and `*.CodeGen` drive generation of the full reference-data controller/service/repository layer.
