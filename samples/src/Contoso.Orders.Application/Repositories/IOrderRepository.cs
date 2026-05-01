namespace Contoso.Orders.Application.Repositories;

public interface IOrderRepository
{
    Task<Contracts.Order?> GetAsync(string id);

    Task<DataResult<Contracts.Order>> CreateAsync(Contracts.Order order);

    Task<DataResult<Contracts.Order>> UpdateAsync(Contracts.Order order);

    Task<DataResult> DeleteAsync(string id);

    Task<ItemsResult<Contracts.OrderLite>> QueryAsync(QueryArgs? query, PagingArgs? paging);
}