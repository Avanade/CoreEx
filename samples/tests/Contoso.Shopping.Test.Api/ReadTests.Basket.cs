namespace Contoso.Shopping.Test.Api;

public partial class ReadTests
{
    [Test]
    public void Get_NotFound()
    {
        Test.Http()
            .Run(HttpMethod.Get, $"/api/baskets/{404.ToGuid()}")
            .AssertNotFound();
    }

    [Test]
    public void Get_Found()
    {
        Test.Http<Basket>()
            .IgnoreChangeLog()
            .IgnoreETag()
            .ExpectJsonFromResource("Basket_Get_Found.res.json", _pathsToIgnore)
            .Run(HttpMethod.Get, $"/api/baskets/{3002.ToGuid()}")
            .AssertOK();
    }
}