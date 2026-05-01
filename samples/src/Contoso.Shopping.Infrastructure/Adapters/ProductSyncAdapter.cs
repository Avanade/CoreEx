namespace Contoso.Shopping.Infrastructure.Adapters;

[ScopedService<IProductSyncAdapter>]
public class ProductSyncAdapter(ShoppingEfDb ef) : IProductSyncAdapter
{
    private readonly ShoppingEfDb _ef = ef.ThrowIfNull();

    /// <inheritdoc/>
    /// <remarks>Persists to the internal event-based replication store.</remarks>
    public async Task<Result> ModifyAsync(Product product)
    {
        var model = ProductMapper.To.Map(product);
        var exists = await ExistsAsync(model.Id!).ConfigureAwait(false);
        if (!exists)
            return (await _ef.Products.CreateWithResultAsync(model).ConfigureAwait(false)).AsResult();
        else
            return (await _ef.Products.UpdateWithResultAsync(model).ConfigureAwait(false)).AsResult();
    }

    private Task<bool> ExistsAsync(string id) => _ef.Products.Query().AnyAsync(p => p.Id == id);

    /// <inheritdoc/>
    /// <remarks>Removes the product from the internal event-based replication store.</remarks>
    public async Task<Result> DeleteAsync(string id) => (await _ef.Products.DeleteWithResultAsync(id).ConfigureAwait(false)).AsResult();
}