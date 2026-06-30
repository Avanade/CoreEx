namespace Contoso.Orders.Application.Repositories;

public interface IOrderRepository
{
    Task<Contracts.Order?> GetAsync(string id, CancellationToken ct = default);

    Task<DataResult<Contracts.Order>> CreateAsync(Contracts.Order order, CancellationToken ct = default);

    Task<DataResult<Contracts.Order>> UpdateAsync(Contracts.Order order, CancellationToken ct = default);

    Task<DataResult> DeleteAsync(string id, CancellationToken ct = default);

    Task<ItemsResult<Contracts.OrderLite>> QueryAsync(QueryArgs? query, PagingArgs? paging, CancellationToken ct = default);
}