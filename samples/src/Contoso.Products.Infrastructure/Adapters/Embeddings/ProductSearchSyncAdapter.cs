namespace Contoso.Products.Infrastructure.Adapters.Embeddings;

/// <summary>Exposes product search reconciliation through the application-owned boundary.</summary>
[ScopedService<IProductSearchSyncAdapter>]
public sealed class ProductSearchSyncAdapter(Repositories.ProductSearchIndex index, Repositories.ProductLexicalSearchIndex lexicalIndex, IConfiguration configuration) : IProductSearchSyncAdapter
{
    private readonly Repositories.ProductSearchIndex _index = index.ThrowIfNull();
    private readonly Repositories.ProductLexicalSearchIndex _lexicalIndex = lexicalIndex.ThrowIfNull();
    private readonly bool _isLexical = Repositories.ProductQueryArgsConfig.ResolveMode(configuration) == QuerySearchMode.FullText;

    /// <inheritdoc/>
    public Task<Result> SynchronizeAsync(string id, CancellationToken cancellationToken = default)
        => Result.GoAsync(() => _isLexical ? _lexicalIndex.SynchronizeAsync(id, cancellationToken) : _index.SynchronizeAsync(id, cancellationToken));
}
