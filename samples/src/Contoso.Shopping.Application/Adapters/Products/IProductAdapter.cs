namespace Contoso.Shopping.Application.Adapters.Products;

/// <summary>
/// Enables the Products domain integration, serving as the external dependency boundary (anti-corruption layer) for product-related operations.
/// </summary>
public interface IProductAdapter
{
    /// <summary>
    /// Gets the product.
    /// </summary>
    Task<Result<Product>> GetAsync(string id);

    /// <summary>
    /// Reserves the inventory (product * quantity) per basket item. 
    /// </summary>
    Task<Result> ReserveInventoryAsync(Domain.Basket basket);

    /// <summary>
    /// Confirms the reservation(s) for the basket.
    /// </summary>
    Task<Result> CreateConfirmReservationCommand(Domain.Basket basket);

    /// <summary>
    /// Cancels the reservation(s) for the basket.
    /// </summary>
    Task<Result> CancelReservationAsync(Domain.Basket basket);
}