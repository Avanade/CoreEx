namespace Contoso.Shopping.Infrastructure.Adapters;

[ScopedService<IProductAdapter>]
public class ProductAdapter(ShoppingEfDb ef, IEventPublisher eventPublisher, ProductsHttpClient client, [FromKeyedServices("AzureServiceBus")] IEventPublisher serviceBusPublisher) : IProductAdapter
{
    private readonly ShoppingEfDb _ef = ef.ThrowIfNull();
    private readonly IEventPublisher _eventPublisher = eventPublisher.ThrowIfNull();
    private readonly ProductsHttpClient _client = client.ThrowIfNull();
    private readonly IEventPublisher _serviceBusPublisher = serviceBusPublisher.ThrowIfNull();

    /// <inheritdoc/>
    /// <remarks>Leverages the internal event-based replication store.</remarks>
    public Task<Result<Product>> GetAsync(string id)
        => Result.GoAsync(() => _ef.Products.GetWithResultAsync(id))
                 .ThenAs(p => ProductMapper.From.Map(p));

    /// <inheritdoc/>
    /// <remarks>Invokes the Products API directly (real-time) to perform reservation; resulting BusinessException will bubble out.</remarks>
    public async Task<Result> ReserveInventoryAsync(Domain.Basket basket)
    {
        // Get the list of non-stocked products in the basket; we don't need to reserve inventory for those, and we want to avoid sending them in the reservation request to the Products API.
        var products = basket.Items.Select(i => i.ProductId).ToArray();
        products = await _ef.Products.Query().Where(p => products.Contains(p.Id!) && !p.IsNonStocked).Select(p => p.Id!).ToArrayAsync();

        // Check where no inventory reservation needed, so return success immediately; i.e. all products in the basket are non-stocked.
        if (products.Length == 0)
            return Result.Success;

        // Create the reservation request for the basket.
        var req = new Clients.MovementRequest
        {
            Id = basket.Id,
            Products = basket.Items.Where(i => products.Contains(i.ProductId)).ToDataMap(
                x => x.ProductId,
                x => new Clients.MovementRequestProduct
                {
                    Quantity = x.Pricing.Quantity,
                    UnitOfMeasure = x.Pricing.UnitOfMeasure
                })
        };

        // Reserve the inventory for the basket using the typed http client; if successful, return the basket (unchanged).
        return await _client.CreateReservationAsync(req).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    /// <remarks>Confirms the inventory reservation for the basket by creating the necessary confirmation command-style message to be sent/published as part of the consuming check-out unit-of-work.</remarks>
    public Task<Result> CreateConfirmReservationCommand(Domain.Basket basket)
    {
        _eventPublisher.Add(EventData.CreateCommand("products", "reservation", "confirm").WithKey(basket.Id));
        return Result.SuccessTask;
    }

    /// <inheritdoc/>
    /// <remarks>This is invoked when the check-out unit-of-work fails; therefore, we need to bypass the Outbox (it may have been the failure point) and send via the message broker directly.</remarks>
    public Task<Result> CancelReservationAsync(Domain.Basket basket)
    {
        _serviceBusPublisher.Add(EventData.CreateCommand("products", "reservation", "cancel").WithKey(basket.Id));
        return Result.GoAsync(() => _serviceBusPublisher.PublishAsync());
    }
}