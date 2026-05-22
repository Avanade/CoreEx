# CoreEx.Azure.Messaging.ServiceBus.Abstractions

> Provides the foundational base classes for the Azure Service Bus receiver pipeline: `ServiceBusReceiverBase` and its typed variant, `ServiceBusReceiverOptionsBase`, `ServiceBusMessageActionsBase`, concrete message-actions adapters, and the `ServiceBusErrorClassifier` utility.

## Overview

The `Abstractions` namespace contains the building blocks that `ServiceBusReceiver<TSubscriber>` and `ServiceBusSessionReceiver<TSubscriber>` are built on top of. Direct consumers of the package rarely need to reference these types; they exist to allow extension and testing of the receiver infrastructure without depending on specific SDK processor types.

`ServiceBusReceiverBase` manages the receiver lifecycle — start, pause, resume, stop — and co-ordinates the two Polly resiliency pipelines (circuit breaker at the receiver level, retry at the message level). `ServiceBusReceiverBase<TSubscriber>` adds the scoped DI resolution of `TSubscriber`, `ExecutionContext` configuration, and the full per-message processing flow. `ServiceBusReceiverOptionsBase` centralises the shared configuration that both `ServiceBusReceiverOptions` and `ServiceBusSessionReceiverOptions` inherit.

`IServiceBusMessageActions` is the abstraction over the SDK's `ProcessMessageEventArgs` and `ProcessSessionMessageEventArgs` complete/abandon/dead-letter/defer operations. `ServiceBusMessageActionsBase` provides the metrics-recording wrapper, while `ProcessMessageEventArgsActions` and `ProcessSessionMessageEventArgsActions` are the concrete SDK-bound implementations. `ServiceBusErrorClassifier` classifies `ServiceBusException` errors (lock lost, transient, idle connection) and logs at the appropriate severity.

## Key capabilities

- 🧱 **Receiver lifecycle management**: `ServiceBusReceiverBase` exposes `StartAsync`, `PauseAsync`, `ResumeAsync`, and `StopAsync` with a `ServiceStatus` state machine and `SemaphoreSlim` guard to prevent concurrent state transitions.
- 🔄 **Typed scoped execution**: `ServiceBusReceiverBase<TSubscriber>` resolves a fresh `TSubscriber` and `ExecutionContext` per message from a DI scope, then executes with `MessageResiliency` (Polly retry) wrapped inside `ReceiverResiliency` (circuit breaker).
- 🔧 **Shared options base**: `ServiceBusReceiverOptionsBase` carries `QueueOrTopicName`, `SubscriptionName`, `ReceiverResiliency`, `MessageResiliency`, `ExecutionContextConfigure`, and `SubscriberServiceKey`; supports configuration-key indirection via `config:`, `^`, and `%` prefixes.
- 📊 **Metrics-instrumented message actions**: `ServiceBusMessageActionsBase` records `MessagesReceivedComplete`, `MessagesReceivedAbandoned`, `MessagesReceivedDeadLetter`, and `MessagesReceivedDeferred` counters on each disposition call before delegating to the SDK.
- 🛡 **Error classification**: `ServiceBusErrorClassifier.ClassifyAndLogError` inspects `ProcessErrorEventArgs`, distinguishes lock-lost, transient, and idle-connection-closed conditions, and logs at `Information` rather than `Error` to reduce noise.

## Key types

| Type | Description |
|------|-------------|
| _[`ServiceBusReceiverBase`](./ServiceBusReceiverBase.cs)_ | Abstract base managing processor lifecycle (start/pause/resume/stop), `ServiceStatus` state machine, `ReceiverResiliency` circuit breaker, and `OnProcessMessageAsync` dispatch. |
| _[`ServiceBusReceiverBase<TSubscriber>`](./ServiceBusReceiverBaseT.cs)_ | Typed abstract receiver; resolves scoped `TSubscriber` and `ExecutionContext` per message, applies `MessageResiliency` retry, and maps `Result` outcomes to SDK message actions (complete/abandon/dead-letter). |
| _[`ServiceBusReceiverOptionsBase`](./ServiceBusReceiverOptionsBase.cs)_ | Abstract options base; holds queue/topic name, subscription name, both Polly resiliency pipelines, `ExecutionContextConfigure`, and `SubscriberServiceKey`. |
| _[`ServiceBusMessageActionsBase`](./ServiceBusMessageActionsBase.cs)_ | Abstract `IServiceBusMessageActions` implementation; adds `ServiceBusMetrics` counters around complete/abandon/dead-letter/defer before delegating to the concrete SDK operation. |
| **[`ProcessMessageEventArgsActions`](./ProcessMessageEventArgsActions.cs)** | Concrete `ServiceBusMessageActionsBase` for `ProcessMessageEventArgs`; delegates complete/abandon/dead-letter to the SDK args. |
| **[`ProcessSessionMessageEventArgsActions`](./ProcessSessionMessageEventArgsActions.cs)** | Session-aware variant of `ProcessMessageEventArgsActions` for `ProcessSessionMessageEventArgs`. |
| **[`ServiceBusErrorClassifier`](./ServiceBusErrorClassifier.cs)** | Static utility; classifies `ServiceBusException` into lock-lost, transient, and idle-connection-closed categories and logs at the appropriate level. |

## Related namespaces

- **[`CoreEx.Azure.Messaging.ServiceBus`](../README.md)** - Parent package; `ServiceBusReceiver<TSubscriber>` and `ServiceBusSessionReceiver<TSubscriber>` inherit from these base types.