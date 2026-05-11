namespace Contoso.Orders.Application.Interfaces;

public interface IOrderReadService
{
    Task<Contracts.Order?> GetAsync(string id);

    Task<ItemsResult<OrderLite>> QueryAsync(QueryArgs? query, PagingArgs? paging);
}