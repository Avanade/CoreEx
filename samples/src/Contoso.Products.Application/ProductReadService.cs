namespace Contoso.Products.Application;

[ScopedService<IProductReadService>]
public class ProductReadService(IProductRepository repository) : IProductReadService
{
    private readonly IProductRepository _repository = repository.ThrowIfNull();

    public Task<Product?> GetAsync(string id) => _repository.GetAsync(id);

    public Task<ItemsResult<ProductLite>> QueryAsync(QueryArgs? query, PagingArgs? paging) => _repository.QueryAsync(query, paging);

    public Task<JsonElement> QuerySchemaAsync() => _repository.QuerySchemaAsync();
}