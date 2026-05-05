namespace Contoso.Orders.Application.Validators;

public class OrderValidator : Validator<Order, OrderValidator>
{
    private static readonly Validator<OrderItem> _itemValidator = Validator.Create<OrderItem>()
        .HasProperty(x => x.ProductId, p => p.Mandatory().MaximumLength(100))
        .HasProperty(x => x.Quantity, p => p.GreaterThanOrEqualTo(0m).PrecisionScale(null, 4))
        .HasProperty(x => x.UnitPrice, p => p.GreaterThanOrEqualTo(0m).PrecisionScale(null, 4));

    public OrderValidator()
    {
        Property(o => o.CustomerId).Mandatory().MaximumLength(100);
        Property(o => o.Status).Mandatory().IsValid();
        Property(o => o.Items).Collection(with => with.WithItemValidator(_itemValidator));
    }
}