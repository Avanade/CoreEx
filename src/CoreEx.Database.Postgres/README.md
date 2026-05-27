# CoreEx.Database.Postgres

> Provides the PostgreSQL (Npgsql) implementation of `IDatabase`: `PostgresDatabase`, `PostgresUnitOfWork`, outbox relay, Postgres-specific parameter extensions, and OpenTelemetry metrics for outbox operations.

## Overview

`CoreEx.Database.Postgres` is the PostgreSQL provider package built on top of `CoreEx.Database`. It supplies concrete implementations of every abstract type in that package — `PostgresDatabase`, `PostgresCommand`, `PostgresDatabaseArgs`, `PostgresDatabaseColumns` — and adds PostgreSQL-specific capabilities: structured error-code mapping from PostgreSQL `SqlState` strings to CoreEx semantic exceptions, and `PostgresUnitOfWork` for transactional outbox support.

The error-code mapping convention mirrors the SQL Server convention: stored procedures and functions raise well-known `SQLSTATE` codes (56001–56010) to signal domain exceptions without any application-layer switch statements. Unique-violation errors (`23505` by default) are automatically converted to `DuplicateException`.

The outbox sub-namespace provides ready-to-use `PostgresOutboxPublisher`, `PostgresOutboxRelay`, and `PostgresOutboxRelayHostedService` with .NET Meter-backed counters and histograms for enqueue throughput and relay lag.

## Key capabilities

- 🗃️ **PostgresDatabase**: Concrete `IDatabase` for `NpgsqlConnection` (constructed from `NpgsqlDataSource`); maps PostgreSQL `SqlState` strings to CoreEx semantic exceptions; configurable `DuplicateErrorNumbers`; `RowVersion` encoded via `EncodedStringToUInt32Converter`.
- 🔢 **Error-code convention**: `SQLSTATE` values 56001–56007 and 56010 map to `ValidationException`, `BusinessException`, `AuthorizationException`, `ConcurrencyException`, `NotFoundException`, `ConflictException`, `DuplicateException`, and `DataConsistencyException` respectively — identical to the SQL Server convention.
- 🔁 **PostgresUnitOfWork**: `IDatabaseUnitOfWork` implementation wrapping `TransactionAsync` with `PostgresUnitOfWorkInvoker`; optionally accepts an `IEventPublisher` outbox for transactional event enqueuing.
- 📤 **Outbox relay**: `PostgresOutboxPublisher` (writes to outbox table), `PostgresOutboxRelay` (polls and publishes), and `PostgresOutboxRelayHostedService` (timer-driven hosted service) — all PostgreSQL-specific subclasses of the base `CoreEx.Database.Outbox` types.
- 📊 **Outbox metrics**: `PostgresMetrics` exposes .NET `Meter` instruments: `postgres.outbox.enqueue` (counter), `postgres.outbox.relay.batch.size` (counter), `postgres.outbox.batch.oldest_lag` and `postgres.outbox.batch.newest_lag` (histograms in ms).
- 📡 **OpenTelemetry**: `CoreExPostgresExtensions.AddCoreExPostgresOpenTelemetry` wires `PostgresInvoker` activity sources and the outbox meter into the OTEL tracer and meter providers.
- ⚙️ **DI registration**: `AddPostgresDatabase<TDatabase>(services, configure?)` registers `PostgresDatabase` as a scoped service; `AddPostgresUnitOfWork(services, configure?)` registers `PostgresUnitOfWork`.

## Key types

