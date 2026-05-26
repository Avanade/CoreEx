# CoreEx.Events.Publishing

> Provides the two-phase event publishing pipeline: an in-process queue (`IEventQueue`) drained by a single `PublishAsync()` call, with destination resolution and CloudEvents formatting wired in.

## Overview

The publishing namespace defines the end-to-end flow for dispatching `EventData` to an underlying messaging transport. Application and infrastructure code interacts solely with the `IEventQueue` / `IEventPublisher` abstractions, keeping business logic decoupled from any specific message broker.

Events are first added to an in-process `LinkedList`-backed queue via the `Add(...)` overloads. When the unit of work commits, `PublishAsync()` drains the entire queue in one call to the transport-specific `OnPublishAsync` implementation. Before dispatch each `EventData` is formatted via `IEventFormatter` (which populates CloudEvents attributes and tracing headers) and paired with a resolved destination name via `IDestinationProvider`, producing a `DestinationEvent` record. `Rollback(count)` and `Reset()` allow the outbox relay or retry logic to undo or restart queued events without data loss.

Transport-specific publisher implementations (e.g. Azure Service Bus, RabbitMQ) inherit from `EventPublisherBase` and only need to implement `OnPublishAsync(DestinationEvent[], CancellationToken)`.

## Key capabilities

- 📤 **In-process queue**: Thread-safe `Add(...)` overloads accept `EventData`, `CloudEvent`, named-destination variants, and `DestinationEvent` directly; `IsEmpty`, `Count`, and `GetEvents()` allow the host to inspect state before flushing.
- 🔒 **Single-publish guard**: `HasBeenPublished` prevents accidental double-dispatch; `Reset()` clears the guard and queue for replay or retry scenarios.
- ↩️ **Rollback support**: `Rollback(count)` removes the last _n_ Add operations from the queue without clearing earlier entries — useful for partial-failure recovery in outbox relays.
- 📍 **Destination resolution**: `IDestinationProvider` maps each event to a topic/queue name via `CreateFrom(EventData)`, `CreateFrom(string)`, or `CreateNew(MessageType, domainName)`; `FixedDestinationProvider` routes all events to a single configurable destination, reading a default from configuration.
- 📊 **OpenTelemetry instrumentation**: `EventPublisherInvoker` wraps `OnPublishAsync` in a diagnostic activity span.
- 🔇 **No-op publisher**: `NoOpEventPublisher` silently discards all events — suitable for testing and stub environments.

## Key types

| Type | Description |
|------|-------------|
| [`IEventQueue`](./IEventQueue.cs) | Core queuing contract: `Add(EventData)`, `Add(destination, EventData)`, `Add(destination, CloudEvent)`, `Add(DestinationEvent)`, `Clear()`, `IsEmpty`, `Count`. |
| [`IEventPublisher`](./IEventPublisher.cs) | Extends `IEventQueue` with `PublishAsync()`, `HasBeenPublished`, `Reset()`, `Rollback(count)`, and `GetEvents()`. |
| _[`EventPublisherBase`](./EventPublisherBase.cs)_ | Thread-safe abstract base; wires formatting, destination resolution, single-publish guard, and OpenTelemetry; implementors override `OnPublishAsync`. |
| [`IDestinationProvider`](./IDestinationProvider.cs) | Resolves destination (topic/queue) names from `EventData`, a string, or `MessageType` + domain name. |
| **[`FixedDestinationProvider`](./FixedDestinationProvider.cs)** | `IDestinationProvider` that routes all events to a single destination; defaults to the `CoreEx.Events:Destination` configuration key or `"default"`. |
| **[`DestinationEvent`](./DestinationEvent.cs)** | Immutable record pairing a resolved destination name with a formatted `CloudEvent`; passed to `OnPublishAsync`. |
| **[`EventPublisherInvoker`](./EventPublisherInvoker.cs)** | `InvokerBase` subclass that wraps publish operations with OpenTelemetry activity spans. |
| **[`NoOpEventPublisher`](./NoOpEventPublisher.cs)** | Silent `IEventPublisher` implementation that discards all events without error. |

## Related namespaces

- **[`CoreEx.Events`](../README.md)** - Parent package; defines `EventData`, `IEventFormatter`, and `MessageType` consumed by this pipeline.
- **[`CoreEx.Events.Subscribing`](../Subscribing/README.md)** - Complementary subscribing pipeline that receives and dispatches incoming events.
- **[`CoreEx.Database.Outbox`](../../CoreEx.Database/Outbox/README.md)** - Outbox publisher wraps `IEventPublisher`; events are persisted transactionally then flushed to the transport by a relay host.