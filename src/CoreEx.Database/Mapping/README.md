# CoreEx.Database.Mapping

> Provides `IDatabaseMapper<T>` / `IDatabaseMapper<TSource, TDestination>` mapper interfaces, the `DatabaseMapper<T>` abstract base, and the `DatabaseMapper` static utility for reading and writing convention-based columns (`RowVersion`, `TenantId`, `IsDeleted`, change-log).

## Overview

`CoreEx.Database.Mapping` defines the explicit mapping contracts between CoreEx entity types and ADO.NET `DatabaseRecord` / `DatabaseParameterCollection`. Rather than using reflection or convention scanning, each entity type has a dedicated mapper class that overrides `OnMapFromDb` and `OnMapToDb`, calling `record.GetValue<T>("ColumnName")` and `parameters.AddParameter("ColumnName", value)` for each property.

The `DatabaseMapper` static utility provides `MapStandardFromDb` and `MapStandardToDb` extension methods that handle the cross-cutting columns — `RowVersion` → `IETag`, `TenantId`, `IsDeleted`, and the four change-log columns — so individual mapper implementations only write their entity-specific columns. These are called once per mapper as part of the `FromDb`/`ToDb` chain.

## Key types

| Type | Description |
|------|--------------|
| **[`IDatabaseMapper<T>`](./IDatabaseMapperT.cs)** | Bidirectional mapper: `MapFromDb(DatabaseRecord, OperationType) → T` and `MapToDb(T, DatabaseParameterCollection, OperationType)`. |
| **[`IDatabaseMapper<TSource, TDestination>`](./IDatabaseMapper.cs)** | Two-type variant: maps `TSource` → `TDestination` (e.g. entity → database model) and `TDestination` → `TSource`. |
| **[`DatabaseMapper<T>`](./DatabaseMapperT.cs)** | Abstract base for `IDatabaseMapper<T>`; implements both direction methods by delegating to `OnMapFromDb` and `OnMapToDb` virtuals; calls `MapStandardFromDb` / `MapStandardToDb` for convention columns. |
| **[`DatabaseMapper`](./DatabaseMapper.cs)** | Static utility: `MapStandardFromDb(item, record, operationType)` reads `RowVersion`/`TenantId`/`IsDeleted`/change-log columns; `MapStandardToDb(item, parameters, operationType)` writes them. |

## Related Namespaces

- **[`CoreEx.Database`](../README.md)** - `DatabaseRecord` and `DatabaseParameterCollection` are the ADO.NET wrappers consumed by mappers.
- **[`CoreEx.Mapping`](../../CoreEx/Mapping/README.md)** - `Mapper.MapStandardFrom` is the in-memory equivalent of `DatabaseMapper.MapStandardFromDb` for entity-to-entity mapping.
- **[`CoreEx.Entities`](../../CoreEx/Entities/README.md)** - `IETag`, `ITenantId`, `ILogicallyDeleted`, `IChangeLog`, and `IChangeLogEx` are the entity contracts whose properties are mapped by `MapStandardFromDb`/`MapStandardToDb`.