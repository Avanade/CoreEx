namespace Contoso.Orders.Application.Validators;

public class OrderValidator : Validator<Order, OrderValidator>
{
    public OrderValidator()
    {
        Property(o => o.CustomerId).Mandatory().MaximumLength(100);
        Property(o => o.Status).Mandatory().IsValid();
    }
}