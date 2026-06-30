namespace Contoso.Orders.Application;

[ScopedService<IOrderReadService>]
public class OrderReadService(IOrderRepository repository) : IOrderReadService
{
    private readonly IOrderRepository _repository = repository.ThrowIfNull();

    public Task<Order?> GetAsync(string id, CancellationToken ct = default) => _repository.GetAsync(id, ct);

    public Task<ItemsResult<OrderLite>> QueryAsync(QueryArgs? query, PagingArgs? paging, CancellationToken ct = default)
        => _repository.QueryAsync(query, paging, ct);
}