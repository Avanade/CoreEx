# coreex-contract: Workflow

Full workflow for creating or modifying a hand-authored contract in `*.Contracts`. Follow the path that matches the request; do not mix paths.

---

## Phase 1 — Clarify Before Writing

Answer these questions before emitting any code. Batch all questions — do not interrupt per-property.

| Question | Default | Notes |
|---|---|---|
| Root or subordinate? | Ask if not explicit | Root = own identity + own endpoint; subordinate = accessed via parent |
| Identifier type? | `string?` | Confirm before using any other type — never silently choose `Guid` |
| Needs `IETag`? | Yes (root default) | Omit only on explicit request — default for root contracts |
| Needs `IChangeLog`? | Ask (root only) | Add when created/updated audit trail is required |
| Sub-folder? | Flat root | Only create a sub-folder if user explicitly asks |
| Custom `[Schema]` for events? | None | Only apply on explicit request; default is version `1.0`, name = `Type.Name` |

---

## Path A — New Root Contract

A root contract owns its own identity and is accessed via its own API endpoint (e.g. `Product`, `Order`, `Employee`).

### A1 — Determine interfaces

| Interface | When |
|---|---|
| `IIdentifier<T>` | Always on root. Default `T` = `string?` — confirm before changing |
| `IETag` | Always on root (optimistic concurrency) — omit only on explicit request |
| `IChangeLog` | When created/updated audit trail needed — ask |

### A2 — Property type resolution (5-step, process all before asking anything)

1. **Honour explicit types** — if the user specifies a CLR type, use it as-is; no question.

2. **Infer obvious primitives by name pattern (silent):**

   | Name pattern | Inferred type |
   |---|---|
   | `First*`, `Last*`, `*Name`, `*Description`, `*Text`, `*Notes`, `*Comment`, `Sku`, `Email`, `Phone`, `Url` | `string?` |
   | `Is*`, `Has*`, `Can*`, `Allow*` | `bool` |
   | `*Price`, `*Amount`, `*Cost`, `*Rate`, `*Total`, `*Balance`, `*Percentage` | `decimal` |
   | `*Date`, `*On`, `*At`, `Created*`, `Updated*`, `Deleted*` | `DateTime?` |
   | `*Quantity`, `*Qty` | `decimal` |
   | `*Count`, `*Number`, `*Sequence` | `int` |

3. **Check `ref-data.yaml` for unresolved noun properties** — search `entities:` in `*.CodeGen/ref-data.yaml`:
   - **Found** → wire silently as `[ReferenceData<T>]` `public partial string? {Name}Code { get; set; }`. No question.
   - **Not found** → add to the candidate list for step 4.

4. **Batch ask once for all remaining candidates:**
   > "The following properties could be reference data types — which should be added to `ref-data.yaml`? (select any, or none to treat as plain): `Status`, `Priority`"
   Never ask per-property. Never interrupt before all properties are analysed.

5. **Emit the complete contract in one pass** after all types are resolved (and any `ref-data.yaml` entries added + CodeGen run if needed).

### A3 — Base class consideration

If the new contract shares 3+ properties with an existing contract, propose extracting a `XxxBase` abstract partial class. Wait for user confirmation before refactoring.

### A4 — Assemble the contract

```csharp
namespace {Solution}.Contracts;

/// <summary>Represents the <c>{Name}</c> contract.</summary>
[Contract]
public partial class {Name} : {Base?} IIdentifier<string?>, IETag, IChangeLog
{
    /// <inheritdoc/>
    [ReadOnly(true)]
    public string? Id { get; set; }

    /// <summary>Gets or sets the ...</summary>
    public string? SomeField { get; set; }

    /// <summary>Gets or sets the SKU (upper-cased on set).</summary>
    public string? Sku { get => field; set => field = value?.ToUpper(); }

    /// <summary>Gets or sets the status (reference data).</summary>
    [ReferenceData<Status>]
    [Localization("Status")]     // only if auto-derived label is wrong
    public partial string? StatusCode { get; set; }

    /// <summary>Gets or sets a value indicating whether the record is inactive.</summary>
    [ReadOnly(true)]
    public bool IsInactive { get; set; }

    /// <inheritdoc/>
    [ReadOnly(true)]
    public string? ETag { get; set; }

    /// <inheritdoc/>
    [ReadOnly(true)]
    public ChangeLog? ChangeLog { get; set; }
}
```

