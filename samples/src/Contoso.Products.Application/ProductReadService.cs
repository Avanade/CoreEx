namespace Contoso.Products.Application;

[ScopedService<IProductReadService>]
public class ProductReadService(IProductRepository repository) : IProductReadService
{
    private readonly IProductRepository _repository = repository.ThrowIfNull();

    public Task<Product?> GetAsync(string id, CancellationToken ct = default) => _repository.GetAsync(id, ct);

    public Task<ItemsResult<ProductLite>> QueryAsync(QueryArgs? query, PagingArgs? paging, CancellationToken ct = default) => _repository.QueryAsync(query, paging, ct);

    public Task<JsonElement> QuerySchemaAsync(CancellationToken ct = default) => _repository.QuerySchemaAsync(ct);
}