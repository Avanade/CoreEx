# CoreEx.Azure.Messaging.ServiceBus — AI Usage Guide

Provides Azure Service Bus publishing and subscriber hosting for the CoreEx events pipeline.

## Publishing

Register the publisher in `Program.cs`. When the outbox pattern is used (recommended), Service Bus is **not** the default `IEventPublisher` — the outbox publisher is. Pass `addAsDefaultIEventPublisher: false`.

```csharp
// Outbox-backed API or Subscribe host — outbox publishes to the DB; relay forwards to Service Bus
builder.Services.AddAzureServiceBusPublisher((_, c) =>
{
    c.SessionIdStrategy = ServiceBusSessionStrategy.UsePartitionKeyConvertedToAnId;
}, addAsDefaultIEventPublisher: false);

// Outbox Relay host — Service Bus IS the default publisher (no outbox layer here)
builder.Services.AddAzureServiceBusPublisher((_, c) =>
{
    c.SessionIdStrategy = ServiceBusSessionStrategy.UsePartitionKeyConvertedToAnId;
});
```

`SessionIdPartitionSize` can be configured independently with `CoreEx:Host:ServiceBus:SessionIdPartitionSize` (default: `PartitionKey.DefaultPartitionSize`). Exact destination names can override `SessionIdStrategy` and/or `SessionIdPartitionSize` under `CoreEx:Host:ServiceBus:Destinations:{destination}`. Prefer stable entity keys and tune bounded bucketing only where the ordering/concurrency profile warrants it. Do not reuse the relational outbox setting: direct Service Bus publishing can bypass the outbox, and the two sizes control separate ordering and concurrency boundaries.

Changing a destination's session strategy or bucket count can move the same key to a different session. A rolling deployment with mixed producer policies can therefore break per-key ordering. Pause all affected producers (including direct publishers), drain upstream outboxes and affected broker queues/subscriptions and in-flight handlers, then switch policies consistently before resuming. Changing receiver concurrency alone does not change the producer's key-to-session mapping.

Default publisher send retries use `ServiceBusErrorClassifier.IsTransient`. At the publication boundary, classifier-confirmed Service Bus failures become generic CoreEx `TransientException` failures with the final broker exception retained as `InnerException` and error logging enabled; permanent failures and caller cancellation remain unchanged. Batch creation is translated by the same boundary but is outside the send retry pipeline. CoreEx WebApi defaults to HTTP 503 and `Retry-After: 90` for this exception. Acceptance may be unconfirmed or partial, so retries require stable message identities and application-level idempotency; this is not a rollback guarantee. A committed outbox-backed API request is not retroactively failed by a later relay publish failure.

## Native Publishing and Customization

