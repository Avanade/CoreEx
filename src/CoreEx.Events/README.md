# CoreEx.Events

> Provides the CoreEx event publishing and subscribing infrastructure: `EventData` ↔ CloudEvents formatting, a two-phase queue-then-publish pipeline, and configurable subscriber dispatch with structured error handling.

## Overview

`CoreEx.Events` is the messaging backbone of the CoreEx framework. It defines the contracts and base implementations used by every host that sends or receives integration events — whether those are Azure Service Bus messages, outbox-relayed events, or any other transport.

**Publishing** follows a two-phase pattern: application code buffers `EventData` (or `CloudEvent`) objects into a lightweight in-process queue, then a single `PublishAsync()` call drains the queue to the underlying transport atomically. An `IDestinationProvider` resolves topic/queue names from event metadata, and `IEventFormatter` converts between `EventData` and the CloudNative CloudEvents specification, attaching distributed tracing headers automatically.

**Subscribing** is built around `EventSubscriberBase`, which receives a `CloudEvent` from the host transport, converts it back to `EventData` via `IEventFormatter`, and dispatches to the matching `SubscribedBase` handler registered with a `SubscribedManager`. Every error path — transient retries, dead-letter, silent completion, catastrophic failure — is expressed as a configurable `ErrorHandling` enum value, keeping subscriber code free of try/catch scaffolding.

## Key capabilities

- 🔄 **`EventData` ↔ CloudEvents bridge**: `IEventFormatter` / `EventFormatter` convert between the CoreEx `EventData` envelope and the CloudNative CloudEvents spec, including distributed-tracing header propagation (`traceparent`, `tracestate`, baggage) attached when an event is *published*.
- 🔗 **Outbox relay trace-linking**: `CloudEventTracingExtensions.LinkTraceContext` reads a *previously-stored* event's `traceparent`/`tracestate` extension attributes back out and adds them as an `ActivityLink` on the current `Activity` - used by an outbox relay to connect its own publish span back to each original producer's trace (deliberately does not propagate `baggage`; see the XML doc remarks for why a batched, fan-in relay can't do that safely). Shared unchanged by `CoreEx.Database.Outbox` and `CoreEx.Cosmos.Outbox`'s relays.
- 🧹 **App-wide payload redaction**: `EventFormatter.DataExcludePaths` applies a `CoreEx.Json.JsonFilter` exclude (recursive descent, e.g. `$..etag`) to every event's `Data` during `Format()`, so a property can be stripped from all published events in one place rather than at every `EventData.WithValue()` call site. Defaults to excluding `$..etag` — an optimistic-concurrency token that has no meaning to a downstream consumer and cannot be reliably captured for events raised transactionally via an outbox against a NoSQL store; set to `null`/empty to opt out.
- 📤 **Queue-then-publish pipeline**: Events are buffered in-process and dispatched atomically via `PublishAsync()`; `Rollback(count)` and `Reset()` support outbox and retry patterns.
- 📍 **Destination resolution**: `IDestinationProvider` dynamically generates topic/queue names from an `EventData`, an explicit destination string, or from `MessageType` and domain name.
- 📥 **Structured subscriber dispatch**: `SubscribedManager` routes incoming events to `[Subscribe]`-decorated handlers, enforces inbox idempotency checks, and manages ambiguous- and not-subscribed outcomes.
- 🛡️ **Configurable error handling**: `ErrorHandling` enum values (`CompleteAsSilent`, `Retry`, `DeadLetter`, `Catastrophic`, and more) are mapped per exception type via a fluent `ErrorHandler` configurator — no boilerplate try/catch in subscriber code.
- 📊 **OpenTelemetry metrics**: `EventSubscriberMetrics` exposes a `messages.received` counter via `System.Diagnostics.Metrics`; `EventPublisherInvoker` and `SubscribedInvoker` wrap operations in activity spans.
- 🧩 **`MessageType` discrimination**: Distinguishes `Event`, `Command`, and `ReplyTo` semantics for destination-name generation.

## Key types

| Type | Description |
|------|-------------|
| [`IEventFormatter`](./IEventFormatter.cs) | Formats/parses `EventData`, converts to/from `CloudEvent`, adds distributed-tracing headers. |
| **[`EventFormatter`](./EventFormatter.cs)** | Default `IEventFormatter` implementation; handles CloudEvents attribute mapping, trace propagation, and (via `DataExcludePaths`) app-wide `JsonFilter`-based redaction of the event `Data` payload. |
| **[`MessageType`](./MessageType.cs)** | Enum: `Event`, `Command`, `ReplyTo` — used in destination-name generation. |
| **[`CloudEventTracingExtensions`](./CloudEventTracingExtensions.cs)** | `LinkTraceContext(Activity?, IEnumerable<CloudEvent>)` - links an activity to each event's originating W3C trace context; used by an outbox relay's publish span, not by ordinary publishing. |

## Namespaces

| Namespace | Description | Documentation |
|-----------|-------------|---------------|
| **`CoreEx.Events.Publishing`** | Two-phase queue-then-publish pipeline: `IEventQueue`, `IEventPublisher`, `EventPublisherBase`, `IDestinationProvider`, `DestinationEvent`, `NoOpEventPublisher`. | [📖 README](./Publishing/README.md) |
| **`CoreEx.Events.Subscribing`** | Subscriber dispatch and error handling: `EventSubscriberBase`, `SubscribedManager`, `SubscribedBase`, `ErrorHandler`, `ErrorHandling`, subscriber exceptions. | [📖 README](./Subscribing/README.md) |

## Related namespaces

- **[`CoreEx`](../CoreEx/README.md)** - Defines `EventData`, `CloudEvent` interop, `ExecutionContext`, and `Result<T>` used throughout the events pipeline.
- **[`CoreEx.Database.Outbox`](../CoreEx.Database/Outbox/README.md)** / **[`CoreEx.Cosmos.Outbox`](../CoreEx.Cosmos/README.md#namespaces)** - Outbox-pattern publishers that wrap `IEventPublisher`; persist events transactionally (relational outbox table / Cosmos DB `TransactionalBatch`) and relay them via a background host (poll-loop / Change Feed Processor), sharing the same `CloudEventTracingExtensions` and harmonized metric naming.
- **[`CoreEx.DomainDriven`](../CoreEx.DomainDriven/README.md)** - `Aggregate<TId, TSelf>` accumulates `EventData` internally; the application layer forwards those to the publishing queue within the same unit-of-work.
- **[`CoreEx.Invokers`](../CoreEx/Invokers/README.md)** - `EventPublisherInvoker` and `SubscribedInvoker` provide OpenTelemetry activity wrapping for publish and receive operations.

## AI Usage Guide

An [`AGENTS.md`](./AGENTS.md) file is included with this package. AI coding assistants (GitHub Copilot, Claude, Cursor, etc.) that support workspace-injected package documentation will automatically surface concise usage guidance, code examples, and `Do Not` rules for this package without requiring a local CoreEx checkout.