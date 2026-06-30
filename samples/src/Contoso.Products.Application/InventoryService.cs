namespace Contoso.Products.Application;

[ScopedService<IInventoryService>]
public class InventoryService(IInventoryRepository repository) : IInventoryService
{
    private readonly IInventoryRepository _repository = repository.ThrowIfNull();

    public async Task<decimal> GetOnHandAsync(string productId, CancellationToken ct = default)
    {
        var qtyOnHand = await _repository.GetOnHandAsync(productId, true, ct).ConfigureAwait(false);
        return qtyOnHand ?? 0m;
    }
}