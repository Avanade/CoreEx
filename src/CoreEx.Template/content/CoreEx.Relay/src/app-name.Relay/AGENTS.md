# domain-name Outbox Relay Host -- AI Agent Guide

This is the **Outbox Relay host** for the `domain-name` domain, part of the `solution-name` microservice. It reads events from the transactional outbox in the database and forwards them to the configured message broker.

> **Before answering any CoreEx question:** check whether `.github/docs/coreex/` is populated at the solution root. If empty, run `/coreex-docs-sync` first. The package guides at `.github/docs/coreex/agents/` are especially relevant -- check `CoreEx.Events.md` and the database-specific guide.

---

## Host Responsibilities

The relay is a **background worker host** -- it runs one or more `IHostedService` instances that continuously poll the database outbox and publish outstanding events. It should contain:

- `Program.cs` -- startup, DI registration, no controllers needed
- No business logic; no domain types; no Application layer references

Read `.github/docs/coreex/hosts-layer.md` for the full host-layer guide and the outbox relay pattern.

---

## How the Outbox Relay Works

1. The API (or Subscriber) host writes events to the **database outbox table** instead of publishing them directly -- this ensures events are committed atomically with business data.
2. The relay polls the outbox on a configurable interval.
3. It publishes each event to the message broker (e.g. Azure Service Bus).
4. On successful publish, the event row is marked as sent (or deleted).

This guarantees at-least-once delivery without distributed transactions.

---

## Configuration

The relay connects to the same database as the API host -- use the **same Aspire resource name** so Aspire wires up the connection string automatically:

<!-- #if implement-sqlserver -->
- `builder.AddSqlClientConnection("SqlServer")` -- matches the Aspire SQL Server resource
<!-- #elif implement-postgres -->
- `builder.AddNpgsqlDataSource("Postgres")` -- matches the Aspire PostgreSQL resource
<!-- #else -->
- No database configured -- this relay has no data provider
<!-- #endif -->
<!-- #if implement-servicebus -->
- `builder.AddAzureServiceBusClient("ServiceBus")` -- matches the Aspire Service Bus resource
<!-- #endif -->

Relay timing and retry settings are configured via `appsettings.json` -- read `.github/docs/coreex/hosts-layer.md` for the outbox relay configuration schema.

---

## This Host's Feature Configuration

<!-- #if implement-sqlserver -->
- **Database:** SQL Server outbox (`AddSqlServerOutboxRelay()` + `AddSqlServerOutboxRelayHostedService()`)
<!-- #elif implement-postgres -->
- **Database:** PostgreSQL outbox (`AddPostgresOutboxRelay()` + `AddPostgresOutboxRelayHostedService()`)
<!-- #else -->
- **Database:** None -- this relay has no database outbox configured
<!-- #endif -->
<!-- #if implement-servicebus -->
- **Message broker:** Azure Service Bus (`AddAzureServiceBusPublisher()`)
<!-- #else -->
- **Message broker:** None configured
<!-- #endif -->

---

## Key Packages

| Package | Purpose |
|---|---|
<!-- #if implement-sqlserver -->
| `CoreEx.Database.SqlServer` | SQL Server outbox relay implementation |
<!-- #endif -->
<!-- #if implement-postgres -->
| `CoreEx.Database.Postgres` | PostgreSQL outbox relay implementation |
<!-- #endif -->
<!-- #if implement-servicebus -->
| `CoreEx.Azure.Messaging.ServiceBus` | Azure Service Bus publisher |
<!-- #endif -->
| `CoreEx.Events` | Event publishing abstractions |

---

## Relevant Docs

- `.github/docs/coreex/hosts-layer.md` -- relay startup patterns and outbox configuration
- `.github/docs/coreex/infrastructure-layer.md` -- outbox schema and EF Core setup
- `.github/docs/coreex/local-dev.md` -- running locally with .NET Aspire
<!-- #if implement-sqlserver -->
- `.github/docs/coreex/agents/CoreEx.Database.SqlServer.md` -- SQL Server outbox details
<!-- #endif -->
<!-- #if implement-postgres -->
- `.github/docs/coreex/agents/CoreEx.Database.Postgres.md` -- PostgreSQL outbox details
<!-- #endif -->
<!-- #if implement-servicebus -->
- `.github/docs/coreex/agents/CoreEx.Azure.Messaging.ServiceBus.md` -- Service Bus publisher
<!-- #endif -->
- `.github/docs/coreex/agents/CoreEx.Events.md` -- event publishing abstractions
