# CoreEx.Events — AI Usage Guide

Provides the CoreEx event publishing and subscribing infrastructure: `EventData` ↔ CloudEvents formatting, a two-phase queue-then-publish pipeline, and configurable subscriber dispatch.

## Publishing — Two-Phase Pattern

New `EventData`/create factories snapshot `ExecutionContext.CorrelationId`; property assignment or `WithCorrelationId(null)` can deliberately clear it. Never add formatter/publisher/relay fallback logic. Incoming JSON/CloudEvent/native envelopes preserve their own correlation, including absence. Subscriber handling imports authoritative envelope correlation; invalid legacy metadata is warned about, context is cleared, and payload processing continues. Batch publishing spans omit correlation, including the invoker's ambient tag; stored message correlation is unchanged. Causation and trace identity remain independent. See [correlation and causation](./README.md#correlation-and-causation).

Buffer events on `IUnitOfWork.Events` (the `IEventQueue`) inside `TransactionAsync`, then let the outbox publisher commit them with the database transaction. Do not call `PublishAsync()` directly from application code when using the outbox pattern.

Use `EventData.CreateEventWith` to construct a single event and `EventData.CreateEventsWith` for multiple events. Pass an `EventAction` enum value (or a plain string) as the action.

```csharp
using CoreEx.Events;
using CoreEx.Events.Publishing;

// Single entity event — subject/entity derived automatically from SchemaAttribute or type name
return dr.WhereMutated(v =>
    _unitOfWork.Events.Add(EventData.CreateEventWith(v, EventAction.Created)));

// Delete — no value; set the key explicitly
dr.WhereMutated(() =>
    _unitOfWork.Events.Add(
        EventData.CreateEvent<Order>(EventAction.Deleted).WithKey(id)));

// Multiple events from a collection
_unitOfWork.Events.Add(
    EventData.CreateEventsWith(items, EventAction.Updated, ConfigureEvent));

// Custom action string and explicit destination topic
_unitOfWork.Events.Add(
    "orders-topic",
    EventData.CreateEventWith(contract, "checkedout"));
```

`EventAction` provides the standard past-tense action values: `Created`, `Updated`, `Deleted`, `Activated`, `Deactivated`, `Cancelled`, `CheckedOut`, and more.

## Native Wire Representation

Use `EventData.CreateNativeWith(value).WithKey(key)` for ordinary JSON, `CreateNativeWithData(BinaryData)` for exact bytes, or `.AsNative()` on an existing event/command to preserve its routing intent. `CreateNative()` has no payload. Representation is `DataRepresentation`, not another `MessageType`. Native messages bypass automatic payload redaction; explicit exclusions still apply. `WithValue` resets opaque intent. Keep the existing partition-key policy and enqueue through `IUnitOfWork.Events` inside its transaction when using an outbox.

