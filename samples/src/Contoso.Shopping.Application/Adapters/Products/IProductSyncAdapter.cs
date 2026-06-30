namespace Contoso.Shopping.Application.Adapters.Products;

/// <summary>
/// Enables the Products-domain data synchronization operations via evet-based subscriptions.
/// </summary>
public interface IProductSyncAdapter
{
    /// <summary>
    /// Modifies (creates/updates) the product.
    /// </summary>
    Task<Result> ModifyAsync(Product product, CancellationToken ct = default);

    /// <summary>
    /// Deletes the replicated product.
    /// </summary>
    Task<Result> DeleteAsync(string id, CancellationToken ct = default);
}