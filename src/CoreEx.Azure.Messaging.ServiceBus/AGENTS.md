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
- [Hosts layer](../../samples/docs/hosts-layer.md) — Subscribe and Outbox.Relay host `Program.cs` shapes and Azure Service Bus wiring.
- [Infrastructure layer](../../samples/docs/infrastructure-layer.md) — outbox table setup and relay publisher integration.
- [Patterns](../../samples/docs/patterns.md) — transactional outbox, event publishing, and subscriber error-handling patterns.
