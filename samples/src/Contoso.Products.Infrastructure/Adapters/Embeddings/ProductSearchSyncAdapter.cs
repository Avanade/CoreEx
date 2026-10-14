namespace Contoso.Products.Infrastructure.Adapters.Embeddings;

/// <summary>Exposes product search reconciliation through the application-owned boundary.</summary>
[ScopedService<IProductSearchSyncAdapter>]
public sealed class ProductSearchSyncAdapter(Repositories.ProductSearchIndex index) : IProductSearchSyncAdapter
{
    private readonly Repositories.ProductSearchIndex _index = index.ThrowIfNull();

    /// <inheritdoc/>
    public Task<Result> SynchronizeAsync(string id, CancellationToken cancellationToken = default) => Result.GoAsync(() => _index.SynchronizeAsync(id, cancellationToken));
}
