# CoreEx.UnitTesting — AI Usage Guide

The single test-support package for the entire CoreEx ecosystem. One `<PackageReference>` covers events, outbox, Service Bus, caching, validation, HTTP, and all assertion helpers.

## Project Reference

```xml
<PackageReference Include="CoreEx.UnitTesting" Version="..." />
```

No additional CoreEx test packages are needed — this package covers everything.

## Test Class Shape (NUnit)

```csharp
[TestFixture]
public class OrderServiceTest : UnitTestBase
{
    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        await Test.ClearFusionCacheAsync().ConfigureAwait(false);
        // seed your test database here
    }

    [Test]
    public void Create_Order_Published_To_Outbox()
        => Test.ScopedType<OrderService>()
               .ExpectChangeLogCreated()
               .ExpectIdentifier()
               .ExpectSqlServerOutboxEvents(new CloudEvent { ... })
               .Run(s => s.CreateAsync(new Order { ... }));
}
```

## Event / Outbox Expectations

Use the database-specific expectation method that matches your domain's persistence provider. Do not mix SQL Server and PostgreSQL helpers.

```csharp
// SQL Server outbox
.ExpectSqlServerOutboxEvents(new CloudEvent { Subject = "contoso.orders.order.created.v1", ... })
.ExpectNoSqlServerOutboxEvents()

// PostgreSQL outbox
.ExpectPostgresOutboxEvents(new CloudEvent { Subject = "contoso.orders.order.created.v1", ... })
.ExpectNoPostgresOutboxEvents()

// Azure Service Bus direct publisher (no outbox)
.ExpectAzureServiceBusEvents(new CloudEvent { ... })
.ExpectNoAzureServiceBusEvents()
```

### Picking the per-event assertor: value vs no-value events

```csharp
// Value-carrying events (Create/Update): reconstructs the expected event payload from the tester's own
// returned value (AssertArgs.Value).
.ExpectSqlServerOutboxEvents(e => e.AssertWithValue("contoso", "contoso.orders.order.created.v1"))

// AssertWithValue factory overload (rare): supply the expected payload directly instead of relying on the
// tester's returned value - for a host-less GenericTester<T> (no IValueExpectations<TValue>, so there is no
// AssertArgs.Value to reconstruct from), or when the published event's payload legitimately differs from what
// the operation returns.
.ExpectSqlServerOutboxEvents(e => e.AssertWithValue(() => expectedPayload, "contoso", "contoso.orders.order.created.v1"))

// No-value events (Delete, or any 204 No Content): there is no returned value to reconstruct a payload from at
// all, so AssertMetadata compares metadata only - destination + title/subject + the key (e.g. the deleted id).
.ExpectSqlServerOutboxEvents(e => e.AssertMetadata("contoso", "contoso.orders.order.deleted", deletedId))
```

Reach for the plain `AssertWithValue(destination, subject)` first for value-carrying events; only use the `valueFactory` overload when the returned value genuinely isn't the event's payload. Use `AssertMetadata` for no-value events — `AssertWithValue` has nothing to reconstruct from in that case.

## Validation Assertions

```csharp
// Assert validator passes
await ProductValidator.Default.AssertSuccessAsync(new Product { Sku = "SKU001" });

// Assert validator fails with specific field errors
await ProductValidator.Default.AssertErrorsAsync(
    new Product { Sku = "" },
    ("Sku", "Sku is required."));
```

## Testing That an Expectation Fails

```csharp
// Use NUnit's Assert.Throws — NOT AwesomeAssertions' Should().Throw() — to assert that an
// Expect*/Assert* check itself correctly fails (e.g. writing a test for a validator's negative path).
Assert.Throws<AssertionException>(() => Test.Http<Order>()
    .ExpectIdentifier()
    .Run(HttpMethod.Post, "api/orders", invalidOrder)
    .AssertCreated());
```

## Subscribe / Relay Host Tests

```csharp
// Subscribe host
[TestFixture]
public class OrderSubscriberTest : UnitTestBase
{
    [Test]
    public void Receive_OrderCreated()
        => Test.Type<OrderCreatedSubscriber>()
               .ExpectNoSqlServerOutboxEvents()
               .Run(s => s.ReceiveAsync(CreateCloudEvent("contoso.orders.order.created.v1", order)));
}
```

## JSON Seed Data

```csharp
// Load seed data from embedded YAML with token substitution
var data = await JsonDataReader.ParseYamlAsync("Resources/data.yaml");
await db.SeedAsync(data);
```

## ExecutionContext Scoping

```csharp
Test.ScopedType<OrderService>()
    .WithUser("test@contoso.com")
    .Run(s => s.GetAsync(id));
```

## Do Not

- Do not add separate per-feature test packages (e.g. `CoreEx.UnitTesting.Events`) — they do not exist; all test helpers are in this package.
- Do not use `ExpectSqlServerOutboxEvents` for a PostgreSQL domain or vice versa.
- Do not call `PublishAsync()` in tests — the `EventPublisherDecorator` (registered by `UseExpectedEventPublisher`) captures events automatically.
- Do not forget `await Test.ClearFusionCacheAsync()` in `[OneTimeSetUp]` for tests involving cached reference data.
- Do not use FluentAssertions — the CoreEx test framework uses AwesomeAssertions (`AwesomeAssertions` NuGet package).
- Do not wrap an `Expect*`/`Assert*` check in `Action act = () => ...; act.Should().Throw<Exception>();` to test that it fails — UnitTestEx's `Implementor.AssertFail` marks the NUnit test as failed the moment it fires, regardless of whether your code later catches the resulting exception. Use NUnit's `Assert.Throws<AssertionException>(...)` instead (see "Testing That an Expectation Fails" above).

## Further Reading

- [README](./README.md) — full expectations, outbox helpers, `JsonDataReader`, and `UnitTestExExtensions` API reference.
- [UnitTestEx](https://github.com/Avanade/UnitTestEx) — the underlying test-host framework.
- [AwesomeAssertions](https://github.com/AwesomeAssertions/AwesomeAssertions) — fluent assertion library used internally.
- [Testing](../../samples/docs/testing.md) — comprehensive real-world guide covering unit, integration, API, Subscribe, and Relay test patterns with concrete examples from the sample solution.
- [Patterns](../../samples/docs/patterns.md) — test-specific patterns including outbox assertion, mock HTTP client, inter-domain mock strategy, and `FusionCache` reset.
