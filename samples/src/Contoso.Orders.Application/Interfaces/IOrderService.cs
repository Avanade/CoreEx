namespace Contoso.Orders.Application.Interfaces;

public interface IOrderService
{
    Task<Contracts.Order?> GetAsync(string id);

    Task<Contracts.Order> CreateAsync(Contracts.Order order);

    Task<Contracts.Order> UpdateAsync(Contracts.Order order);

    Task DeleteAsync(string id);
}