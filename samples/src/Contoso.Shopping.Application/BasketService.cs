namespace Contoso.Shopping.Application;

[ScopedService<IBasketService>]
public class BasketService(IUnitOfWork unitOfWork, IBasketRepository repository, IProductAdapter productAdapter, ILogger<BasketService> logger) : IBasketService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork.ThrowIfNull();
    private readonly IBasketRepository _repository = repository.ThrowIfNull();
    private readonly IProductAdapter _productAdapter = productAdapter.ThrowIfNull();
    private readonly ILogger _logger = logger.ThrowIfNull();

    /// <inheritdoc/>
    public Task<Result<Basket>> CreateAsync(string customerId)
    {
        // No validation for customerId is performed here, but could be added as required (e.g. check for valid format, or existence of the customer in the system).

        // Create the aggregate representation.
        var aggregate = Domain.Basket.CreateNew(customerId.ThrowIfNullOrEmpty());

        // Orchestrate the creation of the basket within a transaction, ensuring that any events are only published if the transaction is successful.
        return _unitOfWork.ExecuteAsync(async () =>
        {
            // Create the aggregate in the repository, which will return the created aggregate with any updates (e.g. Id).
            var br = await _repository.CreateAsync(aggregate).ConfigureAwait(false);
            return br.ThenAs(b =>
            {
                // Map the result to the contract representation.
                var contract = BasketMapper.Map(b);

                // Add an event for the creation of the basket.
                _unitOfWork.Events.Add(EventData.CreateEventWith(contract, EventAction.Created));

                return contract;
            });
        });
    }

    /// <inheritdoc/>
    public async Task<Result<Contracts.Basket>> ApplyDiscountAsync(string basketId, DiscountCoupon discountCoupon)
    {
        if (discountCoupon.IsInactive)
            return Result.BusinessError("Discount coupon either does not exist or is no longer active.", e => e.WithErrorCode("discount-inactive"));

        return await OrchestrateUpdateAsync(basketId, basket => basket.ApplyDiscount(discountCoupon), EventAction.Updated);
    }

    /// <inheritdoc/>
    public async Task<Result<Basket>> ItemAddAsync(string basketId, BasketItemAddRequest item)
    {
        // Validate the request, and ensure the product exists (and retrieve the product details for use in the basket item).
        var pr = await Result.GoAsync(() => BasketItemAddRequestValidator.Default.ValidateWithResultAsync(item))
            .ThenAsAsync(item => new ProductPolicy(_productAdapter).EnsureExistsAsync(item.ProductId!));

        if (pr.IsFailure)
            return pr.AsResult();

        // Add the item to the basket.
        return await OrchestrateUpdateAsync(basketId, basket =>
        {
            var product = pr.Value;

            return basket.ItemAdd(Domain.BasketItem.CreateNew(
                product.Id!,
                product.Sku!,
                product.Text!,
                new Domain.ValueObjects.ItemPricing { UnitOfMeasure = product.UnitOfMeasure!, Quantity = item.Quantity, UnitPrice = product.Price }));
        });
    }

    /// <inheritdoc/>
    public async Task<Result<Basket>> ItemUpdateAsync(string basketId, string basketItemId, BasketItemUpdateRequest item)
    {
        var vr = await BasketItemUpdateRequestValidator.Default.ValidateWithResultAsync(item);
        if (vr.IsFailure)
            return vr.AsResult();

        return await OrchestrateUpdateAsync(basketId, basket => basket.ItemUpdate(basketItemId, item.Quantity, item.ETag));
    }

    /// <inheritdoc/>
    public Task<Result<Basket>> ItemDeleteAsync(string basketId, string basketItemId)
        => OrchestrateUpdateAsync(basketId, basket => basket.ItemDelete(basketItemId), EventAction.Updated);

    /// <summary>
    /// Orchestrate the update of the basket by first retrieving the aggregate, performing the mutation, and then updating within a transaction, ensuring that any events are only published if the transaction is successful.
    /// </summary>
    private async Task<Result<Basket>> OrchestrateUpdateAsync(string basketId, Func<Domain.Basket, Result> mutate, EventAction action = EventAction.Updated)
    {
        // Retrieve the aggregate from the repository for modification, and handle any failure (e.g. not found).
        var br = await _repository.GetAsync(basketId).ConfigureAwait(false);
        if (br.IsFailure)
            return br.AsResult();

        // Perform the mutation on the aggregate, then proceed to update.
        return await mutate.ThrowIfNull().Invoke(br.Value)
            .ThenAsAsync(() => UpdateAsync(br.Value, action))
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the "actual" update of the basket within a transaction, ensuring that any events are only published if the transaction is successful.
    /// </summary>
    private Task<Result<Basket>> UpdateAsync(Domain.Basket basket, EventAction action) => _unitOfWork.ExecuteAsync(async () =>
    {
        if (!basket.HasChanges)
            return Result.Ok(BasketMapper.Map(basket));

        // Update the aggregate in the repository, which will return the updated aggregate with any updates (e.g. Id).
        var ur = await _repository.UpdateAsync(basket).ConfigureAwait(false);

        return ur.ThenAs(basket =>
        {
            // Map the result to the contract representation.
            var contract = BasketMapper.Map(basket);

            // Add an event for the update of the basket.
            _unitOfWork.Events.Add(EventData.CreateEventWith(contract, action));

            return contract;
        });
    });

    /// <inheritdoc/>
    public Task<Result<Contracts.Basket>> CheckoutAsync(string basketId) => Result
        .GoAsync(() => _repository.GetAsync(basketId))                      // Retrieve the basket aggregate from the repository.
        .Then(basket => basket.Checkout().ThenAs(() => basket))             // Checkout the basket.
        .ThenAsync(basket => _productAdapter.ReserveInventoryAsync(basket)) // Reserve the inventory; external service call, not transactional, fail-fast.
        .ThenAsAsync(async basket =>
        {
            try
            {
                return await _unitOfWork.ExecuteAsync(() => Result
                    .GoAsync(() => _repository.UpdateAsync(basket))     // Update the basket aggregate to reflect the checkout (e.g. status change).
                    .ThenAsAsync(async basket =>
                    {
                        // Map the result to the contract representation.
                        var contract = BasketMapper.Map(basket);

                        // Outbox the "checked-out" event for the basket aggregate.
                        _unitOfWork.Events.Add(EventData.CreateEventWith(contract, EventAction.CheckedOut));

                        // Note: the reservation confirmation is outboxed as a command to the products domain, as it's an action that we want to occur asynchronously after the transaction has completed, but it doesn't
                        // necessarily need to be performed immediately after the checkout event (e.g. it could be performed after some delay, or after some other events have been processed). By using a command, we
                        // also have more flexibility in how we handle failures and retries for the reservation confirmation, without impacting the checkout event processing.
                        return (await _productAdapter.CreateConfirmReservationCommand(basket).ConfigureAwait(false))
                            .ThenAs(() => contract);
                    })).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (_logger.IsEnabled(LogLevel.Error))
                    _logger.LogError(ex, "An error occurred during the checkout process; the reserved inventory will be cancelled asynchronously directly bypassing the Outbox.");

                // Cancel the inventory reservation directly, bypassing the Outbox, as the transaction has failed at this point and we don't want to leave the inventory in a reserved state.
                await _productAdapter.CancelReservationAsync(basket).ConfigureAwait(false);

                // It's bad, keep throwing.
                throw; 
            }
        });
}