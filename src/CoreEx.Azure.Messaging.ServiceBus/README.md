# CoreEx.Azure.Messaging.ServiceBus

> Provides Azure Service Bus integration for CoreEx: a `ServiceBusPublisher` implementing `IEventPublisher`, subscriber bases wired to `EventSubscriberBase`, and receiver hosts with built-in resiliency, metrics, and session support.

## Overview

`CoreEx.Azure.Messaging.ServiceBus` is the Azure Service Bus transport binding for the CoreEx events pipeline. It maps the CloudEvents-based `IEventPublisher` / `EventSubscriberBase` abstractions defined in `CoreEx.Events` directly onto the Azure Service Bus SDK, so no application-level code needs to reference Service Bus types directly.

**Publishing** is handled by `ServiceBusPublisher`, which inherits `EventPublisherBase` and implements `OnPublishAsync` using the SDK's safe-batch send API. Each `DestinationEvent` is converted from a `CloudEvent` to a `ServiceBusMessage` (structured or binary content mode), with CloudEvents attributes optionally written to message application properties. Session-ID assignment is controlled by the `ServiceBusSessionStrategy` enum.

When `UsePartitionKeyConvertedToAnId` is selected, `CoreEx:Host:ServiceBus:SessionIdPartitionSize` controls the publisher-wide session bucket count (default `4`). Destination-specific settings can override the strategy and/or bucket count when destinations have different ordering or throughput needs:

```json
{
  "CoreEx": {
    "Host": {
      "ServiceBus": {
        "SessionIdPartitionSize": 8,
        "Destinations": {
          "contoso": {
            "SessionIdStrategy": "UsePartitionKeyAsIs"
          },
          "contoso-products": {
            "SessionIdStrategy": "UsePartitionKeyConvertedToAnId",
            "SessionIdPartitionSize": 2
          }
        }
      }
    }
  }
}
```

The destination keys are the exact Service Bus entity names. Unspecified destination values inherit the publisher-wide strategy or bucket count. This setting is intentionally separate from relational outbox partition sizing because hosts may publish directly to Service Bus as well as through an outbox relay.

Prefer a stable business/aggregate key with `UsePartitionKeyAsIs` when ordering is required per entity; Service Bus then serializes each session while different sessions can be processed concurrently. Use `UsePartitionKeyConvertedToAnId` only when bounding session cardinality is useful, accepting that unrelated keys may collide and serialize together. More buckets do not themselves increase throughput: the receiving host must also allow enough concurrent sessions. A command destination's lower message volume does not inherently require a smaller count; tune it only when measurement or an explicit ordering/concurrency requirement justifies it. Avoid changing a destination's bucketing profile while relevant messages are queued or in flight, because a changed count may route the same key to another session.

**Subscribing** is layered: `ServiceBusSubscriberBase` extends `EventSubscriberBase` to accept a raw `ServiceBusReceivedMessage`, converting it to a `CloudEvent` before delegating upward. `ServiceBusSubscribedSubscriber` adds `SubscribedManager` dispatch so that `[Subscribe]`-decorated handlers are resolved automatically from the message subject and source. The `ServiceBusReceiver<TSubscriber>` and `ServiceBusSessionReceiver<TSubscriber>` classes wrap the SDK `ServiceBusProcessor` / `ServiceBusSessionProcessor` lifetimes, and `ServiceBusReceiverHostedService<TReceiver>` integrates them with the .NET hosted-service model including pause/resume and health-check support.

Resiliency is provided out-of-the-box: `ServiceBusReceiverResiliency` supplies factory methods for a receiver-level circuit breaker and a per-message retry pipeline (via Polly), both of which are applied by default in `ServiceBusReceiverOptionsBase`. Both factories are now thin wrappers over `CoreEx.Hosting`'s generic `CircuitBreakerResiliency<TOwner>`/`RetryResiliency<TOwner>` (promoted there so `CoreEx.Cosmos`'s Change Feed Processor-based outbox relay shares the exact same self-pause/self-resume behaviour) - this package supplies only the Service-Bus-specific pause-reason/dead-letter-exclusion/retry-classification wiring.

## Native output and send-time customization

