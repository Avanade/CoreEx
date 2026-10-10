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

    [TestCase(null, "Email is required.")]
    [TestCase("", "Email is required.")]
    [TestCase("not-an-email", "Email is an invalid e-mail address.")]
    public void GetAsync_InvalidResponse_ReturnsDependencyFailure(string? email, string message) => Test.Scoped(test =>
    {
        _mockHttpGetRequest.Respond.WithJson(new { id = "cust-1", email });

        test.Run(async _ =>
        {
            var client = ExecutionContext.GetRequiredService<CustomersHttpClient>();
            var result = await client.GetAsync("cust-1").ConfigureAwait(false);
            result.IsFailure.Should().BeTrue();
            var error = result.Error.Should().BeOfType<HttpRequestException>().Subject;
            error.StatusCode.Should().Be(HttpStatusCode.OK);
            var messages = error.InnerException.Should().BeOfType<ValidationException>().Which.Messages;
            messages.Should().ContainSingle().Which.Property.Should().Be("email");
            messages.Should().ContainSingle().Which.Text.ToString().Should().Be(message);
        }).AssertSuccess();

        _mockHttpGetRequest.Verify();
    });

    [Test]
    public void GetAsync_EmptyObject_ReportsRequiredEmail() => Test.Scoped(test =>
    {
        _mockHttpGetRequest.Respond.WithJson(new { });

        test.Run(async _ =>
        {
            var client = ExecutionContext.GetRequiredService<CustomersHttpClient>();
            var result = await client.GetAsync("cust-1").ConfigureAwait(false);
            var messages = result.Error.Should().BeOfType<HttpRequestException>().Subject.InnerException.Should()
                .BeOfType<ValidationException>().Which.Messages;
            messages.Should().ContainSingle().Which.Property.Should().Be("email");
            messages.Should().ContainSingle().Which.Text.ToString().Should().Be("Email is required.");
        }).AssertSuccess();

        _mockHttpGetRequest.Verify();
    });

    [TestCase(null)]
    [TestCase("")]
    [TestCase("cust-1")]
    [TestCase("another-customer")]
    public void GetAsync_ValidEmail_DoesNotValidateIdentityOrOptionalDetails(string? id) => Test.Scoped(test =>
    {
        _mockHttpGetRequest.Respond.WithJson(new { id, email = "frank.foster@example.com" });

        test.Run(async _ =>
        {
            var client = ExecutionContext.GetRequiredService<CustomersHttpClient>();
            var result = await client.GetAsync("cust-1").ConfigureAwait(false);
            result.IsSuccess.Should().BeTrue();
            result.Value.Id.Should().Be(id);
            result.Value.Email.Should().Be("frank.foster@example.com");
            result.Value.FirstName.Should().BeNull();
            result.Value.LastName.Should().BeNull();
            result.Value.ShippingAddress.Should().BeNull();
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
