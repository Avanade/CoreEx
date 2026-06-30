namespace Contoso.Products.Application.Interfaces;

public interface IMovementService
{
    /// <summary>
    /// Creates a reservation for inventory movement(s); inventory is adjusted, bit still requires confirmation to be finalized.
    /// </summary>
    Task<List<Movement>> CreateReservationAsync(MovementRequest request, CancellationToken ct = default);

    /// <summary>
    /// Confirms a reservation for inventory movement(s); inventory is finalized and cannot be canceled after this step.
    /// </summary>
    Task<List<Movement>> ConfirmReservationAsync(string referenceId, CancellationToken ct = default);

    /// <summary>
    /// Cancels a reservation and reverses any inventory adjustments made during the reservation.
    /// </summary>
    Task<List<Movement>> CancelReservationAsync(string referenceId, CancellationToken ct = default);

    /// <summary>
    /// Adjusts inventory movement(s) directly without creating a reservation; inventory is adjusted immediately and cannot be canceled.
    /// </summary>
    Task<List<Movement>> AdjustAsync(MovementRequest request, CancellationToken ct = default);
}