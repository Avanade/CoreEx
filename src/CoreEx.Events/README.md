# CoreEx.Events

> Provides the CoreEx event publishing and subscribing infrastructure: `EventData` ↔ CloudEvents formatting, a two-phase queue-then-publish pipeline, and configurable subscriber dispatch with structured error handling.

## Overview

`CoreEx.Events` is the messaging backbone of the CoreEx framework. It defines the contracts and base implementations used by every host that sends or receives integration events — whether those are Azure Service Bus messages, outbox-relayed events, or any other transport.

**Publishing** follows a two-phase pattern: application code buffers `EventData` (or `CloudEvent`) objects into a lightweight in-process queue, then `PublishAsync()` sends them to the underlying transport or persists them in an outbox. Only an enlisted transactional outbox provides database atomicity; direct transport publishing can partially succeed. An `IDestinationProvider` resolves topic/queue names from event metadata, and `IEventFormatter` converts between `EventData` and the CloudNative CloudEvents specification, attaching trace parent/state automatically; ambient baggage capture requires explicit opt-in.

**Subscribing** is built around `EventSubscriberBase`, which receives a `CloudEvent` from the host transport, converts it back to `EventData` via `IEventFormatter`, and dispatches to the matching `SubscribedBase` handler registered with a `SubscribedManager`. Every error path — transient retries, dead-letter, silent completion, catastrophic failure — is expressed as a configurable `ErrorHandling` enum value, keeping subscriber code free of try/catch scaffolding.

