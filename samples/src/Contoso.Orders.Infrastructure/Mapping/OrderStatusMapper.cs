namespace Contoso.Orders.Infrastructure.Mapping;

internal class OrderStatusMapper : BiDirectionMapper<Contracts.OrderStatus, Persistence.OrderStatus, OrderStatusMapper>
{
    protected override Persistence.OrderStatus OnMap(Contracts.OrderStatus source) => throw new NotImplementedException();

    protected override Contracts.OrderStatus OnMap(Persistence.OrderStatus source) => new()
    {
        Id = source.Id!,
        Code = source.Code,
        Text = source.Text,
        SortOrder = source.SortOrder,
        IsInactive = !source.IsActive,
        ETag = source.ETag
    };
}