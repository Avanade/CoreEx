namespace Contoso.Shopping.Application;

[ScopedService<IBasketReadService>]
public class BasketReadService(IBasketRepository repository) : IBasketReadService
{
    private readonly IBasketRepository _repository = repository.ThrowIfNull();

    /// <inheritdoc/>
    public Task<Result<Basket>> GetAsync(string basketId, CancellationToken ct = default)
        => Result.GoAsync(() => _repository.GetAsync(basketId, ct))
                 .ThenAs(b => BasketMapper.Map(b));

    /// <inheritdoc/>
    public Task<JsonElement> QuerySchemaAsync(CancellationToken ct = default) => _repository.QuerySchemaAsync(ct);

    /// <inheritdoc/>
    public Task<ItemsResult<Contracts.Basket>> QueryAsync(string customerId, QueryArgs? query, PagingArgs? paging, CancellationToken ct = default)
        => _repository.QueryAsync(customerId, query, paging, ct);
}