# CoreEx.Database.Extended

> Provides `DatabaseColumns` convention column names, `DatabaseWildcard` LIKE-pattern translation, and the `IMultiSetArgs` / `MultiSetSingleArgs<T>` / `MultiSetCollArgs<T>` descriptors for reading multiple result sets in a single round-trip.

## Overview

`CoreEx.Database.Extended` contains the opt-in capabilities that most database operations rely on without being core ADO.NET primitives.

`DatabaseColumns` centralizes the convention-based column names (`RowVersion`, `TenantId`, `IsDeleted`, `CreatedBy`, `CreatedDate`, `UpdatedBy`, `UpdatedDate`) as overridable properties. Mappers and the `DatabaseMapper` static utility reference these names so a single configuration change renames a column application-wide.

`DatabaseWildcard` takes a CoreEx `WildcardResult` and produces the database `LIKE` pattern string (`%`, `_`, escaped), abstracting the difference between `StartsWith`, `EndsWith`, `Contains`, and exact-match patterns so query builders don't scatter `% + value + %` strings.

The multi-result-set types allow a single stored procedure or multi-statement SQL call to return several result sets; `DatabaseCommand.SelectMultiSetAsync` reads each set in order, invoking the appropriate `IMultiSetArgs` descriptor to map rows and collect results.

## Key types

| Type | Description |
|------|--------------|
| **[`DatabaseColumns`](./DatabaseColumns.cs)** | Convention column name properties (`RowVersionName`, `TenantIdName`, `IsDeletedName`, change-log column names); instances on `IDatabase.NamedColumns` are overridable per database. |
| **[`DatabaseWildcard`](./DatabaseWildcard.cs)** | Translates `WildcardResult` to a database `LIKE` pattern with configurable multi-char (`%`), single-char (`_`), and escape character; exposes `Replace(string)` for raw wildcard text. |
| **[`MultiSetSingleArgs<T>`](./MultiSetSingleArgsT.cs)** | `IMultiSetArgs` for a single-row result set: invokes `IDatabaseMapper<T>.MapFromDb` once and stores the mapped value; supports `IsMandatory` / `StopOnNull`. |
| **[`MultiSetCollArgs<T>`](./MultiSetCollArgsT.cs)** | `IMultiSetArgs` for a collection result set: invokes mapper for each row, accumulates into a list; supports `MinimumRows`, `MaximumRows`, `StopOnNull`. |
| [`IMultiSetArgs`](./IMultiSetArgs.cs) | Base interface for multi-result-set descriptors: `MinimumRows`, `MaximumRows`, `StopOnNull`, `DatasetRecord(DatabaseRecord)`. |
| [`IMultiSetArgs<T>`](./IMultiSetArgsT.cs) | Generic variant adding a `Mapper` property and `GetResult()` for retrieving the mapped output after dataset processing. |

## Related Namespaces

- **[`CoreEx.Database`](../README.md)** - `DatabaseCommand.SelectMultiSetAsync` consumes `IMultiSetArgs` descriptors; `IDatabase.NamedColumns` returns a `DatabaseColumns` instance; `IDatabase.Wildcard` is a `DatabaseWildcard`.
- **[`CoreEx.Wildcards`](../../CoreEx/Wildcards/README.md)** - `WildcardResult` is the input type consumed by `DatabaseWildcard.Replace`.