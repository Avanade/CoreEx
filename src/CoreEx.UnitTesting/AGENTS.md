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

## Validation Assertions

```csharp
// Assert validator passes
await ProductValidator.Default.AssertSuccess(new Product { Sku = "SKU001" });

// Assert validator fails with specific field errors
await ProductValidator.Default.AssertErrors(
    new Product { Sku = "" },
    ("Sku", "Sku is required."));
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

## Further Reading

- [README](./README.md) — full expectations, outbox helpers, `JsonDataReader`, and `UnitTestExExtensions` API reference.
- [UnitTestEx](https://github.com/Avanade/UnitTestEx) — the underlying test-host framework.
- [AwesomeAssertions](https://github.com/AwesomeAssertions/AwesomeAssertions) — fluent assertion library used internally.
- [Testing](../../samples/docs/testing.md) — comprehensive real-world guide covering unit, integration, API, Subscribe, and Relay test patterns with concrete examples from the sample solution.
- [Patterns](../../samples/docs/patterns.md) — test-specific patterns including outbox assertion, mock HTTP client, inter-domain mock strategy, and `FusionCache` reset.
