namespace Contoso.Products.Test.Api;

public partial class ReadTests
{
    [Test]
    public void Product_QtyOnHand_NotFound()
    {
        Test.Http()
            .Run(HttpMethod.Get, "/api/products/404/on-hand")
            .AssertNotFound();
    }

    [Test]
    public void Product_QtyOnHand_None()
    {
        Test.Http<int>()
            .Run(HttpMethod.Get, $"/api/products/{32.ToGuid()}/on-hand")
            .AssertOK()
            .Value.Should().Be(0);
    }

    [Test]
    public void Product_QtyOnHand_Value()
    {
        Test.Http<decimal>()
            .Run(HttpMethod.Get, $"/api/products/{1.ToGuid()}/on-hand")
            .AssertOK()
            .Value.Should().Be(3);
    }
}