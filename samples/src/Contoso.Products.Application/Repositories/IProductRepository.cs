namespace Contoso.Products.Application.Repositories;

public interface IProductRepository
{
    Task<Contracts.Product?> GetAsync(string id);

    Task<DataResult<Contracts.Product>> CreateAsync(Contracts.Product product);

    Task<DataResult<Contracts.Product>> UpdateAsync(Contracts.Product product);

    Task<DataResult> DeleteAsync(string id);

    Task<JsonElement> QuerySchemaAsync();

    Task<ItemsResult<Contracts.ProductLite>> QueryAsync(QueryArgs? query, PagingArgs? paging);

    Task<Dictionary<string, Contracts.ProductReserve>> GetForReservationAsync(string[] ids);
}