---
applyTo: "**/*.Test*/**/*.cs"
---

# Test Conventions

## NuGet / Project References

| Package | Key types provided |
|---|---|
| `CoreEx.UnitTesting` | `WithApiTester<T>`, `WithGenericTester<T>`, `Test.Http()`, `Test.Http<T>()`, `Test.MigrateSqlServerDataAsync<T>()`, `Test.ClearFusionCacheAsync()`, `Test.UseExpectedSqlServerOutboxPublisher()`, `Test.UseExpectedAzureServiceBusPublisher()`, `Test.ReplaceHttpClientFactory()`, `.ExpectIdentifier()`, `.ExpectETag()`, `.ExpectChangeLogCreated()`, `.ExpectJsonFromResource()`, `.ExpectSqlServerOutboxEvents()`, `.ExpectNoSqlServerOutboxEvents()`, `.AssertCreated()`, `.AssertOK()`, `.AssertBadRequest()`, `.AssertErrors()`, `.AssertJsonFromResource()`, `.AssertLocationHeader()`, `Test.Scoped()` |
| `UnitTestEx` | `MockHttpClientFactory`, `MockHttpClientRequest`, `.WithJsonResourceBody()`, `.WithAnyBody()`, `.Respond.With()`, `.Respond.WithJsonResource()`, `.Verify()` |
| `NUnit` | `[TestFixture]`, `[Test]`, `[OneTimeSetUp]` |
| `AwesomeAssertions` | `.Should()`, `.Be()` |

## Project Types

| Project suffix | Base class | Scope |
|---|---|---|
| `*.Test.Api` | `WithApiTester<Program>` | Full integration — real DB, cache, events, HTTP |
| `*.Test.Unit` | `WithGenericTester<EntryPoint>` | Component/unit — isolated, no infrastructure |
| `*.Test.Subscribe` | `WithApiTester<Program>` | Integration over subscriber host |
| `*.Test.Outbox.Relay` | `WithApiTester<Program>` | Integration over relay host |

## One-Time Setup

Every integration test class must have a `[OneTimeSetUp]` method that runs once before the suite. Order of operations is fixed:

1. Migrate + seed the database.
2. Clear the hybrid cache.
3. Set up event capture publishers.
4. Set up HTTP client mocks (where applicable).

```csharp
[OneTimeSetUp]
public async Task OneTimeSetUpAsync()
{
    await Test.MigrateSqlServerDataAsync<TestData>(DbMigration.ConfigureMigrationArgs).ConfigureAwait(false);
    await Test.ClearFusionCacheAsync().ConfigureAwait(false);

    Test.UseExpectedSqlServerOutboxPublisher();
    Test.UseExpectedAzureServiceBusPublisher(); // Shopping only

    var mcf = MockHttpClientFactory.Create();
    _mockHttpReserveRequest = mcf.CreateClient("ProductsApi")
        .Request(HttpMethod.Post, "api/inventory/reserve");
    Test.ReplaceHttpClientFactory(mcf);
}
```

## Test Data (data.yaml)

Test data lives in `Data/data.yaml` in the `*.Test.Common` project. The `TestData` marker class in that project is the assembly locator — do not rename or move it.

IDs are written as integers in the YAML file and resolved to GUIDs at load time via `n.ToGuid()`. Use the same helper in test code to reference those IDs:

```csharp
var product = Test.Http()
    .Run(HttpMethod.Get, $"/api/products/{1.ToGuid()}")
    .AssertOK();
```

## Fluent Test Pattern

Always use the `Test.Http()` / `Test.Http<T>()` fluent chain:

1. **Set expectations** (before calling `.Run`).
2. **Execute** with `.Run(method, path, body?)`.
3. **Assert** the response.

```csharp
// Simple GET
Test.Http()
    .Run(HttpMethod.Get, $"/api/products/{1.ToGuid()}")
    .AssertOK()
    .AssertJsonFromResource("ReadTests.Product_Get_Found.res.json", "etag", "changelog");

// POST with event assertion
var created = Test.Http<Product>()
    .ExpectIdentifier()
    .ExpectETag()
    .ExpectChangeLogCreated()
    .ExpectJsonFromResource("ProductMutateTests.Create_Success.res.json")
    .ExpectSqlServerOutboxEvents(e => e
        .AssertWithValue("contoso", "contoso.products.product.created.v1"))
    .Run(HttpMethod.Post, "/api/products", product)
    .AssertCreated()
    .AssertLocationHeader(r => new Uri($"/api/products/{r!.Id}", UriKind.Relative))
    .Value!;

// Validation error
Test.Http()
    .Run(HttpMethod.Post, "/api/products", invalidProduct)
    .AssertBadRequest()
    .AssertErrors("Text is required.", "Price must be greater than or equal to zero.");

// Verify no events published
Test.Http()
    .ExpectNoSqlServerOutboxEvents()
    .Run(HttpMethod.Post, $"/api/baskets/{basketId}/checkout")
    .AssertBadRequest();
```

## Resource-Based JSON Assertions

Expected response bodies are stored as `.res.json` files in `Resources/`. Reference them by their dot-separated path within the Resources folder. Exclude volatile fields (etag, changelog timestamps, traceId) by passing them as additional params:

```csharp
.AssertJsonFromResource("ReadTests.Product_Get_Found.res.json", "etag", "changelog");
.AssertJsonFromResource("Basket_Checkout_Insufficient_Quantity.products.res.json", "traceid");
```

## HTTP Client Mocking

Define the mock request field at class level and configure its response inside each test method. Always call `.Verify()` after the test action to confirm the mock was actually invoked:

```csharp
// Class level
private MockHttpClientRequest _mockHttpReserveRequest = null!;

// OneTimeSetUp
_mockHttpReserveRequest = mcf.CreateClient("ProductsApi")
    .Request(HttpMethod.Post, "api/inventory/reserve");

// In test — success path
_mockHttpReserveRequest
    .WithJsonResourceBody("Basket_Checkout_Success.products.req.json")
    .Respond.With(HttpStatusCode.OK);

// In test — error path
_mockHttpReserveRequest.WithAnyBody()
    .Respond.WithJsonResource(
        "Basket_Checkout_Insufficient_Quantity.products.res.json",
        HttpStatusCode.BadRequest,
        System.Net.Mime.MediaTypeNames.Application.ProblemJson);

// After action
_mockHttpReserveRequest.Verify();
```

## Event Publisher Expectations

Use `ExpectSqlServerOutboxEvents` and `ExpectAzureServiceBusEvents` before `.Run` to assert that the operation produces the expected events:

```csharp
.ExpectSqlServerOutboxEvents(e => e
    .AssertWithValue("contoso", "contoso.products.product.created.v1"))
```

Use `ExpectNoSqlServerOutboxEvents()` when the operation must not produce any events (e.g., a failed checkout).

## Unit Tests

Unit tests use `Test.Scoped(test => { ... })` to get an isolated execution context:

```csharp
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
```

## NUnit Attributes

Use `[TestFixture]` on the class (inherited from base when using `WithApiTester`) and `[Test]` on individual test methods. Do not use `[TestCase]` for integration tests — use separate named methods for clarity.

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
