# Contoso Samples

The `samples` folder contains reference implementations of two domain microservices built with CoreEx.

- Products.
- Shopping.

Additional sample areas are currently work in progress:

- Orders.
- Order.Workflow.

Each domain includes three runnable hosts:

- API host (`*.Api`).
- Outbox Relay host (`*.Outbox.Relay`).
- Event Subscriber host (`*.Subscribe`).

The sample also includes supporting projects for contracts, application, infrastructure, domain/data, and test coverage.

## Architecture

The two domains are hosted as independent microservices that communicate via synchronous HTTP and asynchronous messaging over Azure Service Bus.

```mermaid
graph TB
    subgraph INFRA["Shared Infrastructure"]
        direction LR
        SQLSERVER[("SQL Server\n:1433")]
        REDIS[("Redis\n:6379")]
        ASB[["Azure Service Bus\nEmulator\n:5672"]]
        ASPIRE["Aspire Dashboard\n:18888"]
    end

    subgraph PRODUCTS["Contoso.Products Domain"]
        direction TB
        PAPI["Products API\n─────────────────\nGET/POST/PUT/PATCH/DELETE /products\nPOST /inventory/reserve\nPOST /inventory/adjust\nGET /products/{id}/on-hand\nGET /refdata"]
        PAPP["Products Application\n─────────────────\nProductService\nMovementService\nInventoryService"]
        PINFRA["Products Infrastructure\n─────────────────\nProductRepository\nMovementRepository\nInventoryRepository\nProductsOutboxPublisher"]
        PSUBSCRIBE["Products.Subscribe\n─────────────────\nReservationConfirmSubscriber\nReservationCancelSubscriber"]
        POUTBOX["Products.Outbox.Relay\n─────────────────\nOutbox to Service Bus\nPartitioned relay"]
        PDB[("SQL Server\n[Products] schema\n─────────────\nProduct\nInventory\nMovement\nOutbox / OutboxLease\nRef data")]
    end

    subgraph SHOPPING["Contoso.Shopping Domain"]
        direction TB
        SAPI["Shopping API\n─────────────────\nPOST /customers/{id}/baskets\nPOST /{id}/checkout\nPUT /{id}/apply-discount\nPOST/PUT/DELETE /{id}/items\nGET /baskets"]
        SAPP["Shopping Application\n─────────────────\nBasketService\nBasketReadService"]
        SDOMAIN["Shopping Domain\n─────────────────\nBasket (Aggregate Root)\nBasketItem (Entity)\nItemPricing (Value Object)"]
        SINFRA["Shopping Infrastructure\n─────────────────\nBasketRepository\nShoppingOutboxPublisher\nProductAdapter (ACL)\nProductsHttpClient\nProductSyncAdapter"]
        SSUBSCRIBE["Shopping.Subscribe\n─────────────────\nProductModifySubscriber\nProductDeleteSubscriber"]
        SOUTBOX["Shopping.Outbox.Relay\n─────────────────\nOutbox to Service Bus\nPartitioned relay"]
        SDB[("SQL Server\n[Shopping] schema\n─────────────\nBasket\nBasketItem\nProduct (replica)\nOutbox / OutboxLease\nRef data")]
    end

    PAPI --> PAPP
    PAPP --> PINFRA
    PINFRA --> PDB
    POUTBOX -->|"Poll Outbox table"| PDB

    SAPI --> SAPP
    SAPP --> SDOMAIN
    SAPP --> SINFRA
    SINFRA --> SDB
    SOUTBOX -->|"Poll Outbox table"| SDB
    SINFRA -->|"L1/L2 Hybrid Cache"| REDIS

    SINFRA -->|"① HTTP POST /api/inventory/reserve\nReserve inventory at checkout"| PAPI

    POUTBOX -->|"② Publish product.created/updated/deleted"| ASB
    ASB -->|"③ Consume product events (replication)"| SSUBSCRIBE
    SSUBSCRIBE -->|"④ Sync product replica"| SDB

    SOUTBOX -->|"⑤ Publish reservation.confirm (on checkout success)"| ASB
    SINFRA -->|"⑥ Publish reservation.cancel (on checkout failure, direct)"| ASB
    ASB -->|"⑦ Consume reservation commands"| PSUBSCRIBE
    PSUBSCRIBE --> PAPP

    PDB --- SQLSERVER
    SDB --- SQLSERVER

    PAPI -.->|"OpenTelemetry"| ASPIRE
    SAPI -.->|"OpenTelemetry"| ASPIRE
```

