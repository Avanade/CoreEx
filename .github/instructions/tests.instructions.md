---
applyTo: "**/*.Test*/**/*.cs"
description: "Test conventions: test project types (Api/Unit/Subscribe/Relay), base classes, one-time setup patterns, and assertion helpers"
tags: ["testing", "unit-tests", "integration-tests", "test-helpers", "nunit"]
---

# Test Conventions

## NuGet / Project References

| Package | Key types provided |
|---|---|
| `CoreEx.UnitTesting` | `WithApiTester<T>`, `WithGenericTester<T>`, `Test.Http()`, `Test.Http<T>()`, `Test.Scoped()`, `Test.ScopedType<T>()`, `Test.ClearFusionCacheAsync()`, `Test.ReplaceHttpClientFactory()` |
| `CoreEx.UnitTesting.Database.SqlServer` | `Test.MigrateSqlServerDataAsync<T>()`, `Test.UseExpectedSqlServerOutboxPublisher()`, `.ExpectSqlServerOutboxEvents()`, `.ExpectNoSqlServerOutboxEvents()` |
| `CoreEx.UnitTesting.Database.Postgres` | `Test.MigratePostgresDataAsync<T>()`, `Test.UseExpectedPostgresOutboxPublisher()`, `.ExpectPostgresOutboxEvents()`, `.ExpectNoPostgresOutboxEvents()` |
| `CoreEx.UnitTesting.Azure.ServiceBus` | `Test.UseExpectedAzureServiceBusPublisher()`, `Test.GetAndClearAzureServiceBusAsync()` |
| `CoreEx.UnitTesting.AspNetCore` | `.ExpectIdentifier()`, `.ExpectETag()`, `.ExpectChangeLogCreated()`, `.ExpectJsonFromResource()`, `.AssertCreated()`, `.AssertOK()`, `.AssertBadRequest()`, `.AssertErrors()`, `.AssertJsonFromResource()`, `.AssertLocationHeader()` |
| `UnitTestEx` | `MockHttpClientFactory`, `MockHttpClientRequest`, `.WithJsonResourceBody()`, `.WithAnyBody()`, `.Respond.With()`, `.Respond.WithJsonResource()`, `.Verify()` |
| `NUnit` | `[TestFixture]`, `[Test]`, `[OneTimeSetUp]` |
| `AwesomeAssertions` | `.Should()`, `.Be()`, `.HaveCount()` |

## Project Types

| Project suffix | Base class | Scope |
|---|---|---|
| `*.Test.Api` | `WithApiTester<Program>` | Full integration — real DB, cache, outbox, HTTP |
| `*.Test.Unit` | `WithGenericTester<EntryPoint>` | Component/unit — isolated, no infrastructure |
| `*.Test.Subscribe` | `WithApiTester<Program>` | Integration over subscriber host |
| `*.Test.Outbox.Relay` | `WithApiTester<Program>` | Integration over relay host |

**Rule**: intra-domain dependencies (database, cache, outbox) are real; inter-domain HTTP calls and direct broker publishes are always mocked.

---

## One-Time Setup

Every integration test class must have a `[OneTimeSetUp]` method. Order of operations is fixed:

1. Migrate + seed the domain database.
2. Clear the hybrid cache.
3. Register event-capture publishers.
4. Set up inter-domain HTTP mocks (Shopping only).

**Shopping (SQL Server):**
```csharp
[OneTimeSetUp]
public async Task OneTimeSetUpAsync()
{
    await Test.MigrateSqlServerDataAsync<TestData>(DbMigration.ConfigureMigrationArgs).ConfigureAwait(false);
    await Test.ClearFusionCacheAsync().ConfigureAwait(false);

    Test.UseExpectedSqlServerOutboxPublisher();
    Test.UseExpectedAzureServiceBusPublisher();

    var mcf = MockHttpClientFactory.Create();
    _mockHttpReserveRequest = mcf.CreateClient("ProductsApi")
        .Request(HttpMethod.Post, "api/inventory/reserve");
    Test.ReplaceHttpClientFactory(mcf);
}
```

**Products (Postgres):**
```csharp
[OneTimeSetUp]
public async Task OneTimeSetUpAsync()
{
    await Test.MigratePostgresDataAsync<TestData>(DbMigration.ConfigureMigrationArgs).ConfigureAwait(false);
    await Test.ClearFusionCacheAsync().ConfigureAwait(false);

    Test.UseExpectedPostgresOutboxPublisher();
}
```

