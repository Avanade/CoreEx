---
applyTo: "**/Contracts/**/*.cs"
description: "Contract (DTO) conventions: source generation, marker attributes, reference data, ETag, and ChangeLog support"
tags: ["contracts", "dto", "source-generation", "reference-data", "etag"]
---

# Contract (DTO) Conventions

## File Placement

Contracts live **flat in the root of the `*.Contracts` project** — both hand-authored (`Product.cs`) and generated (`Product.g.cs`) files. **Do not create sub-folders** such as `Entities/`, `Models/`, or `RefData/` to group them; the samples place every contract at the project root regardless of whether it is a root entity, subordinate, request/response DTO, or reference-data type. Only introduce a sub-folder if the **user explicitly asks** for one. The same flat-root convention applies to the matching files in the other layers (validators, services, repositories, mappers) unless their instructions state otherwise.

## NuGet / Project References

| Package | Key types provided |
|---|---|
| `CoreEx` | `[Contract]`, `IIdentifier<T>`, `ICompositeKey`, `IETag`, `IChangeLog`, `ChangeLog`, `[ReadOnly]`, `[Localization]`; includes the `CoreEx.Generator` Roslyn source generator — no separate package reference required |
| `CoreEx.RefData` | `ReferenceData<T>`, `ReferenceDataCollection<T>`, `[ReferenceData<T>]`, `[ReferenceData]`, `ReferenceDataSortOrder` |

```xml
<ItemGroup>
  <PackageReference Include="CoreEx" />
  <PackageReference Include="CoreEx.RefData" />
</ItemGroup>
```

## Root vs. Subordinate (contract or entity)

Note that the terms Contract and Entity are interchangeable in this context. The conventions below apply to all DTOs that represent a resource, whether they are persisted entities or plain request/response models.

