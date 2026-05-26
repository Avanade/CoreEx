# CoreEx.Azure.Messaging.ServiceBus

> Provides Azure Service Bus integration for CoreEx: a `ServiceBusPublisher` implementing `IEventPublisher`, subscriber bases wired to `EventSubscriberBase`, and receiver hosts with built-in resiliency, metrics, and session support.

## Overview

`CoreEx.Azure.Messaging.ServiceBus` is the Azure Service Bus transport binding for the CoreEx events pipeline. It maps the CloudEvents-based `IEventPublisher` / `EventSubscriberBase` abstractions defined in `CoreEx.Events` directly onto the Azure Service Bus SDK, so no application-level code needs to reference Service Bus types directly.

**Publishing** is handled by `ServiceBusPublisher`, which inherits `EventPublisherBase` and implements `OnPublishAsync` using the SDK's safe-batch send API. Each `DestinationEvent` is converted from a `CloudEvent` to a `ServiceBusMessage` (structured or binary content mode), with CloudEvents attributes optionally written to message application properties. Session-ID assignment is controlled by the `ServiceBusSessionStrategy` enum.

**Subscribing** is layered: `ServiceBusSubscriberBase` extends `EventSubscriberBase` to accept a raw `ServiceBusReceivedMessage`, converting it to a `CloudEvent` before delegating upward. `ServiceBusSubscribedSubscriber` adds `SubscribedManager` dispatch so that `[Subscribe]`-decorated handlers are resolved automatically from the message subject and source. The `ServiceBusReceiver<TSubscriber>` and `ServiceBusSessionReceiver<TSubscriber>` classes wrap the SDK `ServiceBusProcessor` / `ServiceBusSessionProcessor` lifetimes, and `ServiceBusReceiverHostedService<TReceiver>` integrates them with the .NET hosted-service model including pause/resume and health-check support.

Resiliency is provided out-of-the-box: `ServiceBusReceiverResiliency` supplies factory methods for a receiver-level circuit breaker and a per-message retry pipeline (via Polly), both of which are applied by default in `ServiceBusReceiverOptionsBase`.

## Key capabilities

- 📤 **Safe-batch publishing**: `ServiceBusPublisher` groups `DestinationEvent` by destination and sends via the SDK batch API, recording send/fail counters and duration histograms via `ServiceBusMetrics`.
- 🛡 **CloudEvents — Service Bus bridge**: `ServiceBusExtensions` converts `CloudEvent` — `ServiceBusMessage` (structured or binary) and back, propagating distributed-tracing headers and CloudEvents attributes as application properties.
- 📥 **Subscriber dispatch**: `ServiceBusSubscribedSubscriber` uses `SubscribedManager` to route each received message to the correct `[Subscribe]`-decorated handler by subject and source.
- 🔄 **Session support**: `ServiceBusSessionReceiver<TSubscriber>` wraps `ServiceBusSessionProcessor`; `ServiceBusSessionStrategy` controls how `EventData.PartitionKey` is mapped to a `SessionId` (none, as-is, or converted to a bounded partition ID).
- 🔧 **Hosted-service lifecycle**: `ServiceBusReceiverHostedService<TReceiver>` integrates the receiver with `IHostedService`, forwarding start/pause/resume/stop to the underlying processor and reporting degraded health during pause.
- 🛡 **Built-in resiliency**: `ServiceBusReceiverResiliency` provides a circuit-breaker pipeline (`ReceiverResiliency`) and a per-message retry pipeline (`MessageResiliency`) pre-wired into every `ServiceBusReceiverOptionsBase` instance.
- 📊 **OpenTelemetry metrics**: `ServiceBusMetrics` exposes a `CoreEx.Azure.Messaging.ServiceBus` meter with counters for sent, failed, completed, dead-lettered, and abandoned messages, plus a send-duration histogram; `ServiceBusReceiverInvoker` wraps receive operations in activity spans.
- 📡 **Dependency injection helpers**: `CoreExServiceBusExtensions` (`AddAzureServiceBusPublisher`, `AddAzureServiceBusSubscribedSubscriber`, `AzureServiceBusReceiving()`) and `CoreExServiceBusExtensions.AddAzureServiceBusOpenTelemetry` wire everything into the DI container with a single fluent call each.

## Key types

