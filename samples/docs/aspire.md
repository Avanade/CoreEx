# Aspire & End-to-End

## Overview

[.NET Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/get-started/aspire-overview) is the local orchestration layer for the Contoso samples. It starts and manages all domain hosts simultaneously as a single distributed application, wires their OpenTelemetry signals into a central dashboard, and exposes health, log, trace, and metric views across every process in one place.

Aspire is the required foundation for any activity that involves **cross-domain interaction** — because that interaction only exists when all hosts are running together. The intra-domain host tests (`*.Test.Api`, `*.Test.Subscribe`, `*.Test.Relay`) run in isolation and do not need Aspire; the E2E Runner does.

---

## What Aspire orchestrates

The `Contoso.Aspire` AppHost (`samples/aspire/Contoso.Aspire/AppHost.cs`) registers all six production hosts and the Orders workflow worker:

| Resource name | Host project | Endpoints exposed |
|---|---|---|
| `products-api` | `Contoso.Products.Api` | HTTP + `/health/ready/detailed` |
| `products-relay` | `Contoso.Products.Relay` | HTTP + `/health/ready/detailed` + hosted-service controls |
| `products-subscribe` | `Contoso.Products.Subscribe` | HTTP + `/health/ready/detailed` + hosted-service controls |
| `shopping-api` | `Contoso.Shopping.Api` | HTTP + `/health/ready/detailed` |
| `shopping-relay` | `Contoso.Shopping.Relay` | HTTP + `/health/ready/detailed` + hosted-service controls |
| `shopping-subscribe` | `Contoso.Shopping.Subscribe` | HTTP + `/health/ready/detailed` + hosted-service controls |
| `order-workflow-worker` | `Contoso.Order.Workflow.Worker` | HTTP + `/health` + DTS Dashboard link |
| `orders-api` | `Contoso.Orders.Api` | HTTP + `/health/ready/detailed` (waits for workflow worker) |

Hosted-service resources (outbox relays and subscribers) also get **Pause all services** and **Resume all services** commands surfaced as buttons in the Aspire Dashboard, backed by the `/hosted-services/all/pause` and `/hosted-services/all/resume` management endpoints. This allows controlled simulation of relay downtime or subscriber lag without restarting the process.

---

## Starting Aspire

### Prerequisites

Before starting Aspire, infrastructure containers must be running:

```bash
podman compose -f docker-compose.yml up -d
```

