# CoreEx.Events — AI Usage Guide

Provides the CoreEx event publishing and subscribing infrastructure: `EventData` ↔ CloudEvents formatting, a two-phase queue-then-publish pipeline, and configurable subscriber dispatch.

## Publishing — Two-Phase Pattern

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
        EventData.CreateEventWith<Order>(default, EventAction.Deleted).WithKey(id)));

// Multiple events from a collection
_unitOfWork.Events.Add(
    EventData.CreateEventsWith(items, EventAction.Updated, ConfigureEvent));

// Custom action string and explicit destination topic
_unitOfWork.Events.Add(
    "orders-topic",
    EventData.CreateEventWith(contract, "checkedout"));
```

`EventAction` provides the standard past-tense action values: `Created`, `Updated`, `Deleted`, `Activated`, `Deactivated`, `Cancelled`, `CheckedOut`, and more.

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

## Subscribers

Subscriber dispatch is handled by `SubscribedManager`. Subscriber classes are marked `[Subscribe("subject")]` and discovered automatically by `AddSubscribersUsing<T>()`.

See [CoreEx.Azure.Messaging.ServiceBus](../CoreEx.Azure.Messaging.ServiceBus/README.md) for the full subscriber wiring example.

## DestinationProvider

Implement `IDestinationProvider` to derive topic/queue names from `EventData`. Return `null` to fall back to the next registered provider.

```csharp
public class MyDestinationProvider : IDestinationProvider
{
    public string? GetDestination(EventData @event)
        => @event.Subject?.StartsWith("contoso.orders") == true
            ? "orders-topic"
            : null;
}
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
