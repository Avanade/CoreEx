# CoreEx.UnitTesting

> Provides the complete CoreEx unit- and integration-testing toolkit: fluent expectations, event-capture assertions, and convenience extensions that bridge UnitTestEx with every major CoreEx subsystem.

## Overview

`CoreEx.UnitTesting` is the single testing package that covers the entire CoreEx ecosystem. Rather than shipping a separate `CoreEx.UnitTesting.Events`, `CoreEx.UnitTesting.SqlServer`, or similar per-feature test packages, all capabilities are consolidated here deliberately. Unit- and integration-testing packages are never deployed to production, so the additional transitive references carry no runtime cost; consolidating into one package eliminates version-alignment friction, reduces `<PackageReference>` noise in test projects, and makes the full test surface immediately discoverable.

The package extends [UnitTestEx](https://github.com/Avanade/UnitTestEx) — the CoreEx-recommended test host — with CoreEx-specific registration helpers, expectations, and assertion extensions. These operate by injecting an `EventPublisherDecorator` into the DI container so published events are captured during a test run and then asserted after the fact, without touching the real publisher. The one-off setup hook (`UnitTestExOneOffTestSetUp`) wires CoreEx defaults (`JsonDefaults`, `AuthenticationUser`, `ValidationException` error matching) into the UnitTestEx infrastructure automatically when the assembly is loaded.

`CoreEx.Data.Json`'s `JsonDataReader` — a JSON-to-`JsonNode` bridge that supports parameterised `^token`-style substitutions for generated values (ids, timestamps, tenant/user context) and multiple naming conventions — is a transitive dependency (via `CoreEx.Data`), not owned by this package. It makes it straightforward to seed SQL Server, PostgreSQL, or Cosmos DB test databases directly from `data.yaml` or JSON resource files.

## Motivation

- A single package covers all CoreEx test concerns; test projects need only one `<PackageReference>` to access every helper regardless of which CoreEx features the service under test uses.
- Keeping test support consolidated maximises discoverability: developers find assertions for events, reference data, outbox patterns, caching, validation, and HTTP all in one place rather than hunting across multiple packages.
- Automatic one-off set-up ensures CoreEx defaults (serialization, user context, `ValidationException` error mapping) are applied consistently across all test frameworks (NUnit, xUnit, MSTest) without manual wiring.
- `EventPublisherDecorator` allows event expectations to be declared and asserted against the real outbox/publisher pipeline without requiring live infrastructure.

## Key capabilities

- 🧪 **Unified test package**: one `<PackageReference>` covers testing support for all CoreEx subsystems — events, outbox (SQL Server and PostgreSQL), Azure Service Bus, caching (FusionCache), validation, HTTP, ETag, identifier, and change-log assertions.
- 📦 **Automatic CoreEx wiring**: `UnitTestExOneOffTestSetUp` registers CoreEx JSON defaults, the environment user, and `ValidationException` error assertion handling with UnitTestEx on first assembly load.
- ✅ **Fluent value expectations**: `UnitTestExExpectations` static class extends `IValueExpectations<TValue, TSelf>` and `IExpectations<TSelf>` with `ExpectIdentifier`, `ExpectETag`, `ExpectChangeLogCreated`, `ExpectChangeLogUpdated`, and `IgnorePaths` to assert domain-entity lifecycle concerns.
- 💬 **Event-capture and assertion**: `UseExpectedEventPublisher` replaces the real publisher with `EventPublisherDecorator` at test-host setup time; `ExpectEvents`/`ExpectNoEvents` plus publisher-specific overloads (`ExpectSqlServerOutboxEvents`, `ExpectPostgresOutboxEvents`, `ExpectAzureServiceBusEvents`) declare and then verify published events as `CloudEvent` JSON comparisons.
- ⚡ **Publisher-specific outbox helpers**: dedicated `ExpectSqlServerOutboxEvents`, `ExpectNoSqlServerOutboxEvents`, `ExpectPostgresOutboxEvents`, `ExpectNoPostgresOutboxEvents`, `ExpectAzureServiceBusEvents`, and `ExpectNoAzureServiceBusEvents` extension methods target each outbox/publisher by its registered service key.
- 🔧 **Broad extension surface**: `UnitTestExExtensions` adds `Scoped`/`ExecutionContext` scoping helpers, `CreateCloudEventFrom` event builders, `AssertProblemDetails` HTTP response assertions, `ClearFusionCacheAsync` cache reset, and validator-level `AssertSuccess`/`AssertErrors` shortcuts directly on `TesterBase` and `IValidator<T>`.
- 📝 **Validation shortcuts**: `AssertSuccess` and `AssertErrors` extension methods execute a `CoreEx.Validation.IValidator<T>` and assert the outcome inline, using AwesomeAssertions for readable failure messages.
- 🔄 **FusionCache test reset**: `ClearFusionCacheAsync` clears the registered `IFusionCache` between test runs to prevent state bleed in cached reference-data or other cache-backed scenarios.

## Key types

| Type | Description |
|------|-------------|
| **[`UnitTestExOneOffTestSetUp`](./UnitTestExOneOffTestSetUp.cs)** | Internal one-off UnitTestEx initializer; configures CoreEx JSON serialization defaults, the environment user name, and `ValidationException` error-assertion integration automatically on assembly load. |
| **[`UnitTestExExpectations`](./UnitTestExExpectations.cs)** | Static partial class; aggregates all CoreEx-specific value and tester expectation extension methods: `ExpectIdentifier`, `ExpectETag`, `ExpectChangeLogCreated`, `ExpectChangeLogUpdated`, `IgnorePaths`, and the full event and outbox expectation surface. |
| **[`UnitTestExExtensions`](./UnitTestExExtensions.cs)** | Static partial class; aggregates all CoreEx-specific `TesterBase` and `IValidator<T>` extension methods: `Scoped`, `CreateCloudEventFrom`, `AssertProblemDetails`, `ClearFusionCacheAsync`, `UseExpectedEventPublisher`, `AssertSuccess`, and `AssertErrors`. |

## Namespaces

| Namespace | Description | Documentation |
|-----------|-------------|---------------|
| **`CoreEx.UnitTesting.Events`** | `EventExpectationsConfig`, `EventExpectationAssertor`, `EventExpectations`, and `EventPublisherDecorator`; the infrastructure for capturing and asserting published events in tests. | [📖 README](./Events/README.md) |

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