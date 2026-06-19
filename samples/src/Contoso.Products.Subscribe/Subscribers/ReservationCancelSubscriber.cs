namespace Contoso.Products.Subscribe.Subscribers;

[ScopedService, Subscribe("contoso.products.reservation.cancel")]
public class ReservationCancelSubscriber : SubscribedBase
{
    private readonly IMovementService _service;

    public ReservationCancelSubscriber(IMovementService service)
    {
        _service = service.ThrowIfNull();
        ErrorHandler = ReservationConfirmSubscriber.DefaultErrorHandler;
    }

    protected async override Task<Result> OnReceiveAsync(EventData @event, EventSubscriberArgs args, CancellationToken cancellationToken = default)
    {
        var referenceId = @event.Key.Required();
        await _service.CancelReservationAsync(referenceId).ConfigureAwait(false);
        return Result.Success;
    }
}