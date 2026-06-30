namespace Contoso.Orders.Infrastructure.Repositories;

[ScopedService<IReferenceDataRepository>]
public class ReferenceDataRepository(OrdersEfDb ef) : IReferenceDataRepository
{
    private readonly OrdersEfDb _ef = ef.ThrowIfNull();

    public Task<Contracts.OrderStatusCollection> GetAllOrderStatusesAsync(CancellationToken ct = default)
        => _ef.OrderStatuses.Query().ToMappedItemsAsync<Persistence.OrderStatus, Contracts.OrderStatusCollection, Contracts.OrderStatus>(OrderStatusMapper.From, ct);
}