Native Service Bus Subject comes from formatted Title via CloudEvent `type`; there is no duplicate Subject metadata or omission option. Eligible custom CloudEvent extensions become canonical string native headers, without a second dictionary or CLR type preservation. `ReplyTo`, `CorrelationId` and `CausationId` are on `EventData`; standard CloudEvents use formatter-configured extension names, while native internal envelopes use canonical mapped names so relay formatter configuration cannot expose mapped metadata as custom headers. Neither correlation nor causation is inferred from tracing. Keep standard `correlationid` for automatic Service Bus broker mapping. Native publishing instructions are `[JsonIgnore]`; persist through CloudEvent conversion, not ordinary EventData JSON. Native envelopes do not preserve message intent, since the destination has already been resolved; do not rely on reconstructing the original `MessageType`. `MessageRepresentation` stays in the root namespace; `NativeMessageEnvelopeCodec` is in `.Native`. The Boolean `coreexisdatanative` marker is the only CoreEx-owned protocol extension; the `coreex` extension namespace and standard/mapped CloudEvent properties are reserved. Opaque payloads use `data_base64`, not a separate flag. `Add(EventData)` retains the payload buffer without another copy; `WithData` snapshots caller-owned bytes, while directly assigned `Data` must not be mutated after enqueue. Custom formatters must preserve native metadata. Upgrade all final publishers/relays before enabling native writes and drain native backlog before rolling back. See [native protocol and payload guarantees](./README.md#native-publishing).

## IEventPublisher Registration

`IEventPublisher` is registered by the database outbox publisher in API/application hosts and by Azure Service Bus in relay hosts.

```csharp
// API host — outbox is the publisher; Service Bus is wired separately with addAsDefaultIEventPublisher: false
builder.Services.AddSqlServerOutboxPublisher();

// Relay host — Service Bus is the publisher
builder.Services.AddAzureServiceBusPublisher();
```

## IEventFormatter

Use the default `EventFormatter`; only customise when you need non-standard CloudEvents attribute mapping.

```csharp
builder.Services.AddEventFormatter();   // registers EventFormatter as IEventFormatter
```

From 4.4, ambient baggage capture and incoming baggage propagation are separate opt-in policies, both default false. Use `AddEventFormatter(formatter => formatter.CaptureBaggage = true)` for publishing ambient baggage, and `PropagateBaggage = true` on the owning `EventSubscriberBase` for receive propagation. Explicit `EventData.TraceBaggage`/`AddTracing` baggage and supplied envelope baggage remain wire data independently of capture. CloudEvent/native imports share bounded W3C parsing and preserve existing activity keys; trace linking remains automatic, and relay batches never import envelope baggage. Only enable propagation for trusted producers; baggage is not authenticated tenant/user context. Use standard trace attribute names for Service Bus mirroring and relay extraction.

## Subscribers

Subscriber dispatch is handled by `SubscribedManager`. Subscriber classes are marked `[Subscribe("subject")]` and discovered automatically by `AddSubscribersUsing<T>()`.

Payload cleanup is opt-in: `EventSubscriberBase.AutoCleanValue` defaults false because published events are generally canonical already, making receive cleanup typically redundant (unlike default-on WebApi request cleanup). `SubscribedBase.AutoCleanValue` is a per-handler/message-type nullable override: null inherits the owner, true enables and false disables. Assign it in the handler constructor, not by modifying a global payload-type registration. All subjects on that handler share the setting; separate handlers can differ even with the same payload CLR type. Enabled cleanup uses `CleanArgs.Default` after deserialization and before required checks/validation/handling, without collapsing root/nested objects or altering serialized envelopes, keys or metadata. Cleanup errors follow normal receive error handling. See [cleanup policy](./Subscribing/README.md#optional-payload-cleanup).

See [CoreEx.Azure.Messaging.ServiceBus](../CoreEx.Azure.Messaging.ServiceBus/README.md) for the full subscriber wiring example.

## DestinationProvider

An `IDestinationProvider` derives the topic/queue name for each published message. Register exactly one; the built-ins cover the common cases:

- `AddNamedDestinationProvider()` (**default for new solutions**) — events go to the single shared destination (topic) read from `CoreEx:Events:Destination`; commands go to a per-target-domain queue named `{Destination}-{domain}` (e.g. `contoso-products`), so only the named domain consumes them. A command with no destination domain is self-addressed to the host domain. Both explicit and defaulted command domains use invariant lowercase, keeping destination-specific session policy lookups consistent; the base destination and explicitly supplied destination names are unchanged.
- `AddFixedDestinationProvider("name")` — every message (events and commands) goes to one fixed destination.

For anything else, implement `IDestinationProvider` (`CreateFrom(EventData, bool)`, `CreateFrom(string, bool)`, `CreateNew(MessageType, string?, bool)`) and register it in place of the built-ins.

```csharp
builder.Services.AddNamedDestinationProvider(); // Destination defaults from CoreEx:Events:Destination.
```

## Do Not

- Do not call `IEventPublisher.PublishAsync()` from application-service code — buffering via `IUnitOfWork.Events` is required so events are committed atomically with the database transaction.
- Do not enqueue events outside of `TransactionAsync` — they will not be committed atomically.
- Do not create `CloudEvent` objects manually — use `EventData` and let `IEventFormatter` handle CloudEvents serialization.

## Further Reading

- [README](./README.md) — `EventData`, `IEventFormatter`, `SubscribedManager`, `ErrorHandler`, and `ErrorHandling` reference.
- [CoreEx.Azure.Messaging.ServiceBus](../CoreEx.Azure.Messaging.ServiceBus/README.md) — Service Bus transport binding.
- [CoreEx.Database.SqlServer](../CoreEx.Database.SqlServer/README.md) / [CoreEx.Database.Postgres](../CoreEx.Database.Postgres/README.md) — outbox publisher implementations.
- [Application layer](../../samples/docs/application-layer.md) — how events are enqueued on `IUnitOfWork.Events` inside `TransactionAsync` and drained from aggregates in real service code.
- [Infrastructure layer](../../samples/docs/infrastructure-layer.md) — outbox table wiring, relay host setup, and `IEventPublisher` registration.
- [Patterns](../../samples/docs/patterns.md) — transactional outbox pattern, event naming conventions, and subscriber error-handling strategies.
