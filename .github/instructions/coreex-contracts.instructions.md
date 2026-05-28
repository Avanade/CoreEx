---
applyTo: "**/Contracts/**/*.cs"
description: "Contract (DTO) conventions: source generation, marker attributes, reference data, ETag, and ChangeLog support"
tags: ["contracts", "dto", "source-generation", "reference-data", "etag"]
---

# Contract (DTO) Conventions

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

## Unified API and Messaging Surface

The same contract type is used for both the HTTP API response **and** the event message payload. A `Product` returned from `GET /api/products/{id}` is the same `Contracts.Product` type published as a `product.created` event body. Do not split a resource into separate API and event DTOs.

When a domain **consumes** events from another domain, declare a local internal representation (e.g., `Application\Adapters\Products\Product`) rather than taking a dependency on the publishing domain's Contracts assembly. Keep the shape consistent with the published contract, but own it locally to preserve the anti-corruption boundary.

## Source Generation

Mark entity contract classes with the `[Contract]` attribute and declare them `partial`. The `CoreEx` package ships with a bundled Roslyn source generator ([`CoreEx.Generator`](https://github.com/Avanade/CoreEx/tree/main/gen/CoreEx.Generator)) that activates automatically — no extra package reference is needed. It emits serialization, equality, and change-tracking code into a paired `*.g.cs` file at compile time. Never manually implement those generated members.

```csharp
[Contract]
public partial class Product : ProductBase, IETag, IChangeLog { }
```

Plain value-object or request contracts that do not need generated members (equality, cloning, etc.) can be declared as ordinary, non-`partial` classes without `[Contract]`:

```csharp
// No [Contract] needed — no generated members required.
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
    public string? ETag { get; set; }

    [ReadOnly(true)]
    public ChangeLog? ChangeLog { get; set; }
}
```

## ReadOnly Properties

Decorate server-assigned properties with `[ReadOnly(true)]` to signal that clients cannot supply them. Common examples: `Id`, `ETag`, `ChangeLog`, `CategoryCode` (derived from SubCategory). NSwag/OpenAPI automatically excludes these from inbound request schemas.

## Reference Data Properties

Use `[ReferenceData<TRefData>]` on code properties that back a reference data relationship. Three conditions must all be met for the source generator to emit the navigation accessor:

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

## Localization Labels

Decorate properties with `[Localization("Human label")]` when the default property name would produce a poor validation error message:

```csharp
[Localization("Sub-category")]
public partial string? SubCategoryCode { get; set; }
// Validation error: "Sub-category is required." (not "SubCategoryCode is required.")
```

## Inheritance for Shared Fields

Extract shared fields into an abstract `XxxBase` class when multiple contracts share the same core properties. This keeps validation and mapping code DRY.

A projection subclass that adds no source-generated behavior (no `IETag`, `IChangeLog`, etc.) does **not** need `[Contract]` or `partial`:

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

// Projection — plain class, no generated members needed.
public class ProductLite : ProductBase
{
    public decimal QtyOnHand { get; set; }
}
```

## Typed Collection Classes

Pair entity contracts with a typed collection class when the contract represents a resource commonly returned as a list:

```csharp
public partial class MovementCollection : List<Movement> { }
```

## Reference Data Contracts

Reference data types inherit from `ReferenceData<TSelf>` and use `[ReferenceData]` attribute. Pair each type with a typed collection class:

```csharp
[ReferenceData]
public partial class Category : ReferenceData<Category> { }

public class CategoryCollection() : ReferenceDataCollection<Category>(ReferenceDataSortOrder.Code) { }
```

Reference data contract `*.g.cs` files are generated by the domain's `*.CodeGen` project from `ref-data.yaml`. A hand-authored partial class in the same namespace can extend the generated type with additional computed members or constants:

```csharp
// MovementKind.g.cs — generated, do not edit.
// MovementKind.cs — hand-authored extension.
public partial class MovementKind
{
    public const string Adjust = "A";
    public const string Issue  = "I";
    public const string Receive = "R";
}
```

For reference data that carries additional stored fields (e.g., `UnitOfMeasure.Scale`), add those as plain properties on the hand-authored partial and mark computed ones with `[JsonIgnore]`:

```csharp
public partial class UnitOfMeasure
{
    [JsonIgnore]
    public int Precision => 16 - Scale; // Scale is a generated stored field
}
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
| `*.g.cs` (ref-data types) | `*.CodeGen` project (`ref-data.yaml` + Handlebars templates) | `ref-data.yaml` or the templates |
| `*.g.cs` (contract members) | Roslyn source generator (`CoreEx.Generator`) | The `[Contract]`-decorated partial class |

## Do Not

- Do not reference another domain's Contracts assembly to consume its events — declare a local adapter model instead.
- Do not add `[Contract]` or `partial` to plain value-object or request classes that need no generated members.
- Do not implement members that the Roslyn source generator emits (equality, cloning, serialization helpers).
- Do not place domain rules, validators, or service calls in contract classes.
- Do not edit `*.g.cs` files directly — regenerate via the appropriate tooling.

## Further Reading

- [Contracts Layer Guide](https://github.com/Avanade/CoreEx/blob/main/samples/docs/contracts-layer.md) — unified API/event surface, source generation, reference data, and internal adapter models.
- [CoreEx.Generator](https://github.com/Avanade/CoreEx/tree/main/gen/CoreEx.Generator) — Roslyn source generator that processes `[Contract]` and `[ReferenceData]` annotations.
- [CoreEx.RefData README](https://github.com/Avanade/CoreEx/blob/main/src/CoreEx.RefData/README.md) — reference data types, collections, and sort order.
- [Tooling Guide](https://github.com/Avanade/CoreEx/blob/main/samples/docs/tooling.md) — `*.CodeGen` project usage and `ref-data.yaml` configuration.