| Type | Description |
|------|-------------|
| **[`ServiceBusPublisher`](./ServiceBusPublisher.cs)** | `IEventPublisher` implementation for Azure Service Bus; sends batched `ServiceBusMessage` objects, supports structured/binary CloudEvents content mode and session-ID strategies. |
| _[`ServiceBusSubscriberBase`](./ServiceBusSubscriberBase.cs)_ | Abstract `EventSubscriberBase` for Service Bus; accepts a raw `ServiceBusReceivedMessage`, converts it to a `CloudEvent`, and provides an `OnBeforeReceiveAsync` hook. |
| **[`ServiceBusSubscribedSubscriber`](./ServiceBusSubscribedSubscriber.cs)** | Concrete `ServiceBusSubscriberBase` that uses `SubscribedManager` to dispatch each message to the matching `[Subscribe]`-decorated handler. |
| **[`ServiceBusReceiver<TSubscriber>`](./ServiceBusReceiver.cs)** | Manages a `ServiceBusProcessor` lifetime (start/pause/resume/stop) and delegates each received message to the scoped `TSubscriber`. |
| **[`ServiceBusSessionReceiver<TSubscriber>`](./ServiceBusSessionReceiver.cs)** | Session-enabled variant of `ServiceBusReceiver<TSubscriber>`; wraps `ServiceBusSessionProcessor`. |
| **[`ServiceBusReceiverHostedService<TReceiver>`](./ServiceBusReceiverHostedService.cs)** | `IHostedService` adapter for any `ServiceBusReceiverBase`; supports pause/resume and reports degraded health while paused. |
| **[`ServiceBusReceiverOptions`](./ServiceBusReceiverOptions.cs)** | Options for `ServiceBusReceiver<TSubscriber>`; factory methods `CreateForQueue` / `CreateForTopicSubscription`; defaults to `PeekLock`, `AutoCompleteMessages=false`, `MaxConcurrentCalls=1`. |
| **[`ServiceBusSessionReceiverOptions`](./ServiceBusSessionReceiverOptions.cs)** | Session-specific options for `ServiceBusSessionReceiver<TSubscriber>`. |
| **[`ServiceBusReceiverResiliency`](./ServiceBusReceiverResiliency.cs)** | Factory for Polly `ResiliencePipeline<Result>` — `CreateReceiverCircuitBreakerResiliency` and `CreateMessageRetryResiliency`; applied by default in `ServiceBusReceiverOptionsBase`. |
| **[`ServiceBusSessionStrategy`](./ServiceBusSessionStrategy.cs)** | Enum: `None`, `UsePartitionKeyAsIs`, `UsePartitionKeyConvertedToAnId`; controls how the publisher assigns a `SessionId` to each outbound message. |
| [IServiceBusMessageActions](./IServiceBusMessageActions.cs) | Defines complete/abandon/dead-letter/defer actions for a received message; implemented by `ProcessMessageEventArgsActions` and `ProcessSessionMessageEventArgsActions` in the Abstractions folder. |
| **[`ServiceBusMetrics`](./ServiceBusMetrics.cs)** | Static class exposing the `CoreEx.Azure.Messaging.ServiceBus` `Meter` with send/receive counters and a send-duration histogram. |
| **[`ServiceBusExtensions`](./ServiceBusExtensions.cs)** | Extension methods for converting `CloudEvent` to/from `ServiceBusMessage` in structured or binary content mode. |

## Namespaces

| Namespace | Description | Documentation |
|-----------|-------------|---------------|
| **`CoreEx.Azure.Messaging.ServiceBus.Abstractions`** | Base receiver infrastructure: `ServiceBusReceiverBase`, typed `ServiceBusReceiverBase<TSubscriber>`, `ServiceBusReceiverOptionsBase`, `ServiceBusMessageActionsBase`, message-actions adapters, and `ServiceBusErrorClassifier`. | [📖 README](./Abstractions/README.md) |

## Related namespaces

- **[`CoreEx.Events`](../CoreEx.Events/README.md)** - Defines `IEventPublisher`, `EventPublisherBase`, `EventSubscriberBase`, `SubscribedManager`, and `IEventFormatter` that this package binds to Azure Service Bus.
- **[`CoreEx.Events.Publishing`](../CoreEx.Events/Publishing/README.md)** - `IDestinationProvider` and `DestinationEvent` used by `ServiceBusPublisher` during batched dispatch.
- **[`CoreEx.Events.Subscribing`](../CoreEx.Events/Subscribing/README.md)** - `ErrorHandling`, `ErrorHandler`, and subscriber exception types consumed by the receiver pipeline.
- **[`CoreEx.Database.Outbox`](../CoreEx.Database/Outbox/README.md)** - Outbox relay publisher that produces events later consumed by a `ServiceBusReceiver`-based relay host.