### Inter-Domain Communication

**① Synchronous HTTP — Shopping → Products**

During basket checkout, Shopping calls `POST /api/inventory/reserve` on the Products API directly via `ProductAdapter` (anti-corruption layer) to validate and reserve stock in real time.

**② – ④ Async event replication — Products → Shopping**

Products publishes `product.created`, `product.updated`, and `product.deleted` events through its Outbox → Relay → Service Bus. Shopping.Subscribe consumes these and keeps a local `[Shopping].[Product]` replica in sync for offline queries.

**⑤ – ⑦ Async reservation commands — Shopping → Products**

- On checkout **success**: Shopping enqueues a `reservation.confirm` command via its Outbox → Relay → Service Bus → `Products.Subscribe`, which confirms the pending inventory movement.
- On checkout **failure**: Shopping publishes `reservation.cancel` directly to Service Bus (bypassing the Outbox, since the database transaction has been rolled back) so the pending reservation is released.

### Key Patterns

| Pattern | Where Used |
|---|---|
| Transactional Outbox | Both domains — atomic event publishing with DB writes |
| Anti-Corruption Layer | `ProductAdapter` / `ProductsHttpClient` in Shopping |
| DDD Aggregate | `Basket` aggregate root with `BasketItem` and `ItemPricing` |
| Hybrid Cache (L1 + L2) | Shopping API — FusionCache with Redis backplane |
| Outbox Relay (partitioned) | Both domains — dedicated relay host per domain |
| Railway-Oriented Programming | `Result<T>` flow control throughout |
| ETag / Optimistic Concurrency | `Basket` implements `IETag` |

## What This Demonstrates

These samples are intended to show practical CoreEx usage across:

- API composition and HTTP behaviors.
- Data access and persistence workflows.
- Outbox and event publishing/subscribing patterns.
- End-to-end host orchestration with Aspire.
- Unit and integration testing for service behaviors.

## Project Layout

- `samples/src/Contoso.Products.*` for the Products domain services and supporting projects.
- `samples/src/Contoso.Shopping.*` for the Shopping domain services and supporting projects.
- `samples/aspire/Contoso.Aspire` to orchestrate both domains and view logs, traces, and metrics.
- `samples/tests` for unit, integration, host-level tests, and the E2E runner.

## Prerequisites