Typed payload cleanup is available through `EventSubscriberBase.AutoCleanValue` (default false) and a nullable per-handler `SubscribedBase.AutoCleanValue` override. Unlike WebApi request input, published events are generally expected to be canonical already, so consumer cleanup is typically redundant and can reinterpret producer-owned facts. Enable it deliberately where the receiving boundary owns normalization; see [cleanup behavior and overrides](./Subscribing/README.md#optional-payload-cleanup).

## Key capabilities

- 🔄 **`EventData` ↔ CloudEvents bridge**: `IEventFormatter` / `EventFormatter` convert between the CoreEx `EventData` envelope and the CloudNative CloudEvents spec, including automatic trace parent/state propagation and explicit baggage policies.
- 🔗 **Outbox relay trace-linking**: `CloudEventTracingExtensions.LinkTraceContext` reads a *previously-stored* event's `traceparent`/`tracestate` extension attributes back out and adds them as an `ActivityLink` on the current `Activity` - used by an outbox relay to connect its own publish span back to each original producer's trace (deliberately does not propagate `baggage`; see the XML doc remarks for why a batched, fan-in relay can't do that safely). Shared unchanged by `CoreEx.Database.Outbox` and `CoreEx.Cosmos.Outbox`'s relays.
- 🧹 **App-wide payload redaction**: `EventFormatter.DataExcludePaths` applies a `CoreEx.Json.JsonFilter` exclude (recursive descent, e.g. `$..etag`) to every event's `Data` during `Format()`, so a property can be stripped from all published events in one place rather than at every `EventData.WithValue()` call site. Defaults to excluding `$..etag` — an optimistic-concurrency token that has no meaning to a downstream consumer and cannot be reliably captured for events raised transactionally via an outbox against a NoSQL store; set to `null`/empty to opt out.
- 📤 **Queue-then-publish pipeline**: Events are buffered in-process and dispatched atomically via `PublishAsync()`; `Rollback(count)` and `Reset()` support outbox and retry patterns.
- 📍 **Destination resolution**: `IDestinationProvider` dynamically generates topic/queue names from an `EventData`, an explicit destination string, or from `MessageType` and domain name.
- 📥 **Structured subscriber dispatch**: `SubscribedManager` routes incoming events to `[Subscribe]`-decorated handlers, enforces inbox idempotency checks, and manages ambiguous- and not-subscribed outcomes.
- 🛡️ **Configurable error handling**: `ErrorHandling` enum values (`CompleteAsSilent`, `Retry`, `DeadLetter`, `Catastrophic`, and more) are mapped per exception type via a fluent `ErrorHandler` configurator — no boilerplate try/catch in subscriber code.
- 📊 **OpenTelemetry metrics**: `EventSubscriberMetrics` exposes a `messages.received` counter via `System.Diagnostics.Metrics`; `EventPublisherInvoker` and `SubscribedInvoker` wrap operations in activity spans.
- 🧩 **`MessageType` discrimination**: Distinguishes `Event`, `Command`, and `ReplyTo` semantics for destination-name generation.

## Native publishing

`MessageType` describes intent (event, command, reply); `MessageRepresentation` describes the external envelope (`CloudEvent`, the default, or `Native`). They are independent. Native messages use the same `IEventPublisher`, destination resolution, partition requirements, outboxes and relays. The final Service Bus publisher emits only the payload, not a CloudEvent.

```csharp
var message = EventData.CreateNativeWith(new { OrderId = orderId, ETag = "external-token" })
    .WithKey(orderId)
    .WithTitle("partner.order.created");
message.CorrelationId = orderId;
message.SetAttribute("revision", "1");
// Inside the existing unit-of-work transaction:
unitOfWork.Events.Add("partner-orders", message);

// Keep command routing; change only the wire representation.
var command = EventData.CreateCommandWith("products", request, "reserve").AsNative().WithKey(orderId);
unitOfWork.Events.Add(command);

// Preserve these exact bytes, even though their media type is JSON.
var opaque = EventData.CreateNativeWithData(BinaryData.FromString("{ \"x\": 1 }\n", "application/json"))
    .WithKey(orderId);
```

`CreateNative()` creates a payload-free native event. `CreateNativeWith<T>` and `WithValue` serialize CLR values as JSON; a string becomes a JSON string, not a pre-serialized document. JSON guarantees content equivalence, not whitespace/byte identity. `CreateNativeWithData` and `WithData` copy opaque bytes and preserve them exactly; queueing then retains the owned byte memory without another payload-sized copy until encoding or transport requires one. `WithValue` resets opaque intent. Native messages bypass automatic `EventFormatter.DataExcludePaths` filtering, including default ETag removal; explicit call-site exclusions still apply. Use the payload methods rather than assigning `Data` when replacing the payload kind.

Native Service Bus Subject comes from the formatted `EventData.Title`, carried by CloudEvent `type`; there is no duplicate native Subject field or omission option. ReplyTo and CorrelationId remain on `EventData` and are stored as ordinary CloudEvent extensions.

Use the existing `Attributes` collection (`SetAttribute`) for custom metadata in either representation. Native output maps eligible custom CloudEvent extensions to unprefixed Service Bus application properties as canonical strings, not CLR-type-preserving values. Existing CloudEvent extension constraints apply: ordinary custom values are strings, not arbitrary objects/numeric primitives. Underscore-prefixed entries and null values are omitted. CloudEvents standard attributes, tracing, configured CoreEx-mapped names, and the reserved `coreex` extension namespace are not exported as custom headers; framework-added tenant/auth/source/schema metadata is not exported indiscriminately.

There is no second public dictionary or custom scalar type-tagging protocol. Receivers interpret strings according to their contract. If a broker filter or external protocol requires a numeric/typed property, set it in the final publisher's `ConfigureMessage` callback (on the relay for outbox delivery); receiver parsing cannot change broker-side filtering.

`DataRepresentation` and `IsDataOpaque` are publishing instructions marked `[JsonIgnore]`, like `MessageType`. Native persistence retains only the representation and delivery metadata needed by the outbox and final publisher; converting an internal CloudEvent back to `EventData` does not restore the original `MessageType` or guarantee an exact `EventData` round-trip. Ordinary `EventData` JSON is not a durable message format. Use formatter conversion and the CloudEvent encode/decode helpers for persistence. `MessageRepresentation` remains in `CoreEx.Events` because it describes both wire representations.

### Correlation and causation

`EventData.CorrelationId` groups messages in a business workflow; `CausationId` identifies the immediately triggering message (normally its Id). Both are optional and are never inferred from trace IDs or ambient context. They map to CloudEvent string extensions `correlationid` and `causationid` for both representations. Standard CloudEvents honor the configured `EventFormatter.CorrelationIdAttributeName` and `CausationIdAttributeName`; native internal envelopes use canonical names so relay formatter configuration cannot expose mapped metadata as custom headers. Direct CloudEvents use the existing generic `SetExtensionAttribute`/`TryGetExtensionAttribute` helpers; there is no dedicated accessor API or special validation policy.

Service Bus broker mapping recognizes the standard `correlationid` name, just as tracing recognizes standard trace names. Keep the default for automatic broker CorrelationId mapping; a custom alias remains CE metadata and requires explicit `ConfigureMessage` mapping when broker CorrelationId is needed.

These names align with the upstream [Correlation extension document](https://github.com/cloudevents/spec/blob/main/cloudevents/extensions/correlation.md), not mandatory CloudEvents 1.0 core attributes. Correlation is mapped to Service Bus's broker CorrelationId for both representations, including when optional CE attributes are excluded. Causation stays CE metadata; explicitly export it using the final publisher's `ConfigureMessage` callback if a native external protocol requires it. Neither value changes tracing, routing or message identity.

`Add(EventData)` formats and queues the payload without making another payload-sized byte copy; `WithValue` serializes into an owned buffer, and `WithData` snapshots caller-owned bytes when assigned. If assigning `Data` directly, do not mutate or reuse its backing bytes after adding the event to a publisher queue and before publication completes. The queue captures the destination, formatted type and representation marker; subsequent changes to the `EventData` object do not rewrite those envelope fields. Custom formatters must preserve representation and durable metadata or enqueueing fails. Advanced callers supplying `CloudEvent`/`DestinationEvent` directly retain their existing object-ownership contract and must not mutate queued envelopes; the final transport still validates the native marker.

### Internal storage contract and rollout

Native messages retain an internal CloudEvent for queue/outbox compatibility. The only CoreEx-owned protocol extension is the Boolean `coreexisdatanative` marker; `type`, `replyto`, custom attributes and payload data remain in their normal CloudEvent fields/extensions. Framework-mapped metadata uses canonical extension names in native envelopes regardless of formatter aliases, allowing relays with different formatter configuration to suppress it safely rather than export tenant/authentication values as custom headers. Opaque payloads use CloudEvents `data_base64`, preserving their bytes without a separate opaque flag or JSON filtering. Message intent is not persisted: destination selection has already happened before the envelope enters the queue/outbox, and native reconstruction does not promise to restore the original `MessageType`. Do not populate the marker or any other `coreex`-prefixed extension through `EventData.Attributes`. Missing/false marker means ordinary CloudEvent output; malformed marker values fail explicitly. `CoreEx.Events.Native.NativeMessageEnvelopeCodec` validates and reads the marker and eligible custom extensions; applications should normally use the public factories and formatter configuration instead.

Ordinary JSON is stored in CE `data`; opaque data uses `data_base64`, decoded before sending. Complete media types, including parameters, survive reconstruction. JSON objects, arrays, scalars and explicit JSON `null` are retained; `WithValue(null)` means absent data instead. Absent and zero-length bodies both become empty Service Bus bodies. The formatter uses configured title/source conventions; standalone native messages use `urn:coreex:native` when no source is configured and `coreex.native` if the resulting title is blank. The normal partition-key requirement is unchanged.

**Upgrade every relay/final publisher before enabling native writes.** Older versions ignore the marker and can publish the internal CloudEvent externally. Do not roll back while native messages remain queued; drain or quarantine the backlog first. No outbox schema migration is needed. Native delivery does not change atomicity, at-least-once delivery, deduplication or session policies.

### CloudEvent payload compatibility changes

Structured decoding now retains JSON token bytes and complete content types, decodes non-JSON text without adding JSON quotes, and restores Base64 as `BinaryData`. Non-JSON `BinaryData` encodes using `data_base64`; JSON `BinaryData` uses validated `data`, including `+json` media types and content-type parameters. Explicit JSON `null` no longer collapses to absent data. `ConvertFromCloudEvent` still requires normalized `BinaryData`; normalize external envelopes with the decode helpers rather than supplying arbitrary CLR payloads. Binary encoding validates required CE attributes, just like structured encoding.

Structured inference recognizes `application/cloudevents+json` (case-insensitive, parameters allowed), not arbitrary media types sharing the `application/cloudevents` prefix. See [Service Bus compatibility changes](../CoreEx.Azure.Messaging.ServiceBus/README.md#cloudevent-binding-compatibility-changes) for required metadata, version and timestamp behavior.

## Baggage policy and 4.4 migration

**Behavior change for 4.4:** incoming CloudEvent baggage is no longer automatically imported into `Activity.Current`, and publishing no longer automatically captures ambient activity baggage. Trace parent/state capture and producer linking remain automatic; message processing, validation and settlement are unchanged.

Enable receive propagation on the owning `EventSubscriberBase` (including `ServiceBusSubscribedSubscriber` or `ServiceBusSubscriberBase<TValue>`) with `PropagateBaggage = true`. This single policy covers both CloudEvent envelopes and native Service Bus application properties. The shared `EventBaggage` parser/importer rejects encoded headers over 8192 UTF-8 bytes, considers at most 64 entries per import, bounds decoded imports by their encoded size, and preserves all existing activity baggage keys. Malformed/oversized tracing metadata does not reject otherwise-valid event data. Transport metadata is imported first, so it wins over duplicate envelope keys when enabled. Baggage propagation does not require a trace parent and never creates an activity or authenticated tenant/user context.

Enable ambient capture separately with `AddEventFormatter(formatter => formatter.CaptureBaggage = true)`. Explicit `EventData.TraceBaggage`, explicit `AddTracing(..., traceBaggage: ...)` arguments and baggage already present in a supplied CloudEvent remain intentional wire data, independent of ambient capture. Parsing an envelope retains bounded `EventData.TraceBaggage` even when activity import is disabled; the setting controls propagation, not removal of message metadata. Only opt in for trusted producers and approved baggage keys: baggage flows to downstream dependencies and can contain sensitive data.

Outbox relay batch activities continue to link originating traces without importing any event's baggage, avoiding cross-event leakage. Formatter-configured trace attribute names are honored by envelope conversion; Service Bus transport mirroring and relay extraction still recognize standard trace names, so use the defaults for end-to-end transport/relay tracing.

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
| **`CoreEx.Events.Native`** | Durable native-message protocol codec; representation remains independent of event/command intent. | [Native publishing](#native-publishing) |
| **`CoreEx.Events.Publishing`** | Two-phase queue-then-publish pipeline: `IEventQueue`, `IEventPublisher`, `EventPublisherBase`, `IDestinationProvider`, `DestinationEvent`, `NoOpEventPublisher`. | [📖 README](./Publishing/README.md) |
| **`CoreEx.Events.Subscribing`** | Subscriber dispatch and error handling: `EventSubscriberBase`, `SubscribedManager`, `SubscribedBase`, `ErrorHandler`, `ErrorHandling`, subscriber exceptions. | [📖 README](./Subscribing/README.md) |

## Related namespaces

- **[`CoreEx`](../CoreEx/README.md)** - Defines `EventData`, `CloudEvent` interop, `ExecutionContext`, and `Result<T>` used throughout the events pipeline.
- **[`CoreEx.Database.Outbox`](../CoreEx.Database/Outbox/README.md)** / **[`CoreEx.Cosmos.Outbox`](../CoreEx.Cosmos/README.md#namespaces)** - Outbox-pattern publishers that wrap `IEventPublisher`; persist events transactionally (relational outbox table / Cosmos DB `TransactionalBatch`) and relay them via a background host (poll-loop / Change Feed Processor), sharing the same `CloudEventTracingExtensions` and harmonized metric naming.
- **[`CoreEx.DomainDriven`](../CoreEx.DomainDriven/README.md)** - `Aggregate<TId, TSelf>` accumulates `EventData` internally; the application layer forwards those to the publishing queue within the same unit-of-work.
- **[`CoreEx.Invokers`](../CoreEx/Invokers/README.md)** - `EventPublisherInvoker` and `SubscribedInvoker` provide OpenTelemetry activity wrapping for publish and receive operations.

## AI Usage Guide

An [`AGENTS.md`](./AGENTS.md) file is included with this package. AI coding assistants (GitHub Copilot, Claude, Cursor, etc.) that support workspace-injected package documentation will automatically surface concise usage guidance, code examples, and `Do Not` rules for this package without requiring a local CoreEx checkout.
