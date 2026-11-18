# CoreEx.UnitTesting

> Provides the complete CoreEx unit- and integration-testing toolkit: fluent expectations, event-capture assertions, and convenience extensions that bridge UnitTestEx with every major CoreEx subsystem.

## Overview

`CoreEx.UnitTesting` is the single testing package that covers the entire CoreEx ecosystem. Rather than shipping a separate `CoreEx.UnitTesting.Events`, `CoreEx.UnitTesting.SqlServer`, or similar per-feature test packages, all capabilities are consolidated here deliberately. Unit- and integration-testing packages are never deployed to production, so the additional transitive references carry no runtime cost; consolidating into one package eliminates version-alignment friction, reduces `<PackageReference>` noise in test projects, and makes the full test surface immediately discoverable.

The package extends [UnitTestEx](https://github.com/Avanade/UnitTestEx) — the CoreEx-recommended test host — with CoreEx-specific registration helpers, expectations, and assertion extensions. These inject an `EventPublisherDecorator` into DI to capture successful publications while delegating to the real publisher by default; `bypassPassThrough: true` explicitly selects a no-op inner publisher. The one-off setup hook (`UnitTestExOneOffTestSetUp`) wires CoreEx defaults (`JsonDefaults`, `AuthenticationUser`, `ValidationException` error matching) into the UnitTestEx infrastructure automatically when the assembly is loaded.

`CoreEx.Data.Json`'s `JsonDataReader` — a JSON-to-`JsonNode` bridge that supports parameterised `^token`-style substitutions for generated values (ids, timestamps, tenant/user context) and multiple naming conventions — is a transitive dependency (via `CoreEx.Data`), not owned by this package. It makes it straightforward to seed SQL Server, PostgreSQL, or Cosmos DB test databases directly from `data.yaml` or JSON resource files.

## Motivation

- A single package covers all CoreEx test concerns; test projects need only one `<PackageReference>` to access every helper regardless of which CoreEx features the service under test uses.
- Keeping test support consolidated maximises discoverability: developers find assertions for events, reference data, outbox patterns, caching, validation, and HTTP all in one place rather than hunting across multiple packages.
- Automatic one-off set-up ensures CoreEx defaults (serialization, user context, `ValidationException` error mapping) are applied consistently across all test frameworks (NUnit, xUnit, MSTest) without manual wiring.
- `EventPublisherDecorator` observes the configured publisher without changing production code. Pass-through tests need that publisher's infrastructure; explicit bypass mode enables isolated publishing assertions.

## Key capabilities

- 🧪 **Unified test package**: one `<PackageReference>` covers testing support for all CoreEx subsystems — events, outbox (SQL Server and PostgreSQL), Azure Service Bus, caching (FusionCache), validation, HTTP, ETag, identifier, and change-log assertions.
- 📦 **Automatic CoreEx wiring**: `UnitTestExOneOffTestSetUp` registers CoreEx JSON defaults, the environment user, and `ValidationException` error assertion handling with UnitTestEx on first assembly load.
- ✅ **Fluent value expectations**: `UnitTestExExpectations` static class extends `IValueExpectations<TValue, TSelf>` and `IExpectations<TSelf>` with `ExpectIdentifier`, `ExpectETag`, `ExpectChangeLogCreated`, `ExpectChangeLogUpdated`, and `IgnorePaths` to assert domain-entity lifecycle concerns.
- 💬 **Event-capture and assertion**: `UseExpectedEventPublisher` replaces the real publisher with `EventPublisherDecorator` at test-host setup time; `ExpectEvents`/`ExpectNoEvents` plus publisher-specific overloads (`ExpectSqlServerOutboxEvents`, `ExpectPostgresOutboxEvents`, `ExpectAzureServiceBusEvents`) declare and then verify published events as `CloudEvent` JSON comparisons.
- ⚡ **Publisher-specific outbox helpers**: dedicated `ExpectSqlServerOutboxEvents`, `ExpectNoSqlServerOutboxEvents`, `ExpectPostgresOutboxEvents`, `ExpectNoPostgresOutboxEvents`, `ExpectAzureServiceBusEvents`, and `ExpectNoAzureServiceBusEvents` extension methods target each outbox/publisher by its registered service key.
- 🔄 **Service Bus emulator setup**: `ResetLocalAzureServiceBusAsync` validates an explicit development-emulator loopback connection, waits for management readiness, then resets the caller-specified entities. It is destructive and does not replace the caller's fixture opt-in or broker-isolation responsibility.
- 🔧 **Broad extension surface**: `UnitTestExExtensions` adds `Scoped`/`ExecutionContext` scoping helpers, `CreateCloudEventFrom` event builders, `AssertProblemDetails` HTTP response assertions, `ClearFusionCacheAsync` cache reset, and validator-level `AssertSuccess`/`AssertErrors` shortcuts directly on `TesterBase` and `IValidator<T>`.
- 📝 **Validation shortcuts**: `AssertSuccess` and `AssertErrors` extension methods execute a `CoreEx.Validation.IValidator<T>` and assert the outcome inline, using AwesomeAssertions for readable failure messages.
- 🔄 **FusionCache test reset**: `ClearFusionCacheAsync` clears the registered `IFusionCache` between test runs to prevent state bleed in cached reference-data or other cache-backed scenarios.

## Native message expectations

Use `ExpectEvents(e => e.AssertNativeMessage(destination, expectedEventData))`, or the provider-specific outbox/Service Bus expectation overload, to compare a native message's internal envelope, payload and durable options. The expected `EventData` must have native representation. Opaque bytes compare through their Base64 storage form; normal JSON compares as JSON. Native payloads retain ETags unless explicitly removed, unlike the default CE formatter policy. The existing ignored metadata/data paths still apply.

`ToServiceBusReceivedMessage` honors declarative native mapping but does not execute `ServiceBusPublisher.ConfigureMessage` or publisher session assignment. For final broker headers, byte identity, callback behavior and session policy, assert actual received messages in relay/transport tests.

### Local Service Bus emulator setup

Use the local reset helper only for an isolated Azure Service Bus emulator. It requires `UseDevelopmentEmulator=true`, a namespace-level shared-key connection and a `localhost`/loopback endpoint; it waits for the management health endpoint before resetting. The default management port is `5300` and the default readiness timeout is one minute:

```csharp
var connectionString = configuration.GetConnectionString("ServiceBus")
    ?? throw new InvalidOperationException("The ServiceBus connection string is required.");
await UnitTestExExtensions.ResetLocalAzureServiceBusAsync(
    connectionString,
    ServiceBus.GetQueues(),
    ServiceBus.GetTopicsAndSubscriptions(),
    cancellationToken: cancellationToken).ConfigureAwait(false);
```

The method deletes and recreates only the supplied entities, discarding their messages. Manual setup fixtures must independently require deliberate test selection and an environment-variable opt-in before calling it, and must ensure listeners, producers, consumers and competing setup runs are stopped. Use the existing `ResetAzureServiceBusAsync` methods for other Azure Service Bus environments; those methods do not apply the emulator-only connection guard/readiness polling.

## File and Blob component tests

Test application-owned rules, mappings, configuration and failure decisions, not every CoreEx/Azure SDK/.NET permutation. Pure processor tests can supply a fresh `MemoryStream` over immutable fixture bytes; they do not need Blob clients. For Blob-boundary component tests, `CoreEx.UnitTesting.Blobs` supplies SDK-level doubles that work with the real `BlobReadSource`, `BlobJsonlReport` and `BlobCsvReport`:

```csharp
var source = new StubBlobClient(new BinaryData("sku,quantity\nSKU-1,2\n"));
var reports = new RecordingBlobContainerClient();
var selected = await BlobReadSource.CreateAsync(source, cancellationToken);
var report = new BlobJsonlReport(reports.GetAppendBlobClient("attempt.jsonl"));
await report.StartAsync(cancellationToken);
await report.WriteAsync(new { kind = "diagnostic", code = "application.rule" }, cancellationToken);
await report.CompleteAsync(new { kind = "terminal" }, "ContentFailed", cancellationToken);
var recorded = reports.Blobs["attempt.jsonl"];
// Assert application records from recorded.Content and the outcome in recorded.Metadata.
```

Add `CoreEx.UnitTesting.Blobs`, `CoreEx.Azure.Storage.Blobs` and `Azure.Storage.Blobs.Specialized` to test global imports for this example. In a host test, supply the source to the function and replace its `BlobContainerClient` registration with `reports`; keep the production report adapter and assert its business records rather than writing another storage protocol simulator.

`StubBlobClient` copies fixture bytes and returns a fresh read stream each time with a fixed URI/revision. Use `StubBlobClient.FromResource("catalog-valid.csv")` for a fixture embedded in the calling test assembly, or `StubBlobClient.FromResource<TestData>("catalog-valid.csv")` to select the assembly explicitly, such as a shared `Test.Common` project. Both overloads preserve the resource bytes and accept optional URI/ETag overrides. The stub's `BeforeGetProperties`/`BeforeRead` actions support failure injection. `RecordingBlobContainerClient` captures reports by name; each `RecordedBlob` exposes opaque `Content`, `Blocks`, `ContentType` and `Metadata`. Its `BeforeCreate`/`BeforeAppend`/`BeforeSetMetadata` actions receive the report name and can count calls or throw. Existing report names cannot be overwritten.

These are deliberately buffered, sequential component-test helpers for small fixtures, not a file-testing framework or Azure emulator. They do not model conditional ETag/append-position enforcement, ranges, SDK retries, concurrent writers or trigger delivery. Use representative application scenarios and a small Azurite/Core Tools runtime proof when those boundaries matter.

## Event publishing tests

Register `services.UseExpectedAzureServiceBusPublisher()` (or `UseExpectedEventPublisher(serviceKey)`) to observe the real publisher. Choose `bypassPassThrough: true` explicitly for isolated tests without its infrastructure. Successful publications accumulate across child scopes in the same tester/request; rollback removes only the calling decorator's captures. A failed publication is not recorded as successful, even if a real broker may have accepted part of its batch.

On a Functions/non-HTTP owning tester, `tester.OnEventPublish(serviceKey, action)` can count attempts or throw before a selected publish. In Functions tests, create the trigger tester before registering the hook: trigger creation resets shared state. HTTP request testers retain their request-specific overload. Use a fresh tester per independent test. Assert commands through `ExpectAzureServiceBusEvents`/`ExpectEvents`; reserve final wire/custom transport assertions for a small transport/runtime test.

## Key types

| Type | Description |
|------|-------------|
| **[`UnitTestExOneOffTestSetUp`](./UnitTestExOneOffTestSetUp.cs)** | Internal one-off UnitTestEx initializer; configures CoreEx JSON serialization defaults, the environment user name, and `ValidationException` error-assertion integration automatically on assembly load. |
| **[`UnitTestExExpectations`](./UnitTestExExpectations.cs)** | Static partial class; aggregates all CoreEx-specific value and tester expectation extension methods: `ExpectIdentifier`, `ExpectETag`, `ExpectChangeLogCreated`, `ExpectChangeLogUpdated`, `IgnorePaths`, and the full event and outbox expectation surface. |
| **[`UnitTestExExtensions`](./UnitTestExExtensions.cs)** | Static partial class; aggregates all CoreEx-specific `TesterBase` and `IValidator<T>` extension methods: `Scoped`, `CreateCloudEventFrom`, `AssertProblemDetails`, `ClearFusionCacheAsync`, `UseExpectedEventPublisher`, `AssertSuccess`, and `AssertErrors`. |


### Aspire external infrastructure

`AddExternalConnectionString` (in the `UnitTestEx` namespace, alongside the other Aspire extensions) wraps a secret
parameter sourced from `ConnectionStrings:{name}` in a visible connection-string resource:

```csharp
var database = builder.AddExternalConnectionString("Cosmos", endpointKey: "AccountEndpoint").WithIconName("DatabaseMultiple");
var redis = builder.AddExternalConnectionString("redis").WithIconName("Database");
builder.AddProject<Projects.Customers_Api>("customers-api").WithReference(database).WithReference(redis);
```

Use `"Postgres"` or `"SqlServer"` without `endpointKey` for relational providers. Plain `AddConnectionString(name)`
creates parameter-only resources omitted from Aspire 13.5.4's Graph/Table views. Compose retains infrastructure
lifecycle ownership; the resource's running status indicates resolved configuration, not connectivity. Hosts still
perform readiness checks. Cosmos's configured endpoint is exposed as a credential-free HTTP/HTTPS URL for dashboard
peer matching; missing, invalid, non-HTTP, or credential-bearing endpoint URLs fail explicitly. Dashboard labels
depend on span endpoint attributes, not database-provider names; explicit ports help avoid ambiguous local matches.

## Namespaces

| Namespace | Description | Documentation |
|-----------|-------------|---------------|
| **`CoreEx.UnitTesting.Events`** | `EventExpectationsConfig`, `EventExpectationAssertor`, `EventExpectations`, and `EventPublisherDecorator`; the infrastructure for capturing and asserting published events in tests. | [📖 README](./Events/README.md) |
| **`CoreEx.UnitTesting.Blobs`** | `StubBlobClient`, `RecordingBlobContainerClient` and `RecordedBlob`; bounded input fixtures and opaque append-report capture without live storage or HTTP simulation. | [Component testing](#file-and-blob-component-tests) |

## Related namespaces

- **[`CoreEx`](../CoreEx/README.md)** - Provides `ExecutionContext`, `ValidationException`, `EventData`, and `JsonDefaults` wired in by `UnitTestExOneOffTestSetUp`.
- **[`CoreEx.Data`](../CoreEx.Data/Json/README.md)** - `JsonDataReader` (in the `CoreEx.Data.Json` namespace) — a transitive dependency, not owned by this package — parses JSON/YAML with `^token` placeholder substitution to seed SQL Server, PostgreSQL, or Cosmos DB test databases directly from `data.yaml`/JSON resource files.
- **[`CoreEx.Events`](../CoreEx.Events/README.md)** - Defines `IEventPublisher` and `EventData` that `EventPublisherDecorator` wraps for event capture.
- **[`CoreEx.Azure.Messaging.ServiceBus`](../CoreEx.Azure.Messaging.ServiceBus/README.md)** - `ServiceBusPublisher` whose service key is targeted by `ExpectAzureServiceBusEvents`.
- **[`CoreEx.Database.SqlServer`](../CoreEx.Database.SqlServer/README.md)** - `SqlServerOutboxPublisher` whose service key is targeted by `ExpectSqlServerOutboxEvents`.
- **[`CoreEx.Database.Postgres`](../CoreEx.Database.Postgres/README.md)** - `PostgresOutboxPublisher` whose service key is targeted by `ExpectPostgresOutboxEvents`.
- **[`CoreEx.Cosmos`](../CoreEx.Cosmos/README.md)** - `CosmosDbBatch`/`CosmosDbContainerExtensions` (in the `CoreEx.Cosmos.Extended` namespace) — a transitive dependency, not owned by this package — provision/reset a Cosmos DB container and import raw JSON for test seeding.
- **[`CoreEx.Caching.FusionCache`](../CoreEx.Caching.FusionCache/README.md)** - `IFusionCache` implementation cleared by `ClearFusionCacheAsync`.
- **[`CoreEx.Validation`](../CoreEx.Validation/README.md)** - `IValidator<T>` and `ValidationException` targeted by the `AssertSuccess`/`AssertErrors` and one-off setup extensions.

## Additional Resources

- [UnitTestEx](https://github.com/Avanade/UnitTestEx) - The underlying test-host framework that `CoreEx.UnitTesting` extends; provides `TesterBase`, `ApiTester`, `GenericTester`, `IExpectations`, and the framework-agnostic assertion infrastructure.
- [AwesomeAssertions](https://github.com/AwesomeAssertions/AwesomeAssertions) - Fluent assertion library used by the validation shortcuts and internal assertion helpers.

## AI Usage Guide

An [`AGENTS.md`](./AGENTS.md) file is included with this package. AI coding assistants (GitHub Copilot, Claude, Cursor, etc.) that support workspace-injected package documentation will automatically surface concise usage guidance, code examples, and `Do Not` rules for this package without requiring a local CoreEx checkout.
