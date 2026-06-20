# CoreEx.Events.Subscribing

> Provides the CoreEx subscriber dispatch pipeline: `EventSubscriberBase` receives and converts incoming CloudEvents, `SubscribedManager` routes them to `[Subscribe]`-decorated handlers, and a structured `ErrorHandling` system replaces boilerplate try/catch logic.

## Overview

The subscribing namespace defines the inbound half of the CoreEx events pipeline. Transport-specific subscriber hosts (Azure Service Bus, RabbitMQ, etc.) inherit from `EventSubscriberBase`, which handles the CloudEvent → `EventData` conversion and all cross-cutting concerns before handing off to application-level `SubscribedBase` handlers.

`SubscribedManager` acts as the dispatcher: it holds a registry of `SubscribedBase` types decorated with `[Subscribe]` attributes, resolves the single matching handler for each received event, and optionally invokes an `IEventSubscriberInbox` to enforce idempotent processing before the handler runs. Unmatched events and ambiguous matches are each governed by configurable `ErrorHandling` values.

Every error outcome in the pipeline — transient retry, dead-letter, silent completion, catastrophic escalation — is expressed as an `ErrorHandling` enum value. The `ErrorHandler` class provides a fluent per-exception-type mapping, eliminating repetitive try/catch scaffolding from subscriber code. `EventSubscriberMetrics` exposes an OpenTelemetry `messages.received` counter for observability.

## Key capabilities

- 🔄 **CloudEvent → `EventData` conversion**: `EventSubscriberBase` uses `IEventFormatter.ConvertFromCloudEvent` and `Parse` to produce a fully populated `EventData` before any handler is invoked.
- 📬 **`SubscribedManager` dispatch**: Registers `SubscribedBase` handlers by `[Subscribe]` attribute; routes each event to exactly one handler or applies `NotSubscribedHandling` / `AmbiguousSubscriberHandling` policies.
- ✉️ **Typed `SubscribedBase<TValue>`**: Deserializes `EventData.Data` to a strongly-typed `TValue` before invoking `OnReceiveAsync`, with `InvalidDataHandling` governing deserialization failures.
- 🛡️ **Structured error handling**: `ErrorHandling` enum values are mapped per exception type via `ErrorHandler`; `AutoTransientHandling` automatically promotes `IExtendedException.IsTransient` exceptions to `Retry`; `WhereIsExtendedErrorHandling` catches all remaining `IExtendedException.IsError` cases.
- 📥 **Inbox idempotency**: `IEventSubscriberInbox.InboxCheckAsync` can be enabled globally via `SubscribedManager.RequiresInbox(...)` or per-handler via `SubscribedBase.RequiresInboxCheck`; `InboxFailureHandling` governs failed checks.
- 📊 **OpenTelemetry metrics**: `EventSubscriberMetrics` records a `messages.received` counter (with outcome tags) via `System.Diagnostics.Metrics`; `SubscribedInvoker` wraps handler invocations in activity spans.
- 🗂️ **`EventSubscriberArgs`**: Carries per-receive context including `MessageUniqueKey`, resiliency attempt count, and an open `Properties` dictionary for transport-specific metadata.

## Key types

| Type | Description |
|------|-------------|
| _[`EventSubscriberBase`](./EventSubscriberBase.cs)_ | Abstract transport subscriber base; converts `CloudEvent` → `EventData`, delegates to `SubscribedManager`, enforces tenant-id and error-handling policies. |
| **[`SubscribedManager`](./SubscribedManager.cs)** | Registers and dispatches to `SubscribedBase` handlers; controls `NotSubscribedHandling`, `AmbiguousSubscriberHandling`, and optional inbox idempotency. |
| _[`SubscribedBase`](./SubscribedBase.cs)_ | Abstract per-event handler; exposes per-handler `ErrorHandler`, `InvalidDataHandling`, `RequiresInboxCheck`, and `JsonSerializerOptions` overrides. |
| _[`SubscribedBase<TValue>`](./SubscribedBaseT.cs)_ | Typed variant of `SubscribedBase` that deserializes `EventData.Data` to `TValue` before calling `OnReceiveAsync`. |
| **[`SubscribeAttribute`](./SubscribeAttribute.cs)** | Decorates `SubscribedBase` types to declare the events they handle; matched by `SubscribedManager` during routing. |
| **[`ErrorHandler`](./ErrorHandler.cs)** | Fluent configurator mapping exception types (and `IExtendedException` signals) to `ErrorHandling` values; used on both `EventSubscriberBase` and individual `SubscribedBase` handlers. |
| **[`ErrorHandling`](./ErrorHandling.cs)** | Enum: `None`, `CompleteAsSilent`, `CompleteAsInformation`, `CompleteAsWarning`, `CompleteAsError`, `Retry`, `DeadLetter`, `Catastrophic`. |
| **[`ErrorHandlerArgs`](./ErrorHandlerArgs.cs)** | Carries exception, source, and context to `ErrorHandler` resolution; used internally during error processing. |
| **[`EventSubscriberArgs`](./EventSubscriberArgs.cs)** | Per-receive arguments: `MessageUniqueKey`, `AttemptCount`, `Owner`, and open `Properties` dictionary for transport metadata. |
| **[`EventSubscriberMetrics`](./EventSubscriberMetrics.cs)** | Exposes the `CoreEx.Events.Subscribing` `Meter` and `messages.received` counter for OpenTelemetry metric collection. |
| **[`SubscribedInvoker`](./SubscribedInvoker.cs)** | `InvokerBase` subclass that wraps `SubscribedBase` handler invocations in OpenTelemetry activity spans. |
| [`IEventSubscriberInbox`](./IEventSubscriberInbox.cs) | Enables inbox idempotency: `InboxCheckAsync` returns `true` when the event should be processed, `false` to skip (already seen). |

## Namespaces

| Namespace | Description | Documentation |
|-----------|-------------|---------------|
| **`CoreEx.Events.Subscribing.Exceptions`** | Subscriber-specific exception types raised by the error handling pipeline. | [📖 README](./Exceptions/README.md) |

## Related namespaces

- **[`CoreEx.Events`](../README.md)** - Parent package; defines `EventData`, `IEventFormatter`, and `CloudEvent` interop consumed throughout.
- **[`CoreEx.Events.Publishing`](../Publishing/README.md)** - Complementary publishing pipeline for outbound events.
- **[`CoreEx.Invokers`](../../CoreEx/Invokers/README.md)** - `SubscribedInvoker` extends `InvokerBase` to wrap handler executions with OpenTelemetry activities.