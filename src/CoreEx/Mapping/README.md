# CoreEx.Mapping

> Provides the `IMapper<TSource, TDestination>` family of interfaces and `Mapper` utility for explicit, type-safe property mapping between domain model, DTO, and database entity types — without AutoMapper or reflection-based conventions.

## Overview

`CoreEx.Mapping` defines a set of small, explicit mapper interfaces that infrastructure packages (`CoreEx.Database`, `CoreEx.EntityFrameworkCore`) and application code use to convert between types. Rather than convention-based mapping, CoreEx favours explicit implementation: a mapper class implements `IMapper<TSource, TDestination>` and writes the property assignments directly, keeping the mapping logic visible, testable, and refactor-safe.

Bi-directional mappers (`IBiDirectionMapper<T1, T2>`) combine `T1→T2` and `T2→T1` in a single class, and the two- and three-type variants allow reuse across related mapping paths. `MapperExtensions` adds `MapInto<TDestination>` extension methods on any source type to enable fluent calling syntax without explicit mapper references.

The static `Mapper` utility handles the cross-cutting "standard property" mapping across CoreEx entity contracts (`IIdentifier`, `IETag`, `ITenantId`, `IPartitionKey`, `ILogicallyDeleted`, `ITypeDiscriminator`, `IChangeLog`), so explicit mapper implementations call one line (`destination.MapStandardFrom(source)`) rather than hand-coding the same guard-then-assign pattern for every entity property.

## Key capabilities

- 🔁 **Explicit mapping contracts**: `IMapper<TSource, TDestination>` and `IIntoMapper<TSource, TDestination>` provide typed, single-responsibility mapper interfaces with no reflection overhead.
- ↔️ **Bi-directional mappers**: `IBiDirectionMapper<T1, T2>` combines both directions in one class, reducing class proliferation for symmetric domain↔DTO mappings.
- 🔗 **Tri-type mappers**: `IMapper<T1, T2, T3>` and `IBiDirectionMapper<T1, T2, T3>` support three-way mapping (e.g. entity → create DTO + update DTO) in a single implementation.
- 🏗️ **Standard property mapping**: `Mapper.MapStandardFrom<TSource, TDestination>()` automatically copies `IIdentifier`, `IETag`, `ITenantId`, `IPartitionKey`, `ILogicallyDeleted`, `ITypeDiscriminator`, and `IChangeLog`/`IChangeLogEx` properties between source and destination where both implement the matching interfaces.
- 🧩 **Fluent extension methods**: `MapperExtensions.MapInto<TDestination>(this TSource, IMapper<TSource, TDestination>)` enables `source.MapInto(mapper)` syntax for clean, readable mapping call sites.

## Key types

| Type | Description |
|------|-------------|
| **[`Mapper`](./Mapper.cs)** | Static utility: `MapStandardFrom` copies standard CoreEx entity contract properties (identifier, ETag, tenant, partition key, change log, etc.) between source and destination. |
| [`IMapper<TSource, TDestination>`](./IMapperT.cs) | Core mapping interface: `Map(TSource source) → TDestination`. |
| [`IIntoMapper<TSource, TDestination>`](./IIntoMapperT.cs) | Variant of `IMapper` that maps into an existing destination: `MapInto(TSource source, TDestination destination)`. |
| [`IBiDirectionMapper<T1, T2>`](./IBiDirectionMapper.cs) | Combines `IMapper<T1,T2>` and `IMapper<T2,T1>` in a single interface for symmetric mappings. |
| [`MapperExtensions`](./MapperExtensions.cs) | Extension methods: `MapInto<TDestination>()` and `MapStandardFrom()` for fluent mapping syntax. |
| _[`MapperBase<TSource, TDestination>`](./MapperT2.cs)_ | Abstract base providing `Map` and `MapInto` infrastructure; concrete mappers override `OnMap`. |

## Related Namespaces

- **[`CoreEx.Entities`](../Entities/README.md)** - Defines the standard entity contract interfaces (`IIdentifier<T>`, `IETag`, `IChangeLog`, etc.) that `Mapper.MapStandardFrom` automatically copies.
- **[`CoreEx.Data`](../Data/README.md)** - `ITenantId`, `IPartitionKey`, `ILogicallyDeleted`, and `ITypeDiscriminator` from `CoreEx.Data` are also handled by `Mapper.MapStandardFrom`.
- **[`CoreEx.Database`](../../CoreEx.Database/README.md)** - Database mappers implement `IMapper<TEntity, TDbModel>` and call `MapStandardFrom` for the standard fields.
- **[`CoreEx.EntityFrameworkCore`](../../CoreEx.EntityFrameworkCore/README.md)** - EF Core mappers follow the same `IMapper<TEntity, TEfModel>` pattern.