namespace Contoso.Orders.Test.Api;

public partial class ReadTests
{
    [Test]
    public void Order_Get_NotFound()
    {
        Test.Http()
            .Run(HttpMethod.Get, "/api/orders/404")
            .AssertNotFound();
    }

    [Test]
    public void Order_Get_Found()
    {
        var order = Test.Http<Order>()
            .Run(HttpMethod.Get, "/api/orders/ORD-1001")
            .AssertOK()
            .Value!;

        order.Id.Should().Be("ORD-1001");
        order.CustomerId.Should().Be("CUST-1001");
        order.StatusCode.Should().Be("P");
        order.ETag.Should().NotBeNullOrEmpty();
        order.Items.Should().NotBeNull().And.BeEmpty();
    }

    [Test]
    public void Order_Get_Not_Modified()
    {
        var r = Test.Http()
            .Run(HttpMethod.Get, "/api/orders/ORD-1001")
            .AssertOK()
            .Response;

        r.Headers.ETag.Should().NotBeNull();

        Test.Http()
            .Run(HttpMethod.Get, "/api/orders/ORD-1001", requestModifier: rm => rm.WithIfNoneMatch(r.Headers.ETag!.Tag))
            .AssertNotModified();
    }
}