If the user specifies that it is Reference Data then treat as defined by [Reference Data Contracts](#reference-data-contracts).

Before generating any contract, determine whether it is a **root** or a **subordinate** contract.  
Always ask the user if this is not explicit in their request.

| Concern | Root | Subordinate |
|---|---|---|
| `IIdentifier<string?>` | ✅ Always (unless explicitly requested otherwise). The user may also specify a different type | ❌ Omit |
| `IETag` | ✅ Always (unless explicitly requested otherwise) | ❌ Omit |
| `IChangeLog` | ✅ When audit trail is needed (ask) | ❌ Omit |
| `[ReadOnly(true)]` on `Id`, `ETag`, `ChangeLog` | ✅ Required | N/A |
| `[Contract]` + `partial` | ✅ Always | ✅ By default on all hand-authored contracts (provides deep copy, cloning, and reflection-free validation); omit only when explicitly requested |

**Root** — owns its own identity, is persisted independently, and is retrieved/mutated via its own API endpoint (e.g. `Product`, `Order`, `Person`).

**Subordinate** — a child, line-item, value object, or request/response DTO that is generally accessed through a root (e.g. `OrderLine`, `Address`, `BasketItemAddRequest`).

> **Agent instruction:** When asked to create a contract and the category is not explicit,  
> ask: *"Is `{Name}` a root contract (it has its own identity) or a subordinate contract (accessed only through a parent)?"*  
> Do not assume root. Do not generate `IIdentifier`, `IETag`, or `IChangeLog` until confirmed.

> **Identifier type — do not substitute.** The identifier type is `IIdentifier<string?>` by **default**. Use `string?` unless the user **explicitly** states a different type (e.g. "use a `Guid` id", "the key is an `int`"). **Never** silently change it — in particular, do **not** default to `Guid` because it is common elsewhere. The chosen type is authoritative and must flow through unchanged to every downstream artefact: the `ref-data.yaml` `idType`, the persistence model, and the database primary-key column type (a `string?` id → `NVARCHAR(50)`/`VARCHAR(50)`, **not** `UNIQUEIDENTIFIER`/`UUID`). If a plan or prior step states one type, the implementation must match it exactly — flag any discrepancy rather than resolving it by changing the type.

## Unified API and Messaging Surface

The same contract type is used for both the HTTP API response **and** the event message payload. A `Product` returned from `GET /api/products/{id}` is the same `Contracts.Product` type published as a `product.created` event body. Do not split a resource into separate API and event DTOs.

When a domain **consumes** events from another domain, declare a local internal representation (e.g., `Application\Adapters\Products\Product`) rather than taking a dependency on the publishing domain's Contracts assembly. Keep the shape consistent with the published contract, but own it locally to preserve the anti-corruption boundary.

## Source Generation

Mark entity contract classes with the `[Contract]` attribute and declare them `partial`. The `CoreEx` package ships with a bundled Roslyn source generator ([`CoreEx.Generator`](https://github.com/Avanade/CoreEx/tree/main/gen/CoreEx.Generator)) that activates automatically — no extra package reference is needed. It emits serialization, equality, and change-tracking code into a paired `*.g.cs` file at compile time. Never manually implement those generated members.

> Both `[Contract]` and `[ReferenceData]` trigger this Roslyn source generator. For reference data types the flow is two-stage: the `*.CodeGen` project (OnRamp/Handlebars) first generates the class decorated with `[ReferenceData]`, then the Roslyn generator processes that attribute at compile time to emit additional members -- see [Reference Data Contracts](#reference-data-contracts).

```csharp
[Contract]
public partial class Product : ProductBase, IETag, IChangeLog { }
```

### How the generator runs (do not chase phantom build problems)

The Roslyn source generator runs **automatically as part of compilation** — every `dotnet build` and every IDE design-time build. You do **not** trigger it manually, and there is no separate step to "make it generate".

Common misconceptions to avoid:
- **Missing generated members before a build is normal — not an error to fix by hand.** If a `partial` property or class has no visible implementation, it is because the project simply hasn't been built yet. Build it; do **not** hand-author the generated partial implementation or create the `.g.cs` file to "unblock" things.
- **Other compilation errors do not stop the generator.** Roslyn runs source generators on the parsed compilation regardless of unrelated errors; there is **no build-ordering requirement and no "circular dependency"** whereby errors elsewhere prevent generation. Do not invent such a dependency — fix the actual reported errors and rebuild.
- **If a member is still missing after a clean build, the contract declaration is malformed**, not the build process. Check that the class has `[Contract]` and is `partial`, the property is `partial`, and the attributes are correct (see below) — then rebuild. Never substitute a hand-written implementation for the generated one.

Apply `[Contract]` + `partial` to **all** hand-authored contract classes by default — including plain request/response DTOs. Source-generated deep copy, cloning, and reflection-free validation members are valuable even when no `[ReferenceData<T>]` properties are present. Omit `[Contract]` only when the user explicitly requests a plain, generation-free class:

```csharp
// ✅ Default — [Contract] + partial on all hand-authored contracts, including plain request types.
[Contract]
public partial class BasketItemAddRequest
{
    public string? ProductId { get; set; }
    public decimal Quantity { get; set; }
}

// Omit only when the user explicitly asks for a plain class with no generated members.
public class BasketItemAddRequest
{
    public string? ProductId { get; set; }
    public decimal Quantity { get; set; }
}
```

## Interfaces

Implement the appropriate CoreEx marker interfaces depending on the entity's behavior:

| Interface | When to use |
|---|---|
| `IIdentifier<T>` | Entity has a single primary key |
| `ICompositeKey` | Entity has a multi-part key |
| `IETag` | Entity participates in optimistic concurrency / IF-MATCH |
| `IChangeLog` | Entity records created/updated audit metadata |

All three are typically combined on mutable entities:

```csharp
[Contract]
public partial class Product : ProductBase, IETag, IChangeLog
{
    [ReadOnly(true)]
    public string? Id { get; set; }

    [ReadOnly(true)]
    public ChangeLog? ChangeLog { get; set; }

    [ReadOnly(true)]
    public string? ETag { get; set; }
}
```

## Documentation Comments

Give **every contract property a `<summary>`** (and the contract class itself). The standard `Id`/`ETag`/`ChangeLog` members — which implement `IIdentifier`/`IETag`/`IChangeLog` — may use `<inheritdoc/>` instead. See [XML Documentation Comments](#) in the conventions. (Common mistake: leaving the contract properties undocumented — they each need a summary.)

```csharp
/// <summary>Represents the <c>Employee</c> contract.</summary>
[Contract]
public partial class Employee : IIdentifier<string?>, IETag, IChangeLog
{
    /// <inheritdoc/>
    [ReadOnly(true)]
    public string? Id { get; set; }

    /// <summary>Gets or sets the employee's first name.</summary>
    public string? FirstName { get; set; }

    /// <summary>Gets or sets the employee's gender (reference data).</summary>
    [ReferenceData<Gender>]
    public partial string? GenderCode { get; set; }

    /// <inheritdoc/>
    [ReadOnly(true)]
    public ChangeLog? ChangeLog { get; set; }

    /// <inheritdoc/>
    [ReadOnly(true)]
    public string? ETag { get; set; }
}
```

## ReadOnly Properties

Decorate server-assigned properties with `[ReadOnly(true)]` to signal that clients cannot supply them. Common examples: `Id`, `ETag`, `ChangeLog`, `CategoryCode` (derived from SubCategory). NSwag/OpenAPI automatically excludes these from inbound request schemas.

## Reference Data Properties

Use `[ReferenceData<TRefData>]` on code properties that back a reference data relationship. Two conditions must both be met for the source generator to emit the navigation accessor:

1. The **class** is decorated with `[Contract]` and declared `partial`.
2. The **property** is declared `partial`.

```csharp
[Contract]
public partial class ProductBase : IIdentifier<string?>
{
    [ReferenceData<SubCategory>]
    [Localization("Sub-category")]
    public partial string? SubCategoryCode { get; set; }

    [ReferenceData<UnitOfMeasure>]
    [Localization("Unit-of-measure")]
    public partial string? UnitOfMeasureCode { get; set; }
}
```

The generated code exposes a strongly-typed `SubCategory` property alongside the raw `SubCategoryCode` string. If either `[Contract]` or `partial` is missing from the class, the navigation property will not be generated and the code will not compile correctly.

> **Only `[ReferenceData<T>]` properties are `partial` — not every property in the class.** The class is `partial` (it has a generated `.g.cs` part), but among its **properties** *only* the `[ReferenceData<T>]`-decorated code properties are declared `partial` (the generator supplies their implementation part — the typed navigation). Every other property is an **ordinary auto-property** with no `partial` keyword. Do **not** assume that because the class is `partial` its properties must be too — marking a plain property `partial` with no generated implementation fails to compile with **CS9248** *("partial property … must have an implementation part")*.
>
> ```csharp
> public partial class Employee : IIdentifier<string?>
> {
>     public string? FirstName { get; set; }              // ✅ plain — NOT partial
>     public decimal Salary { get; set; }                 // ✅ plain — NOT partial
>
>     [ReferenceData<Gender>]
>     public partial string? GenderCode { get; set; }     // ✅ partial — generator emits the `Gender` navigation
>
>     // public partial string? FirstName { get; set; }   // ❌ CS9248 — no generated implementation for a non-ref-data property
> }
> ```

> **Agent instruction — property type resolution:** When generating contract properties, apply this hierarchy for every property first, then generate the complete contract in a single pass. Do not ask about individual properties mid-list.
>
> **Step 1 — Honour explicit types**  
> If the user specifies a CLR type (e.g. `string Gender`, `int Rating`), use it as-is. No lookup. No question.
>
> **Step 2 — Infer obvious primitives by name pattern (silent — no question)**
>
> | Name pattern | Inferred type |
> |---|---|
> | `First*`, `Last*`, `*Name`, `*Description`, `*Text`, `*Notes`, `*Comment`, `Sku`, `Email`, `Phone`, `Url` | `string?` |
> | `Is*`, `Has*`, `Can*`, `Allow*` | `bool` |
> | `*Price`, `*Amount`, `*Cost`, `*Rate`, `*Total`, `*Balance`, `*Percentage` | `decimal` |
> | `*Date`, `*On`, `*At`, `Created*`, `Updated*`, `Deleted*` | `DateTime?` |
> | `*Quantity`, `*Qty` | `decimal` |
> | `*Count`, `*Number`, `*Sequence` | `int` |
>
> **Step 3 — Check `ref-data.yaml` for any remaining untyped noun properties**  
> Search `entities:` in `tools/[domain].CodeGen/ref-data.yaml` for each unresolved property name:
> - **Found** — wire up silently as `[ReferenceData<T>]` `public partial string? {Name}Code { get; set; }`. No question.
> - **Not found** — add to the candidates list for Step 4.
>
> **Step 4 — Ask once, for all remaining candidates, at the end**  
> After processing every property, if any candidates remain unresolved, ask a single question:  
> *"The following properties could be reference data types — which should I add to `ref-data.yaml`? (select any, or none to treat as plain properties): `Gender`, `Status`, `Priority`"*  
> Never ask per-property. Never interrupt before all properties have been analysed.
>
> **Step 5 — Single batch edit and one CodeGen run**  
> For all properties the user confirms as reference data:
> 1. Add **all** confirmed types to `ref-data.yaml` under `entities:` in a **single edit**.
> 2. Offer to run `dotnet run` from the `*.CodeGen` directory **once** to generate all of them in one pass.
> 3. On success, summarise the generated artefacts; on failure relay the **complete output verbatim** then fix `ref-data.yaml` and offer to re-run. Do not create `.g.cs` files manually.
>
> **Step 6 — Generate the complete contract in one pass**  
> Once all types are resolved and CodeGen has run (if needed), emit the full contract:
> - Reference data properties: `[ReferenceData<T>]` `public partial string? {Name}Code { get; set; }` — the property name is always `{Name}Code`; the navigation property `{Name}` is Roslyn-generated and must not be hand-authored.
> - Plain properties: use the inferred or explicit CLR type.
> - Apply `[Localization("Human label")]` **only** where the auto-derived label would be wrong/undesired (e.g. `SubCategoryCode` → `"Sub-category"`). Do **not** add it when the value would equal the default (e.g. `[Localization("Salary")]` on `Salary` is redundant).
> - For any property confirmed as plain (Step 4, user selected none or a subset), use `string?` as the default if no better type can be inferred.

## Localization Labels

CoreEx automatically derives a human-friendly label from the property name (the PascalCase name is split into words in sentence case — e.g. `DateOfBirth` → "Date of birth"). **Only** decorate a property with `[Localization("Human label")]` when that default would be wrong or undesired — typically to drop a `Code` suffix or hyphenate (e.g. `SubCategoryCode` → "Sub-category").

```csharp
// ✅ Needed — default "Sub category code" is undesired
[Localization("Sub-category")]
public partial string? SubCategoryCode { get; set; }
// Validation error: "Sub-category is required." (not "Sub category code is required.")

// ❌ Redundant — the default already yields "Salary"; do not annotate
[Localization("Salary")]
public decimal Salary { get; set; }
```

Omit `[Localization]` whenever the attribute value would equal the auto-derived label — it is noise. Add it only to change the label.

## Inheritance for Shared Fields

Extract shared fields into an abstract `XxxBase` class when multiple contracts share the same core properties. This keeps validation and mapping code DRY.

A projection subclass uses `[Contract]` + `partial` by default. Omit only when the user explicitly requests a plain class:

```csharp
[Contract]
public abstract partial class ProductBase : IIdentifier<string?>
{
    public string? Id { get; set; }
    public string? Sku { get; set; }
    public string? Text { get; set; }
    public decimal Price { get; set; }
}

[Contract]
public partial class Product : ProductBase, IETag, IChangeLog { /* additions only */ }

// ✅ Default — [Contract] + partial even for a lightweight projection.
[Contract]
public partial class ProductLite : ProductBase
{
    public decimal QtyOnHand { get; set; }
}
```

## Reference Data Contracts

Reference data contracts are **generated, not hand-authored**. The source of truth is the `entities:` section of `ref-data.yaml` in the domain's `*.CodeGen` project. Running the CodeGen generates all artefacts across every layer -- contract class, API endpoint, service method, repository interface, repository implementation, and mapper -- as `.g.cs` files that must never be edited directly.

> **Agent instruction:** When asked to create or modify a reference data type:
> 1. Edit `ref-data.yaml` in `tools/[domain].CodeGen/` -- add or update the entry under `entities:`.
> 2. Offer to run `dotnet run` from the CodeGen directory on the user's behalf.
> 3. If confirmed, execute it and summarise the generated artefacts on success; on failure relay the **complete output verbatim** — it provides the diagnostic needed to fix the entry.
> 4. On failure, fix the issue in `ref-data.yaml` and offer to re-run -- do not create or edit `.g.cs` files to work around a generation error.
> 5. If the user declines, remind them to run `dotnet run` from the `*.CodeGen` directory before the new types are available.
>
> If the user **explicitly requests** hand-authoring instead of CodeGen, use the pattern shown in [Hand-authored contracts (explicit request only)](#hand-authored-contracts-explicit-request-only) below.

### `ref-data.yaml` -- entity definition

The standard `IReferenceData` properties (`Id`, `Code`, `Text`, `Description`, `SortOrder`, `IsActive`, `StartsOn`, `EndsOn` etc.) are automatically included in every generated type -- do not declare them under `properties:`. Only additional domain-specific columns need to be listed, and most reference data entities require none at all.

```yaml
entities:
- name: Brand                   # minimal form -- no extra properties needed
- name: Category                # same; just name is sufficient for most entities
- name: SubCategory
  properties:
  - name: CategoryCode
    type: ^Category             # ^ prefix = ref-data navigation property (typed accessor generated)
- name: UnitOfMeasure
  plural: UnitsOfMeasure        # override irregular pluralization
  idType: Guid                  # override identifier type; defaults to string
  properties:
  - name: Scale
    type: int                   # additional stored column (not part of IReferenceData)
  - name: DiscountPercentage
    type: decimal
    excludeContract: true       # exclude from generated contract (persistence model only)
```

Key `entities:` options:

| Key | Required | Default | Purpose |
|---|---|---|---|
| `name` | Yes | -- | Entity name (PascalCase) |
| `plural` | No | Auto-pluralized | Override when pluralization is irregular |
| `idType` | No | `string` | Identifier type override (e.g. `Guid`, `int`) |
| `properties[].name` | Yes (if any) | -- | Additional stored property name |
| `properties[].type` | Yes (if any) | -- | CLR type; prefix `^` for a ref-data navigation accessor |
| `properties[].excludeContract` | No | `false` | Exclude from the generated contract (persistence only) |

### Hand-authored extensions (optional)

After CodeGen runs, an optional hand-authored `partial` class in the same namespace can add constants or computed members. Do not redeclare `[ReferenceData]`, the base class, or the collection class -- those are owned by the generator.

```csharp
// MovementKind.cs -- hand-authored extension; MovementKind.g.cs is generated, do not edit.
public partial class MovementKind
{
    public const string Adjust  = "A";
    public const string Issue   = "I";
    public const string Receive = "R";
}

// UnitOfMeasure.cs -- computed property derived from a generated stored field.
public partial class UnitOfMeasure
{
    [JsonIgnore]
    public int Precision => 16 - Scale; // Scale is a generated stored field
}
```

### Hand-authored contracts (explicit request only)

If the user explicitly requests a hand-authored reference data contract (i.e., without CodeGen), declare the type and its paired collection class directly:

```csharp
[ReferenceData]
public partial class Category : ReferenceData<Category>;

public class CategoryCollection() : ReferenceDataCollection<Category>(ReferenceDataSortOrder.Code);
```

Use `ReferenceData<TId, TSelf>` when a non-string identifier type is required:

```csharp
[ReferenceData]
public partial class Priority : ReferenceData<int, Priority>;

public class PriorityCollection() : ReferenceDataCollection<Priority>(ReferenceDataSortOrder.Code);
```

## Casing Transformations

Apply casing transforms in the property setter, not in the validator, when a field has a canonical form:

```csharp
public string? Sku { get => field; set => field = value?.ToUpper(); }
```

## JsonIgnore

Use `[JsonIgnore]` for computed or internal properties that must not appear in the API response or request body:

```csharp
[JsonIgnore]
public bool IsQuantityValidForKind => KindCode switch { ... };
```

## No Business Logic in Contracts

Contracts are data transfer objects. Keep them free of domain rules, validation logic, and service calls. Read-only computed helpers (like `IsQuantityValidForKind` above) are acceptable shorthands but must not mutate state.

## Generated Code

Never create or edit `*.g.cs` files directly.

| File pattern | Generator | Change instead |
|---|---|---|
| `*.g.cs` (ref-data types, cross-layer) | `*.CodeGen` project (`ref-data.yaml` + Handlebars/OnRamp) | Edit `ref-data.yaml` and re-run `dotnet run` |
| `*.g.cs` (contract members) | Roslyn source generator (`CoreEx.Generator`) | The `[Contract]`-decorated partial class |

## Do Not

- Do not reference another domain's Contracts assembly to consume its events — declare a local adapter model instead.
- Do not omit `[Contract]` and `partial` from hand-authored contract classes without an explicit user request — all hand-authored contracts use `[Contract]` + `partial` by default.
- Do not implement members that the Roslyn source generator emits (equality, cloning, serialization helpers).
- Do not place domain rules, validators, or service calls in contract classes.
- Do not leave contract properties without a `<summary>` — every property gets one (standard `Id`/`ETag`/`ChangeLog` may use `<inheritdoc/>`). See the *Documentation Comments* section.
- Do not add a redundant `[Localization]` attribute whose value equals the auto-derived label (e.g. `[Localization("Salary")]` on `Salary`) — only annotate to change the label.
- Do not edit `*.g.cs` files directly — regenerate via the appropriate tooling.
- Do not hand-author a generated partial implementation (or create its `.g.cs`) because it is "missing" — it appears after a build; the generator runs automatically during compilation.
- Do not invent a build-ordering or "circular dependency" excuse for missing generated code — Roslyn runs the generator even when other errors exist; fix the real errors and rebuild.
- Do not emit `#nullable enable` or `#nullable restore` pragma directives — nullable is enabled project-wide via `Directory.Build.props`.
- Do not create a sub-folder (e.g. `Entities/`, `Models/`, `RefData/`) to house contracts — place every contract flat in the `*.Contracts` root (see *File Placement*); only nest when the user explicitly asks.

## Further Reading

- [Contracts Layer Guide](https://github.com/Avanade/CoreEx/blob/main/samples/docs/contracts-layer.md) — unified API/event surface, source generation, reference data, and internal adapter models.
- [CoreEx.Generator](https://github.com/Avanade/CoreEx/tree/main/gen/CoreEx.Generator) — Roslyn source generator that processes `[Contract]` and `[ReferenceData]` annotations.
- [CoreEx.RefData README](https://github.com/Avanade/CoreEx/blob/main/src/CoreEx.RefData/README.md) — reference data types, collections, and sort order.
- [Tooling Guide](https://github.com/Avanade/CoreEx/blob/main/samples/docs/tooling.md) — `*.CodeGen` project usage and `ref-data.yaml` configuration.
