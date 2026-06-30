namespace Contoso.Products.Application.Repositories;

public interface IProductRepository
{
    Task<Contracts.Product?> GetAsync(string id, CancellationToken ct = default);

    Task<DataResult<Contracts.Product>> CreateAsync(Contracts.Product product, CancellationToken ct = default);

    Task<DataResult<Contracts.Product>> UpdateAsync(Contracts.Product product, CancellationToken ct = default);

    Task<DataResult> DeleteAsync(string id, CancellationToken ct = default);

    Task<JsonElement> QuerySchemaAsync(CancellationToken ct = default);

    Task<ItemsResult<Contracts.ProductLite>> QueryAsync(QueryArgs? query, PagingArgs? paging, CancellationToken ct = default);

    Task<Dictionary<string, Contracts.ProductReserve>> GetForReservationAsync(string[] ids, CancellationToken ct = default);
}