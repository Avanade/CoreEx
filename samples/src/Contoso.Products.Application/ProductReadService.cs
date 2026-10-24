namespace Contoso.Products.Application;

[ScopedService<IProductReadService>]
public class ProductReadService(IProductRepository repository, IProductQueryRepository queryRepository) : IProductReadService
{
    private readonly IProductRepository _repository = repository.ThrowIfNull();
    private readonly IProductQueryRepository _queryRepository = queryRepository.ThrowIfNull();

    /// <inheritdoc/>
    public Task<Product?> GetAsync(string id, CancellationToken cancellationToken = default) => _repository.GetAsync(id, cancellationToken);

    /// <inheritdoc/>
    public Task<ItemsResult<ProductLite>> QueryAsync(QueryArgs? query, PagingArgs? paging, CancellationToken cancellationToken = default) => _queryRepository.QueryAsync(query, paging, cancellationToken);

    /// <inheritdoc/>
    public Task<JsonElement> QuerySchemaAsync(CancellationToken cancellationToken = default) => _queryRepository.QuerySchemaAsync(cancellationToken);
}
