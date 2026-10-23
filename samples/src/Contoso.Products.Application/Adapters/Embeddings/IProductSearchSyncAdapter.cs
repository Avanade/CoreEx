namespace Contoso.Products.Application.Adapters.Embeddings;

/// <summary>Reconciles a product's derived search document with its current authoritative state.</summary>
public interface IProductSearchSyncAdapter
{
    /// <summary>Indexes current content or removes all projections when the product is deleted or missing.</summary>
    Task<Result> SynchronizeAsync(string id, CancellationToken cancellationToken = default);
}