- .NET SDK (matching the repo requirements).
- A container runtime (Docker or Podman).
- Aspire CLI — install once:

  ```bash
  # Linux/macOS
  curl -sSL https://aspire.dev/install.sh | bash

  # Windows (PowerShell)
  iwr -useb https://aspire.dev/install.ps1 | iex
  ```

  Verify with `aspire --version`. See [aspire.dev/get-started/install-cli](https://aspire.dev/get-started/install-cli/) for details.

## Start Infrastructure

Start sample dependencies using the root compose file:

```bash
podman compose -f docker-compose.yml up -d
```

Stop dependencies when finished:

```bash
podman compose -f docker-compose.yml down
```

## Initialize Sample Databases

From the repository root:

```bash
dotnet run --project samples/src/Contoso.Products.Database -- Migrate
dotnet run --project samples/src/Contoso.Products.Database -- Data

dotnet run --project samples/src/Contoso.Shopping.Database -- Migrate
dotnet run --project samples/src/Contoso.Shopping.Database -- Data
```

## Run With Aspire

From the repository root:

```bash
aspire run
```

Or using `dotnet run`:

```bash
dotnet run --project samples/aspire/Contoso.Aspire
```

This is the easiest way to run both domains and inspect runtime behavior through centralized logs, traces, and metrics.

## Run Tests

The `samples/tests` folder contains unit and integration-style tests that exercise API, relay, and subscriber functionality.

```bash
dotnet test samples/tests/Contoso.Products.Test.Unit
dotnet test samples/tests/Contoso.Products.Test.Api
dotnet test samples/tests/Contoso.Products.Test.Outbox.Relay
dotnet test samples/tests/Contoso.Products.Test.Subscribe
dotnet test samples/tests/Contoso.Shopping.Test.Api
```

### Unit Tests

`Contoso.Products.Test.Unit` contains isolated component tests (e.g. validators). These use `WithGenericTester<EntryPoint>` from `CoreEx.UnitTesting` and do not require a running database, cache, or message broker.

### Integration Tests (API)

`Contoso.Products.Test.Api` and `Contoso.Shopping.Test.Api` are full integration tests that spin up the real API under test via `WithApiTester<Program>`. They require infrastructure (SQL Server, Redis, Service Bus emulator) to be running.

#### Data Seeding and One-Time Setup

Each integration test project has a `[OneTimeSetUp]` method that runs once before the test suite starts:

1. **Migrate and seed the database** — `Test.MigrateSqlServerDataAsync<TestData>(DbMigration.ConfigureMigrationArgs)` applies pending schema migrations and resets all rows in the domain's schema to the contents of `Data/data.yaml` defined in the corresponding `*.Test.Common` project. The migration args include a `DataResetFilterPredicate` scoped to only the domain's SQL schema so test runs for one domain cannot affect the other.

2. **Clear the hybrid cache** — `Test.ClearFusionCacheAsync()` flushes both the in-process L1 cache and the Redis L2 cache to ensure tests start from a known state.

3. **Set up event capture** — `Test.UseExpectedSqlServerOutboxPublisher()` and (where relevant) `Test.UseExpectedAzureServiceBusPublisher()` wrap the event publishers with decorators so tests can assert which events were published to the outbox or Service Bus.

4. **Mock downstream HTTP clients** — Shopping tests replace the `IHttpClientFactory` with a `MockHttpClientFactory` that intercepts calls to the Products API (e.g. `POST api/inventory/reserve`) so the Shopping API can be tested in isolation without a running Products API.

```csharp
[OneTimeSetUp]
public async Task OneTimeSetUpAsync()
{
    await Test.MigrateSqlServerDataAsync<TestData>(DbMigration.ConfigureMigrationArgs).ConfigureAwait(false);
    await Test.ClearFusionCacheAsync().ConfigureAwait(false);

    Test.UseExpectedSqlServerOutboxPublisher();
    Test.UseExpectedAzureServiceBusPublisher();

    var mcf = MockHttpClientFactory.Create();
    _mockHttpReserveRequest = mcf.CreateClient("ProductsApi").Request(HttpMethod.Post, "api/inventory/reserve");
    Test.ReplaceHttpClientFactory(mcf);
}
```

#### Test Data (data.yaml)

Test data is defined in YAML in each domain's `*.Test.Common` project under `Data/data.yaml`. The `TestData` class in that project is used as an assembly marker so the framework can locate the file. Data includes products, inventory levels, movements, and (for Shopping) pre-existing baskets.

IDs are expressed as small integers in YAML and converted to GUIDs at load time using `n.ToGuid()` helpers (e.g. `1.ToGuid()`), keeping data files human-readable while still using GUID primary keys at the database level.

#### Resource-Based Response Expectations

The `Resources/` folder in each test project contains JSON files used for two purposes:

- **Expected responses** — compared against the actual API response body (specified paths such as `etag` or `changelog` are excluded from comparison to avoid timestamp sensitivity).
- **Mock request/response bodies** — used to set up what the `MockHttpClientRequest` expects to receive and what it should return.

#### Fluent Test Pattern

Tests use a fluent expectation chain from `CoreEx.UnitTesting`:

```csharp
// Assert a successful POST that publishes an outbox event.
var created = Test.Http<Product>()
    .ExpectIdentifier()
    .ExpectETag()
    .ExpectChangeLogCreated()
    .ExpectJsonFromResource("ProductMutateTests.Create_Success.res.json")
    .ExpectSqlServerOutboxEvents(e => e.AssertWithValue("contoso", "contoso.products.product.created.v1"))
    .Run(HttpMethod.Post, "/api/products", product)
    .AssertCreated()
    .Value!;

// Assert a validation failure.
Test.Http()
    .Run(HttpMethod.Post, "/api/products", invalidProduct)
    .AssertBadRequest()
    .AssertErrors("Text is required.", "Price must be greater than or equal to zero.");

// Assert a checkout that calls the Products API mock and publishes an outbox event.
_mockHttpReserveRequest
    .WithJsonResourceBody("Basket_Checkout_Success.products.req.json")
    .Respond.With(HttpStatusCode.OK);

Test.Http<Basket>()
    .ExpectSqlServerOutboxEvents(e => e.AssertWithValue("contoso", "contoso.shopping.basket.checkedout.v1"))
    .Run(HttpMethod.Post, $"/api/baskets/{basketId}/checkout")
    .AssertOK();

_mockHttpReserveRequest.Verify(); // Confirms the mock was actually called.
```

## E2E Runner

`Contoso.E2E.Runner` is an interactive console application for running end-to-end scenarios against both running APIs. It supports one-shot scenario execution, full basket lifecycle tests, and a parallel load simulation mode with live statistics.

### Prerequisites

Both domain APIs (and their relay/subscriber hosts) must be running before using the E2E runner. The easiest way to achieve this is via Aspire (see [Run With Aspire](#run-with-aspire) above).

### Start the Runner

From the repository root:

```bash
dotnet run --project samples/tests/Contoso.E2E.Runner
```

On startup the runner checks `/health/ready` on both APIs. Press `ESC` to skip the health check if needed.

### Configuration

Default endpoints are defined in [samples/tests/Contoso.E2E.Runner/appsettings.json](tests/Contoso.E2E.Runner/appsettings.json). Override any value with a matching environment variable using `__` as a separator:

```bash
E2E__Products__BaseAddress=https://localhost:7200
E2E__Shopping__BaseAddress=https://localhost:7219
```

### Interactive Menu

After the health check passes the runner displays a menu with three groups.

**Set-Up** (run once before testing):

| Choice | What it does |
|---|---|
| Database Migration and Base Data Refresh | Runs DbEx migrations for both databases and resets base reference data. |
| Data Seeding for E2E Testing | Calls `POST /api/inventory/adjust` to set on-hand quantity to 1000 units for every active, stocked product. Run this before Shopping basket tests to ensure stock is available. |

**Scenarios** (repeatable, APIs must be healthy):

| Choice | What it does |
|---|---|
| Product Query Lifecycle | Queries products with randomised category, brand, and text filters. |
| Product Update Lifecycle | Selects a random product, toggles a description suffix, and PUTs the update. |
| Product Quantity Lifecycle | Queries the on-hand inventory for a random product. |
| Shopping Basket Lifecycle | Creates a basket, adds 1–4 random items, optionally applies the `SAVE10` coupon, checks out, and verifies the resulting basket state. Exercises the full cross-domain flow including inventory reservation and reservation confirmation. |

**Other:**

| Choice | What it does |
|---|---|
| Run all scenarios as simulation | Runs all four scenarios in parallel workers (configurable parallelism and delay per scenario). Displays a live dashboard with per-scenario iteration counts, success rates, and throughput. Press `ESC` to stop gracefully. Errors are written to `logs/load-simulation-errors.log`. |
| Retry APIs | Re-runs the health check without restarting the runner. |
| Exit | Exits the runner. |

### Recommended First-Run Order

1. Start infrastructure: `podman compose -f docker-compose.yml up -d`.
2. Migrate and seed databases (see [Initialize Sample Databases](#initialize-sample-databases)).
3. Start all hosts via Aspire: `aspire run`.
4. Start the runner: `dotnet run --project samples/tests/Contoso.E2E.Runner`.
5. Select **Database Migration and Base Data Refresh** to apply any pending migrations.
6. Select **Data Seeding for E2E Testing** to stock inventory.
7. Run individual scenarios or select **Run all scenarios as simulation** for load testing.

## Troubleshooting

### Outbox Relay Does Not Pick Up Messages

Symptom:

- API operations succeed but published events are not processed by the relay/subscriber as expected.

Likely cause:

- Local machine UTC time is skewed relative to message timestamps (for example, clock drift where local UTC is ahead of expected outbox processing windows).

What to try:

1. Verify and correct system date/time and time zone settings.
2. Restart the outbox relay host (or restart all sample hosts from Aspire).
3. If behavior persists, restart the machine to force a clean time sync and host restart state (this resolved the issue in local testing).

### Dependencies Not Healthy

Symptom:

- Hosts fail on startup or repeatedly log dependency connectivity errors.

What to try:

1. Ensure infrastructure containers are running via `podman compose -f docker-compose.yml up -d`.
2. Check container health/status with `docker ps` or `podman ps`.
3. Restart with `podman compose -f docker-compose.yml down && podman compose -f docker-compose.yml up -d`.

### Database Errors On Startup

Symptom:

- API or relay hosts fail with database/schema-related errors.

What to try:

1. Re-run migrations for both databases.
2. Re-run sample data seeding for both domains.
3. Confirm the SQL dependency container is healthy before restarting hosts.
