namespace Contoso.Orders.Infrastructure.Mapping;

public class OrderMapper : BiDirectionMapper<Contracts.Order, Persistence.Order, OrderMapper>
{
    protected override Persistence.Order OnMap(Contracts.Order source) => new()
    {
        Id = source.Id,
        CustomerId = source.CustomerId,
        StatusCode = source.Status?.Code,
        Items = source.Items?.Select(i => new Persistence.OrderItem
        {
            Id = i.Id,
            OrderId = source.Id,
            ProductId = i.ProductId,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice
        }).ToList() ?? []
    };

    protected override Contracts.Order OnMap(Persistence.Order source) => new()
    {
        Id = source.Id,
        CustomerId = source.CustomerId,
        StatusCode = source.StatusCode,
        Items = source.Items?.Select(i => new Contracts.OrderItem
        {
            Id = i.Id,
            ProductId = i.ProductId,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice
        }).ToList() ?? []
    };
}