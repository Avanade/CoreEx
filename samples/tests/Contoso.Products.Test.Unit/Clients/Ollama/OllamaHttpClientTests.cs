namespace Contoso.Products.Test.Unit.Clients.Ollama;

public class OllamaHttpClientTests : WithGenericTester<EntryPoint>
{
    private UnitTestEx.Mocking.MockHttpClientRequest _request = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        var factory = MockHttpClientFactory.Create();
        _request = factory.CreateClient(OllamaHostingExtensions.HttpClientName).Request(HttpMethod.Post, "api/embed");
        _request.WithAnyBody();
        Test.ReplaceHttpClientFactory(factory);
    }

    [Test]
    public void EmbedQuery_ValidResponse_ReturnsFullVector() => Test.Scoped(test =>
    {
        var vector = Enumerable.Repeat(0.1f, OllamaHttpClient.Dimensions).ToArray();
        _request.Respond.WithJson(new { model = OllamaHttpClient.ModelName, embeddings = new[] { vector } });
        test.Run(async _ =>
        {
            var result = await ExecutionContext.GetRequiredService<OllamaHttpClient>().EmbedQueryAsync("bike cleaner").ConfigureAwait(false);
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Equal(vector);
        }).AssertSuccess();
        _request.Verify();
    });

    [TestCase("missing")]
    [TestCase("empty")]
    [TestCase("wrong-dimensions")]
    [TestCase("zero")]
    [TestCase("multiple")]
    [TestCase("wrong-model")]
    [TestCase("missing-model")]
    [TestCase("null-vector")]
    public void EmbedDocument_InvalidSuccessfulResponse_ReturnsDependencyFailure(string scenario) => Test.Scoped(test =>
    {
        var vector = Enumerable.Repeat(0.1f, OllamaHttpClient.Dimensions).ToArray();
        float[][]? embeddings = scenario switch
        {
            "missing" => null,
            "empty" => [],
            "wrong-dimensions" => [new float[3]],
            "zero" => [new float[OllamaHttpClient.Dimensions]],
            "multiple" => [vector, vector],
            "null-vector" => [null!], // Exercises an invalid provider payload, not an application-produced vector.
            _ => [vector]
        };
        _request.Respond.WithJson(new { model = scenario == "missing-model" ? null : scenario == "wrong-model" ? "another-model" : OllamaHttpClient.ModelName, embeddings });
        test.Run(async _ =>
        {
            var result = await ExecutionContext.GetRequiredService<OllamaHttpClient>().EmbedDocumentAsync("Name: Cleaner").ConfigureAwait(false);
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeOfType<HttpRequestException>().Which.InnerException.Should().BeOfType<ValidationException>();
        }).AssertSuccess();
        _request.Verify();
    });

    [TestCase(HttpStatusCode.InternalServerError)]
    [TestCase(HttpStatusCode.NotFound)]
    [TestCase(HttpStatusCode.BadRequest)]
    public void EmbedQuery_ProviderError_ReturnsFailure(HttpStatusCode status) => Test.Scoped(test =>
    {
        _request.Respond.With(status);
        test.Run(async _ =>
        {
            var result = await ExecutionContext.GetRequiredService<OllamaHttpClient>().EmbedQueryAsync("cleaner").ConfigureAwait(false);
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeOfType<HttpRequestException>();
        }).AssertSuccess();
        _request.Verify();
    });

    [TestCase(true, "search_query: ")]
    [TestCase(false, "search_document: ")]
    public async Task Embed_UsesCorrectInstructionAndDisablesTruncation(bool query, string prefix)
    {
        using var handler = new InspectHandler(async (request, ct) =>
        {
            request.RequestUri.Should().Be(new Uri("http://localhost:11434/api/embed"));
            var json = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(ct));
            using (json)
            {
                json.RootElement.GetProperty("model").GetString().Should().Be(OllamaHttpClient.ModelName);
                json.RootElement.GetProperty("input").EnumerateArray().Should().ContainSingle().Which.GetString().Should().Be(prefix + "cleaner");
                json.RootElement.GetProperty("truncate").GetBoolean().Should().BeFalse();
                json.RootElement.GetProperty("keep_alive").GetString().Should().Be("5m");
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(new { model = OllamaHttpClient.ModelName, embeddings = new[] { Enumerable.Repeat(0.1f, 768).ToArray() } }))
            };
        });
        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:11434/") };
        using var sdk = new OllamaApiClient(http);
        var client = new OllamaHttpClient(sdk);
        var result = query ? await client.EmbedQueryAsync(" cleaner ") : await client.EmbedDocumentAsync(" cleaner ");
        result.IsSuccess.Should().BeTrue();
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("  ")]
    public async Task Embed_BlankInputDoesNotCallProvider(string? text)
    {
        using var sdk = new OllamaApiClient(new Uri("http://localhost:11434/"));
        var client = new OllamaHttpClient(sdk);
        Func<Task> action = () => client.EmbedQueryAsync(text!);
        await action.Should().ThrowAsync<ArgumentException>();
    }

    [Test]
    public async Task Embed_NonFiniteCoordinate_ReturnsDependencyFailure()
    {
        var values = string.Join(',', Enumerable.Repeat("0.1", OllamaHttpClient.Dimensions - 1).Append("1e100"));
        using var handler = new InspectHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent($"{{\"model\":\"{OllamaHttpClient.ModelName}\",\"embeddings\":[[{values}]]}}")
        }));
        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:11434/") };
        using var sdk = new OllamaApiClient(http);
        var result = await new OllamaHttpClient(sdk).EmbedQueryAsync("cleaner");
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<HttpRequestException>().Which.InnerException.Should().BeOfType<ValidationException>();
    }

    [Test]
    public async Task Embed_CancellationIsPropagated()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        using var http = new HttpClient { BaseAddress = new Uri("http://localhost:11434/") };
        using var sdk = new OllamaApiClient(http);
        var client = new OllamaHttpClient(sdk);
        Func<Task> action = () => client.EmbedQueryAsync("cleaner", cancellation.Token);
        await action.Should().ThrowAsync<OperationCanceledException>();
    }

    [Test]
    public async Task Registration_UsesAspireClientWithServiceDiscoveryAndResilience()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration["Ollama:BaseAddress"] = "http://ollama";
        builder.Configuration["Ollama:Resilience:Retry:Delay"] = "00:00:00.001";
        builder.Configuration["Ollama:Resilience:Retry:UseJitter"] = "false";
        builder.Configuration["Ollama:Resilience:Retry:MaxRetryAttempts"] = "1";
        builder.Configuration["Services:ollama:http:0"] = "http://localhost:11434";
        builder.AddProductEmbeddings();
        var requests = 0;
        builder.Services.AddHttpClient(OllamaHostingExtensions.HttpClientName).ConfigurePrimaryHttpMessageHandler(() => new InspectHandler((request, _) =>
        {
            request.RequestUri.Should().Be(new Uri("http://localhost:11434/api/embed"));
            requests++;
            return Task.FromResult(new HttpResponseMessage(requests == 1 ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(new { model = OllamaHttpClient.ModelName, embeddings = new[] { Enumerable.Repeat(0.1f, 768).ToArray() } }))
            });
        }));

        using var host = builder.Build();
        host.Services.GetRequiredService<IOllamaApiClient>().Should().BeOfType<OllamaApiClient>();
        using var http = host.Services.GetRequiredService<IHttpClientFactory>().CreateClient(OllamaHostingExtensions.HttpClientName);
        http.Timeout.Should().Be(Timeout.InfiniteTimeSpan);
        var result = await host.Services.GetRequiredService<OllamaHttpClient>().EmbedQueryAsync("cleaner");
        result.IsSuccess.Should().BeTrue();
        requests.Should().Be(2);
    }

    [TestCase("null")]
    [TestCase("{")]
    public async Task Embed_InvalidJson_ReturnsDependencyFailure(string body)
    {
        using var handler = new InspectHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body) }));
        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:11434/") };
        using var sdk = new OllamaApiClient(http);
        var result = await new OllamaHttpClient(sdk).EmbedQueryAsync("cleaner");
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<HttpRequestException>();
    }

    private sealed class InspectHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> inspect) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => inspect(request, cancellationToken);
    }
}
