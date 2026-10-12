namespace Contoso.Shopping.Application.Interfaces;

public interface IBasketReadService
{
    /// <summary>
    /// Get the <see cref="Contracts.Basket"/> for the specified <paramref name="basketId"/>
    /// </summary>
    Task<Result<Contracts.Basket>> GetAsync(string basketId, CancellationToken ct = default);

    /// <summary>
    /// Gets the <see cref="QueryArgs"/> schema.
    /// </summary>
    Task<JsonElement> QuerySchemaAsync(CancellationToken ct = default);

    /// <summary>
    /// Queries the <see cref="Contracts.Basket"/>s (summary only; excludes the items and shipping address) for the specified <paramref name="customerId"/>.
    /// </summary>
    /// <param name="customerId">The customer identifier; always applied as a filter.</param>
    /// <param name="query">The <see cref="QueryArgs"/>.</param>
    /// <param name="paging">The <see cref="PagingArgs"/>.</param>
    /// <returns>The resulting baskets.</returns>
    Task<ItemsResult<Contracts.Basket>> QueryAsync(string customerId, QueryArgs? query, PagingArgs? paging, CancellationToken ct = default);
}