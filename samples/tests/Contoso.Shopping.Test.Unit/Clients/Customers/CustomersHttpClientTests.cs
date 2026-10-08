namespace Contoso.Shopping.Test.Unit.Clients.Customers;

public class CustomersHttpClientTests : WithGenericTester<EntryPoint>
{
    private UnitTestEx.Mocking.MockHttpClientRequest _mockHttpGetRequest = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        var mcf = UnitTestEx.MockHttpClientFactory.Create();
        _mockHttpGetRequest = mcf.CreateClient("CustomersApi").Request(HttpMethod.Get, "api/customers/cust-1");
        Test.ReplaceHttpClientFactory(mcf);
    }

    [Test]
    public void GetAsync_Found_ReturnsCustomer() => Test.Scoped(test =>
    {
        _mockHttpGetRequest.Respond.WithJson(new
        {
            id = "cust-1",
            firstName = "Frank",
            lastName = "Foster",
            email = "frank.foster@example.com",
            shippingAddress = new { street1 = "1 Test Street", city = "Springfield", postCode = "49007", state = "IL" },
            customerTypeCode = "IND"
        });

        test.Run(async _ =>
        {
            var client = ExecutionContext.GetRequiredService<CustomersHttpClient>();
            var result = await client.GetAsync("cust-1").ConfigureAwait(false);
            result.IsSuccess.Should().BeTrue();
            result.Value.Id.Should().Be("cust-1");
            result.Value.Email.Should().Be("frank.foster@example.com");
            result.Value.ShippingAddress.Should().NotBeNull();
            result.Value.ShippingAddress!.City.Should().Be("Springfield");
        }).AssertSuccess();

        _mockHttpGetRequest.Verify();
    });

    [Test]
    public void GetAsync_NotFound_ReturnsNotFoundError() => Test.Scoped(test =>
    {
        _mockHttpGetRequest.Respond.With(HttpStatusCode.NotFound);

        test.Run(async _ =>
        {
            var client = ExecutionContext.GetRequiredService<CustomersHttpClient>();
            var result = await client.GetAsync("cust-1").ConfigureAwait(false);
            result.IsNotFoundError.Should().BeTrue();
        }).AssertSuccess();

        _mockHttpGetRequest.Verify();
    });

    [Test]
    public void GetAsync_ServerError_ReturnsFailure() => Test.Scoped(test =>
    {
        _mockHttpGetRequest.Respond.With(HttpStatusCode.InternalServerError);

        test.Run(async _ =>
        {
            var client = ExecutionContext.GetRequiredService<CustomersHttpClient>();
            var result = await client.GetAsync("cust-1").ConfigureAwait(false);
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeOfType<HttpRequestException>();
        }).AssertSuccess();

        _mockHttpGetRequest.Verify();
    });
}
