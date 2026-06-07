---
applyTo: "**/*.Test*/**/*.cs"
description: "Test conventions: test project types (Api/Unit/Subscribe/Relay), base classes, one-time setup patterns, and assertion helpers"
tags: ["testing", "unit-tests", "integration-tests", "test-helpers", "nunit"]
---

# Test Conventions

## NuGet / Project References

| Package | Key types provided |
|---|---|
| `CoreEx.UnitTesting` | Base testers and common helpers: `WithApiTester<T>`, `WithGenericTester<T>`, `Test.Http()`, `Test.Http<T>()`, `Test.Scoped()`, `Test.ScopedType<T>()`, `Test.ClearFusionCacheAsync()`, `Test.ReplaceHttpClientFactory()`; database helpers: `Test.MigrateSqlServerDataAsync<T>()`, `Test.UseExpectedSqlServerOutboxPublisher()`, `.ExpectSqlServerOutboxEvents()`, `.ExpectNoSqlServerOutboxEvents()`, `Test.MigratePostgresDataAsync<T>()`, `Test.UseExpectedPostgresOutboxPublisher()`, `.ExpectPostgresOutboxEvents()`, `.ExpectNoPostgresOutboxEvents()`; messaging helpers: `Test.UseExpectedAzureServiceBusPublisher()`, `Test.GetAndClearAzureServiceBusAsync()`; ASP.NET Core assertions: `.ExpectIdentifier()`, `.ExpectETag()`, `.ExpectChangeLogCreated()`, `.ExpectJsonFromResource()`, `.AssertCreated()`, `.AssertOK()`, `.AssertBadRequest()`, `.AssertErrors()`, `.AssertJsonFromResource()`, `.AssertLocationHeader()` |
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
4. Set up inter-domain HTTP mocks (for domains with cross-domain adapters).

**(SQL Server example):**
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

**(PostgreSQL example):**
```csharp
[OneTimeSetUp]
public async Task OneTimeSetUpAsync()
{
    await Test.MigratePostgresDataAsync<TestData>(DbMigration.ConfigureMigrationArgs).ConfigureAwait(false);
    await Test.ClearFusionCacheAsync().ConfigureAwait(false);

    Test.UseExpectedPostgresOutboxPublisher();
}
```

**Outbox assertion helpers are database-specific.** Use `UseExpectedPostgresOutboxPublisher` / `ExpectPostgresOutboxEvents` for PostgreSQL domains; use `UseExpectedSqlServerOutboxPublisher` / `ExpectSqlServerOutboxEvents` for SQL Server domains. Never mix them.

`DataResetFilterPredicate` in `DbMigration.ConfigureMigrationArgs` scopes the reset to the domain's own schema — multiple domains' test runs do not corrupt each other even when run concurrently.

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

// POST — PostgreSQL domain (Postgres outbox)
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

// POST — SQL Server domain (SQL Server outbox)
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

Mock request bodies use `.req.json`; mock response bodies from a downstream API use `.{domain}.res.json` (prefixed with the remote domain name by convention).

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

## Unit Tests — Validators

Unit tests are for logic with **no external dependencies**; any injected services are **mocked**. **Validators are the primary unit-test target** — they encode the most conditional logic. Application service orchestration is exercised by the host integration tests (`*.Test.Api` / `*.Test.Subscribe`), **not** here.

Maintain **one test class per validator**, under `*.Test.Unit/Validators/`, named `{Validator}Tests` and extending `WithGenericTester<EntryPoint>` — `EntryPoint` (in the template) configures the DI/host services the validator needs. Each `[Test]` runs inside `Test.Scoped(test => { ... })`. Invoke the validator exactly as the application does:

```csharp
public class ProductValidatorTests : WithGenericTester<EntryPoint>
{
    [Test]
    public void Empty_Required() => Test.Scoped(test =>
    {
        var p = new Product();
        ProductValidator.Default.AssertErrors(p,
            ("sku", "Sku is required."),
            ("text", "Text is required."),
            ("subCategory", "Sub-category is required."),
            ("unitOfMeasure", "Unit-of-measure is required."));
    });

    [Test]
    public void Success() => Test.Scoped(test =>
    {
        var p = new Product { Sku = "X", Text = "Test", SubCategoryCode = "XC", UnitOfMeasureCode = "EA", Price = 9.99m };
        ProductValidator.Default.AssertSuccess(p);
    });
}
```

- **`Validator<T, TSelf>`** (has a `Default`) → call `XxxValidator.Default`.
- **`Validator<T>`** (injected deps) → mock the dependency with `Mock<IXxx>` configured in `[OneTimeSetUp]`, then `new XxxValidator(_mock.Object)`:

