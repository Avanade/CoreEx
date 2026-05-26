# Testing

The sample test suite is organised around a clear principle: **each test project tests exactly one deployable host in isolation**. This mirrors the production topology — each host is an independently deployable process — and keeps test concerns sharply separated. The isolation boundary also determines what is real and what is mocked within each project.

---

## Intra-domain vs. inter-domain testing

Understanding this distinction is the key to understanding every test setup decision in the samples.

**Intra-domain** means everything that is *tightly coupled* to the domain under test — its own database, cache, outbox, and internal service logic. These are real dependencies that belong to the domain and are included in the test environment as-is.

**Inter-domain** means everything that is *loosely coupled* — interactions with other domains or external services. These are represented by an abstraction (adapter, HTTP client) at the domain boundary and are **always mocked** in tests. The domain under test should never reach outside its own boundary during a test run.

### Applied to the samples

| Dependency | Domain | Intra or Inter? | How handled in tests |
|---|---|---|---|
| Products PostgreSQL database | Products | **Intra** | Real — migrated and seeded in `[OneTimeSetUp]` |
| Products FusionCache | Products | **Intra** | Real — cleared in `[OneTimeSetUp]` |
| Products outbox (PostgreSQL) | Products | **Intra** | Real — captured via `UseExpectedPostgresOutboxPublisher()` |
| Shopping SQL Server database | Shopping | **Intra** | Real — migrated and seeded in `[OneTimeSetUp]` |
| Shopping FusionCache | Shopping | **Intra** | Real — cleared in `[OneTimeSetUp]` |
| Shopping outbox (SQL Server) | Shopping | **Intra** | Real — captured via `UseExpectedSqlServerOutboxPublisher()` |
| Products API (`POST /api/inventory/reserve`) | Shopping | **Inter** | Mocked — `MockHttpClientFactory` intercepts the outbound call |
| Azure Service Bus (direct publish) | Shopping | **Inter** | Captured via `UseExpectedAzureServiceBusPublisher()` |
| Azure Service Bus (relay) | Products Outbox Relay | **Inter** | Real — drained and asserted via `GetAndClearAzureServiceBusAsync` |

The Shopping `Basket_Checkout_Save_Failure` test is the sharpest illustration of the boundary: when the outbox write fails mid-checkout, Shopping falls back to publishing a `reservation.cancel` command *directly* to Service Bus (bypassing the outbox, since the DB transaction has already rolled back). The test asserts that:
- No outbox events are published (intra-domain write failed, as injected).
- One direct Service Bus event *is* published (inter-domain cancel, asserted via `ExpectAzureServiceBusEvents`).
- The basket remains `Active` (state was correctly rolled back).

This single test exercises three layers of the intra/inter boundary in one shot.

---

## Test project map

| Project | What it tests | Base class | Infrastructure needed |
|---|---|---|---|
| `*.Test.Unit` | Validators and isolated components | `WithGenericTester<EntryPoint>` | None |
| `*.Test.Api` | Full API host, intra-domain | `WithApiTester<Program>` | DB + Cache; inter-domain HTTP mocked |
| `*.Test.Subscribe` | Subscribe host, intra-domain | `WithApiTester<Program>` | DB; Service Bus simulated in-process |
| `*.Test.Outbox.Relay` | Outbox relay host, intra-domain | `WithApiTester<Program>` | DB + real Service Bus |
| `*.Test.Common` | Shared test data and migration helpers | — (referenced by the above) | — |
| `Contoso.E2E.Runner` | Cross-domain scenarios | Interactive console | All infra + all hosts running |

---

## Unit tests (`*.Test.Unit`)

### Why

Validators and other stateless components can be tested with no database, cache, or broker. These tests are fast, deterministic, and require zero infrastructure.

### How

`WithGenericTester<EntryPoint>` builds a minimal DI container configured by `EntryPoint.ConfigureApplication`. The `EntryPoint` registers only what is needed: `ExecutionContext`, a memory cache, and a `ReferenceDataProvider` loaded from the *real* `ref-data.yaml` embedded in the domain's `*.Database` project. Tests therefore run against production-representative reference data, not a separate mock fixture.

When a validator has a repository dependency (as `MovementRequestValidator` does), that repository is **mocked** with `Mock<IProductRepository>`. This is not an inter-domain mock — the repository is an intra-domain interface — but at the unit level all I/O dependencies are replaced to keep tests isolated and fast.

```csharp
// Contoso.Products.Test.Unit — no infrastructure, real reference data
public class ProductValidatorTests : WithGenericTester<EntryPoint>
{
    [Test]
    public void Empty_Required() => Test.Scoped(test =>
    {
        var p = new Product();
        new ProductValidator().AssertErrors(p,
            ("sku", "Sku is required."),
            ("text", "Text is required."),
            ("subCategory", "Sub-category is required."),
            ("unitOfMeasure", "Unit-of-measure is required."));
    });
}
```

