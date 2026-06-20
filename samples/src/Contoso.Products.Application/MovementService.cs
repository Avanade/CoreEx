namespace Contoso.Products.Application;

[ScopedService<IMovementService>]
public class MovementService(IUnitOfWork unitOfWork, IProductRepository productRepository, IMovementRepository movementRepository) : IMovementService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork.ThrowIfNull();
    private readonly IProductRepository _productRepository = productRepository.ThrowIfNull();
    private readonly IMovementRepository _movementRepository = movementRepository.ThrowIfNull();

    /// <inheritdoc/>
    public async Task<List<Movement>> CreateReservationAsync(MovementRequest request)
    {
        // Validate the request
        var vr = await new MovementRequestValidator(productRepository).ValidateAsync(request);
        vr.ThrowOnError();

        // Build up the list of movements to create for the reservation.
        var movements = new List<Movement>();
        foreach (var product in request.Products!.Where(kv => kv.Value.Quantity > 0))
        {
            movements.Add(new Movement
            {
                ReferenceId = request.Id,
                Kind = MovementKind.Issue,
                Status = MovementStatus.Pending,
                ProductId = product.Key,
                Quantity = product.Value.Quantity * -1,
                UnitOfMeasure = product.Value.UnitOfMeasure
            });
        }

        // Exit early if there are no movements to create (e.g. all products had a quantity of 0).
        if (movements.Count == 0)
            return movements;

        // Adjust the inventory levels, create the reservations, and emit the events in a unit-of-work transaction.
        await _unitOfWork.TransactionAsync(async () =>
        {
            // Adjust inventory levels and persist movements.
            movements = await _movementRepository.CreateAsync(movements).ConfigureAwait(false);

            // Emit events for the movements that were created.
            _unitOfWork.Events.Add(EventData.CreateEventsWith(movements, nameof(MovementStatus.Pending), ConfigureEvent));
        }).ConfigureAwait(false);

        return movements;
    }

    /// <inheritdoc/>
    public Task<List<Movement>> ConfirmReservationAsync(string referenceId) => _unitOfWork.TransactionAsync(async () =>
    {
        // Confirm all movements for the specified reservation.
        var movements = await _movementRepository.ConfirmAsync(referenceId).ConfigureAwait(false);
        if (movements.Count == 0)
            throw new NotFoundException().WithKey(referenceId).WithErrorCode("pending-reservation-not-found");

        // Emit events for the movements that were confirmed.
        _unitOfWork.Events.Add(EventData.CreateEventsWith(movements, nameof(MovementStatus.Confirmed), ConfigureEvent));

        return movements;
    });

    /// <inheritdoc/>
    public Task<List<Movement>> CancelReservationAsync(string referenceId) => _unitOfWork.TransactionAsync(async () =>
    {
        // Cancel all movements for the specified reservation.
        var movements = await _movementRepository.CancelAsync(referenceId).ConfigureAwait(false);
        if (movements.Count == 0)
            throw new NotFoundException().WithKey(referenceId).WithErrorCode("pending-reservation-not-found");

        // Emit events for the movements that were canceled.
        _unitOfWork.Events.Add(EventData.CreateEventsWith(movements, nameof(MovementStatus.Canceled), ConfigureEvent));

        return movements;
    });

    /// <inheritdoc/>
    public async Task<List<Movement>> AdjustAsync(MovementRequest request)
    {
        // Validate the request
        var vr = await new MovementRequestValidator(productRepository).ValidateAsync(request);
        vr.ThrowOnError();

        // Build up the list of movements to create for the reservation.
        var movements = new List<Movement>();
        foreach (var product in request.Products!.Where(kv => kv.Value.Quantity > 0))
        {
            movements.Add(new Movement
            {
                ReferenceId = request.Id,
                Kind = MovementKind.Adjust,
                Status = MovementStatus.Confirmed,
                ProductId = product.Key,
                Quantity = product.Value.Quantity,
                UnitOfMeasure = product.Value.UnitOfMeasure
            });
        }

        // Exit early if there are no movements to create (e.g. all products had a quantity of 0).
        if (movements.Count == 0)
            return movements;

        // Adjust the inventory levels, and emit the events in a unit-of-work transaction.
        await _unitOfWork.TransactionAsync(async () =>
        {
            // Adjust inventory levels and persist movements.
            movements = await _movementRepository.CreateAsync(movements).ConfigureAwait(false);

            // Emit events for the movements that were created.
            _unitOfWork.Events.Add(EventData.CreateEventsWith(movements, nameof(MovementStatus.Confirmed), ConfigureEvent));
        }).ConfigureAwait(false);

        return movements;
    }

    /// <summary>
    /// Configure the event name for the specified movement and event data. This is used to emit different events for issue, receive, and adjust movements, which can be useful for event subscribers that want to handle them differently.
    /// </summary>
    private static void ConfigureEvent(Movement m, EventData ed)
    {
        var extra = m.Kind!.Code switch
        {
            MovementKind.Issue => nameof(MovementKind.Issue),
            MovementKind.Receive => nameof(MovementKind.Receive),
            _ => nameof(MovementKind.Adjust)
        };

        ed.Entity += $".{extra}";
        ed.WithPartitionKey(m.ProductId);   // Partition events by product identifier to ensure ordering of events for the same product.
    }
}