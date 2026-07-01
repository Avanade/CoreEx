namespace Contoso.Shopping.Test.Unit.Clients.Products;

public class ProductsHttpClientTests : WithGenericTester<EntryPoint>
{
    private UnitTestEx.Mocking.MockHttpClientRequest _mockHttpReserveRequest = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        var mcf = UnitTestEx.MockHttpClientFactory.Create();
        _mockHttpReserveRequest = mcf.CreateClient("ProductsApi").Request(HttpMethod.Post, "api/inventory/reserve");
        Test.ReplaceHttpClientFactory(mcf);
    }

    [Test]
    public void CreateReservationAsync_Success_ReturnsSuccess() => Test.Scoped(async test =>
    {
        _mockHttpReserveRequest.WithAnyBody()
            .Respond.With(HttpStatusCode.NoContent);

        test.Run(async _ =>
        {
            var client = ExecutionContext.GetRequiredService<ProductsHttpClient>();
            var result = await client.CreateReservationAsync(new MovementRequest { Id = "basket-1" }).ConfigureAwait(false);
            result.IsSuccess.Should().BeTrue();
        });

        _mockHttpReserveRequest.Verify();
    });

    [Test]
    public void CreateReservationAsync_ServerError_ReturnsFailure() => Test.Scoped(async test =>
    {
        _mockHttpReserveRequest.WithAnyBody()
            .Respond.With(HttpStatusCode.InternalServerError);

        test.Run(async _ =>
        {
            var client = ExecutionContext.GetRequiredService<ProductsHttpClient>();
            var result = await client.CreateReservationAsync(new MovementRequest { Id = "basket-1" }).ConfigureAwait(false);
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeOfType<HttpRequestException>();
        });

        _mockHttpReserveRequest.Verify();
    });

    [Test]
    public void CreateReservationAsync_BusinessError_ReturnsBusinessFailure() => Test.Scoped(async test =>
    {
        _mockHttpReserveRequest.WithAnyBody()
            .Respond.WithJson(new
            {
                type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                title = "Business Error",
                status = 422,
                code = "UnprocessableEntity"
            }, HttpStatusCode.UnprocessableContent, "application/problem+json");

        test.Run(async _ =>
        {
            var client = ExecutionContext.GetRequiredService<ProductsHttpClient>();
            var result = await client.CreateReservationAsync(new MovementRequest { Id = "basket-1" }).ConfigureAwait(false);
            result.IsFailure.Should().BeTrue();
        });

        _mockHttpReserveRequest.Verify();
    });
}
