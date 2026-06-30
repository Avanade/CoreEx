namespace Contoso.Orders.Application.Interfaces;

public interface IOrderReadService
{
    Task<Contracts.Order?> GetAsync(string id, CancellationToken ct = default);

    Task<ItemsResult<OrderLite>> QueryAsync(QueryArgs? query, PagingArgs? paging, CancellationToken ct = default);
}