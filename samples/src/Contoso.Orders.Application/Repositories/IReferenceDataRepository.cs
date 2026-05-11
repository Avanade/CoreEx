namespace Contoso.Orders.Application.Repositories;

public interface IReferenceDataRepository
{
    Task<OrderStatusCollection> GetAllOrderStatusesAsync();
}