namespace Contoso.Products.Infrastructure.Clients.Ollama;

/// <summary>
/// Provides validated, full-dimensional Nomic retrieval embeddings through the local Ollama API.
/// </summary>
/// <param name="httpClient">The typed client configured for the Ollama endpoint.</param>
public sealed class OllamaHttpClient(HttpClient httpClient)
{
    /// <summary>Gets the versioned model tag used for this sample's embedding profile.</summary>
    public const string ModelName = "nomic-embed-text:v1.5";

    /// <summary>Gets the native embedding dimensions; reduced-dimensional embeddings are not used.</summary>
    public const int Dimensions = 768;

    /// <summary>Gets the profile identifying model, dimensions and task-prefix recipe.</summary>
    public const string Profile = "ollama:nomic-embed-text:v1.5:768:retrieval-prefix-v1";

    // Ollama defaults to truncation when the false value is omitted; do not use CoreEx's omit-default serialization here.
    private static readonly JsonSerializerOptions _requestOptions = new(JsonSerializerDefaults.Web);

    private static readonly Validator<EmbedResponse> _validator = Validator.Create<EmbedResponse>()
        .HasProperty(r => r.Model, c => c.Mandatory().Error("The embedding response model does not match the requested profile.").WhenValue(v => v != ModelName))
        .HasProperty(r => r.Embeddings, c => c.Mandatory().Error("The embedding response must contain one finite, non-zero 768-dimensional vector.")
            .WhenValue(v => v is not [{ Length: Dimensions } vector] || vector.Any(x => !float.IsFinite(x)) || !vector.Any(x => x != 0)));

    private readonly HttpClient _httpClient = httpClient.ThrowIfNull();

    /// <summary>Embeds product text using Nomic's document instruction.</summary>
    public Task<Result<float[]>> EmbedDocumentAsync(string text, CancellationToken ct = default) => EmbedAsync(text, "search_document: ", ct);

    /// <summary>Embeds search text using Nomic's query instruction.</summary>
    public Task<Result<float[]>> EmbedQueryAsync(string text, CancellationToken ct = default) => EmbedAsync(text, "search_query: ", ct);

    /// <summary>Requests one embedding without silently truncating oversized input.</summary>
    private Task<Result<float[]>> EmbedAsync(string text, string prefix, CancellationToken ct)
    {
        text = text.ThrowIfNull().Trim().ThrowIfNullOrEmpty(nameof(text));
        return _httpClient.SendValue(HttpMethod.Post, "api/embed", new { model = ModelName, input = prefix + text, truncate = false, keep_alive = "5m" }, jsonSerializerOptions: _requestOptions)
            .WithValidator(_validator)
            .ToResultAsync(ct)
            .ThenAs(r => r.Embeddings is [{ Length: Dimensions } vector] ? vector : throw new HttpRequestException("The validated embedding response contains no vector."));
    }

    /// <summary>Represents only the fields consumed from Ollama's embedding response.</summary>
    private sealed class EmbedResponse
    {
        /// <summary>Gets or sets the model that produced the vector.</summary>
        public string? Model { get; set; }

        /// <summary>Gets or sets the embedding batch; this client requires exactly one vector.</summary>
        public float[][]? Embeddings { get; set; }
    }
}
