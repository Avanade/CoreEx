# CoreEx.Database

> Provides the `IDatabase` / `Database` ADO.NET abstraction, `DatabaseCommand` fluent query builder, `DatabaseRecord` row reader, multi-result-set support, convention-based column mapping, database wildcard translation, typed mapper contracts, and the transactional outbox relay infrastructure for publishing events from a relational database.

## Overview

`CoreEx.Database` wraps ADO.NET (`DbConnection`, `DbCommand`, `DbDataReader`) with a fluent, OpenTelemetry-instrumented API that integrates tightly with CoreEx entity contracts. Every operation flows through `DatabaseCommand`, which accumulates a `DatabaseParameterCollection`, opens the connection on demand, and executes via `DatabaseInvoker` to emit spans and structured log entries.

The package follows an explicit-mapping philosophy: `IDatabaseMapper<TSource, TDestination>` implementations hand-write the column-to-property assignments (calling `DatabaseRecord.GetValue<T>(columnName)` and `DatabaseParameterCollection.AddParameter`). The `DatabaseMapper` static utility handles the cross-cutting columns (`RowVersion` → `IETag`, `TenantId`, `IsDeleted`, `CreatedBy`/`CreatedOn`/`UpdatedBy`/`UpdatedOn`) so individual mappers only write their entity-specific columns.

