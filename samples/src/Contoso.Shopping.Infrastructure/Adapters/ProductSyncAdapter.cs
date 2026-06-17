namespace Contoso.Shopping.Infrastructure.Adapters;

[ScopedService<IProductSyncAdapter>]
public class ProductSyncAdapter(ShoppingEfDb ef) : IProductSyncAdapter
{
    private readonly ShoppingEfDb _ef = ef.ThrowIfNull();

    /// <inheritdoc/>
    /// <remarks>Persists to the internal event-based replication store.</remarks>
    public async Task<Result> ModifyAsync(Product product) => (await _ef.Products.UpsertWithResultAsync(ProductMapper.To.Map(product)).ConfigureAwait(false)).AsResult();

    /// <inheritdoc/>
    /// <remarks>Removes the product from the internal event-based replication store.</remarks>
    public async Task<Result> DeleteAsync(string id) => (await _ef.Products.DeleteWithResultAsync(id).ConfigureAwait(false)).AsResult();
}