And databases must have been migrated and seeded at least once (see [tooling.md — Database Management](tooling.md#database-management-database)):

```bash
dotnet run --project samples/src/Contoso.Products.Database -- all
dotnet run --project samples/src/Contoso.Shopping.Database -- all
```

### Start all hosts

```bash
# From repo root — using the Aspire CLI
aspire run

# Or using dotnet run directly
dotnet run --project samples/aspire/Contoso.Aspire
```

Both commands start the AppHost, which in turn launches all registered projects as child processes. The Aspire Dashboard opens automatically in the browser.

### Aspire Dashboard

The dashboard (default: `http://localhost:15174`) provides:

- **Resources** — live status, health endpoint results, and URLs for every host.
- **Console logs** — per-resource structured log stream.
- **Structured logs** — searchable across all resources by severity, trace ID, or message content.
- **Traces** — distributed trace views showing cross-domain request flows (e.g. Shopping checkout → Products inventory reserve, then the async reservation confirm path).
- **Metrics** — runtime and custom metrics per resource.

The hosted-service command buttons (Pause / Resume) are also surfaced here, making it easy to pause the outbox relay on one domain and observe the effect on the other.

---

## The Aspire + E2E pairing

Aspire provides the running environment; the **E2E Runner** provides the workload.

```
  ┌─────────────────────────────────────────────────────────────┐
  │  Aspire (running)                                           │
  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐       │
  │  │ Products API │  │Products Relay│  │Products Sub  │  ...  │
  │  └──────┬───────┘  └──────────────┘  └──────┬───────┘       │
  │         │  (real HTTP + Service Bus + DB)    │              │
  │  ┌──────┴───────┐  ┌──────────────┐  ┌──────┴───────┐       │
  │  │ Shopping API │  │Shopping Relay│  │Shopping Sub  │  ...  │
  │  └──────────────┘  └──────────────┘  └──────────────┘       │
  └──────────────────────────┬──────────────────────────────────┘
                             │  real HTTP calls
                    ┌────────┴────────┐
                    │  E2E Runner     │
                    │  (workload)     │
                    └─────────────────┘
```

The E2E Runner calls the live APIs over real HTTP. No mocks. Cross-domain flows execute fully: Shopping checkout calls Products inventory, the outbox relay forwards events to Service Bus, the subscriber processes them, state is updated. The Aspire Dashboard shows the full distributed trace of each operation.

---

## E2E Runner

`Contoso.E2E.Runner` is an interactive console application (`samples/tests/Contoso.E2E.Runner`) that drives cross-domain scenarios against the running APIs. It serves two purposes:

1. **Functional validation** — verify that the end-to-end flows work correctly against real infrastructure, catching issues that intra-domain tests cannot (network timeouts, Service Bus lag, cross-domain state consistency).
2. **Load simulation** — run all scenarios in parallel workers to exercise the system under realistic concurrency, surfacing performance issues and race conditions.

### Starting the runner

From the repo root:

```bash
dotnet run --project samples/tests/Contoso.E2E.Runner
```

On startup, the runner checks `/health/ready` on all APIs and displays their status. Press `ESC` to skip the health check if a domain is intentionally down.

### Configuration

Default endpoints and simulation parameters are defined in `appsettings.json`. Override with environment variables using `__` as the separator:

```bash
E2E__Products__BaseAddress=https://localhost:7200
E2E__Shopping__BaseAddress=https://localhost:7219
```

Per-scenario simulation parallelism and delay bounds are also configurable:

```json
"Simulations": {
  "Shopping-Basket": {
    "Parallelism": 3,
    "MinDelayMilliseconds": 250,
    "MaxDelayMilliseconds": 750
  }
}
```

### Scenarios

#### Set-Up (run once)

| Scenario | What it does |
|---|---|
| Database Migration and Base Data Refresh | Runs DbEx migrations for all domain databases and resets base reference data. |
| Data Seeding for E2E Testing | Calls `POST /api/inventory/adjust` to set on-hand quantity to 1 000 units for every active, stocked product. Required before basket scenarios to ensure stock is available. |

#### Functional scenarios (repeatable)

| Scenario | Cross-domain? | What it exercises |
|---|---|---|
| Product Query Lifecycle | No | Queries products with randomised filters (category, brand, text). |
| Product Update Lifecycle | No | Fetches a random product, toggles a description suffix, PUTs the update. |
| Product Quantity Lifecycle | No | Queries on-hand inventory for a random product. |
| Shopping Basket Lifecycle | **Yes** | Creates a basket, adds 1–4 random items, optionally applies the `SAVE10` coupon, checks out. Triggers the full cross-domain flow: synchronous inventory reservation (Shopping → Products HTTP), transactional outbox publish, relay forwarding, and async reservation confirmation (Service Bus → Products Subscribe). |

The Shopping Basket Lifecycle is the richest scenario — it exercises every inter-domain integration path in a single run and is the primary validation tool for the full system.

#### Load simulation

Selecting **Run all scenarios as simulation** starts all four scenarios simultaneously in parallel worker pools. Each scenario runs its own workers concurrently with configurable parallelism and randomised per-step delays (to simulate realistic user behavior rather than a hammer load).

The live dashboard shows:
- Per-scenario iteration count, success count, error count, and success rate.
- Total throughput across all scenarios.
- A rolling buffer of recent events (step completions and errors) with timestamps.
- A spinner indicating the simulation is still running.

Press `ESC` to stop gracefully. Errors are written to `logs/load-simulation-errors.log` alongside the executable.

### Recommended first-run order

1. Start infrastructure: `podman compose -f docker-compose.yml up -d`
2. Migrate and seed: `dotnet run --project samples/src/Contoso.Products.Database` (and Shopping, Orders)
3. Start Aspire: `aspire run`
4. Start the runner: `dotnet run --project samples/tests/Contoso.E2E.Runner`
5. Run **Database Migration and Base Data Refresh** (picks up any pending migrations)
6. Run **Data Seeding for E2E Testing** (stocks inventory for basket scenarios)
7. Run individual scenarios, or select **Run all scenarios as simulation** for load testing
8. Observe distributed traces, logs, and metrics in the Aspire Dashboard while the runner executes

---

## Relationship to the testing strategy

| Layer | Aspire needed? | E2E Runner needed? |
|---|---|---|
| Unit tests (`*.Test.Unit`) | No | No |
| Intra-domain host tests (`*.Test.Api`, `*.Test.Subscribe`, `*.Test.Relay`) | No | No |
| Cross-domain functional validation | Yes | Yes |
| Load / concurrency simulation | Yes | Yes |

See [testing.md](testing.md) for the intra-domain testing guide. The E2E Runner is the complement to that guide — it covers the inter-domain surface that intra-domain tests deliberately leave mocked.
