# CoreEx.Events

> Provides the CoreEx event publishing and subscribing infrastructure: `EventData` ↔ CloudEvents formatting, a two-phase queue-then-publish pipeline, and configurable subscriber dispatch with structured error handling.

## Overview

`CoreEx.Events` is the messaging backbone of the CoreEx framework. It defines the contracts and base implementations used by every host that sends or receives integration events — whether those are Azure Service Bus messages, outbox-relayed events, or any other transport.

**Publishing** follows a two-phase pattern: application code buffers `EventData` (or `CloudEvent`) objects into a lightweight in-process queue, then a single `PublishAsync()` call drains the queue to the underlying transport atomically. An `IDestinationProvider` resolves topic/queue names from event metadata, and `IEventFormatter` converts between `EventData` and the CloudNative CloudEvents specification, attaching distributed tracing headers automatically.

**Subscribing** is built around `EventSubscriberBase`, which receives a `CloudEvent` from the host transport, converts it back to `EventData` via `IEventFormatter`, and dispatches to the matching `SubscribedBase` handler registered with a `SubscribedManager`. Every error path — transient retries, dead-letter, silent completion, catastrophic failure — is expressed as a configurable `ErrorHandling` enum value, keeping subscriber code free of try/catch scaffolding.

## Key capabilities

- 🔄 **`EventData` ↔ CloudEvents bridge**: `IEventFormatter` / `EventFormatter` convert between the CoreEx `EventData` envelope and the CloudNative CloudEvents spec, including distributed-tracing header propagation (`traceparent`, `tracestate`, baggage).
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
| **[`EventFormatter`](./EventFormatter.cs)** | Default `IEventFormatter` implementation; handles CloudEvents attribute mapping and trace propagation. |
| **[`MessageType`](./MessageType.cs)** | Enum: `Event`, `Command`, `ReplyTo` — used in destination-name generation. |

## Namespaces

| Namespace | Description | Documentation |
|-----------|-------------|---------------|
| **`CoreEx.Events.Publishing`** | Two-phase queue-then-publish pipeline: `IEventQueue`, `IEventPublisher`, `EventPublisherBase`, `IDestinationProvider`, `DestinationEvent`, `NoOpEventPublisher`. | [📖 README](./Publishing/README.md) |
| **`CoreEx.Events.Subscribing`** | Subscriber dispatch and error handling: `EventSubscriberBase`, `SubscribedManager`, `SubscribedBase`, `ErrorHandler`, `ErrorHandling`, subscriber exceptions. | [📖 README](./Subscribing/README.md) |

## Related namespaces

- **[`CoreEx`](../CoreEx/README.md)** - Defines `EventData`, `CloudEvent` interop, `ExecutionContext`, and `Result<T>` used throughout the events pipeline.
- **[`CoreEx.Database.Outbox`](../CoreEx.Database/Outbox/README.md)** - Outbox-pattern publisher that wraps `IEventPublisher`; persists events transactionally and relays them via a background relay host.
- **[`CoreEx.DomainDriven`](../CoreEx.DomainDriven/README.md)** - `Aggregate<TId, TSelf>` accumulates `EventData` internally; the application layer forwards those to the publishing queue within the same unit-of-work.
- **[`CoreEx.Invokers`](../CoreEx/Invokers/README.md)** - `EventPublisherInvoker` and `SubscribedInvoker` provide OpenTelemetry activity wrapping for publish and receive operations.

## AI Usage Guide

An [`AGENTS.md`](./AGENTS.md) file is included with this package. AI coding assistants (GitHub Copilot, Claude, Cursor, etc.) that support workspace-injected package documentation will automatically surface concise usage guidance, code examples, and `Do Not` rules for this package without requiring a local CoreEx checkout.