**Outbox assertion helpers are database-specific.** Use `UseExpectedPostgresOutboxPublisher` / `ExpectPostgresOutboxEvents` for Products; use `UseExpectedSqlServerOutboxPublisher` / `ExpectSqlServerOutboxEvents` for Shopping. Never mix them.

`DataResetFilterPredicate` in `DbMigration.ConfigureMigrationArgs` scopes the reset to the domain's own schema — Products and Shopping test runs do not corrupt each other even when run concurrently.

---

## Test Data (`data.yaml`)

Test data lives in `Data/data.yaml` in the `*.Test.Common` project. The `TestData` marker class locates the YAML file — do not rename or move it.

IDs are written as small integers in the YAML and resolved to GUIDs at load time. Reference them consistently in tests via `n.ToGuid()`:

```csharp
Test.Http().Run(HttpMethod.Get, $"/api/products/{1.ToGuid()}").AssertOK();
```

---

## API Test Pattern

Use the `Test.Http()` / `Test.Http<T>()` fluent chain: **set expectations → execute → assert**.

```csharp
// GET
Test.Http()
    .Run(HttpMethod.Get, $"/api/products/{1.ToGuid()}")
    .AssertOK()
    .AssertJsonFromResource("ReadTests.Product_Get_Found.res.json", "etag", "changelog");

// POST — Products (Postgres outbox)
var created = Test.Http<Product>()
    .ExpectIdentifier()
    .ExpectETag()
    .ExpectChangeLogCreated()
    .ExpectJsonFromResource("ProductMutateTests.Create_Success.res.json")
    .ExpectPostgresOutboxEvents(e => e
        .AssertWithValue("contoso", "contoso.products.product.created.v1"))
    .Run(HttpMethod.Post, "/api/products", product)
    .AssertCreated()
    .AssertLocationHeader(r => new Uri($"/api/products/{r!.Id}", UriKind.Relative))
    .Value!;

// POST — Shopping (SQL Server outbox)
Test.Http<Basket>()
    .ExpectSqlServerOutboxEvents(e => e
        .AssertWithValue("contoso", "contoso.shopping.basket.checkedout.v1"))
    .Run(HttpMethod.Post, $"/api/baskets/{basketId}/checkout", checkoutRequest)
    .AssertOK();

// Validation error
Test.Http()
    .Run(HttpMethod.Post, "/api/products", invalidProduct)
    .AssertBadRequest()
    .AssertErrors("Text is required.", "Price must be greater than or equal to zero.");

// Assert no events on failure path
Test.Http()
    .ExpectNoSqlServerOutboxEvents()
    .Run(HttpMethod.Post, $"/api/baskets/{basketId}/checkout")
    .AssertBadRequest();
```

## Resource-Based JSON Assertions

Expected response bodies live in `Resources/` as `.res.json` files. Reference them by dot-separated path. Exclude volatile fields as extra parameters:

```csharp
.AssertJsonFromResource("ReadTests.Product_Get_Found.res.json", "etag", "changelog");
.AssertJsonFromResource("Basket_Checkout_Insufficient_Quantity.products.res.json", "traceid");
```

Mock request bodies use `.req.json`; mock response bodies from a downstream API use `.products.res.json` (by convention, prefixed with the remote domain name).

---

## HTTP Client Mocking

Declare `MockHttpClientRequest` fields at class level; configure responses per test; always call `.Verify()` after the action:

```csharp
// Class level
private MockHttpClientRequest _mockHttpReserveRequest = null!;

// OneTimeSetUp
var mcf = MockHttpClientFactory.Create();
_mockHttpReserveRequest = mcf.CreateClient("ProductsApi")
    .Request(HttpMethod.Post, "api/inventory/reserve");
Test.ReplaceHttpClientFactory(mcf);

// In test — success path
_mockHttpReserveRequest
    .WithJsonResourceBody("Basket_Checkout_Success.products.req.json")
    .Respond.With(HttpStatusCode.OK);
_mockHttpReserveRequest.Verify();

// In test — error path
_mockHttpReserveRequest.WithAnyBody()
    .Respond.WithJsonResource(
        "Basket_Checkout_Insufficient_Quantity.products.res.json",
        HttpStatusCode.BadRequest,
        System.Net.Mime.MediaTypeNames.Application.ProblemJson);
_mockHttpReserveRequest.Verify();
```

---

## Unit and Validator Tests

Unit tests use `Test.Scoped(test => { ... })`. For relay-style tests that need a named scoped type, use `Test.ScopedType<ExecutionContext>`:

