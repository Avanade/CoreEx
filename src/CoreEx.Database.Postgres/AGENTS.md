# CoreEx.Database.Postgres — AI Usage Guide

PostgreSQL implementation of `IDatabase` / `IUnitOfWork` with transactional outbox support.

## Registration

```csharp
// Program.cs
builder.AddAzureNpgsqlDataSource("Postgres");   // Aspire resource name
builder.Services
    .AddPostgresDatabase()
    .AddPostgresUnitOfWork()
    .AddPostgresOutboxPublisher()               // transactional outbox publisher
    .AddDbContext<MyDbContext>()
    .AddEfDb<MyEfDb>();
```

## Error Code Convention

Functions/procedures raise `SQLSTATE` values to signal domain exceptions — no application-layer switch statements.

| SQLSTATE | CoreEx exception |
|---|---|
| 56001 | `ValidationException` |
| 56002 | `BusinessException` |
| 56004 | `ConcurrencyException` |
| 56005 | `NotFoundException` |
| 56006 | `ConflictException` |
| 56007 | `DuplicateException` |

## Outbox

`PostgresOutboxPublisher` writes events to the outbox table within the current `TransactionAsync` scope. The `PostgresOutboxRelayHostedService` polls and forwards them to `IEventPublisher` (typically Azure Service Bus).

```csharp
// Relay host Program.cs
builder.Services
    .AddPostgresDatabase()
    .AddPostgresUnitOfWork()
    .AddPostgresOutboxRelay();

builder.AddPostgresOutboxRelayHostedService();  // called on builder, not builder.Services
```

`PostgresOutboxRelayHostedService` self-pauses/self-resumes via `DatabaseOutboxRelayHostedServiceBase.Resiliency` (a Polly circuit breaker, same shape as the Cosmos DB and Azure Service Bus relays) - a sustained failure ratio pauses the whole hosted service for an exponentially-increasing backoff, then automatically resumes to re-test recovery, without requiring a manual `ResumeAsync()` call. A failure for one partition no longer prevents other, unrelated partitions from being attempted in the same tick.

Poison-message/dead-letter handling is still **not implemented** - a permanently-failing row is cancelled and rescheduled with backoff forever, with no built-in give-up. The claim query claims a *strictly contiguous* run starting from the oldest pending row for a given tenant/partition, stopping at the first still-leased-or-unavailable row - a permanently-failing row therefore stays the oldest pending row forever and blocks every row after it in the same partition from ever being claimed, indefinitely, not just delayed. The circuit breaker mitigates the blast radius (other partitions keep flowing, and the host self-recovers once the underlying cause clears) but does not solve this - the affected partition itself remains stuck until an operator intervenes.

**Detecting a stuck partition:** `postgres.outbox.enqueue` continuing to climb while `postgres.outbox.relay.publish` stays flat for the same partition is the signal. `postgres.outbox.relay.oldest_lag`/`newest_lag` are recorded on both a successful and a failed publish attempt, so they keep climbing (rather than going silent) for as long as a batch keeps failing - alert on a sustained rise in `postgres.outbox.relay.oldest_lag`, not just on `postgres.outbox.relay.publish.failed`, since a low failure count can still mean one partition has been stuck for a long time.

## OpenTelemetry

```csharp
builder.WithCoreExTelemetry()
    .WithCoreExPostgresTelemetry()
    .UseOtlpExporter();
```

## Do Not

- Do not mix `UseExpectedSqlServerOutboxPublisher` / `ExpectSqlServerOutboxEvents` in tests for a Postgres-backed domain — use the Postgres equivalents.
- Do not call `AddPostgresOutboxRelayHostedService()` on `builder.Services` — call it on `builder`.

## Further Reading

- [README](./README.md) — full API reference including `PostgresDatabase`, metrics, and Npgsql extensions.
- [CoreEx.Database](../CoreEx.Database/README.md) — abstract base types.
- [Infrastructure layer](../../samples/docs/infrastructure-layer.md) — PostgreSQL-specific repository, mapper, and outbox wiring in the Products sample.
- [Tooling](../../samples/docs/tooling.md) — `*.Database` project (DbEx) for PostgreSQL schema generation and outbox infrastructure setup.
