namespace Contoso.Products.Application.Interfaces;

public interface IProductReadService
{
    Task<Contracts.Product?> GetAsync(string id);

    Task<ItemsResult<ProductLite>> QueryAsync(QueryArgs? query, PagingArgs? paging);

    Task<JsonElement> QuerySchemaAsync();
}