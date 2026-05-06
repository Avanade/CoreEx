---
applyTo: "**/Contracts/**/*.cs"
description: "Contract (DTO) conventions: source generation, marker attributes, reference data, ETag, and ChangeLog support"
tags: ["contracts", "dto", "source-generation", "reference-data", "etag"]
---

# Contract (DTO) Conventions

## NuGet / Project References

| Package | Key types provided |
|---|---|
| `CoreEx` | `[Contract]`, `IIdentifier<T>`, `ICompositeKey`, `IETag`, `IChangeLog`, `ChangeLog`, `[ReadOnly]`, `[Localization]` |
| `CoreEx.RefData` | `ReferenceData<T>`, `ReferenceDataCollection<T>`, `[ReferenceData<T>]`, `[ReferenceData]`, `ReferenceDataSortOrder` |
| `CoreEx.Gen` | Roslyn source generator — add as `OutputItemType="Analyzer" ReferenceOutputAssembly="false"` |

```xml
<ItemGroup>
  <ProjectReference Include="CoreEx.Gen.csproj" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
  <ProjectReference Include="CoreEx.csproj" />
  <ProjectReference Include="CoreEx.RefData.csproj" />
</ItemGroup>
```

## Source Generation

Mark contract classes with the `[Contract]` attribute and declare them `partial`. Roslyn source generation fills in serialization, equality, and change-tracking code. Never manually implement the generated members.

```csharp
[Contract]
public partial class Product : ProductBase, IETag, IChangeLog { }
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
public partial class Product : ProductBase, IIdentifier<string?>, IETag, IChangeLog
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

Decorate server-assigned properties with `[ReadOnly(true)]` to signal that clients cannot supply them. Common examples: `Id`, `ETag`, `ChangeLog`, `CategoryCode` (derived from SubCategory).

## Reference Data Properties

Use `[ReferenceData<TRefData>]` on code properties that back a reference data relationship. Declare the property `partial` so source generation can emit the navigation accessor:

```csharp
[ReferenceData<SubCategory>]
[Localization("Sub-category")]
public partial string? SubCategoryCode { get; set; }

[ReferenceData<UnitOfMeasure>]
[Localization("Unit-of-measure")]
public partial string? UnitOfMeasureCode { get; set; }
```

The generated code exposes a strongly-typed `SubCategory` property alongside the raw code.

## Localization Labels

Decorate properties with `[Localization("Human label")]` when the default property name would produce a poor validation error message:

```csharp
[Localization("Sub-category")]
public partial string? SubCategoryCode { get; set; }
// Validation error: "Sub-category is required." (not "SubCategoryCode is required.")
```

## Inheritance for Shared Fields

Extract shared fields into an abstract `XxxBase` class when multiple contracts share the same core properties. This keeps validation and mapping code DRY:

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

[Contract]
public partial class ProductLite : ProductBase { /* subset for list queries */ }
```

## Reference Data Contracts

Reference data types inherit from `ReferenceData<TSelf>` and use `[ReferenceData]` attribute. Pair each type with a typed collection class:

```csharp
[ReferenceData]
public partial class Category : ReferenceData<Category> { }

public class CategoryCollection() : ReferenceDataCollection<Category>(ReferenceDataSortOrder.Code) { }
```

For reference data that carries additional fields (e.g., `UnitOfMeasure.Scale`), add those as plain properties and mark computed ones with `[JsonIgnore]`:

```csharp
[ReferenceData]
public partial class UnitOfMeasure : ReferenceData<UnitOfMeasure>
{
    public int Scale { get; init; }

    [JsonIgnore]
    public int Precision => 16 - Scale;
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

Contracts are data transfer objects. Keep them free of domain rules, validation logic, and service calls. Computed helpers (like the `IsQuantityValidForKind` example above) are acceptable read-only shorthands but must not mutate state.
