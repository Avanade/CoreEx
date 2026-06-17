namespace Contoso.Products.Application;

[ScopedService<IInventoryService>]
public class InventoryService(IInventoryRepository repository) : IInventoryService
{
    private readonly IInventoryRepository _repository = repository.ThrowIfNull();

    public async Task<decimal> GetOnHandAsync(string productId)
    {
        var qtyOnHand = await _repository.GetOnHandAsync(productId, true).ConfigureAwait(false);
        return qtyOnHand ?? 0m;
    }
}