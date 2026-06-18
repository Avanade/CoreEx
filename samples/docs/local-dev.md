# Local Development Setup

This guide covers everything needed to run the Contoso sample hosts locally: the containerised infrastructure layer, connection string patterns, per-host startup, and how to graduate to the full Aspire-orchestrated environment for cross-domain work.

---

## Prerequisites

| Requirement | Notes |
|---|---|
| **Podman** (preferred) or **Docker** | Podman requires no daemon; `podman compose` is a drop-in replacement for `docker compose` |
| **.NET SDK** | Match the `TargetFramework` in `Directory.Build.props` |
| **Aspire workload** | `dotnet workload install aspire` — required only when running the full Aspire AppHost |

### Git line-ending configuration (Windows)

This repository enforces LF line endings via `.gitattributes`. The default Git for Windows setting (`core.autocrlf=true`) overrides this and expands LF to CRLF on checkout. Set the following once on your machine to prevent it:

```bash
git config --global core.autocrlf input
```

`input` converts CRLF → LF on commit (safety net) and does not expand LF → CRLF on checkout, allowing `.gitattributes` to remain authoritative. macOS and Linux users are unaffected.

---

## Infrastructure services

All infrastructure is defined in `docker-compose.yml` at the repo root. Start everything with:

```bash
# Podman (preferred)
podman compose -f docker-compose.yml up -d

# Docker
docker compose -f docker-compose.yml up -d
```

Stop and remove containers:

```bash
podman compose -f docker-compose.yml down
```

### Service inventory

| Service name | Image | Port(s) | Used by | Notes |
|---|---|---|---|---|
| `db-sql-server` | `mssql/server:2022-latest` | 1433 | Shopping domain; Service Bus emulator | SA password: `yourStrong(!)Password` |
| `db-postgres` | `postgres` | 5432 | Products domain | Password: `yourStrong#!Password` |
| `redis-cache` | `redis:latest` | 6379 | All domains (FusionCache backplane) | No auth by default |
| `servicebus-emulator` | `azure-messaging/servicebus-emulator:latest` | 5672 (AMQP), 5300 (mgmt) | Products.Subscribe, Shopping.Subscribe, all Outbox.Relay hosts | Depends on `db-sql-server`; config mounted from `servicebus/Config.json` |
| `dts-emulator` | `dts/dts-emulator:latest` | 8080, 8082 | Orders.Workflow.Worker, Orders.Api | Task hubs: `default`, `order` |
| `aspire-dashboard` | `aspire-dashboard:latest` | 18888 (UI), 4317 (OTLP) | Optional — any host with OTLP configured | Usable standalone; no need to run the full Aspire AppHost just for traces |

### Service Bus configuration

The emulator is pre-configured by `servicebus/Config.json`. Key values the sample hosts depend on:

| Setting | Value |
|---|---|
| Namespace | `sbemulatorns` |
| Topic | `contoso` |
| Subscription — Products | `products` (session-enabled) |
| Subscription — Shopping | `shopping` (session-enabled) |
| Unit test topics | `unit-test`, `unit-test-2` (used by integration tests) |

The `contoso` topic is shared across both domains. Session-enabled subscriptions ensure ordered, per-entity processing of events.

---

## Connection strings

All sample hosts use the Aspire component configuration key hierarchy in `appsettings.Development.json`. The `Aspire:` prefix is consumed by Aspire component registrations in `Program.cs` — it is not a generic ASP.NET configuration section.

### PostgreSQL (Products domain)

```json
"Aspire": {
  "Npgsql": {
    "ConnectionString": "Server=127.0.0.1;Database=contoso;Username=postgres;Password=yourStrong#!Password"
  }
}
```

### SQL Server (Shopping domain)

```json
"Aspire": {
  "Microsoft": {
    "Data": {
      "SqlClient": {
        "ConnectionString": "Data Source=127.0.0.1,1433;Initial Catalog=Contoso;User id=sa;Password=yourStrong(!)Password;TrustServerCertificate=true"
      }
    }
  }
}
```

### Redis (all domains with FusionCache)

