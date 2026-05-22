# CoreEx.Database.SqlServer

> Provides the SQL Server (`Microsoft.Data.SqlClient`) implementation of `IDatabase`: `SqlServerDatabase`, `SqlServerUnitOfWork`, session-context stamping, outbox relay, SQL Server-specific parameter extensions, and OpenTelemetry metrics for outbox operations.

## Overview

`CoreEx.Database.SqlServer` is the SQL Server provider package built on top of `CoreEx.Database`. It supplies concrete implementations of every abstract type in that package — `SqlServerDatabase`, `SqlServerCommand`, `SqlServerDatabaseArgs`, `SqlServerDatabaseColumns` — and adds SQL Server-specific capabilities: session context propagation via `sp_set_session_context`, structured error-code mapping from SQL Server error numbers to CoreEx semantic exceptions, and `SqlServerUnitOfWork` for transactional outbox support.

The error-code mapping convention means stored procedures can raise well-known user error numbers (56001–56010) to signal domain exceptions without any application-layer switch statements. Duplicate-constraint violations (error numbers 2601, 2627 by default) are automatically converted to `DuplicateException`.

The outbox sub-namespace provides ready-to-use `SqlServerOutboxPublisher`, `SqlServerOutboxRelay`, and `SqlServerOutboxRelayHostedService` with .NET Meter-backed counters and histograms for enqueue throughput and relay lag.

## Key capabilities

- 🗃️ **SqlServerDatabase**: Concrete `IDatabase` for `SqlConnection`; maps SQL Server error numbers to CoreEx semantic exceptions; configurable `DuplicateErrorNumbers`; `RowVersion` encoded as Base64 string via `StringBase64Converter`.
- 🔢 **Error-code convention**: SQL user error numbers 56001–56007 and 56010 map to `ValidationException`, `BusinessException`, `AuthorizationException`, `ConcurrencyException`, `NotFoundException`, `ConflictException`, `DuplicateException`, and `DataConsistencyException` respectively.
- 👤 **Session context**: `SetSqlSessionContextAsync(ExecutionContext?)` invokes `[dbo].[spSetSessionContext]` (configurable) to stamp `Username`, `Timestamp`, `TenantId`, and `UserId` into the SQL Server session context for row-level security and audit triggers.
- 🔁 **SqlServerUnitOfWork**: `IDatabaseUnitOfWork` implementation wrapping `TransactionAsync` with `SqlServerUnitOfWorkInvoker`; optionally accepts an `IEventPublisher` outbox for transactional event enqueuing.
- 📤 **Outbox relay**: `SqlServerOutboxPublisher` (writes to outbox table), `SqlServerOutboxRelay` (polls and publishes), and `SqlServerOutboxRelayHostedService` (timer-driven hosted service) — all SQL Server-specific subclasses of the base `CoreEx.Database.Outbox` types.
- 📊 **Outbox metrics**: `SqlServerMetrics` exposes .NET `Meter` instruments: `sqlserver.outbox.enqueue` (counter), `sqlserver.outbox.relay.batch.size` (counter), `sqlserver.outbox.batch.oldest_lag` and `sqlserver.outbox.batch.newest_lag` (histograms in ms).
- 📡 **OpenTelemetry**: `CoreExSqlServerExtensions.AddCoreExSqlServerOpenTelemetry` wires `SqlServerInvoker` activity sources and the outbox meter into the OTEL tracer and meter providers.
- ⚙️ **DI registration**: `AddSqlServerDatabase<TDatabase>(services, configure?)` registers `SqlServerDatabase` as a scoped service; `AddSqlServerUnitOfWork(services, configure?)` registers `SqlServerUnitOfWork`.

## Key types

