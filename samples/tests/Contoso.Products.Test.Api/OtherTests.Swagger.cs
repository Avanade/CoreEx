namespace Contoso.Products.Test.Api;

public partial class OtherTests
{
    [Test]
    public void Swagger_UI()
    {
        // Hit swagger and assert redirect.
        Test.Http()
            .Run(HttpMethod.Get, "/swagger")
            .Assert(HttpStatusCode.Found)
            .AssertLocationHeader(new Uri("/swagger/index.html", UriKind.Relative));

        // Go to redirected URL and assert basic content.
        var html = Test.Http()
            .Run(HttpMethod.Get, "/swagger/index.html")
            .Assert(HttpStatusCode.OK)
            .GetContent();

        html.Should().Contain("<title>Swagger UI</title>");
    }

    [Test]
    public void Swagger_Json()
    {
        var json = Test.Http()
            .Run(HttpMethod.Get, "/swagger/v1/swagger.json")
            .Assert(HttpStatusCode.OK)
            .AssertContentTypeJson()
            .GetContent();

        json.Should().Contain("Contoso.Products.Api");
    }
}