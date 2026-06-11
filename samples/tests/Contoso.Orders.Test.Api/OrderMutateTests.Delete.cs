namespace Contoso.Orders.Test.Api;

public partial class OrderMutateTests
{
    [Test]
    public void Order_Delete_NotFound()
    {
        Test.Http()
            .Run(HttpMethod.Delete, "/api/orders/404")
            .AssertNoContent();
    }

    [Test]
    public void Order_Delete_Success()
    {
        var order = CreateOrder("DEL-OK");

        Test.Http()
            .Run(HttpMethod.Get, $"/api/orders/{order.Id}")
            .AssertOK();

        Test.Http()
            .ExpectSqlServerOutboxEvents(e => e.AssertMetadata("contoso", "contoso.orders.order.deleted", order.Id!))
            .Run(HttpMethod.Delete, $"/api/orders/{order.Id}")
            .AssertNoContent();

        Test.Http()
            .Run(HttpMethod.Delete, $"/api/orders/{order.Id}")
            .AssertNoContent();

        Test.Http()
            .Run(HttpMethod.Get, $"/api/orders/{order.Id}")
            .AssertNotFound();
    }
}