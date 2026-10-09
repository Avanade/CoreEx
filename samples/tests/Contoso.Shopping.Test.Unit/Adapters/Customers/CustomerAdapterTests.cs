namespace Contoso.Shopping.Test.Unit.Adapters.Customers;

public class CustomerAdapterTests : WithGenericTester<EntryPoint>
{
    private UnitTestEx.Mocking.MockHttpClientRequest _mockHttpFoundRequest = null!;
    private UnitTestEx.Mocking.MockHttpClientRequest _mockHttpNotFoundRequest = null!;
    private UnitTestEx.Mocking.MockHttpClientRequest _mockHttpInvalidRequest = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        var mcf = UnitTestEx.MockHttpClientFactory.Create();
        var client = mcf.CreateClient("CustomersApi");
        _mockHttpFoundRequest = client.Request(HttpMethod.Get, "api/customers/cache-found");
        _mockHttpNotFoundRequest = client.Request(HttpMethod.Get, "api/customers/cache-notfound");
        _mockHttpInvalidRequest = client.Request(HttpMethod.Get, "api/customers/cache-invalid");
        Test.ReplaceHttpClientFactory(mcf);
    }

    private static CustomerAdapter CreateAdapter() => new(ExecutionContext.GetRequiredService<CustomersHttpClient>(), ExecutionContext.GetRequiredService<CoreEx.Caching.IHybridCache>());

    [Test]
    public void GetAsync_InvalidResponse_IsNotCached() => Test.Scoped(test =>
    {
        _mockHttpInvalidRequest.Respond.WithJson(new { id = "cache-invalid", email = "not-an-email" });

        test.Run(async _ =>
        {
            var adapter = CreateAdapter();
            var invalid = await adapter.GetAsync("cache-invalid").ConfigureAwait(false);
            invalid.IsFailure.Should().BeTrue();
            invalid.Error.Should().BeOfType<HttpRequestException>().Subject.InnerException.Should().BeOfType<ValidationException>();

            _mockHttpInvalidRequest.Respond.WithJson(new { id = "cache-invalid", email = "new.customer@example.com" });
            var valid = await adapter.GetAsync("cache-invalid").ConfigureAwait(false);
            valid.IsSuccess.Should().BeTrue();
            valid.Value.Email.Should().Be("new.customer@example.com");
        }).AssertSuccess();
    });

    [Test]
    public void GetAsync_Found_IsCached() => Test.Scoped(test =>
    {
        _mockHttpFoundRequest.Respond.WithJson(new { id = "cache-found", firstName = "Frank", lastName = "Foster", email = "frank.foster@example.com" });

        test.Run(async _ =>
        {
            var adapter = CreateAdapter();
            var r1 = await adapter.GetAsync("cache-found").ConfigureAwait(false);
            r1.IsSuccess.Should().BeTrue();
            r1.Value.Email.Should().Be("frank.foster@example.com");

            // The remote customer is now gone; the second call must be served from the cache and not call the Customers API.
            _mockHttpFoundRequest.Respond.With(HttpStatusCode.NotFound);

            var r2 = await adapter.GetAsync("cache-found").ConfigureAwait(false);
            r2.IsSuccess.Should().BeTrue();
            r2.Value.Email.Should().Be("frank.foster@example.com");
        }).AssertSuccess();
    });

    [Test]
    public void GetAsync_NotFound_IsNotCached() => Test.Scoped(test =>
    {
        _mockHttpNotFoundRequest.Respond.With(HttpStatusCode.NotFound);

        test.Run(async _ =>
        {
            var adapter = CreateAdapter();
            var r1 = await adapter.GetAsync("cache-notfound").ConfigureAwait(false);
            r1.IsNotFoundError.Should().BeTrue();

            // The customer now exists; a not-found must never be cached, so this call must reach the Customers API.
            _mockHttpNotFoundRequest.Respond.WithJson(new { id = "cache-notfound", firstName = "New", lastName = "Customer", email = "new.customer@example.com" });

            var r2 = await adapter.GetAsync("cache-notfound").ConfigureAwait(false);
            r2.IsSuccess.Should().BeTrue();
            r2.Value.Email.Should().Be("new.customer@example.com");
        }).AssertSuccess();
    });
}
