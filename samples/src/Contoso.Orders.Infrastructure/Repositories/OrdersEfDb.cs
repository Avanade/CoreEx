namespace Contoso.Orders.Infrastructure.Repositories;

public sealed class OrdersEfDb(OrdersDbContext dbContext) : EfDb<OrdersDbContext>(dbContext)
{
    public EfDbModel<Persistence.OrderStatus> OrderStatuses => Model<Persistence.OrderStatus>();

    public EfDbMappedModel<Contracts.Order, Persistence.Order, OrderMapper> Orders => Model<Persistence.Order>().ToMappedModel<Contracts.Order, OrderMapper>(OrderMapper.Default);
}