| Type | Description |
|------|-------------|
| **[`SqlServerDatabase`](./SqlServerDatabase.cs)** | Concrete `IDatabase` for `SqlConnection`; error-number to exception mapping; `RowVersion` → `StringBase64Converter`; `SetSqlSessionContextAsync`. |
| **[`SqlServerUnitOfWork`](./SqlServerUnitOfWork.cs)** | `IDatabaseUnitOfWork` implementation; wraps `TransactionAsync` with `SqlServerUnitOfWorkInvoker`; optional transactional outbox via `IEventPublisher Outbox`. |
| **[`SqlServerCommand`](./SqlServerCommand.cs)** | `DatabaseCommand` subclass for `SqlCommand`; adds SQL Server-specific parameter handling and `CommandTimeout` support. |
| **[`SqlServerDatabaseArgs`](./SqlServerDatabaseArgs.cs)** | `DatabaseArgs` subclass with SQL Server-specific defaults (e.g. `CommandTimeout`). |
| **[`SqlServerDatabaseColumns`](./Extended/SqlServerDatabaseColumns.cs)** | `DatabaseColumns` subclass adding `SessionContextUsernameName`, `SessionContextTimestampName`, `SessionContextTenantIdName`, `SessionContextUserIdName` constants for session-context parameters. |
| **[`SqlServerMetrics`](./SqlServerMetrics.cs)** | Static .NET Meter with counters and histograms for outbox enqueue throughput and relay lag. |
| **[`SqlServerOutboxPublisher`](./Outbox/SqlServerOutboxPublisher.cs)** | `DatabaseOutboxPublisherBase` for SQL Server; inserts event rows into the outbox table via a configurable stored procedure. |
| **[`SqlServerOutboxRelay`](./Outbox/SqlServerOutboxRelay.cs)** | `DatabaseOutboxRelayBase<SqlServerDatabase, SqlServerOutboxRelay>` with SQL Server-specific dequeue/complete SQL; records outbox metrics per batch. |
| **[`SqlServerOutboxRelayHostedService`](./Outbox/SqlServerOutboxRelayHostedService.cs)** | `DatabaseOutboxRelayHostedServiceBase<SqlServerOutboxRelay>` timer-driven hosted service for the SQL Server outbox relay. |
| [`SqlServerExtensions`](./SqlServerExtensions.cs) | `IDatabase` / `DatabaseCommand` extensions specific to SQL Server: `Param` helpers for `SqlDbType`-typed parameters and TVP (table-valued parameter) support. |
| [`SqlServerInvoker`](./Extended/SqlServerInvoker.cs) | `DatabaseInvoker` subclass for SQL Server; the `Default` singleton used by `SqlServerDatabase`. |
| [`SqlServerUnitOfWorkInvoker`](./Extended/SqlServerUnitOfWorkInvoker.cs) | `InvokerBase<SqlServerUnitOfWork>` wrapping `TransactionAsync` with OpenTelemetry spans and begin/commit/rollback log entries. |

## Related Namespaces

- **[`CoreEx.Database`](../CoreEx.Database/README.md)** - Abstract base types (`Database<TConn>`, `DatabaseCommand`, `DatabaseRecord`, `DatabaseOutboxRelayBase`, `IDatabaseMapper`) that all SQL Server types extend.
- **[`CoreEx.Data`](../CoreEx.Data/README.md)** - `IUnitOfWork` and `IDatabaseUnitOfWork` interfaces implemented by `SqlServerUnitOfWork`.
- **[`CoreEx.Events`](../CoreEx.Events/README.md)** - `IEventPublisher` is the relay's publication target; `SqlServerOutboxPublisher` implements `IEventPublisher`.
- **[`CoreEx.Invokers`](../CoreEx/Invokers/README.md)** - `SqlServerInvoker` and `SqlServerUnitOfWorkInvoker` extend `InvokerBase` for OpenTelemetry tracing.

## Additional Resources

- [Microsoft.Data.SqlClient](https://github.com/dotnet/SqlClient) - The ADO.NET driver this package uses.
- [sp_set_session_context](https://docs.microsoft.com/en-us/sql/relational-databases/system-stored-procedures/sp-set-session-context-transact-sql) - SQL Server session-context stored procedure used by `SetSqlSessionContextAsync`.