```json
"Aspire": {
  "StackExchange": {
    "Redis": {
      "ConnectionString": "localhost:6379",
      "ConfigurationOptions": {
        "ConnectTimeout": 3000,
        "ConnectRetry": 2
      }
    }
  }
}
```

### Azure Service Bus emulator

All hosts that publish or subscribe add the same base connection string. Subscribe hosts additionally set `QueueOrTopicName` and `SubscriptionName`:

```json
"Aspire": {
  "Azure": {
    "Messaging": {
      "ServiceBus": {
        "ConnectionString": "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;",
        "QueueOrTopicName": "contoso",
        "SubscriptionName": "products"   // or "shopping" for Shopping.Subscribe
      }
    }
  }
}
```

The `UseDevelopmentEmulator=true` flag routes the SDK to the local emulator instead of Azure. `SAS_KEY_VALUE` is a placeholder accepted by the emulator — any non-empty string works.

---

## Running sample databases (required once, and after schema changes)

Before starting any host for the first time, migrate and seed each domain's database:

```bash
dotnet run --project samples/src/Contoso.Products.Database -- all
dotnet run --project samples/src/Contoso.Shopping.Database -- all
```

For Orders (WIP):

```bash
dotnet run --project samples/src/Contoso.Orders.Database -- all
```

Re-run after any migration scripts are added. See [tooling.md](tooling.md) for full detail on the `*.Database` project run order and generated-file ownership.

---

## Running hosts individually (without Aspire)

Use this when working on a single domain or running intra-domain integration tests:

```bash
# Products
dotnet run --project samples/src/Contoso.Products.Api
dotnet run --project samples/src/Contoso.Products.Outbox.Relay
dotnet run --project samples/src/Contoso.Products.Subscribe

# Shopping
dotnet run --project samples/src/Contoso.Shopping.Api
dotnet run --project samples/src/Contoso.Shopping.Outbox.Relay
dotnet run --project samples/src/Contoso.Shopping.Subscribe

# Orders (WIP)
dotnet run --project samples/src/Contoso.Order.Workflow.Worker
dotnet run --project samples/src/Contoso.Orders.Api
```

Intra-domain host tests (`*.Test.Api`, `*.Test.Subscribe`, `*.Test.Outbox.Relay`) start their own in-process test host — they do not require any host process to be running. Infrastructure containers must still be up.

---

## Running with Aspire (cross-domain E2E)

When you need all domains running together — for cross-domain request flows, distributed traces, or E2E testing — use the Aspire AppHost instead of starting hosts individually:

```bash
# Ensure infrastructure is running first (see above)
podman compose -f docker-compose.yml up -d

# Then start the AppHost (starts all 8 hosts as child processes)
aspire run
# or
dotnet run --project samples/aspire/Contoso.Aspire
```

The Aspire Dashboard opens automatically at `http://localhost:15174` and provides live logs, distributed traces, metrics, and health views across all running hosts.

See [aspire.md](aspire.md) for the full guide: orchestrated host inventory, E2E Runner scenarios, hosted-service pause/resume controls, and the recommended first-run order.

---

## Standalone Aspire Dashboard (telemetry only)

The `aspire-dashboard` container in `docker-compose.yml` runs the dashboard independently of the Aspire AppHost. Any host that exports OTLP to `http://localhost:4317` will appear in it, even when running individual hosts via `dotnet run`.

Configure OTLP export in `appsettings.Development.json`:

```json
"OTEL_EXPORTER_OTLP_ENDPOINT": "http://localhost:4317"
```

Or set as an environment variable before running the host.

---

## Podman-specific notes

- `podman compose` is a drop-in replacement for `docker compose` — all `docker compose` commands in this repo work with `podman compose`.
- Podman runs rootless by default; no daemon required.
- On first use, Podman may need to pull `mcr.microsoft.com` images through its registry configuration. If pulls fail, add `docker.io` and `mcr.microsoft.com` to `/etc/containers/registries.conf`.
- The Service Bus emulator and SQL Server images from `mcr.microsoft.com` require accepting the EULA via the `ACCEPT_EULA: Y` environment variable — already set in `docker-compose.yml`.