Key assembly rules:
- `[Contract]` + `partial` on the class — always.
- `[ReadOnly(true)]` on `Id`, `ETag`, `ChangeLog`, and any server-assigned/derived field.
- Only `[ReferenceData<T>]` properties are `partial` — all others are plain auto-properties.
- `[Localization("label")]` — only when the auto-derived sentence-case label would be wrong/undesired. Never add when value equals the default.
- Casing transforms belong in the setter: `set => field = value?.ToUpper()`.
- `[JsonIgnore]` on computed helpers that must not appear in the API or event payload.
- Property order convention: Id → properties (domain fields and ref-data code properties in order specified) → ETag → ChangeLog.
- Optional `[Schema]` for custom event schema metadata — only when user explicitly requests:
  - `[Schema("2.1")]` — override version only (default is `1.0`)
  - `[Schema("2.1", Name = "OtherName")]` — override version and name (default is `Type.Name`)
  - `SchemaUri` — set when a specific schema URI is required: `[Schema("2.1") { SchemaUri = "..." }]`

### A5 — Inheritance base class

When the root contract extends a base class:

```csharp
/// <summary>Provides the base <c>Product</c> contract properties.</summary>
[Contract]
public abstract partial class ProductBase : IIdentifier<string?>
{
    [ReadOnly(true)]
    public string? Id { get; set; }

    public string? Sku { get => field; set => field = value?.ToUpper(); }
    public string? Text { get; set; }
    public decimal Price { get; set; }

    [ReferenceData<Category>]
    public partial string? CategoryCode { get; set; }
}

/// <summary>Represents the <c>Product</c> contract.</summary>
[Contract]
public partial class Product : ProductBase, IETag, IChangeLog
{
    /// <inheritdoc/>
    [ReadOnly(true)]
    public ChangeLog? ChangeLog { get; set; }

    /// <inheritdoc/>
    [ReadOnly(true)]
    public string? ETag { get; set; }
}

/// <summary>Represents a lightweight <c>Product</c> projection.</summary>
[Contract]
public partial class ProductLite : ProductBase
{
    public decimal QtyOnHand { get; set; }
}
```

---

## Path B — New Subordinate or Request Contract

A subordinate is accessed only through a parent (e.g. `BasketItem`, `OrderLine`, `Address`). A request/response object has no identity (e.g. `BasketItemAddRequest`, `ProductReserve`).

```csharp
// Subordinate with identity (nested resource with its own ETag)
/// <summary>Represents the <c>BasketItem</c> contract.</summary>
[Contract]
public partial class BasketItem : IIdentifier<string?>, IETag
{
    /// <inheritdoc/>
    [ReadOnly(true)]
    public string? Id { get; set; }

    /// <summary>Gets or sets the product identifier.</summary>
    [ReadOnly(true)]
    public string? ProductId { get; set; }

    public decimal? Quantity { get; set; }

    [ReferenceData<UnitOfMeasure>]
    [ReadOnly(true)]
    public partial string? UnitOfMeasureCode { get; set; }

    /// <inheritdoc/>
    [ReadOnly(true)]
    public string? ETag { get; set; }
}

// Plain request (no identity needed)
/// <summary>Represents a request to add a product to the basket.</summary>
[Contract]
public partial class BasketItemAddRequest
{
    public string? ProductId { get; set; }
    public decimal Quantity { get; set; }
}
```