```csharp
private readonly Mock<IProductRepository> _mock = new();

[OneTimeSetUp]
public void OneTimeSetUp() => _mock.Setup(x => x.GetForReservationAsync(It.IsAny<string[]>()))
    .ReturnsAsync(new Dictionary<string, ProductReserve> { ["P1"] = new() { UnitOfMeasureCode = "EA" } });

[Test]
public void Invalid_Product() => Test.Scoped(test =>
    new MovementRequestValidator(_mock.Object).AssertErrors(req,
        ("products.P2", "Product is non-stocked and therefore cannot be transacted.")));
```

### Asserting outcomes

- **`AssertSuccess(value)`** — asserts the value passes (no errors).
- **`AssertErrors(value, (jsonName, text)…)`** — asserts the **exact** set of expected errors. Each tuple is `("<json property name>", "<expected message>")`. **Order does not matter**, but **every** produced error must be accounted for (and there must be no extras). Use **JSON** property names (camelCase) with these path forms:
  - **Nested object** — dotted: `person.address.street`.
  - **Array / list item** — `[index]`: `person.addresses[0].street`.
  - **Dictionary** — `<dictionary>.<key>.<valueProperty>`: e.g. `products.P1.unitOfMeasure` means the `products` dictionary, key `P1`, and `unitOfMeasure` is a property of that entry's value. An error on the entry's value itself is just `<dictionary>.<key>` (e.g. `products.P1`). The **actual key** is the path segment — there is no `.value` segment. The literal `key` segment (e.g. `products.key`) appears **only** when the dictionary key is itself null/empty (so there is no key value to name) — it flags the missing/blank key to the consumer.

  If unsure of the exact path a rule produces, confirm it against the validator's actual output rather than guessing.

### Expected message text

