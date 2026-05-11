namespace Contoso.Orders.Test.Api;

public partial class OrderMutateTests
{
    [Test]
    public void Order_Update_NotFound()
    {
        var order = CreateOrder("UPD-NF");

        Test.Http()
            .Run(HttpMethod.Put, "/api/orders/404", order)
            .AssertNotFound();
    }

    [Test]
    public void Order_Update_Concurrency()
    {
        var order = CreateOrder("UPD-CC");

        order.StatusCode = "C";

        Test.Http()
            .Run(HttpMethod.Put, $"/api/orders/{order.Id}", order, requestModifier: r => r.WithIfMatch("AAAAAAAA"))
            .AssertPreconditionFailed();
    }

    [Test]
    public void Order_Update_Success()
    {
        var order = CreateOrder("UPD-OK");

        order.StatusCode = "C";
        order.CustomerId = "CUST-1001-UPDATED";
        order.Items =
        [
            new() { Id = "OI-UPD-1", ProductId = "PROD-1001", Quantity = 1.5m, UnitPrice = 12.34m }
        ];

        var updated = Test.Http<Contoso.Orders.Contracts.Order>()
            .ExpectIdentifier()
            .ExpectETag()
            .ExpectChangeLogUpdated()
            .ExpectValue(order)
            .ExpectSqlServerOutboxEvents(e => e.AssertWithValue("contoso", "contoso.orders.order.updated.v1"))
            .Run(HttpMethod.Put, $"/api/orders/{order.Id}", order)
            .AssertOK()
            .Value!;

        updated.CustomerId.Should().Be("CUST-1001-UPDATED");
        updated.StatusCode.Should().Be("C");
        updated.ETag.Should().NotBe(order.ETag);
    updated.Items.Should().NotBeNull().And.HaveCount(1);
    updated.Items![0].Id.Should().Be("OI-UPD-1");
    updated.Items[0].ProductId.Should().Be("PROD-1001");
    updated.Items[0].Quantity.Should().Be(1.5m);
    updated.Items[0].UnitPrice.Should().Be(12.34m);

        Test.Http<Contoso.Orders.Contracts.Order>()
            .Run(HttpMethod.Get, $"/api/orders/{order.Id}")
            .AssertOK()
            .AssertValue(updated);
    }

    [Test]
    public void Order_Update_NoChanges()
    {
        var order = CreateOrder("UPD-NC");

        var updated = Test.Http<Contoso.Orders.Contracts.Order>()
            .ExpectNoSqlServerOutboxEvents()
            .Run(HttpMethod.Put, $"/api/orders/{order.Id}", order)
            .AssertOK()
            .AssertValue(order, "etag", "changelog")
            .Value!;

        updated.ETag.Should().Be(order.ETag);
        updated.ChangeLog.Should().NotBeNull();
        updated.ChangeLog!.UpdatedBy.Should().BeNull();
        updated.ChangeLog.UpdatedOn.Should().BeNull();
    }
}