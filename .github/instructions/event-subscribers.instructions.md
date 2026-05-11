---
applyTo: "**/Subscribe/**/*.cs"
description: "Event subscriber conventions: SubscribedBase inheritance, Service Bus integration, error handling, and scoped service registration"
tags: ["subscribers", "messaging", "service-bus", "event-handling", "integration"]
---

# Event Subscriber Conventions

## NuGet / Project References

| Package | Key types provided |
|---|---|
| `CoreEx.Azure.Messaging.ServiceBus` | `SubscribedBase`, `[Subscribe(...)]`, `EventSubscriberArgs`, `ErrorHandler`, `ErrorHandling`, `ServiceBusSessionReceiverOptions`, `AzureServiceBusReceiving()`, `.WithSessionReceiver()`, `.WithSubscribedSubscriber()`, `.WithHostedService()` |
| `CoreEx.Events` | `EventData`, `EventData.Key`, `.ToData<T>()` |
| `CoreEx.Results` | `Result`, `Result.Success` |
| `CoreEx` | `[ScopedService]`, `.ThrowIfNull()`, `.Required()` |

## Structure

- Subscriber classes inherit from `SubscribedBase`.
- Decorate with `[ScopedService]` and `[Subscribe("subject.pattern")]`.
- Inject service dependencies via constructor and guard with `.ThrowIfNull()`.
- Override `OnReceiveAsync` — return `Result.Success` on completion.

```csharp
[ScopedService, Subscribe("contoso.products.reservation.confirm")]
public class ReservationConfirmSubscriber : SubscribedBase
{
    private readonly IMovementService _service;

    public ReservationConfirmSubscriber(IMovementService service)
    {
        _service = service.ThrowIfNull();
    }

    protected async override Task<Result> OnReceiveAsync(
        EventData @event,
        EventSubscriberArgs args,
        CancellationToken cancellationToken = default)
    {
        var referenceId = @event.Key.Required();
        await _service.ConfirmReservationAsync(referenceId).ConfigureAwait(false);
        return Result.Success;
    }
}
```

## Subject Naming

Use dot-separated lowercase subject strings in the format:

```
{solution}.{domain}.{entity}.{action}
```

Examples:
- `contoso.products.product.created.v1`
- `contoso.products.product.updated.v1`
- `contoso.products.reservation.confirm`
- `contoso.products.reservation.cancel`
- `contoso.shopping.basket.checkedout.v1`

Versioned event subjects (published from the domain outbox) include `.v1`. Command subjects (point-to-point) do not include a version suffix.

## Error Handling

Define a static `ErrorHandler` when certain known exceptions should be swallowed or handled differently. Assign it to `this.ErrorHandler` in the constructor:

```csharp
internal static readonly ErrorHandler DefaultErrorHandler = new ErrorHandler()
    .Add<NotFoundException>(ex =>
        ex.ErrorCode == "pending-reservation-not-found"
            ? ErrorHandling.CompleteAsInformation
            : null);

public ReservationConfirmSubscriber(IMovementService service)
{
    _service = service.ThrowIfNull();
    ErrorHandler = DefaultErrorHandler;
}
```

- `ErrorHandling.CompleteAsInformation` — consume the message without error; log as informational.
- `null` return — fall through to default error handling (retry / dead-letter).

Share the same `ErrorHandler` instance across related subscribers (e.g., both Confirm and Cancel use the same handler).

## Accessing Event Data

Extract the key and optional data from `EventData`:

```csharp
var referenceId = @event.Key.Required();      // Message key (partition/session key)
var data = @event.ToData<MyEventPayload>();   // Deserialize typed payload
```

Use `.Required()` on the key to throw a descriptive error if it is missing rather than a null reference exception.

## Service Bus Registration

In `Program.cs`, register subscribers using `AddSubscribersUsing<T>()` to discover all subscriber classes in the same assembly:

```csharp
builder.Services.AddSubscribedManager((_, c) => c.AddSubscribersUsing<ReservationConfirmSubscriber>());

builder.Services.AzureServiceBusReceiving()
    .WithSessionReceiver(_ =>
    {
        var o = ServiceBusSessionReceiverOptions.CreateForTopicSubscription();
        o.SessionProcessorOptions.MaxConcurrentSessions = 4;
        return o;
    })
    .WithSubscribedSubscriber()
    .WithHostedService()
    .Build();
```

## Integration-Events Only

- Subscribers react to integration events published over the broker. 
- Do not use MediatR or in-process domain event dispatchers.
- Keep subscriber logic thin — delegate to the Application service layer; do not embed business logic directly in the subscriber.
