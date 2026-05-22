# CoreEx.Database.Outbox

> Implements the [Transactional Outbox pattern](https://microservices.io/patterns/data/transactional-outbox.html): `DatabaseOutboxPublisherBase` writes events to a database outbox table within the business transaction, and `DatabaseOutboxRelayBase` / `DatabaseOutboxRelayHostedServiceBase` poll and forward them to an `IEventPublisher`.

## Overview

The transactional outbox pattern ensures that domain events are reliably published even in the face of network or messaging failures. The pattern works in two parts:

1. **Write**: When a business mutation is committed, `DatabaseOutboxPublisherBase` (an `IEventPublisher`) inserts serialized `EventData` rows into an outbox table inside the same database transaction. Because the event write and the business write are in the same transaction, both succeed or both roll back atomically.

2. **Relay**: `DatabaseOutboxRelayHostedServiceBase` is a `TimerHostedServiceBase` that periodically polls the outbox table, deserializes the pending events, forwards them to the configured `IEventPublisher` (e.g. Azure Service Bus), and marks them as sent — in a separate transaction with idempotency protection.

`DatabaseOutboxRelayBase<TDatabase, TSelf>` contains the relay logic and is parameterized by both the `IDatabase` type and the self-referencing relay type, enabling per-deployment SQL statement customization via `SetStatementsByConvention(schemaName?)`.

## Key capabilities

- ⚛️ **Atomic write**: `DatabaseOutboxPublisherBase.PublishAsync` inserts serialized events into the outbox table within the calling `IUnitOfWork` transaction — no two-phase commit required.
- ⏱️ **Timer-driven relay**: `DatabaseOutboxRelayHostedServiceBase` inherits `TimerHostedServiceBase`'s configurable `Interval`, `FirstInterval`, error back-off, and pause/resume behavior.
- 📦 **Batch publishing**: The relay reads a configurable `MaxDequeueSize` of pending rows per poll, publishes them as a batch to `IEventPublisher`, then marks all as sent in a single update.
- 🔒 **Idempotent relay**: Each outbox row has a `Status` sentinel; the relay skips already-sent rows, enabling safe replay after partial failures.
- ⚙️ **Convention SQL**: `SetStatementsByConvention(schemaName?)` derives the dequeue and complete stored procedure names from the schema name (e.g. `[Outbox].[EventOutboxDequeue]`), reducing boilerplate for standard deployments.
- 📡 **Relay invoker tracing**: `DatabaseOutboxRelayInvoker` wraps each relay batch with an OpenTelemetry span tagging batch size and result.

## Key types

| Type | Description |
|------|--------------|
| **[`DatabaseOutboxPublisherBase`](./DatabaseOutboxPublisherBase.cs)** | Abstract `IEventPublisher` that inserts `EventData` rows into the outbox table within the current database transaction via a configured SQL statement. |
| **[`DatabaseOutboxRelayBase<TDatabase, TSelf>`](./DatabaseOutboxRelayBase.cs)** | Abstract relay implementation: dequeues pending outbox rows, deserializes `EventData`, publishes to `IEventPublisher`, marks as sent. Configure SQL via `SetStatementsByConvention(schema?)` or set `DequeueStatement` / `CompleteStatement` directly. |
| **[`DatabaseOutboxRelayHostedServiceBase`](./DatabaseOutboxRelayHostedServiceBase.cs)** | `TimerHostedServiceBase` that drives `IDatabaseOutboxRelay.RelayAsync` on a configurable interval. |
| **[`DatabaseOutboxRelayHostedServiceBase<TRelay>`](./DatabaseOutboxRelayHostedServiceBaseT.cs)** | Generic variant accepting the `IDatabaseOutboxRelay` implementation via DI. |
| **[`DatabaseOutboxRelayArgs`](./DatabaseOutboxRelayArgs.cs)** | Configuration for the relay: `MaxDequeueSize`, `PartitionKey`, and whether to partition dequeue by partition key. |
| **[`DatabaseOutboxRelayInvoker`](./DatabaseOutboxRelayInvoker.cs)** | `InvokerBase<DatabaseOutboxRelayBase<TDatabase, TSelf>>` emitting OpenTelemetry spans for relay batch executions. |
| [`IDatabaseOutboxRelay`](./IDatabaseOutboxRelay.cs) | Interface with single `RelayAsync(CancellationToken)` method implemented by `DatabaseOutboxRelayBase`. |

## Related Namespaces

- **[`CoreEx.Database`](../README.md)** - `IDatabase` and `DatabaseCommand` are used by relay and publisher implementations to execute outbox SQL.
- **[`CoreEx.Events`](../../CoreEx.Events/README.md)** - `IEventPublisher` is the relay's publication target; `EventData` is the serialized event payload stored in the outbox table.
- **[`CoreEx.Hosting`](../../CoreEx/Hosting/README.md)** - `TimerHostedServiceBase` is the base class for `DatabaseOutboxRelayHostedServiceBase`, providing timer, pause/resume, and health-check integration.
- **[`CoreEx.Invokers`](../../CoreEx/Invokers/README.md)** - `DatabaseOutboxRelayInvoker` extends `InvokerBase` for relay-batch OpenTelemetry tracing.