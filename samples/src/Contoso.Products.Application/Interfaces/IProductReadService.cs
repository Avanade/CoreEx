namespace Contoso.Products.Application.Interfaces;

public interface IProductReadService
{
    Task<Contracts.Product?> GetAsync(string id, CancellationToken ct = default);

    Task<ItemsResult<ProductLite>> QueryAsync(QueryArgs? query, PagingArgs? paging, CancellationToken ct = default);

    Task<JsonElement> QuerySchemaAsync(CancellationToken ct = default);
}