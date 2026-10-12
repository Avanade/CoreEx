namespace Contoso.Shopping.Application.Adapters.Notifications;

/// <summary>
/// Enables outbound customer notifications, serving as the external dependency boundary (anti-corruption layer) for notification-related operations.
/// </summary>
public interface INotificationAdapter
{
    /// <summary>
    /// Sends a confirmation notification for the specified checked-out <paramref name="basket"/>.
    /// </summary>
    Task<Result> SendBasketCheckedOutConfirmationAsync(Contracts.Basket basket, CancellationToken ct = default);
}