Native messages use the same `IEventPublisher`, outbox and session strategies. Create them with `EventData.CreateNativeWith(value)` or `.AsNative()` on a command/event; use `CreateNativeWithData(BinaryData)` for exact bytes. Their external body has no CloudEvent envelope. Subject comes from formatted Title (`CloudEvent.Type`); use `ConfigureMessage` on the final publisher for exceptional overrides. When set, `EventData.Key` is emitted as the `Key` Service Bus application property for both native and CloudEvent messages; it is independent of `PartitionKey` and is preserved when optional CE attributes are excluded. The `Key` property name is reserved, so a native custom extension named `key` is not forwarded. Type and ReplyTo survive storage in ordinary CloudEvent fields/extensions. Eligible custom CloudEvent extensions become unprefixed native string headers; underscore-prefixed/null entries are omitted, and no CLR scalar types are preserved. Standard CloudEvent attributes, configured CoreEx-mapped properties, tracing, and the reserved `coreex` extension namespace are not exported as custom headers. `EventData.CorrelationId` maps through the shared CE extension to broker CorrelationId for both representations, even with optional CE attributes excluded. `CausationId` is shared CE metadata, not an automatic native application property. Use `ConfigureMessage` for explicitly typed broker properties. See [native output](./README.md#native-output-and-send-time-customization).

Configure `ServiceBusPublisher.ConfigureMessage = (destination, envelope, message) => ...` in `AddAzureServiceBusPublisher` on the actual final sending host (relay for outbox delivery). It runs after session assignment and before batching, once per prepared message per whole publish attempt; it is not repeated by batch overflow/send retries, but whole retries/redelivery invoke it again. Keep it deterministic and side-effect-free. All preparation precedes sends, so callback errors send nothing in that invocation. Trusted overrides can defeat ordinary payload/ID/session guarantees. Upgrade all relays before native writes and do not roll back with native backlog.

Structured JSON CE detection does not need duplicated application properties. Binary CE requires supported specversion, source and broker ID/type; it retains supplied time even when optional attributes are excluded and never invents occurrence time from EnqueuedTime. Broker ID/type/content type win binary duplicate conflicts; structured body metadata wins structured conflicts. CE application-property values are canonical strings. See [binding compatibility changes](./README.md#cloudevent-binding-compatibility-changes).

## Receive Payload Cleanup

`AutoCleanValue` defaults false, unlike default-on WebApi request cleanup: events are generally expected to be canonical already, so consumer cleanup is typically redundant and can reinterpret producer-owned data. Enable deliberately for noncanonical feeds or commands that own normalization. Native `ServiceBusSubscriberBase<TValue>` uses the inherited bool; dispatched `SubscribedBase<TValue>` handlers have a nullable per-handler/message-type override (null inherits owner, true enables, false disables). Set it in the handler constructor; separate handlers sharing a payload CLR type may differ. Cleanup precedes required checks, validation and handling, uses `CleanArgs.Default` without object collapse, and does not change serialized data, envelope keys or transport metadata. Cleanup failures use normal error handling, not malformed-data handling. See [cleanup policy](./README.md#optional-payload-cleanup).

## Subscribe Host Wiring

```csharp
// Register the event formatter and subscriber manager first
builder.Services
    .AddEventFormatter()
    .AddSubscribedManager((_, c) => c.AddSubscribersUsing<MySubscriber>());
// AddSubscribersUsing<T> scans the assembly of T and auto-registers all [Subscribe]-decorated classes.

// Wire the Service Bus receiver
builder.Services.AzureServiceBusReceiving()
    .WithSessionReceiver(_ =>
    {
        var o = ServiceBusSessionReceiverOptions.CreateForTopicSubscription();
        o.SessionProcessorOptions.MaxConcurrentSessions = 4;
        return o;
    })
    .WithSubscribedSubscriber()   // routes messages through SubscribedManager
    .WithHostedService()          // runs as a BackgroundService
    .Build();
```

To consume more than one entity from a single host (e.g. the shared event topic subscription **and** the domain's command queue, as routed by `NamedDestinationProvider`), give each receiver its own service keys so they do not collide. The hosted-service key is also the health-check and hosted-service management name, so make it descriptive:

```csharp
builder.Services.AzureServiceBusReceiving()
    .WithSessionReceiver(_ => ServiceBusSessionReceiverOptions.CreateForTopicSubscription(), "receiver-events")
    .WithKeyedSubscribedSubscriber("subscriber-events")
    .WithHostedService("hosted-subscriber-events")
    .Build();

builder.Services.AzureServiceBusReceiving()
    .WithSessionReceiver(_ => ServiceBusSessionReceiverOptions.CreateForQueue("contoso-products"), "receiver-commands")
    .WithKeyedSubscribedSubscriber("subscriber-commands")
    .WithHostedService("hosted-subscriber-commands")
    .Build();
```

Keyed subscribers are resolved in tests via `GetRequiredKeyedService<ServiceBusSubscribedSubscriber>("subscriber-events")`.

## Subscriber Classes

Subscribers are decorated with `[Subscribe("subject")]` and extend `SubscribedBase` (untyped) or `SubscribedBase<TValue>` (typed payload). Register with `[ScopedService]`.

```csharp
// Untyped subscriber
[ScopedService, Subscribe("contoso.orders.order.created.v1")]
public class OrderCreatedSubscriber(IOrderService service) : SubscribedBase
{
    protected override async Task<Result> OnReceiveAsync(EventData @event, EventSubscriberArgs args,
        CancellationToken cancellationToken = default)
    {
        var id = @event.Key.Required();
        await service.ProcessCreatedAsync(id, cancellationToken).ConfigureAwait(false);
        return Result.Success;
    }
}

// Typed subscriber with validation
[ScopedService, Subscribe("contoso.orders.order.updated.v1")]
public class OrderUpdatedSubscriber(IOrderService service) : SubscribedBase<Order>
{
    public override IValidator<Order>? ValueValidator => OrderValidator.Default;

    protected override Task<Result> OnReceiveAsync(Order value, EventData @event,
        EventSubscriberArgs args, CancellationToken cancellationToken = default)
        => service.UpdateAsync(value, cancellationToken);
}
```

## Error Handling

For uniform native JSON, derive from `ServiceBusSubscriberBase<TValue>` rather than `SubscribedBase<TValue>`. Override `ValueValidator` and the typed `OnReceiveAsync(TValue value, ServiceBusReceivedMessage message, EventSubscriberArgs args, CancellationToken cancellationToken)`; no `[Subscribe]` attributes or manager are needed. Register through `.WithSubscriber<MySubscriber>()`, or inject into a Function and use `FunctionReceiveAsync`. The base bypasses CloudEvents entirely and preserves the original message metadata. Its constructor takes the logger only; the unused inherited formatter dependency is satisfied internally.

All Service Bus subscribers enrich the host-owned activity from fixed transport properties `traceparent`/`tracestate`, falling back to Azure's `Diagnostic-Id`, independently of formatter-configured CloudEvent attribute names. Producer contexts already represented by local ancestry or links are not linked again. Malformed tracing metadata is ignored without changing settlement. Set `PropagateBaggage = true` only for trusted producers to import bounded W3C baggage; the default is false and existing baggage keys are preserved. Message/correlation/session IDs are diagnostic tags, not metric dimensions or authenticated identity.

Choose `InvalidDataHandling` for malformed/null bodies separately from `ErrorHandler` mappings for validation/application failures. Defaults remain `CompleteAsError`, not dead-lettering. `ValueIsRequired` defaults to true; when overriding false, use nullable `TValue` for optional reference payloads. `JsonSerializerOptions` controls body deserialization; native payload tenant/authentication policy is application-owned.

Use `ErrorHandler` on a subscriber to map specific exceptions to dead-letter, silent completion, or retry — no try/catch in subscriber code.

```csharp
internal static readonly ErrorHandler DefaultErrorHandler = new ErrorHandler()
    .Add<NotFoundException>(_ => ErrorHandling.CompleteAsInformation);
```

## Do Not

- Do not register `AddAzureServiceBusPublisher` as the default publisher when using the transactional outbox — pass `addAsDefaultIEventPublisher: false`.
- Do not add a new subscriber to `Program.cs` — creating the class with `[Subscribe]` and `[ScopedService]` is sufficient; `AddSubscribersUsing<T>()` discovers it automatically.

## Further Reading

- [README](./README.md) — full publisher, subscriber, and receiver API reference.
- [CoreEx.Events](../CoreEx.Events/README.md) — `IEventPublisher`, `EventSubscriberBase`, and `SubscribedManager` that this package binds to Service Bus.
- [Hosts layer](../../samples/docs/hosts-layer.md) — Subscribe and Relay host `Program.cs` shapes and Azure Service Bus wiring.
- [Infrastructure layer](../../samples/docs/infrastructure-layer.md) — outbox table setup and relay publisher integration.
- [Patterns](../../samples/docs/patterns.md) — transactional outbox, event publishing, and subscriber error-handling patterns.
