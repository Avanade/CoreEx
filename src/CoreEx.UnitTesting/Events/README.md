# CoreEx.UnitTesting.Events

> Provides the event-capture and assertion infrastructure for CoreEx unit tests: the `EventPublisherDecorator` that intercepts published events during a test run, and the `EventExpectations`/`EventExpectationsConfig`/`EventExpectationAssertor` types that declare and verify them.

## Overview

`CoreEx.UnitTesting.Events` implements the two-phase event-testing pattern used across all CoreEx publisher integrations. In the first phase (test setup), `EventPublisherDecorator` is substituted for the real `IEventPublisher` in the DI container via `UseExpectedEventPublisher`; it delegates all `Add`/`SendAsync` calls to the original publisher while also recording each `DestinationEvent` in `TestSharedState`. In the second phase (post-run assertion), `EventExpectations<TSelf>` retrieves the captured events and hands them to each registered `EventExpectationAssertor`, which compares the actual `CloudEvent` JSON against the expected event (from a resource file or a factory delegate), ignoring configurable paths such as `id`, `time`, `subject`, `data.id`, `data.changelog`, and `data.etag` by default.

`EventExpectationsConfig` is the fluent configuration surface exposed to test authors; it controls which paths to ignore, whether to assert that no events were published, whether to assert all events in sequence or as an unordered set, and how to supply expected event content (embedded resource names, `EventData` factories, or fully custom `CloudEvent` comparators). The config is populated by the `ExpectEvents`/`ExpectNoEvents` extension method family defined in `UnitTestExExpectations`.

## Key capabilities

- 🔧 **Non-invasive event capture**: `EventPublisherDecorator` wraps the real publisher transparently — all actual publish operations proceed to the underlying outbox or Service Bus publisher while a copy is stored in `TestSharedState` for later assertion.
- 💬 **Sequenced or unordered event assertions**: `EventExpectationsConfig` supports asserting events in the declared order or as an unordered set; each assertion compares the full `CloudEvent` JSON representation.
- 📝 **Flexible expected-event sources**: `EventExpectationAssertor` accepts an expected `CloudEvent` from an embedded JSON resource, from an `EventData` factory delegate, or from a fully custom `Action<AssertArgs, DestinationEvent>` comparator.
- ✅ **Configurable path ignoring**: `EventExpectationsConfig.DefaultMetadataPathsToIgnore` and `DefaultDataPathsToIgnore` pre-configure the noisiest volatile paths (`id`, `time`, `data.etag`, etc.); individual tests can extend or replace this list.

## Key types

| Type | Description |
|------|-------------|
| **[`EventPublisherDecorator`](./EventPublisherDecorator.cs)** | `IEventPublisher` decorator that forwards all calls to the wrapped inner publisher while recording each published `DestinationEvent` in `TestSharedState` under a configurable service key. |
| **[`EventExpectationsConfig`](./EventExpecationsConfig.cs)** | Fluent configuration for a single publisher's event expectations: paths to ignore, expected-no-events flag, sequenced assertors list, and an optional catch-all `AssertAllEvents` action. Exposes `DefaultMetadataPathsToIgnore` and `DefaultDataPathsToIgnore` as mutable static lists. |
| **[`EventExpectationAssertor`](./EventExpectationAssertor.cs)** | Performs the actual JSON comparison of one expected event against the captured `DestinationEvent`; resolves expected content from a resource file, an `EventData` factory, or a custom delegate; respects the configured ignore paths. |
| **[`EventExpectations`](./EventExpectations.cs)** | `IExpectationExtension` implementation that manages the list of `EventExpectationsConfig` instances per service key and drives post-run assertion for all registered publishers. |

## Related namespaces

- **[`CoreEx.UnitTesting`](../README.md)** - Root namespace; `UnitTestExExpectations` and `UnitTestExExtensions` are the primary consumer-facing entry points that create and wire up the types defined here.
- **[`CoreEx.Events`](../../CoreEx.Events/README.md)** - Defines `IEventPublisher`, `EventData`, and `IEventFormatter` that `EventPublisherDecorator` wraps and `EventExpectationAssertor` uses to convert events to `CloudEvent` for comparison.