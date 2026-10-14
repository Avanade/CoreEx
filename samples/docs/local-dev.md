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
| `db-postgres` | `pgvector/pgvector:0.8.2-pg18-trixie` | 5432 | Products domain | PostgreSQL 18 with pgvector 0.8.2; password: `yourStrong#!Password` |
| `redis-cache` | `redis:latest` | 6379 | All domains (FusionCache backplane) | No auth by default |
| `servicebus-emulator` | `azure-messaging/servicebus-emulator:latest` | 5672 (AMQP), 5300 (mgmt) | Products.Subscribe, Shopping.Subscribe, all Relay hosts | Depends on `db-sql-server`; config mounted from `servicebus/Config.json` |
| `dts-emulator` | `dts/dts-emulator:latest` | 8080, 8082 | Orders.Workflow.Worker, Orders.Api | Task hubs: `default`, `order` |
| `aspire-dashboard` | `aspire-dashboard:latest` | 18888 (UI), 4317 (OTLP) | Optional — any host with OTLP configured | Usable standalone; no need to run the full Aspire AppHost just for traces |

### Optional local embedding model

Products semantic search uses **Ollama 0.13.5** with **`nomic-embed-text:v1.5`**.
Normal Compose startup does not start the `search` profile or download a model. Requests without `$search` do not need it; semantic requests do.
From the repository root:

```powershell
podman compose -f docker-compose.yml --profile search up -d ollama
podman compose -f docker-compose.yml exec ollama ollama pull nomic-embed-text:v1.5
Invoke-RestMethod http://localhost:11434/api/version
.\tools\test-product-embeddings.ps1
```

The runtime image is approximately **4 GB uncompressed** and the model file is approximately **274 MB**.
Allow additional room for downloaded layers, extraction, and container storage; the model size alone is not the disk budget.
This CPU-based setup requests no GPU and limits Ollama to one loaded model and one parallel inference request.

The endpoint binds to host loopback at `localhost:11434` because Ollama has no authentication configured here.
The `ollama-models` named volume keeps downloads across container recreation.
Stopping the container does not delete that volume:

```powershell
podman compose -f docker-compose.yml stop ollama
```

Nomic emits **768-dimensional** vectors with `search_document: ` and `search_query: ` task prefixes.
The client sends `truncate=false`, so an oversized input fails instead of silently changing the searchable document.
The smoke script checks the installed model manifest digest, validates vectors, and prints cosine rankings for seeded catalog examples.
It intentionally fails if the expected semantic ranking is not obtained; do not confuse a working embedding endpoint with good retrieval.

Products API and Subscribe development configuration contain `Ollama:BaseAddress` and longer inference timeout settings.
Other environments must supply their own endpoint/timeouts before searching. PostgreSQL now includes pgvector and `$search` uses exact semantic ranking by default.

Both hosts use the Aspire OllamaSharp client integration via `AddProductEmbeddings`, with CoreEx's named-client
registration providing service discovery, idempotency handling and HTTP resilience. The same registration is used by real-model test setup. This is client wiring only: Compose
still owns the server and model provisioning, and the Application layer still depends on `IProductEmbeddingAdapter`.

The API has no rebuild command or startup rebuild. Products read-test `OneTimeSetUp` explicitly prepares controlled
embeddings; Test.Aspire prepares real embeddings after seeding and clearing the cache, before starting hosts.
Aspire exposes the Compose-owned Ollama endpoint as an external resource and uses the AppHost's `Ollama`
configuration for both fixture preparation and API calls. Start the `search` Compose profile and install the model
above before running Aspire E2E tests; CI does this explicitly too.

`ProductSearchIndex.RebuildAsync` remains callable through a configured Infrastructure service scope. There is
currently no standalone indexing console or HTTP maintenance endpoint. Starting Aspire manually does not seed or
rebuild the catalog. After prepared-fixture execution, `.\tools\test-product-search.ps1` can exercise a normally
running API (default `http://localhost:5207/`). Searchable CRUD changes require another explicit rebuild until
incremental indexing is implemented. Missing/stale eligible embeddings produce HTTP 503 rather than an incomplete
successful search. Ordinary product reads and structured filters remain available.