**Prefer CloudEvents for CoreEx consumers.** Structured and binary CloudEvents both support `ServiceBusSubscribedSubscriber`/`SubscribedManager` and `[Subscribe]` dispatch. Binary mode already sends the payload alone in the body, with CE metadata in broker properties; an unwanted JSON envelope is not by itself a reason to choose native. Choose native when the receiver contract cannot or must not carry CE metadata, and verify that the consumer supports native delivery rather than normal CloudEvent dispatch. This does not change the default structured content mode.

Use the [CoreEx.Events native factories](../CoreEx.Events/README.md#native-publishing) with the existing publisher. Native output ignores CloudEvent `ContentMode`/`IncludeCloudEventAttributes`: the body contains payload bytes only, ContentType is the original complete media type, MessageId is the stable internal event ID, and PartitionKey follows existing mapping. Subject comes from formatted `EventData.Title` (`CloudEvent.Type`), not `EventData.Key`; use `ConfigureMessage` for exceptional Subject overrides. When `EventData.Key` is set, both native and CloudEvent Service Bus messages include it as the `Key` application property, independent of `PartitionKey` and `IncludeCloudEventAttributes`; the name is reserved and a native custom extension named `key` is not forwarded. The producer's type and reply destination survive storage as ordinary CloudEvent fields/extensions, not duplicate native metadata. CorrelationId, ReplyTo and eligible custom extensions are durable. Native custom headers use their attribute names without `ce_`, with canonical string values; underscore-prefixed/null entries are omitted. Standard CloudEvent attributes, tracing, mapped CoreEx fields, and the reserved `coreex` extension namespace are not exported as custom properties. Use `ConfigureMessage` for explicitly typed broker properties. Only standard unprefixed tracing is copied automatically; tenant, authentication, source and schema attributes are not leaked as native application properties.

`ServiceBusPublisher.ConfigureMessage` is an optional synchronous callback for both representations:

```csharp
builder.Services.AddAzureServiceBusPublisher((_, publisher) =>
{
    publisher.ConfigureMessage = (destination, envelope, message) =>
    {
        if (destination == "partner-orders")
            message.TimeToLive = TimeSpan.FromHours(2);
    };
});
```

Register it on the **actual final publisher**, including relay hosts for outbox delivery; callbacks are not serialized into the outbox and cannot depend on the originating API's ambient context. The callback receives the resolved destination, internal CloudEvent (read-only by convention) and constructed SDK message, after standard conversion and destination session assignment, before batching. It can override broker fields, including TTL/scheduling/session behavior, but the caller owns consequences of changing IDs, sessions, CE metadata or bodies.

All messages are prepared before any sends. A callback/conversion failure therefore sends nothing in that invocation. SDK batch checks see callback-modified messages; callbacks do not rerun on batch overflow or send-pipeline retries. They **do** run again on a whole `PublishAsync` retry or relay redelivery: keep them deterministic and side-effect-free, not globally exactly-once. A later oversize or send failure can still follow earlier successful batches.

`ToServiceBusMessage` shares declarative native/CE mapping, but does not apply publisher session policies or callbacks. Native JSON receive remains `ServiceBusSubscriberBase<TValue>`; raw bytes require an application-owned receiver. CE subscribers do not implicitly promote arbitrary native JSON into a CloudEvent.

## CloudEvent binding compatibility changes

`EventData.CorrelationId` is shared by CE and native output and maps to the broker CorrelationId using the standard `correlationid` CE extension. Binary receive prefers this extension when present, otherwise falls back to broker CorrelationId; structured receive retains body authority. Standard CloudEvents honor configurable correlation/causation attribute names; native internal envelopes use canonical names so relay formatter configuration cannot expose mapped metadata as custom headers. Direct CE access uses the existing generic extension helpers, without dedicated accessors or a separate empty-string validation policy. `CausationId` maps to the optional `causationid` CE extension; use `ConfigureMessage` to explicitly export a corresponding native application property if the external protocol needs it. Standard correlation and the `Key` application property are preserved under attribute exclusion; causation remains optional CE metadata.

Binary output always includes `ce_specversion` and `ce_source`, with ID/type in broker MessageId/Subject and content type in ContentType. The `Key` application property carries CloudEvent `subject` when present, including when `IncludeCloudEventAttributes = false`; explicit event occurrence time is retained as `ce_time` under that setting as well. Other optional CE attributes (including schema, partition and custom extensions) are omitted when attributes are excluded. Standard unprefixed tracing is always mirrored. With attributes enabled, CE application-property values use canonical string formatting (including URI/time values) rather than SDK-specific CLR objects.

Structured detection now uses `application/cloudevents+json` without requiring a duplicated `ce_specversion`. The structured body is authoritative over conflicting broker/application properties. Binary receive requires a string specversion supported by the pinned CloudEvents SDK (`1.0`; legacy `0.3` is unsupported) and valid required ID/type/source; broker ID, Subject and ContentType win over duplicated `ce_id`, `ce_type` and `ce_datacontenttype`. Unsupported versions and incomplete binary envelopes fail instead of producing plausible-looking events. Broker EnqueuedTime is no longer fabricated as event occurrence time: absent time remains absent. These are intentional compatibility corrections, not native-only changes.

## Publishing failure semantics

The default `ServiceBusPublisher.SendResiliency` retries only failures classified by `ServiceBusErrorClassifier.IsTransient`. After those retries are exhausted, the publication boundary translates a classifier-confirmed `ServiceBusException` into the generic CoreEx `TransientException`, retaining the final broker exception and its stack trace as `InnerException` and enabling error logging. Transient failures during batch creation use the same translation, but batch creation is not covered by `SendResiliency`. Permanent broker failures, unrelated exceptions, and caller cancellation remain unchanged.

CoreEx WebApi maps `TransientException` to HTTP **503 Service Unavailable**, `errorType = "transient"`, and the default `Retry-After` interval of 90 seconds unless configured or overridden. The public message is generic; broker diagnostics remain in the inner exception rather than the response message. Callers that previously caught transient `ServiceBusException` directly must now catch `TransientException` and inspect its inner exception when transport-specific diagnostics are needed.

A transient failure does not establish that nothing was accepted: an acknowledgement may have been lost, or earlier batches or destinations may already have succeeded. Retrying a publish or the enclosing API operation therefore requires stable message identities and application-level idempotency/deduplication; a 503 is not a rollback guarantee. For an outbox-backed API, a successful database/outbox commit remains successful while later broker failures are handled by the relay.

## Uniform native JSON subscribers

Use `ServiceBusSubscriberBase<TValue>` when every message on an entity has the same JSON body shape. It bypasses CloudEvent conversion and `SubscribedManager`, deserializes with `JsonSerializerOptions` (falling back to `JsonDefaults.SerializerOptions`), requires the value by default, and invokes the optional `ValueValidator` before `OnReceiveAsync(TValue, ServiceBusReceivedMessage, EventSubscriberArgs, CancellationToken)`. The handler retains the original SDK message and arguments; no synthetic `EventData` is created. Override `ValueIsRequired` only when null/default values are intentional, using a nullable `TValue` for optional reference payloads.

The typed base shares existing subscriber error classification, logging, cancellation handling and receive metrics. Configure `InvalidDataHandling` for malformed/missing bodies separately from `ErrorHandler` mappings for validation and application failures. Both policies retain existing defaults (`CompleteAsError` for invalid payloads and extended business/validation errors); explicitly choose dead-lettering or retry where appropriate. Serializer configuration/converter failures that are not `JsonException` flow through normal error classification rather than being silently treated as malformed JSON. `ContentMode` and CloudEvent tenant checks do not apply to native bodies; applications own any native tenant/authentication policy.

Register a typed subscriber with the existing `.WithSubscriber<MySubscriber>()` receiver or session-receiver builder. It also works with the CoreEx Azure Functions `FunctionReceiveAsync` subscriber overload. The subscriber processes and classifies; the host owns settlement and listener resilience. Its constructor takes only the logger; a private shared formatter satisfies the inherited infrastructure dependency without requiring formatter DI or invoking it in native processing. See the executable [native subscriber example](../../tests/CoreEx.Azure.Functions.TestApp/NativeMessageSubscriber.cs).

## Optional payload cleanup

The inherited `AutoCleanValue` defaults to `false` on native JSON subscribers and CloudEvent subscriber owners. This intentionally differs from WebApi's default-on request cleanup: published events are generally expected to be canonical already, making receive cleanup typically redundant and potentially changing producer-owned identifiers or date semantics. Enable it explicitly for noncanonical feeds or commands when the receiving boundary owns normalization.

On `ServiceBusSubscriberBase<TValue>`, set `AutoCleanValue = true` to clean immediately after JSON deserialization and before required-value checks, `ValueValidator` and the handler. CloudEvent handlers deriving from `SubscribedBase<TValue>` have a nullable per-handler/message-type override: null inherits the owner, true enables and false disables cleanup. Set the override in the handler constructor; it is not a global policy attached to the payload CLR type. All subjects matched by that handler share its policy.

Both paths use WebApi's `RuntimeMetadata.Clean(value, CleanArgs.Default)` behavior without root/nested object collapse. Scalar whitespace may become null and fail a required-value check. Serialized bodies, envelopes, keys and transport metadata remain unchanged. Cleanup failures follow normal receive error handling rather than malformed-data handling; broker settlement and retry configuration are unaffected. See [subscriber cleanup policy](../CoreEx.Events/Subscribing/README.md#optional-payload-cleanup).

## Receive tracing and transport metadata

Both native JSON and CloudEvent subscribers call `ServiceBusReceivedMessage.EnrichReceiveActivity()`. A valid application-property `traceparent` takes precedence over Azure's `Diagnostic-Id`; an absent/invalid `traceparent` falls back to a valid `Diagnostic-Id`. `tracestate` accompanies the chosen context. These are fixed transport names, independent of `EventFormatter`'s configurable CloudEvent attribute names. Context is linked as remote only when the current activity's local ancestry, parent references and existing links do not already represent that producer. The host's activity and parent are never replaced, no activity is created when tracing is disabled, and malformed tracing properties do not reject the message. CloudEvent envelope context uses the same link deduplication.

Incoming baggage is opt-in through the inherited `EventSubscriberBase.PropagateBaggage = true`, covering both transport headers and CloudEvent envelopes with one policy. The standalone helper uses `message.EnrichReceiveActivity(propagateBaggage: true)`. Shared W3C parsing/import handles encoding and member properties; headers over 8192 UTF-8 bytes are ignored, at most 64 parsed entries are considered per import, decoded imports are also bounded, and existing activity baggage keys are not overwritten. Transport values are imported first, so they win over duplicate envelope keys. Enable only for trusted producers: baggage flows downstream and can contain sensitive data. This changes CloudEvent's former automatic import in 4.4; ambient publishing capture separately requires `EventFormatter.CaptureBaggage = true`.

The helper adds diagnostic tags for message ID, business correlation ID, subject, delivery count, enqueue time and session ID. Business correlation is not treated as trace context. Identifiers are span tags, not metric dimensions; tenant/user/custom properties are not imported into authenticated execution context. Processing and settlement policy remain unchanged.

## Key capabilities

- 📤 **Safe-batch publishing**: `ServiceBusPublisher` groups `DestinationEvent` by destination and sends via the SDK batch API, recording send/fail counters and duration histograms via `ServiceBusMetrics`.
- 🛡 **CloudEvents — Service Bus bridge**: `ServiceBusExtensions` converts `CloudEvent` — `ServiceBusMessage` (structured or binary) and back, propagating distributed-tracing headers and CloudEvents attributes as application properties.
- 📥 **Subscriber dispatch**: `ServiceBusSubscribedSubscriber` uses `SubscribedManager` to route each received message to the correct `[Subscribe]`-decorated handler by subject and source.
- 🔄 **Session support**: `ServiceBusSessionReceiver<TSubscriber>` wraps `ServiceBusSessionProcessor`; `ServiceBusSessionStrategy` controls how `EventData.PartitionKey` is mapped to a `SessionId` (none, as-is, or converted to a bounded partition ID).
- 🔧 **Hosted-service lifecycle**: `ServiceBusReceiverHostedService<TReceiver>` integrates the receiver with `IHostedService`, forwarding start/pause/resume/stop to the underlying processor and reporting degraded health during pause.
- 🛡 **Built-in resiliency**: `ServiceBusReceiverResiliency` provides a circuit-breaker pipeline (`ReceiverResiliency`) and a per-message retry pipeline (`MessageResiliency`) pre-wired into every `ServiceBusReceiverOptionsBase` instance - both delegate to `CoreEx.Hosting`'s generic `CircuitBreakerResiliency<TOwner>`/`RetryResiliency<TOwner>`.
- 📊 **OpenTelemetry metrics**: `ServiceBusMetrics` exposes a `CoreEx.Azure.Messaging.ServiceBus` meter with counters for sent, failed, completed, dead-lettered, and abandoned messages, plus a send-duration histogram; `ServiceBusReceiverInvoker` wraps receive operations in activity spans.
- 📡 **Dependency injection helpers**: `CoreExServiceBusExtensions` (`AddAzureServiceBusPublisher`, `AddAzureServiceBusSubscribedSubscriber`, `AzureServiceBusReceiving()`) and `CoreExServiceBusExtensions.AddAzureServiceBusOpenTelemetry` wire everything into the DI container with a single fluent call each.

## Key types

| Type | Description |
|------|-------------|
| **[`ServiceBusPublisher`](./ServiceBusPublisher.cs)** | `IEventPublisher` implementation for Azure Service Bus; sends batched `ServiceBusMessage` objects, supports structured/binary CloudEvents content mode and session-ID strategies. |
| **[`ServiceBusDestinationSettings`](./ServiceBusDestinationSettings.cs)** | Optional per-destination overrides for the session-ID strategy and hashed session bucket count. |
| _[`ServiceBusSubscriberBase`](./ServiceBusSubscriberBase.cs)_ | Abstract `EventSubscriberBase` for Service Bus; accepts a raw `ServiceBusReceivedMessage`, converts it to a `CloudEvent`, and provides an `OnBeforeReceiveAsync` hook. |
| _[`ServiceBusSubscriberBase<TValue>`](./ServiceBusSubscriberBaseT.cs)_ | Uniform native JSON subscriber with automatic deserialization, optional `ValueValidator`, and shared error classification; bypasses CloudEvent conversion and subject dispatch. |
| **[`ServiceBusSubscribedSubscriber`](./ServiceBusSubscribedSubscriber.cs)** | Concrete `ServiceBusSubscriberBase` that uses `SubscribedManager` to dispatch each message to the matching `[Subscribe]`-decorated handler. |
| **[`ServiceBusReceiver<TSubscriber>`](./ServiceBusReceiver.cs)** | Manages a `ServiceBusProcessor` lifetime (start/pause/resume/stop) and delegates each received message to the scoped `TSubscriber`. |
| **[`ServiceBusSessionReceiver<TSubscriber>`](./ServiceBusSessionReceiver.cs)** | Session-enabled variant of `ServiceBusReceiver<TSubscriber>`; wraps `ServiceBusSessionProcessor`. |
| **[`ServiceBusReceiverHostedService<TReceiver>`](./ServiceBusReceiverHostedService.cs)** | `IHostedService` adapter for any `ServiceBusReceiverBase`; supports pause/resume and reports degraded health while paused. |
| **[`ServiceBusReceiverOptions`](./ServiceBusReceiverOptions.cs)** | Options for `ServiceBusReceiver<TSubscriber>`; factory methods `CreateForQueue` / `CreateForTopicSubscription`; defaults to `PeekLock`, `AutoCompleteMessages=false`, `MaxConcurrentCalls=1`. |
| **[`ServiceBusSessionReceiverOptions`](./ServiceBusSessionReceiverOptions.cs)** | Session-specific options for `ServiceBusSessionReceiver<TSubscriber>`. |
| **[`ServiceBusReceiverResiliency`](./ServiceBusReceiverResiliency.cs)** | Factory for Polly `ResiliencePipeline<Result>` — `CreateReceiverCircuitBreakerResiliency` and `CreateMessageRetryResiliency`; applied by default in `ServiceBusReceiverOptionsBase`. Thin wrappers over `CoreEx.Hosting.CircuitBreakerResiliency<TOwner>`/`RetryResiliency<TOwner>`. |
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
- **[`CoreEx.Hosting`](../CoreEx/Hosting/README.md)** - `CircuitBreakerResiliency<TOwner>`/`RetryResiliency<TOwner>` are the generic self-pausing/retry pipelines that `ServiceBusReceiverResiliency` now delegates to; originally implemented here, promoted for reuse by `CoreEx.Cosmos`'s outbox relay.

## AI Usage Guide

An [`AGENTS.md`](./AGENTS.md) file is included with this package. AI coding assistants (GitHub Copilot, Claude, Cursor, etc.) that support workspace-injected package documentation will automatically surface concise usage guidance, code examples, and `Do Not` rules for this package without requiring a local CoreEx checkout.