The outbox sub-namespace implements the [Transactional Outbox pattern](https://microservices.io/patterns/data/transactional-outbox.html): events are written to an outbox table within the same database transaction as the business mutation; the relay hosted service polls the table and publishes to the configured `IEventPublisher`.

## Key capabilities

- 🔌 **IDatabase abstraction**: `IDatabase` / `Database<TConn>` manage connection lifetime, transaction enlistment, `DateTimeTransform`, `DateTimeOffsetTransform`, `DatabaseColumns` naming conventions, and `DatabaseWildcard` configuration.
- 📝 **Fluent command builder**: `DatabaseCommand` accumulates parameters via `IDatabaseParameters<T>` fluent methods, then executes as non-query, scalar, single-row, first-or-default, collection, or multi-result-set.
- 📖 **DatabaseRecord**: Wraps `DbDataReader` with named and ordinal `GetValue<T>` accessors that apply `DateTimeTransform`, null-safety, and `RowVersion` decoding.
- 🗂️ **Multi-result-set**: `SelectMultiSetAsync` reads multiple result sets in a single round-trip using `IMultiSetArgs` / `MultiSetSingleArgs<T>` / `MultiSetCollArgs<T>` descriptors.
- 🗃️ **Typed mapper contracts**: `IDatabaseMapper<TSource, TDestination>` / `IDatabaseMapper<T>` define the mapping surface; `DatabaseMapper<T>` provides an abstract base with `MapFromDb` / `MapToDb` override points.
- 🏷️ **Standard column mapping**: `DatabaseMapper.MapStandardFromDb` automatically reads `RowVersion` → `IETag`, `TenantId`, `IsDeleted`, and change-log columns from `DatabaseRecord`; `MapStandardToDb` writes them to parameters.
- 🦁 **Database wildcard**: `DatabaseWildcard` translates CoreEx `WildcardResult` selections into database `LIKE` patterns (`%`, `_`) with configurable escape character.
- 📡 **DatabaseInvoker tracing**: Every command execution is wrapped by `DatabaseInvoker` (an `InvokerBase<IDatabase>`) emitting OpenTelemetry spans tagged with command text, operation result, and error details.
- 📤 **Transactional outbox relay**: `DatabaseOutboxRelayBase<TDatabase, TSelf>` polls an outbox table, deserializes events, publishes them via `IEventPublisher`, and marks them as sent — all within a configurable polling hosted service.
- 🔢 **SQL statement abstraction**: `SqlStatement` carries `CommandText`, `CommandType` (`Text` or `StoredProcedure`), and convenience factory methods for inline SQL and stored procedure calls.

## Key types

| Type | Description |
|------|-------------|
| **[`IDatabase`](./IDatabase.cs)** | Core database interface: `GetConnectionAsync`, `CreateCommand(SqlStatement)`, `CurrentTransaction`, `BeginTransactionAsync`, `DateTimeTransform`, `NamedColumns`, `Wildcard`. |
| **[`Database`](./Database.cs)** | Abstract base `IDatabase` implementation for a specific `DbConnection` type; manages connection pooling, transaction stack, and `DatabaseInvoker` invocation. |
| **[`DatabaseCommand`](./DatabaseCommand.cs)** | Fluent command builder: `Parameters` (`DatabaseParameterCollection`), `NonQueryAsync`, `ScalarAsync`, `SelectSingleAsync`, `SelectFirstOrDefaultAsync`, `SelectQueryAsync`, `SelectMultiSetAsync`. |
| **[`DatabaseRecord`](./DatabaseRecord.cs)** | Wraps `DbDataReader`: `GetValue<T>(string/int)`, `GetRowVersion()`, ordinal caching, and null-safe typed reads with `DateTimeTransform` applied. |
| **[`DatabaseParameterCollection`](./DatabaseParameterCollection.cs)** | `DbParameterCollection` wrapper with `AddParameter<T>`, `AddRowVersionParameter`, `AddReturnValueParameter`, and batch helpers; fluent via `IDatabaseParameters<T>`. |
| **[`SqlStatement`](./SqlStatement.cs)** | Carries `CommandText` and `CommandType`; factory methods `StoredProcedure(name)`, `FromText(text)` (in-line) and `FromResource(resourceName)` (embedded resource). |
| **[`DatabaseArgs`](./Abstractions/DatabaseArgs.cs)** | Per-operation options: transaction `IsolationLevel`, `Refresh`, and `TransformException` behavior overrides. |
| **[`DatabaseInvoker`](./Abstractions/DatabaseInvoker.cs)** | `InvokerBase<IDatabase>` emitting OpenTelemetry spans and structured log entries for every command execution. |
| **[`IDatabaseMapper<T>`](./Mapping/IDatabaseMapperT.cs)** | Bidirectional mapper interface: `MapFromDb(DatabaseRecord, OperationType)` → `T`; `MapToDb(T, DatabaseParameterCollection, OperationType)`. |
| **[`DatabaseMapper<T>`](./Mapping/DatabaseMapperT.cs)** | Abstract base for `IDatabaseMapper<T>`; override `OnMapFromDb` and `OnMapToDb`; call `MapStandardFromDb` / `MapStandardToDb` for convention columns. |
| **[`DatabaseMapper`](./Mapping/DatabaseMapper.cs)** | Static utility: `MapStandardFromDb` reads `RowVersion`/`TenantId`/`IsDeleted`/change-log columns; `MapStandardToDb` writes them as parameters. |
| **[`DatabaseWildcard`](./Extended/DatabaseWildcard.cs)** | Translates `WildcardResult` to a database `LIKE` pattern with configurable wildcard (`%`) and single-char (`_`) tokens and escape character. |
| **[`DatabaseColumns`](./Extended/DatabaseColumns.cs)** | Convention column name constants and overrides: `RowVersionName`, `TenantIdName`, `IsDeletedName`, and change-log column names. |
| **[`MultiSetSingleArgs<T>`](./Extended/MultiSetSingleArgsT.cs)** | `IMultiSetArgs` for a single-row result set within `SelectMultiSetAsync`; invokes mapper and stores the mapped value. |
| **[`MultiSetCollArgs<T>`](./Extended/MultiSetCollArgsT.cs)** | `IMultiSetArgs` for a collection result set within `SelectMultiSetAsync`; invokes mapper for each row and stores the collection. |
| **[`IDatabaseUnitOfWork`](./IDatabaseUnitOfWork.cs)** | Extends `IUnitOfWork` with `Database` property for direct database access within a unit-of-work. |
| **[`DatabaseOutboxRelayBase<TDatabase, TSelf>`](./Outbox/DatabaseOutboxRelayBase.cs)** | Abstract base for the outbox relay: polls an outbox table, publishes events via `IEventPublisher`, marks entries as sent, with configurable batch size and SQL statements. |
| **[`DatabaseOutboxRelayHostedServiceBase`](./Outbox/DatabaseOutboxRelayHostedServiceBase.cs)** | `TimerHostedServiceBase` subclass that drives `IDatabaseOutboxRelay.RelayAsync` on a timer. |
| **[`DatabaseOutboxPublisherBase`](./Outbox/DatabaseOutboxPublisherBase.cs)** | Abstract base `IEventPublisher` that writes `EventData` entries to the outbox table within the current database transaction. |

## Namespaces

| Namespace | Description |
|-----------|-------------|
| [**`Abstractions`**](./Abstractions/) | `DatabaseArgs`, `DatabaseArgsBase`, `DatabaseInvoker`, and `IDatabaseParameters` interface. |
| [**`Extended`**](./Extended/) | `DatabaseColumns`, `DatabaseWildcard`, `IMultiSetArgs`, `MultiSetSingleArgs<T>`, `MultiSetCollArgs<T>`. |
| [**`Mapping`**](./Mapping/) | `IDatabaseMapper<T>`, `DatabaseMapper<T>` abstract base, and `DatabaseMapper` static utility. |
| [**`Outbox`**](./Outbox/) | Transactional outbox relay: `DatabaseOutboxRelayBase`, `DatabaseOutboxRelayHostedServiceBase`, `DatabaseOutboxPublisherBase`, `DatabaseOutboxRelayArgs`. |

## Related Namespaces

- **[`CoreEx.Data`](../CoreEx.Data/README.md)** - `IUnitOfWork`, `DataResult`, `QueryArgsConfig`; `IDatabaseUnitOfWork` extends `IUnitOfWork`.
- **[`CoreEx.Mapping`](../CoreEx/Mapping/README.md)** - `Mapper.MapStandardFrom` pattern mirrored by `DatabaseMapper.MapStandardFromDb` for the database layer.
- **[`CoreEx.Wildcards`](../CoreEx/Wildcards/README.md)** - `WildcardResult` produced by `Wildcard.Parse()` is translated to LIKE patterns by `DatabaseWildcard`.
- **[`CoreEx.Invokers`](../CoreEx/Invokers/README.md)** - `DatabaseInvoker` and `DatabaseOutboxRelayInvoker` extend `InvokerBase` for OpenTelemetry tracing.
- **[`CoreEx.Events`](../CoreEx.Events/README.md)** - `IEventPublisher` is the outbox relay's publication target; `EventData` is what the outbox table stores.
- **[`CoreEx.Database.SqlServer`](../CoreEx.Database.SqlServer/README.md)** - SQL Server-specific `Database` implementation, `SqlServerDatabaseExtensions`, and stored-procedure conventions.
- **[`CoreEx.Database.Postgres`](../CoreEx.Database.Postgres/README.md)** - PostgreSQL-specific `Database` implementation and Npgsql extensions.