Rules:
- `[Contract]` + `partial` by default — even for plain request types (provides deep copy, cloning, reflection-free validation).
- Only omit `[Contract]` when user explicitly asks for a plain class with no generated members.
- Subordinates with their own row-level concurrency can carry `IETag`.
- No `IChangeLog` on subordinates unless the subordinate has its own audit trail (rare — ask).

---

## Path C — Modify Existing Contract

### C1 — Add a plain property

Apply the 5-step type resolution. Emit the property with `<summary>`, correct type, and `[ReadOnly(true)]` if server-assigned.

### C2 — Add a ref-data property

1. Confirm the ref-data type exists in `*.CodeGen/ref-data.yaml`. If not, offer to add it via `coreex-refdata`.
2. Ensure the class is `[Contract]` and `partial`.
3. Add: `[ReferenceData<T>]` `public partial string? {Name}Code { get; set; }`
4. Add `[Localization]` only if the auto-label would be wrong.
5. Rebuild — Roslyn emits the typed navigation property automatically.

### C3 — Add an interface

Add to the class declaration and add the corresponding property with `[ReadOnly(true)]` if required:
- `IETag` → add `[ReadOnly(true)] public string? ETag { get; set; }`
- `IChangeLog` → add `[ReadOnly(true)] public ChangeLog? ChangeLog { get; set; }`
- `IIdentifier<T>` → add `[ReadOnly(true)] public T? Id { get; set; }`

### C4 — Remove or rename a property

Check usages across validators, mappers, services, tests before removing. Note that removing a property from a contract that is also used as an event payload is a breaking schema change — flag this to the user.

---

## Path D — Base Class Extraction

When ≥2 contracts share repeated fields, propose extracting a base class.

1. Identify shared fields (e.g. both `Product` and `ProductLite` share `Id`, `Sku`, `Text`, `Price`).
2. Create `{Prefix}Base` as `[Contract]` abstract partial:
   - Shared identity (`IIdentifier<T>`) goes on the base.
   - `IETag` and `IChangeLog` stay on the concrete subclass (only the full entity needs them).
3. Refactor existing contracts to inherit.
4. Rebuild and confirm no regressions.

---

## Phase 2 — Validate

1. `dotnet build` — no errors or warnings.
2. Confirm the Roslyn-generated `.g.cs` was updated (check `obj/` or re-inspect the class in IDE — generated members appear after build).
3. If `[ReferenceData<T>]` properties were added: confirm the typed navigation property (e.g. `Status`) resolves after build.
4. If a property was removed: check downstream validators, mappers, and tests still compile.

---

## Guardrails

- **`[Contract]` + `partial` on all contract classes by default.** Only omit when user explicitly asks for a plain, generation-free class.
- **Never mark plain properties `partial`.** Only `[ReferenceData<T>]`-decorated code properties are `partial`. Making a plain property `partial` fails with CS9248 ("partial property has no implementation part").
- **Never hand-author generated members.** The Roslyn source generator emits serialization, equality, cloning, and change-tracking members into `*.g.cs` at build time. They are absent before a build — that is expected, not an error.
- **Never create or edit `*.g.cs` files directly** — these are owned by the Roslyn generator (contract members) or `*.CodeGen` (ref-data contracts). If a member appears missing, rebuild first.
- **Default identifier = `string?`.** Do not silently change it. `Guid` is not the default.
- **`[Localization]` is noise when redundant.** PascalCase names are auto-split to sentence case ("SubCategoryCode" → "Sub category code"). Only add `[Localization]` to change the label. Never add when value equals the default.
- **Same contract for API and events.** Do not create separate API and messaging DTOs for the same resource.
- **Anti-corruption boundary.** When consuming events from another domain, declare a local adapter model in `Application\Adapters\{Domain}\`. Never take a dependency on the publishing domain's `*.Contracts` assembly.
- **Flat root is the default.** Place contracts in the project root by default. Sub-folders are allowed when the user requests them.
- **Removing a published contract property is a breaking change.** Flag it before proceeding.
