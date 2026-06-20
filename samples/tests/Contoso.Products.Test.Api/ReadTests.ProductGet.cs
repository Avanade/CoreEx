namespace Contoso.Products.Test.Api;

public partial class ReadTests : WithApiTester<Contoso.Products.Api.Program>
{
    [Test]
    public void Product_Get_NotFound()
    {
        Test.Http()
            .Run(HttpMethod.Get, "/api/products/404")
            .AssertNotFound();
    }

    [Test]
    public void Product_Get_IsDeleted()
    {
        Test.Http()
            .Run(HttpMethod.Get, $"/api/products/{18.ToGuid()}")
            .AssertNotFound();
    }

    [Test]
    public void Product_Get_Found()
    {
        Test.Http()
            .Run(HttpMethod.Get, $"/api/products/{1.ToGuid()}")
            .AssertOK()
            .AssertJsonFromResource("ReadTests.Product_Get_Found.res.json", "etag", "changelog");
    }

    [Test]
    public void Product_Get_Not_Modified()
    {
        var r = Test.Http()
            .Run(HttpMethod.Get, $"/api/products/{1.ToGuid()}")
            .AssertOK()
            .Response;

        r.Headers.ETag.Should().NotBeNull();

        Test.Http()
            .Run(HttpMethod.Get, $"/api/products/{1.ToGuid()}", requestModifier: rm => rm.WithIfNoneMatch(r.Headers.ETag.Tag))
            .AssertNotModified();
    }
}