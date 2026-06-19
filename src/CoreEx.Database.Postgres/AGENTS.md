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
