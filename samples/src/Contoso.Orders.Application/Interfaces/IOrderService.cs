namespace Contoso.Orders.Application.Interfaces;

public interface IOrderService
{
    Task<Contracts.Order?> GetAsync(string id, CancellationToken ct = default);

    Task<Contracts.Order> CreateAsync(Contracts.Order order, CancellationToken ct = default);

    Task<Contracts.Order> UpdateAsync(Contracts.Order order, CancellationToken ct = default);

    Task DeleteAsync(string id, CancellationToken ct = default);
}