Error text derives from the standard templates in [`ValidatorStrings.cs`](https://github.com/Avanade/CoreEx/blob/main/src/CoreEx.Validation/ValidatorStrings.cs) (unless a rule overrides the whole message via `.Error(...)`). Placeholders: `{0}` = the property's localized text (label), `{1}` = the value being validated, `{2}` onward = rule-specific extras (compare-to value, max length, etc.). Compose the expected string from the template + label + extras — e.g. `MandatoryFormat` "{0} is required." → `"Sku is required."`; `CompareGreaterThanEqualFormat` "{0} must be greater than or equal to {2}." with compare-text `"zero"` → `"Price must be greater than or equal to zero."`.

### Reference data in unit tests

Validators that use reference data (`.IsValid()`, etc.) resolve it through `EntryPoint.ReferenceDataServiceDecorator`, which loads the **real seeded data** so tests use representative values rather than invented ones. When a validator under test needs a ref-data type the decorator does not yet handle, add a case to its `GetAsync` switch:

```csharp
_ when type == typeof(Gender) => Task.FromResult((IReferenceDataCollection)jdr.Deserialize<GenderCollection>("hr.$^gender")!),
```

`Gender` is the reference-data **contract type**; `"hr.$^gender"` is the appropriately-cased `schema.$^table` key into the pre-configured seed data.

**Only test valid vs not-valid — not active/inactive.** A validator's reference-data rule is the `.IsValid()` extension; assert just the two outcomes: a **valid** code (use a real seeded code) and a **not-valid** code (use a code that is not in the seed — it fails naturally, no arranging required). Do **not** write tests targeting `IReferenceData.IsActive`/`IsInactive` — active/inactive handling is built into `IsValid()`, is framework behaviour we trust, and arranging inactive data just to prove it adds cost for no real coverage.

**Adding test-only entries with `ExtendForTesting`.** The seed YAML is the **production** data set, so it may not contain entries that exercise a validator rule depending on a reference-data entity's **extended property** (a custom property added to the ref-data type). Where such a value is needed, chain `ExtendForTesting(IEnumerable<IReferenceData>)` (from `UnitTestEx`) onto the deserialized collection to append test-only items. It mutates and returns the collection, so it composes inline in the decorator's `GetAsync`:

```csharp
_ when type == typeof(Category) => Task.FromResult((IReferenceDataCollection)jdr
    .Deserialize<CategoryCollection>("products.$^category")!
    .ExtendForTesting([new Category { Id = Runtime.NewId(), Code = "X", OtherProperty = false }])),
```

Give each added entry a **unique `Id`** so it cannot collide with a seeded row, using a value appropriate to the reference data's **identifier type**:
- `string` id (the default) → `Runtime.NewId()` (a unique GUID-as-string).
- `Guid` id → `Runtime.NewGuid()`.
- `int` (or other) id → a unique value of that type that won't clash with seeded ids.

This keeps the production seed clean while letting a test arrange a code carrying the exact extended-property values the scenario needs.

**Arrange via the `{Name}Code` property, not the typed navigation property.** When constructing the entity/contract under test, set the reference-data relationship using the string **`{Name}Code`** property (e.g. `new Employee { GenderCode = "M" }`), **not** the typed `{Name}` navigation property (e.g. `Gender`). The typed property resolves its value through the `ReferenceDataOrchestrator`, which is not wired up while arranging the input — so assigning it directly is unreliable. The validator's `.IsValid()` reads from the code regardless.

```csharp
// ✅ Correct — set the code variant when arranging
var e = new Employee { FirstName = "Jo", LastName = "Bloggs", GenderCode = "M" };

// ❌ Wrong — typed nav property depends on the orchestrator (not set during arrange)
var e = new Employee { FirstName = "Jo", LastName = "Bloggs", Gender = ... };
```

### Coverage

Add as many `[Test]` methods as needed for meaningful coverage — confirm both **error** and **success** outcomes. Exercise each rule's failure path, reference-data validity, and — importantly — **inter-field relationships** (`DependsOn`, conditional `When*` rules, cross-property compares) by constructing inputs that hit each branch. Prefer clear, scenario-named methods over `[TestCase]`. Aim for coverage that is genuinely representative rather than mirroring any prior hand-crafted set.

> For relay-style tests that need a named scoped type, use `Test.ScopedType<ExecutionContext>` (see Outbox Relay Host Tests).

---

## Subscribe Host Tests

Subscribe test classes extend `WithApiTester<Program>` over the subscriber host. The `[OneTimeSetUp]` migrates/seeds the domain DB and clears FusionCache, just like an API test. Subscribe hosts **do** have FusionCache — they are full application-layer consumers that need caching for reference data and idempotency.

```csharp
public class ProductModifySubscriberTests : WithApiTester<YourDomain.Subscribe.Program>
{
    [OneTimeSetUp]
    public async Task OneTimeSetUpAsync()
    {
        await Test.MigrateSqlServerDataAsync<TestData>(DbMigration.ConfigureMigrationArgs).ConfigureAwait(false);
        await Test.ClearFusionCacheAsync().ConfigureAwait(false);

        Test.UseExpectedSqlServerOutboxPublisher();
    }
}
```

---

## Outbox Relay Host Tests

Relay tests extend `WithApiTester<Program>` over the relay host. Use `Test.ScopedType<ExecutionContext>` to write events directly to the outbox, wait for the relay background service to forward them, then assert via `Test.GetAndClearAzureServiceBusAsync()`.

```csharp
public class RelayTests : WithApiTester<YourDomain.Outbox.Relay.Program>
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
- Do not use `UseExpectedSqlServerOutboxPublisher` / `ExpectSqlServerOutboxEvents` in PostgreSQL domain tests — use the Postgres equivalents.
- Do not use `UseExpectedPostgresOutboxPublisher` / `ExpectPostgresOutboxEvents` in SQL Server domain tests — use the SQL Server equivalents.
- Do not call `ClearFusionCacheAsync()` in Outbox Relay host tests — relay hosts have no cache.
- Do not test inter-domain HTTP calls against a real API — always mock with `MockHttpClientFactory`.
- Do not call `Test.ReplaceHttpClientFactory()` inside individual tests — configure it once in `[OneTimeSetUp]`.
- Do not use `FluentAssertions` — use `AwesomeAssertions` (the `AwesomeAssertions` NuGet package).
- Do not omit `.Verify()` after a `MockHttpClientRequest` action — it confirms the mock was actually invoked.
- Do not set a typed reference-data navigation property (e.g. `Gender`) when arranging a test input — set the `{Name}Code` string (e.g. `GenderCode = "M"`); the typed property depends on the `ReferenceDataOrchestrator`, which is not set during arrange.
- Do not write validator tests for reference-data `IsActive`/`IsInactive` — assert only valid vs not-valid via `.IsValid()` (a not-valid case just uses an unseeded code); active/inactive is trusted framework behaviour. Reserve `ExtendForTesting` for rules that depend on a ref-data **extended property**.

## Further Reading

- [Testing Guide](https://github.com/Avanade/CoreEx/blob/main/samples/docs/testing.md) — full test architecture, data seeding, schema isolation, and E2E runner.
- [Pattern Catalog](https://github.com/Avanade/CoreEx/blob/main/samples/docs/patterns.md) — pattern catalog linking testing patterns to layer docs.