```csharp
// Repository-dependent validator — mock the repository, test the logic
public class InventoryReservationRequestValidatorTests : WithGenericTester<EntryPoint>
{
    private readonly Mock<IProductRepository> _mock = new();

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _mock.Setup(x => x.GetForReservationAsync(It.IsAny<string[]>()))
            .ReturnsAsync(new Dictionary<string, ProductReserve>
            {
                ["P1"] = new ProductReserve { UnitOfMeasureCode = "EA", IsNonStocked = false, IsInactive = false },
                ["P2"] = new ProductReserve { UnitOfMeasureCode = "EA", IsNonStocked = true,  IsInactive = false },
                ["P3"] = new ProductReserve { UnitOfMeasureCode = "EA", IsNonStocked = false, IsInactive = true  },
            });
    }

    [Test]
    public void Invalid_Products_Extended() => Test.Scoped(test =>
    {
        var req = new MovementRequest { Id = "100", Products = new()
        {
            ["P2"] = new MovementRequestProduct { UnitOfMeasure = "EA", Quantity = 1 },
            ["P3"] = new MovementRequestProduct { UnitOfMeasure = "EA", Quantity = 1 },
        }};
        new MovementRequestValidator(_mock.Object).AssertErrors(req,
            ("products.P2", "Product is non-stocked and therefore cannot be transacted."),
            ("products.P3", "Product is not active and therefore cannot be transacted."));
    });
}
```

---

## Intra-domain host tests

`WithApiTester<Program>` boots the actual host `Program` (real DI, real middleware, real repositories) and applies test overrides during `[OneTimeSetUp]`. All intra-domain infrastructure is real; all inter-domain calls are mocked.

### API tests (`*.Test.Api`)

The `[OneTimeSetUp]` pattern for all API test classes:

1. **Migrate and seed** — `MigratePostgresDataAsync` / `MigrateSqlServerDataAsync` resets the domain schema to the contents of `Data/data.yaml` in `*.Test.Common`. A `DataResetFilterPredicate` scopes the reset to the domain's own schema so Products and Shopping test runs cannot affect each other.
2. **Clear cache** — `ClearFusionCacheAsync()` flushes L1 (in-process) and L2 (Redis) so tests start from a known state.
3. **Capture events** — `UseExpectedOutboxPublisher()` wraps the outbox publisher with a capture decorator; tests can then assert published events inline.
4. **Mock inter-domain HTTP** (Shopping only) — `MockHttpClientFactory` intercepts `POST api/inventory/reserve` so the Shopping API can be tested without a running Products API.

```csharp
// Contoso.Shopping.Test.Api — intra-domain real, inter-domain mocked
public partial class MutateTests : WithApiTester<Contoso.Shopping.Api.Program>
{
    private UnitTestEx.Mocking.MockHttpClientRequest _mockHttpReserveRequest = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUpAsync()
    {
        await Test.MigrateSqlServerDataAsync<TestData>(DbMigration.ConfigureMigrationArgs).ConfigureAwait(false);
        await Test.ClearFusionCacheAsync().ConfigureAwait(false);

        Test.UseExpectedSqlServerOutboxPublisher();
        Test.UseExpectedAzureServiceBusPublisher();

        // Mock the inter-domain HTTP call to the Products API.
        var mcf = MockHttpClientFactory.Create();
        _mockHttpReserveRequest = mcf.CreateClient("ProductsApi").Request(HttpMethod.Post, "api/inventory/reserve");
        Test.ReplaceHttpClientFactory(mcf);
    }
}
```

Tests are written as fluent chains. Event assertions, response-body comparisons, and HTTP status codes are all inline:

```csharp
// Happy-path checkout — mock the Products API to succeed, assert the outbox event
_mockHttpReserveRequest
    .WithJsonResourceBody("Basket_Checkout_Success.products.req.json")
    .Respond.With(HttpStatusCode.OK);

var v = Test.Http<Basket>()
    .ExpectChangeLogUpdated()
    .ExpectSqlServerOutboxEvents(e => e
        .AssertWithValue("contoso", "contoso.shopping.basket.checkedout.v1")
        .AssertMetadata("contoso", "contoso.products.reservation.confirm", basket.Id))
    .Run(HttpMethod.Post, $"/api/baskets/{basket.Id}/checkout")
    .AssertOK()
    .Value!;

_mockHttpReserveRequest.Verify(); // Confirms the mock was actually called.
```

```csharp
// Fault injection — simulate outbox failure, assert Service Bus cancel is published directly
Test.Http()
    .OnEventPublish(SqlServerOutboxPublisher.DefaultServiceKey,
        () => throw new InvalidOperationException("Simulated outbox failure"))
    .ExpectNoSqlServerOutboxEvents()
    .ExpectAzureServiceBusEvents(e => e.AssertMetadata("contoso", "contoso.products.reservation.cancel", id))
    .Run(HttpMethod.Post, $"/api/baskets/{id}/checkout")
    .AssertInternalServerError();
```

### Subscribe tests (`*.Test.Subscribe`)

`WithApiTester<Subscribe.Program>` boots the subscriber host. No real Service Bus is needed — tests construct a `ServiceBusReceivedMessage` in-process by building `EventData` → `CloudEvent` → `ServiceBusReceivedMessage` and calling `ServiceBusSubscribedSubscriber.ReceiveAsync(...)` directly.