```csharp
// Validator unit test
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

// Repository-dependent validator — mock the dependency, test the logic
public class InventoryValidatorTests : WithGenericTester<EntryPoint>
{
    private readonly Mock<IProductRepository> _mock = new();

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _mock.Setup(x => x.GetForReservationAsync(It.IsAny<string[]>()))
            .ReturnsAsync(new Dictionary<string, ProductReserve> { ["P1"] = new() { UnitOfMeasureCode = "EA" } });
    }

    [Test]
    public void Invalid_Product() => Test.Scoped(test =>
    {
        new MovementRequestValidator(_mock.Object).AssertErrors(req,
            ("products.P2", "Product is non-stocked and therefore cannot be transacted."));
    });
}
```

---

## Subscribe Host Tests

Subscribe test classes extend `WithApiTester<Program>` over the subscriber host. The `[OneTimeSetUp]` migrates/seeds the domain DB just like an API test. There is no FusionCache to clear (Subscribe hosts have no cache).

```csharp
public class ProductModifySubscriberTests : WithApiTester<Contoso.Shopping.Subscribe.Program>
{
    [OneTimeSetUp]
    public async Task OneTimeSetUpAsync()
    {
        await Test.MigrateSqlServerDataAsync<TestData>(DbMigration.ConfigureMigrationArgs).ConfigureAwait(false);
        Test.UseExpectedSqlServerOutboxPublisher();
    }
}
```

---

## Outbox Relay Host Tests

Relay tests extend `WithApiTester<Program>` over the relay host. Use `Test.ScopedType<ExecutionContext>` to write events directly to the outbox, wait for the relay background service to forward them, then assert via `Test.GetAndClearAzureServiceBusAsync()`.

```csharp
public class RelayTests : WithApiTester<Contoso.Products.Outbox.Relay.Program>
{
    [Test]
    public async Task Outbox_Relay()
    {
        Test.ScopedType<ExecutionContext>(test =>
        {
            test.Run(async _ =>
            {
                var pub = ActivatorUtilities.GetServiceOrCreateInstance<PostgresOutboxPublisher>(test.Services);
                pub.Add("contoso", [ce1, ce2]);
                await pub.PublishAsync();

                for (int i = 0; i < 5; i++)
                    await Task.Delay(TimeSpan.FromSeconds(1));

                var list = await Test.GetAndClearAzureServiceBusAsync(
                    ServiceBusSessionReceiverOptions.CreateForTopicSubscription("contoso", "products"));

                list.Should().HaveCount(2);
            }).AssertSuccess();
        });
    }
}
```

The relay host exposes hosted-service management endpoints that can also be exercised in tests:

```csharp
Test.Http()
    .Run(HttpMethod.Post, "/hosted-services/postgres-outbox-relay-03/pause")
    .Response.StatusCode.Should().Be(HttpStatusCode.Accepted);
```

---

## NUnit Attributes

Use `[Test]` on individual test methods. `[TestFixture]` is inherited when using `WithApiTester` or `WithGenericTester`. Do not use `[TestCase]` for integration tests — use separate named methods for clarity.

## Naming Tests

Name test methods as `{Entity}_{Action}_{Outcome}`:

```
Product_Get_Found
Product_Get_NotFound
Product_Create_Success
Product_Create_Bad_Data
Basket_Checkout_Success
Basket_Checkout_Insufficient_Quantity
```

## Do Not

- Do not use `[TestCase]` for integration tests — create separate named test methods for each scenario.
- Do not use `UseExpectedSqlServerOutboxPublisher` / `ExpectSqlServerOutboxEvents` in Products tests — use the Postgres equivalents.
- Do not use `UseExpectedPostgresOutboxPublisher` / `ExpectPostgresOutboxEvents` in Shopping tests — use the SQL Server equivalents.
- Do not call `ClearFusionCacheAsync()` in Subscribe or Outbox Relay host tests — those hosts have no cache.
- Do not test inter-domain HTTP calls against a real API — always mock with `MockHttpClientFactory`.
- Do not call `Test.ReplaceHttpClientFactory()` inside individual tests — configure it once in `[OneTimeSetUp]`.
- Do not use `FluentAssertions` — use `AwesomeAssertions` (the `AwesomeAssertions` NuGet package).
- Do not omit `.Verify()` after a `MockHttpClientRequest` action — it confirms the mock was actually invoked.

## Further Reading

- [`samples/docs/testing.md`](../../samples/docs/testing.md) — full test architecture, data seeding, schema isolation, and E2E runner.
- [`samples/docs/patterns.md`](../../samples/docs/patterns.md) — pattern catalog linking testing patterns to layer docs.
