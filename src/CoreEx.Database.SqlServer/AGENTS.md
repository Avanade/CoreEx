# CoreEx.Database.SqlServer — AI Usage Guide

SQL Server implementation of `IDatabase` / `IUnitOfWork` with session-context stamping and transactional outbox support.

## Registration

```csharp
// Program.cs
builder.AddSqlServerClient("SqlServer");   // Aspire resource name
builder.Services
    .AddSqlServerDatabase()
    .AddSqlServerUnitOfWork()
    .AddSqlServerOutboxPublisher()         // transactional outbox publisher
    .AddDbContext<MyDbContext>()
    .AddEfDb<MyEfDb>();
```

## Session Context

Call `SetSqlSessionContextAsync` at the start of the unit-of-work to stamp `Username`, `Timestamp`, `TenantId`, and `UserId` into the SQL Server session context for audit triggers and row-level security.

```csharp
// Typically called inside the unit-of-work invoker (automatic in SqlServerUnitOfWorkInvoker)
await _db.SetSqlSessionContextAsync(executionContext).ConfigureAwait(false);
```

## Error Number Convention

Stored procedures raise user error numbers 56001–56007/56010 to signal domain exceptions.

| Error number | CoreEx exception |
|---|---|
| 56001 | `ValidationException` |
| 56002 | `BusinessException` |
| 56004 | `ConcurrencyException` |
| 56005 | `NotFoundException` |
| 56006 | `ConflictException` |
| 56007 | `DuplicateException` |

## Outbox

`SqlServerOutboxPublisher` writes events to the outbox table within the current `TransactionAsync` scope. `SqlServerOutboxRelayHostedService` polls and forwards to `IEventPublisher` (typically Azure Service Bus).

```csharp
// Relay host Program.cs
builder.Services
    .AddSqlServerDatabase()
    .AddSqlServerUnitOfWork()
    .AddSqlServerOutboxRelay();

builder.AddSqlServerOutboxRelayHostedService();  // called on builder, not builder.Services
```

Poison-message/dead-letter handling is **not yet implemented** - a permanently-failing row is cancelled and rescheduled with backoff forever, with no built-in give-up. The claim query claims a *strictly contiguous* run starting from the oldest pending row for a given tenant/partition, stopping at the first still-leased-or-unavailable row - a permanently-failing row therefore stays the oldest pending row forever and blocks every row after it in the same partition from ever being claimed, indefinitely, not just delayed.

**Detecting a stuck partition:** `sqlserver.outbox.enqueue` continuing to climb while `sqlserver.outbox.relay.publish` stays flat for the same partition is the signal. `sqlserver.outbox.relay.oldest_lag`/`newest_lag` are recorded on both a successful and a failed publish attempt, so they keep climbing (rather than going silent) for as long as a batch keeps failing - alert on a sustained rise in `sqlserver.outbox.relay.oldest_lag`, not just on `sqlserver.outbox.relay.publish.failed`, since a low failure count can still mean one partition has been stuck for a long time.

## OpenTelemetry

```csharp
builder.WithCoreExTelemetry()
    .WithCoreExSqlServerTelemetry()
    .UseOtlpExporter();
```

## Do Not

- Do not mix `UseExpectedPostgresOutboxPublisher` / `ExpectPostgresOutboxEvents` in tests for a SQL Server-backed domain — use the SQL Server equivalents.
- Do not call `AddSqlServerOutboxRelayHostedService()` on `builder.Services` — call it on `builder`.

## Further Reading

- [README](./README.md) — full API reference including `SqlServerDatabase`, session context, metrics, and TVP extensions.
- [CoreEx.Database](../CoreEx.Database/README.md) — abstract base types.
- [Infrastructure layer](../../samples/docs/infrastructure-layer.md) — SQL Server-specific repository, mapper, and outbox wiring in the Shopping sample.
- [Tooling](../../samples/docs/tooling.md) — `*.Database` project (DbEx) for SQL Server schema generation, session-context setup, and outbox infrastructure.