To prepare and verify a real-model catalog without subsequently running the E2E test that adds an unindexed product,
run the MCP lifecycle on its own. This is test setup: it reseeds all three domain databases and resets broker state,
so use it only against disposable local infrastructure.

```powershell
dotnet test samples\aspire\Contoso.Test.Aspire --filter "FullyQualifiedName~E2ETest.Contoso_Mcp_FullBasketLifecycle"
```

See [the search guide](search.md) for implementation status and relevance findings.

### Service Bus configuration

The emulator is pre-configured by `servicebus/Config.json`. Key values the sample hosts depend on:

| Setting | Value |
|---|---|
| Namespace | `sbemulatorns` |
| Topic | `contoso` |
| Subscription — Products | `products` (session-enabled) |
| Subscription — Shopping | `shopping` (session-enabled) |
| Subscription — Customers relay tests | `customers-relay` (session-enabled observation subscription; no Customers Subscribe host) |
| Queue — Products commands | `contoso-products` (session-enabled; Shopping → Products `reservation.confirm`/`reservation.cancel` commands; Shopping publisher uses a destination-specific session bucket count of `2`) |
| Unit test topics | `unit-test`, `unit-test-2` (used by integration tests) |

The `contoso` topic is shared across Products, Shopping, and Customers for **events**. The `customers-relay` subscription is test-owned observation, not a business consumer. Session-enabled subscriptions ensure ordered, per-entity processing of events. **Commands** are single-consumer: the `NamedDestinationProvider` routes each command to a per-target-domain queue (`contoso-{domain}`, e.g. `contoso-products`) so only the addressed domain sees it. Any new command target needs a matching queue in `servicebus/Config.json` (and in the tests' `ServiceBus.GetQueues()`), and the emulator container must be restarted to pick it up.

Shopping's API, Subscribe, and Relay publisher configurations override `CoreEx:Host:ServiceBus:Destinations:contoso-products:SessionIdPartitionSize` to `2`; the publisher-wide default remains `4` for other destinations. This is a producer-side key-to-session bucket setting, not Service Bus's physical partition count or the Products receiver's `MaxConcurrentSessions`.

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

All hosts that publish or subscribe add the same base connection string. Subscribe hosts consuming a topic additionally set `QueueOrTopicName` and `SubscriptionName` (a command queue receiver, e.g. `contoso-products`, is named explicitly via `CreateForQueue("contoso-products")`):

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
dotnet run --project samples/src/Contoso.Products.Relay
dotnet run --project samples/src/Contoso.Products.Subscribe

# Customers (provision both business containers and $outbox-leases first)
dotnet run --project samples/src/Contoso.Customers.Database -f net10.0 -- All
dotnet run --project samples/src/Contoso.Customers.Api -f net10.0
dotnet run --project samples/src/Contoso.Customers.Relay -f net10.0

# Shopping
dotnet run --project samples/src/Contoso.Shopping.Api
dotnet run --project samples/src/Contoso.Shopping.Relay
dotnet run --project samples/src/Contoso.Shopping.Subscribe

# Orders (WIP)
dotnet run --project samples/src/Contoso.Order.Workflow.Worker
dotnet run --project samples/src/Contoso.Orders.Api
```

Intra-domain host tests (`*.Test.Api`, `*.Test.Subscribe`, `*.Test.Relay`) start their own in-process test host — they do not require any host process to be running. Infrastructure containers must still be up.

Orders API uses HTTPS port `7330` and HTTP port `5330`; the E2E Runner targets `https://localhost:7330`. Keep fixed development ports outside Windows' default dynamic TCP range (`49152`-`65535`), where changing port reservations can prevent Aspire's proxy from binding. Restart the AppHost and runner after changing their endpoint configuration.

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
