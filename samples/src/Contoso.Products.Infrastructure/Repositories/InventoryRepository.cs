namespace Contoso.Products.Infrastructure.Repositories;

[ScopedService<IInventoryRepository>]
public class InventoryRepository(ProductsEfDb ef) : IInventoryRepository
{
    private readonly ProductsEfDb _ef = ef.ThrowIfNull();

    public async Task<decimal?> GetOnHandAsync(string productId, bool throwNotFoundException, CancellationToken ct = default)
    {
        var products = _ef.Products.Model.Query();
        var inventory = _ef.Inventory.Query();

        var q =
            from p in products
            where p.Id == productId
            join i in inventory on p.Id equals i.Id into ig
            from i in ig.DefaultIfEmpty()
            select new
            {
                HasInventory = i != null,
                QtyOnHand = i == null ? 0m : i.QtyOnHand
            };

        var res = await q.SingleOrDefaultAsync(ct).ConfigureAwait(false);

        // Where existence is not considered important, return zero when not found.
        if (res is null)
            return throwNotFoundException ? throw new NotFoundException() : null;

        // Return the current quantity-on-hand where applicable.
        return res.HasInventory ? res.QtyOnHand : null;
    }
}