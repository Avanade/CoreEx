namespace Contoso.Shopping.Test.Unit.Clients.Products;

public class ProductsHttpClientTests
{
    [Test]
    public async Task CreateReservationAsync_Success_ReturnsSuccess()
    {
        var mcf = UnitTestEx.MockHttpClientFactory.Create();
        var mockHttp = mcf.CreateClient("ProductsApi", new Uri("https://products-api/"));
        var request = mockHttp.Request(HttpMethod.Post, "api/inventory/reserve");
        request.WithAnyBody();
        request.Respond.With(HttpStatusCode.NoContent);

        var client = new ProductsHttpClient(mockHttp.GetHttpClient());
        var result = await client.CreateReservationAsync(new MovementRequest { Id = "basket-1" }).ConfigureAwait(false);

        result.IsSuccess.Should().BeTrue();
        request.Verify();
    }

    [Test]
    public async Task CreateReservationAsync_ServerError_ReturnsFailure()
    {
        var mcf = UnitTestEx.MockHttpClientFactory.Create();
        var mockHttp = mcf.CreateClient("ProductsApi", new Uri("https://products-api/"));
        var request = mockHttp.Request(HttpMethod.Post, "api/inventory/reserve");
        request.WithAnyBody();
        request.Respond.With(HttpStatusCode.InternalServerError);

        var client = new ProductsHttpClient(mockHttp.GetHttpClient());
        var result = await client.CreateReservationAsync(new MovementRequest { Id = "basket-1" }).ConfigureAwait(false);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<HttpRequestException>();
        request.Verify();
    }

    [Test]
    public async Task CreateReservationAsync_BusinessError_ReturnsBusinessFailure()
    {
        var mcf = UnitTestEx.MockHttpClientFactory.Create();
        var mockHttp = mcf.CreateClient("ProductsApi", new Uri("https://products-api/"));
        var problemDetails = new
        {
            type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            title = "Business Error",
            status = 422,
            code = "UnprocessableEntity"
        };

        var request = mockHttp.Request(HttpMethod.Post, "api/inventory/reserve");
        request.WithAnyBody();
        request.Respond.WithJson(problemDetails, HttpStatusCode.UnprocessableContent, "application/problem+json");

        var client = new ProductsHttpClient(mockHttp.GetHttpClient());
        var result = await client.CreateReservationAsync(new MovementRequest { Id = "basket-1" }).ConfigureAwait(false);

        result.IsFailure.Should().BeTrue();
        request.Verify();
    }
}
