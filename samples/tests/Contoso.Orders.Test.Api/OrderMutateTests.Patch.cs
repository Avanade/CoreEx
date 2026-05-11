namespace Contoso.Orders.Test.Api;

public partial class OrderMutateTests
{
    [Test]
    public void Order_Patch_NotFound()
    {
        Test.Http()
            .Run(HttpMethod.Patch, "/api/orders/404", new { status = "C" }, requestModifier: r => r.WithMergePatchJsonContentType())
            .AssertNotFound();
    }

    [Test]
    public void Order_Patch_Validation()
    {
        var order = CreateOrder("PAT-VD");

        Test.Http()
            .Run(HttpMethod.Patch, $"/api/orders/{order.Id}", new { status = "ZZ" }, requestModifier: r => r.WithIfMatch(order.ETag).WithMergePatchJsonContentType())
            .AssertBadRequest()
            .AssertErrors("Order status is invalid.");
    }

    [Test]
    public void Order_Patch_Success()
    {
        var order = CreateOrder("PAT-OK");

        order.StatusCode = "X";

        var updated = Test.Http<Contoso.Orders.Contracts.Order>()
            .ExpectSqlServerOutboxEvents(e => e.AssertWithValue("contoso", "contoso.orders.order.updated.v1"))
            .Run(HttpMethod.Patch, $"/api/orders/{order.Id}", new { status = "X" }, requestModifier: r => r.WithIfMatch(order.ETag).WithMergePatchJsonContentType())
            .AssertOK()
            .AssertValue(order, "etag", "changelog")
            .Value!;

        updated.StatusCode.Should().Be("X");
        updated.ETag.Should().NotBe(order.ETag);

        Test.Http<Contoso.Orders.Contracts.Order>()
            .Run(HttpMethod.Get, $"/api/orders/{order.Id}")
            .AssertOK()
            .AssertValue(updated, "etag", "changelog");
    }

    [Test]
    public void Order_Patch_NoChanges()
    {
        var order = CreateOrder("PAT-NC");

        var updated = Test.Http<Contoso.Orders.Contracts.Order>()
            .ExpectNoSqlServerOutboxEvents()
            .Run(HttpMethod.Patch, $"/api/orders/{order.Id}", new { }, requestModifier: r => r.WithIfMatch(order.ETag).WithMergePatchJsonContentType())
            .AssertOK()
            .AssertValue(order, "etag", "changelog")
            .Value!;

        updated.ETag.Should().Be(order.ETag);
    }
}