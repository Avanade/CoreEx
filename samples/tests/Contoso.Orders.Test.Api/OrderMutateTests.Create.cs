namespace Contoso.Orders.Test.Api;

public partial class OrderMutateTests
{
    [Test]
    public void Order_Create_Bad_Data()
    {
        var order = new Contoso.Orders.Contracts.Order
        {
            CustomerId = null,
            StatusCode = "XX"
        };

        Test.Http()
            .Run(HttpMethod.Post, "/api/orders", order)
            .AssertBadRequest()
            .AssertErrors(
                "Customer is required.",
                "Order status is invalid.");
    }

    [Test]
    public void Order_Create_Success()
    {
        var order = new Contoso.Orders.Contracts.Order
        {
            CustomerId = "CUST-3001",
            StatusCode = "P",
            Items =
            [
                new() { Id = "OI-3001", ProductId = "PROD-3001", Quantity = 2, UnitPrice = 19.95m }
            ]
        };

        var created = Test.Http<Contoso.Orders.Contracts.Order>()
            .ExpectIdentifier()
            .ExpectETag()
            .ExpectChangeLogCreated()
            .ExpectSqlServerOutboxEvents(e => e.AssertWithValue("contoso", "contoso.orders.order.created.v1"))
            .Run(HttpMethod.Post, "/api/orders", order)
            .AssertCreated()
            .AssertLocationHeader(r => new Uri($"/api/orders/{r!.Id}", UriKind.Relative))
            .Value!;

        created.Id.Should().NotBeNullOrEmpty();
        created.CustomerId.Should().Be(order.CustomerId);
        created.StatusCode.Should().Be(order.StatusCode);
        created.Items.Should().NotBeNull().And.HaveCount(1);
        created.Items![0].Id.Should().Be("OI-3001");
        created.Items[0].ProductId.Should().Be("PROD-3001");
        created.Items[0].Quantity.Should().Be(2);
        created.Items[0].UnitPrice.Should().Be(19.95m);

        Test.Http<Contoso.Orders.Contracts.Order>()
            .Run(HttpMethod.Get, $"/api/orders/{created.Id}")
            .AssertOK()
            .AssertValue(created);
    }

    [Test]
    public void Order_Create_IdempotencyKey()
    {
        var order = new Contoso.Orders.Contracts.Order
        {
            CustomerId = "CUST-4001",
            StatusCode = "C"
        };

        var ik = Guid.NewGuid().ToString();

        var v1 = Test.Http<Contoso.Orders.Contracts.Order>()
            .ExpectSqlServerOutboxEvents()
            .Run(HttpMethod.Post, "/api/orders", order, requestModifier: r => r.WithIdempotencyKey(ik))
            .AssertCreated()
            .AssertLocationHeader(r => new Uri($"/api/orders/{r!.Id}", UriKind.Relative))
            .Value!;

        var v2 = Test.Http<Contoso.Orders.Contracts.Order>()
            .ExpectNoSqlServerOutboxEvents()
            .Run(HttpMethod.Post, "/api/orders", order, requestModifier: r => r.WithIdempotencyKey(ik))
            .AssertCreated()
            .AssertLocationHeader(r => new Uri($"/api/orders/{r!.Id}", UriKind.Relative))
            .Value!;

        ObjectComparer.Assert(v1, v2);
    }
}