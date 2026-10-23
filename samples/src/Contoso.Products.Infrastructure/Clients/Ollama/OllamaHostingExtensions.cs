namespace Contoso.Products.Infrastructure.Clients.Ollama;

/// <summary>Registers the Aspire Ollama client and Products-specific embedding validation.</summary>
public static class OllamaHostingExtensions
{
    /// <summary>Gets the named HTTP client used by the pinned Aspire OllamaSharp integration.</summary>
    public const string HttpClientName = "Ollama_httpClient";

    /// <summary>Adds embeddings using the configured external Ollama endpoint, service discovery and resilience.</summary>
    public static void AddProductEmbeddings(this IHostApplicationBuilder builder)
    {
        var config = builder.Configuration.GetRequiredSection("Ollama");
        builder.AddOllamaApiClient("Ollama", settings =>
        {
            settings.Endpoint = new Uri(config["BaseAddress"].ThrowIfNullOrEmpty());
            settings.SelectedModel = OllamaHttpClient.ModelName;
        });

        builder.AddNamedHttpClient(HttpClientName, client => client.Timeout = Timeout.InfiniteTimeSpan, configurationSectionName: "Ollama");

        builder.Services.AddSingleton<OllamaHttpClient>();
    }
}