| Type | Description |
|------|-------------|
| **[`PostgresDatabase`](./PostgresDatabase.cs)** | Concrete `IDatabase` for `NpgsqlConnection`; `SqlState` string to exception mapping; `RowVersion` → `EncodedStringToUInt32Converter`; accepts `NpgsqlDataSource` for connection-pool management. |
| **[`PostgresUnitOfWork`](./PostgresUnitOfWork.cs)** | `IDatabaseUnitOfWork` implementation; wraps `TransactionAsync` with `PostgresUnitOfWorkInvoker`; optional transactional outbox via `IEventPublisher Outbox`. |
| **[`PostgresCommand`](./PostgresCommand.cs)** | `DatabaseCommand` subclass for `NpgsqlCommand`; adds Postgres-specific parameter handling. |
| **[`PostgresDatabaseArgs`](./PostgresDatabaseArgs.cs)** | `DatabaseArgs` subclass with PostgreSQL-specific defaults. |
| **[`PostgresDatabaseColumns`](./Extended/PostgresDatabaseColumns.cs)** | `DatabaseColumns` subclass with PostgreSQL-specific convention column name defaults (e.g. snake_case names). |
| **[`PostgresMetrics`](./PostgresMetrics.cs)** | Static .NET Meter with counters and histograms for outbox enqueue throughput and relay lag. |
| **[`PostgresOutboxPublisher`](./Outbox/PostgresOutboxPublisher.cs)** | `DatabaseOutboxPublisherBase` for PostgreSQL; inserts event rows into the outbox table via a configurable function/procedure. |
| **[`PostgresOutboxRelay`](./Outbox/PostgresOutboxRelay.cs)** | `DatabaseOutboxRelayBase<PostgresDatabase, PostgresOutboxRelay>` with PostgreSQL-specific dequeue/complete SQL; records outbox metrics per batch. |
| **[`PostgresOutboxRelayHostedService`](./Outbox/PostgresOutboxRelayHostedService.cs)** | `DatabaseOutboxRelayHostedServiceBase<PostgresOutboxRelay>` timer-driven hosted service for the PostgreSQL outbox relay. |
| [`PostgresExtensions`](./PostgresExtensions.cs) | `IDatabase` / `DatabaseCommand` extensions specific to PostgreSQL: `Param` helpers for `NpgsqlDbType`-typed parameters. |
| [`PostgresInvoker`](./Extended/PostgresInvoker.cs) | `DatabaseInvoker` subclass for PostgreSQL; the `Default` singleton used by `PostgresDatabase`. |
| [`PostgresUnitOfWorkInvoker`](./Extended/PostgresUnitOfWorkInvoker.cs) | `InvokerBase<PostgresUnitOfWork>` wrapping `TransactionAsync` with OpenTelemetry spans and begin/commit/rollback log entries. |

## Related Namespaces

- **[`CoreEx.Database`](../CoreEx.Database/README.md)** - Abstract base types (`Database<TConn>`, `DatabaseCommand`, `DatabaseRecord`, `DatabaseOutboxRelayBase`, `IDatabaseMapper`) that all PostgreSQL types extend.
- **[`CoreEx.Data`](../CoreEx.Data/README.md)** - `IUnitOfWork` and `IDatabaseUnitOfWork` interfaces implemented by `PostgresUnitOfWork`.
- **[`CoreEx.Events`](../CoreEx.Events/README.md)** - `IEventPublisher` is the relay's publication target; `PostgresOutboxPublisher` implements `IEventPublisher`.
- **[`CoreEx.Invokers`](../CoreEx/Invokers/README.md)** - `PostgresInvoker` and `PostgresUnitOfWorkInvoker` extend `InvokerBase` for OpenTelemetry tracing.

## Additional Resources

- [Npgsql](https://www.npgsql.org/) - The PostgreSQL ADO.NET driver this package uses.
- [PostgreSQL Error Codes](https://www.postgresql.org/docs/current/errcodes-appendix.html) - Reference for `SqlState` values including the CoreEx convention codes.

## AI Usage Guide

An [`AGENTS.md`](./AGENTS.md) file is included with this package. AI coding assistants (GitHub Copilot, Claude, Cursor, etc.) that support workspace-injected package documentation will automatically surface concise usage guidance, code examples, and `Do Not` rules for this package without requiring a local CoreEx checkout.