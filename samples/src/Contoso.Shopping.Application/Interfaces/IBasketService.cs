namespace Contoso.Shopping.Application.Interfaces;

public interface IBasketService
{
    /// <summary>
    /// Create a new <see cref="Contracts.Basket"/> for the specified <paramref name="customerId"/>.
    /// </summary>
    Task<Result<Contracts.Basket>> CreateAsync(string customerId);

    /// <summary>
    /// Applies the <paramref name="discountCoupon"/> to the specified <see cref="Contracts.Basket"/>.
    /// </summary>
    Task<Result<Contracts.Basket>> ApplyDiscountAsync(string basketId, DiscountCoupon discountCoupon);

    /// <summary>
    /// Checkout the specified <see cref="Contracts.Basket"/>.
    /// </summary>
    Task<Result<Contracts.Basket>> CheckoutAsync(string basketId);

    /// <summary>
    /// Add (or merge) the specified <paramref name="item"/> into the specified <see cref="Contracts.Basket"/>.
    /// </summary>
    Task<Result<Contracts.Basket>> ItemAddAsync(string basketId, Contracts.BasketItemAddRequest item);

    /// <summary>
    /// Updates an existing <paramref name="item"/> in the specified <see cref="Contracts.Basket"/>.
    /// </summary>
    Task<Result<Contracts.Basket>> ItemUpdateAsync(string basketId, string basketItemId, Contracts.BasketItemUpdateRequest item);

    /// <summary>
    /// Deletes an existing <paramref name="item"/> from the specified <see cref="Contracts.Basket"/>.
    /// </summary>
    Task<Result<Contracts.Basket>> ItemDeleteAsync(string basketId, string basketItemId);
}