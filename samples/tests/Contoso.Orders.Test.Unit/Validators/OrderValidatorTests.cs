namespace Contoso.Orders.Test.Unit.Validators;

public class OrderValidatorTests : WithGenericTester<EntryPoint>
{
    [Test]
    public void Empty_Required() => Test.Scoped(test =>
    {
        var order = new Contoso.Orders.Contracts.Order();
        new OrderValidator().AssertErrors(order,
            ("customerId", "Customer is required."),
            ("status", "Order status is required."));
    });

    [Test]
    public void Invalid_ReferenceData() => Test.Scoped(test =>
    {
        var order = new Contoso.Orders.Contracts.Order { CustomerId = "CUST-1001", StatusCode = "ZZ" };
        new OrderValidator().AssertErrors(order,
            ("status", "Order status is invalid."));
    });

    [Test]
    public void Success() => Test.Scoped(test =>
    {
        var order = new Contoso.Orders.Contracts.Order
        {
            CustomerId = "CUST-1001",
            StatusCode = "P",
            Items =
            [
                new OrderItem { Id = "ORD-1001-1", ProductId = "PROD-100", Quantity = 1.00m, UnitPrice = 12.34m }
            ]
        };

        new OrderValidator().AssertSuccess(order);
    });
}