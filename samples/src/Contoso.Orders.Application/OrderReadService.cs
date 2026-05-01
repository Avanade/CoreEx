namespace Contoso.Orders.Application;

[ScopedService<IOrderReadService>]
public class OrderReadService(IOrderRepository repository) : IOrderReadService
{
    private readonly IOrderRepository _repository = repository.ThrowIfNull();

    public Task<Order?> GetAsync(string id) => _repository.GetAsync(id);

    public Task<ItemsResult<OrderLite>> QueryAsync(QueryArgs? query, PagingArgs? paging)
        => _repository.QueryAsync(query, paging);
}