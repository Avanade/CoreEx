namespace Contoso.Products.Subscribe.Subscribers;

[ScopedService, Subscribe("contoso.products.reservation.confirm")]
public class ReservationConfirmSubscriber : SubscribedBase
{
    /// <summary>
    /// Handle the scenario where a pending reservation is not found; possible where the reservation has expired and been removed, or was reserved with non-stocked products and thus not persisted.
    /// </summary>
    internal static readonly ErrorHandler DefaultErrorHandler = new ErrorHandler().Add<NotFoundException>(ex => ex.ErrorCode == "pending-reservation-not-found" ? ErrorHandling.CompleteAsInformation : null);

    private readonly IMovementService _service;

    public ReservationConfirmSubscriber(IMovementService service)
    {
        _service = service.ThrowIfNull();
        ErrorHandler = DefaultErrorHandler;
    }

    protected async override Task<Result> OnReceiveAsync(EventData @event, EventSubscriberArgs args, CancellationToken cancellationToken = default)
    {
        var referenceId = @event.Key.Required();
        await _service.ConfirmReservationAsync(referenceId).ConfigureAwait(false);
        return Result.Success;
    }
}