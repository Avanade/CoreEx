namespace CoreEx.Test.Unit.Http;

public class HttpClientConfigurationTests
{
    [TestCase(false)]
    [TestCase(true)]
    public async Task NamedClient_BindsOnlyResilienceSection(bool configureResilience)
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
        builder.AddNamedHttpClient("Embedding");
        using var host = builder.Build();

        await host.StartAsync().ConfigureAwait(false);
        using var client = host.Services.GetRequiredService<IHttpClientFactory>().CreateClient("Embedding");
        client.BaseAddress.Should().Be(new Uri("http://localhost:11434/"));
        await host.StopAsync().ConfigureAwait(false);
    }
}
