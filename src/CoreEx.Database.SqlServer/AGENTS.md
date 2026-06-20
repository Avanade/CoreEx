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
