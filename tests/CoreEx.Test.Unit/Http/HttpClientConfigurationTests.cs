namespace CoreEx.Test.Unit.Http;

public class HttpClientConfigurationTests
{
    [TestCase(false, false)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(true, true)]
    public async Task NamedClient_BindsOnlyResilienceSection(bool configureResilience, bool separateSection)
    {
        var builder = Host.CreateApplicationBuilder();
        var settings = new Dictionary<string, string?>
        {
            ["Embedding:BaseAddress"] = "http://localhost:11434/"
        };
        if (configureResilience)
        {
            settings["Embedding:Resilience:AttemptTimeout:Timeout"] = "00:01:00";
            settings["Embedding:Resilience:TotalRequestTimeout:Timeout"] = "00:02:00";
            settings["Embedding:Resilience:CircuitBreaker:SamplingDuration"] = "00:02:00";
        }

        builder.Configuration.AddInMemoryCollection(settings);
        var name = separateSection ? "Sdk_httpClient" : "Embedding";
        if (separateSection)
        {
            builder.Configuration[$"{name}:BaseAddress"] = "http://wrong-endpoint/";
            builder.Configuration[$"{name}:Resilience:AttemptTimeout:Timeout"] = "invalid";
            builder.Services.AddHttpClient(name, client => client.DefaultRequestHeaders.Add("X-Sdk", "preserved"));
            builder.AddNamedHttpClient(name, client => client.Timeout = Timeout.InfiniteTimeSpan, "Embedding");
        }
        else
            builder.AddNamedHttpClient(name);
        using var host = builder.Build();

        await host.StartAsync().ConfigureAwait(false);
        using var client = host.Services.GetRequiredService<IHttpClientFactory>().CreateClient(name);
        client.BaseAddress.Should().Be(new Uri("http://localhost:11434/"));
        if (separateSection)
        {
            client.DefaultRequestHeaders.GetValues("X-Sdk").Should().ContainSingle().Which.Should().Be("preserved");
            client.Timeout.Should().Be(Timeout.InfiniteTimeSpan);
        }
        await host.StopAsync().ConfigureAwait(false);
    }
}
