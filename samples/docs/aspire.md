# Aspire & End-to-End

## Overview

[.NET Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/get-started/aspire-overview) is the local orchestration layer for the Contoso samples. It starts and manages all domain hosts simultaneously as a single distributed application, wires their OpenTelemetry signals into a central dashboard, and exposes health, log, trace, and metric views across every process in one place.

Aspire is the required foundation for any activity that involves **cross-domain interaction** — because that interaction only exists when all hosts are running together. The intra-domain host tests (`*.Test.Api`, `*.Test.Subscribe`, `*.Test.Relay`) run in isolation and do not need Aspire; the E2E Runner does.

---

## What Aspire orchestrates

The `Contoso.Aspire` AppHost (`samples/aspire/Contoso.Aspire/AppHost.cs`) registers all six production hosts, the Orders workflow worker, and a WireMock.Net stub host:

| Resource name | Host project | Endpoints exposed |
|---|---|---|
| `mock-host` | `Contoso.Aspire.MockHost` | WireMock.Net stub server (see [MockHost](#mockhost-stubbing-external-dependencies) below) |
| `products-api` | `Contoso.Products.Api` | HTTP + `/health/ready/detailed` |
| `products-relay` | `Contoso.Products.Relay` | HTTP + `/health/ready/detailed` + hosted-service controls |
| `products-subscribe` | `Contoso.Products.Subscribe` | HTTP + `/health/ready/detailed` + hosted-service controls |
| `shopping-api` | `Contoso.Shopping.Api` | HTTP + `/health/ready/detailed` |
| `shopping-relay` | `Contoso.Shopping.Relay` | HTTP + `/health/ready/detailed` + hosted-service controls |
| `shopping-subscribe` | `Contoso.Shopping.Subscribe` | HTTP + `/health/ready/detailed` + hosted-service controls |
| `order-workflow-worker` | `Contoso.Order.Workflow.Worker` | HTTP + `/health` + DTS Dashboard link |
| `orders-api` | `Contoso.Orders.Api` | HTTP + `/health/ready/detailed` (waits for workflow worker) |

The `AddEndpoints(...)`/`AddHostedServiceSupport()`/`DisableHttpCertificateValidation()`/`AddMockHostProject<...>(...)` calls used to wire up the resources above are extension methods on `IDistributedApplicationBuilder`/`IResourceBuilder<ProjectResource>` provided by the `CoreEx.UnitTesting` package's Aspire support (`<Using Include="UnitTestEx.Aspire" />` in `Contoso.Aspire.csproj`) — there is no local `Extensions.cs`.

Hosted-service resources (outbox relays and subscribers) also get **Pause all services** and **Resume all services** commands surfaced as buttons in the Aspire Dashboard, backed by the `/hosted-services/all/pause` and `/hosted-services/all/resume` management endpoints. This allows controlled simulation of relay downtime or subscriber lag without restarting the process.

### MockHost — stubbing external dependencies

`Contoso.Aspire.MockHost` (`samples/aspire/Contoso.Aspire.MockHost/Program.cs`) is a plain console host that starts a [WireMock.Net](https://github.com/WireMock-Net/WireMock.Net) server and keeps it running for the lifetime of the AppHost:

```csharp
await WireMockConsole.RunAsync(settings => WireMockServer.Start(settings));
```

`WireMockConsole.RunAsync` (from `UnitTestEx.Aspire`) wires up graceful shutdown and console logging around the server; `WireMockServer.Start(settings)` is WireMock.Net's own entry point. It is registered in `AppHost.cs` via `builder.AddMockHostProject<Projects.Contoso_Aspire_MockHost>("mock-host")` so it shows up as a normal resource in the dashboard. It currently has no request/response mappings configured and no domain host references it yet — it exists as the ready-to-use seam for stubbing a *third-party* HTTP dependency (something outside this repo, unlike Products/Shopping which are real intra-repo domains) once one needs to be added to a cross-domain flow.

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

## Contoso.Test.Aspire — automated smoke test

`Contoso.Test.Aspire` (`samples/aspire/Contoso.Test.Aspire/E2ETest.cs`) is an NUnit project that provides an **automated, CI-friendly** counterpart to the interactive E2E Runner described below. Unlike the E2E Runner, it does not require Aspire to already be running — its single test class derives from `WithAspireTester<Projects.Contoso_Aspire>` (from `CoreEx.UnitTesting`'s Aspire support), which starts the whole `Contoso.Aspire` AppHost itself for the duration of the test run.

Its `[OneTimeSetUp]` migrates and seeds the Products (Postgres) and Shopping (SQL Server) databases, clears the Redis cache, and resets the Service Bus emulator's queues/topics/subscriptions to a known state, then waits for `products-api`/`shopping-api` to report healthy — all via `Test.*` helpers (`MigratePostgresDataAsync`, `MigrateSqlServerDataAsync`, `ClearRedisCacheAsync`, `ResetAzureServiceBusAsync`, `WaitForResourceAsync`) resolved against the live AppHost's resources by name. The single `[Test]` (`CreateOrderAndConfirm`) then drives the same cross-domain flow as the E2E Runner's **Shopping Basket Lifecycle** scenario — create/activate a Product, adjust inventory, create a Basket, add items, apply a discount, checkout, then poll until the async inventory reservation is confirmed via the outbox/Service Bus/Subscribe path — asserting each step instead of just reporting success/failure interactively.

Use `Contoso.Test.Aspire` when you want a single deterministic pass/fail signal (e.g. in CI, or as a quick local smoke test after a change) — `dotnet test samples/aspire/Contoso.Test.Aspire`. Use the E2E Runner (below) when you want to explore interactively, run load simulations, or watch traces build up live in the Aspire Dashboard against a long-running AppHost.

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
| Automated cross-domain smoke test (`Contoso.Test.Aspire`) | Self-hosted (`WithAspireTester`) | No |
| Cross-domain functional validation (interactive) | Yes (already running) | Yes |
| Load / concurrency simulation | Yes (already running) | Yes |

See [testing.md](testing.md) for the intra-domain testing guide. `Contoso.Test.Aspire` and the E2E Runner are both complements to that guide — they cover the inter-domain surface that intra-domain tests deliberately leave mocked, one as an automated pass/fail test and the other as an interactive/load-simulation tool.
