namespace CoreEx.Test.Unit.Http;

[TestFixture]
public class HttpClientExtensionsTests
{
    [Test]
    public async Task SendAsync_ResolvesRelativeUriBeforeConfiguration()
    {
        using var handler = new InspectHandler(async (request, ct) =>
        {
            await Task.Yield();
            request.RequestUri.Should().Be(new Uri("https://example.com/api/products?$filter=name%20eq%20%27A%26B%27&$take=5"));
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
        });
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://example.com/") };
        using var response = await client.SendAsync(HttpMethod.Get, "/api/products", r => r.WithQuery("name eq 'A&B'").WithPaging(take: 5));
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }

    [Test]
    public async Task SendAsync_AbsoluteUriDoesNotRequireBaseAddress()
    {
        using var handler = new InspectHandler((request, ct) =>
        {
            request.RequestUri.Should().Be(new Uri("https://example.com/api"));
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        });
        using var client = new HttpClient(handler);
        using var response = await client.SendAsync(HttpMethod.Get, "https://example.com/api");
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }

    [TestCase(null, "application/json")]
    [TestCase("application/merge-patch+json", "application/merge-patch+json")]
    public async Task SendValue_PreservesContentUntilCompletionAndUsesCoreExJson(string? mediaType, string expectedMediaType)
    {
        var payload = new { ProductId = "123", Quantity = 2 };
        using var handler = new InspectHandler(async (request, ct) =>
        {
            await Task.Yield();
            var content = request.Content ?? throw new InvalidOperationException("Request content is required.");
            content.Headers.ContentType?.MediaType.Should().Be(expectedMediaType);
            (await content.ReadAsStringAsync(ct)).Should().Be(System.Text.Json.JsonSerializer.Serialize(payload, CoreEx.Json.JsonDefaults.SerializerOptions));
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
        });
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://example.com/") };
        using var response = await client.SendValueAsync(HttpMethod.Post, "/api", payload, mediaType: mediaType);
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }

    private sealed class InspectHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> inspect) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => inspect(request, cancellationToken);
    }
}