```csharp
// Simulate receiving an inbound command message — no Service Bus required
public partial class SubscriberTests : WithApiTester<Contoso.Products.Subscribe.Program>
{
    [Test]
    public void ReservationConfirm_Success() => Test.Scoped(async test =>
    {
        test.ExpectPostgresOutboxEvents(e => e.AssertCount(3))
            .Run(async _ =>
            {
                var ed = EventData.CreateCommand("products", "reservation", "confirm").WithKey(referenceId);
                var ce = Test.CreateCloudEventFrom(ed);
                var sbm = ce.ToServiceBusReceivedMessage();

                var sbs = test.Services.GetRequiredService<ServiceBusSubscribedSubscriber>();
                var r = await sbs.ReceiveAsync(sbm);
                r.IsSuccess.Should().BeTrue();
            }).AssertSuccess();
    });
}
```

This approach also verifies error handling — `ErrorHandling.CompleteAsSilent` for unrecognised events, `ErrorHandling.CompleteAsInformation` for not-found entities — without needing a live broker.

### Outbox relay tests (`*.Test.Outbox.Relay`)

`WithApiTester<Outbox.Relay.Program>` boots the relay host **with its background services running**. Unlike the API and Subscribe tests, this one touches a real Service Bus because relaying *to* the broker is the entire point.

The pattern is:
1. Publish CloudEvents directly to the outbox table.
2. Wait a few seconds for the running relay background service to pick them up.
3. Drain the Service Bus topic subscription and assert the forwarded messages.

```csharp
public class RelayTests : WithApiTester<Contoso.Products.Outbox.Relay.Program>
{
    [Test]
    public async Task Outbox_Relay()
    {
        var ce1 = Test.CreateCloudEventFromJsonResource("ProductCreatedCloudEvent.json");
        var ce2 = Test.CreateCloudEventFromJsonResource("ProductDeletedCloudEvent.json");

        Test.ScopedType<ExecutionContext>(test =>
        {
            test.Run(async _ =>
            {
                var pub = ActivatorUtilities.GetServiceOrCreateInstance<PostgresOutboxPublisher>(test.Services);
                pub.Add("contoso", [ce1, ce2]);
                await pub.PublishAsync();

                // Allow the relay background service time to forward the events.
                for (int i = 0; i < 5; i++)
                    await Task.Delay(TimeSpan.FromSeconds(1));

                var list = await Test.GetAndClearAzureServiceBusAsync(
                    ServiceBusSessionReceiverOptions.CreateForTopicSubscription("contoso", "products"));

                list.Should().HaveCount(2);
                ObjectComparer.AssertJson(ce1.EncodeToJsonElement().ToString(),
                    list.Single(x => x.MessageId == ce1.Id).Body.ToString());
            }).AssertSuccess();
        });
    }
}
```

The relay host also exposes hosted-service management endpoints (`/hosted-services/{name}/pause`, `/resume`, `/status`), which are exercised directly in tests to verify the relay lifecycle:

```csharp
Test.Http()
    .Run(HttpMethod.Post, "/hosted-services/postgres-outbox-relay-03/pause")
    .Response.StatusCode.Should().Be(HttpStatusCode.Accepted);
```

---

## Test data and seeding

### `*.Test.Common`

Each domain has a `*.Test.Common` project containing:
- `Data/data.yaml` — the canonical test dataset for that domain (products, inventory, baskets, movements, ref data, etc.).
- `TestData` class — an assembly marker used by the migration helpers to locate the YAML file.
- `DbMigration` — exposes `ConfigureMigrationArgs` reused across all test projects for that domain.

### ID conventions

IDs in `data.yaml` are expressed as small integers and converted to GUIDs at load time via `n.ToGuid()` (e.g. `3007.ToGuid()`). This keeps fixtures human-readable while using GUID primary keys at the database level.

### Schema isolation

`DataResetFilterPredicate` scopes each migration reset to the domain's own schema. Running Products and Shopping tests concurrently will not corrupt each other's test data.

---

## Response expectations (`Resources/`)

Each `*.Test.Api` and `*.Test.Outbox.Relay` project has a `Resources/` folder containing JSON files for two purposes:

| File convention | Purpose |
|---|---|
| `*.res.json` | Expected API response body — compared against the actual response, with volatile paths (e.g. `etag`, `changelog`) excluded. |
| `*.req.json` | Mock request body — what the `MockHttpClientRequest` expects to receive from the domain under test. |
| `*.products.res.json` | Mock response body — what the `MockHttpClientRequest` returns to simulate the inter-domain answer. |

---

## E2E testing

`Contoso.E2E.Runner` is an interactive console runner for cross-domain scenarios. Unlike the host tests above it requires **all** infrastructure and **all** hosts to be running simultaneously — orchestrated via Aspire. It tests the complete inter-domain flow end-to-end: basket checkout triggers a real HTTP call to Products, which publishes a real event to Service Bus, which is consumed by the real subscriber. It also supports a parallel load-simulation mode for concurrency and performance validation.

See [aspire.md](aspire.md) for the full Aspire setup, E2E Runner usage, scenario descriptions, load-simulation configuration, and the recommended first-run order.
