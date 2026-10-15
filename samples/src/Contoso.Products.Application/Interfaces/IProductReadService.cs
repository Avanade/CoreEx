namespace Contoso.Products.Application.Interfaces;

public interface IProductReadService
{
    Task<Contracts.Product?> GetAsync(string id, CancellationToken cancellationToken = default);

    Task<ItemsResult<ProductLite>> QueryAsync(QueryArgs? query, PagingArgs? paging, CancellationToken cancellationToken = default);

    Task<JsonElement> QuerySchemaAsync(CancellationToken cancellationToken = default);
}
