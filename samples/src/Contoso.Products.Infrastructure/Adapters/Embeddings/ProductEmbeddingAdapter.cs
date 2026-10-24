namespace Contoso.Products.Infrastructure.Adapters.Embeddings;

/// <summary>Isolates the Products application from Ollama's transport and retrieval instructions.</summary>
/// <param name="client">The validated embedding client.</param>
[ScopedService<IProductEmbeddingAdapter>]
public sealed class ProductEmbeddingAdapter(OllamaHttpClient client) : IProductEmbeddingAdapter
{
    private readonly OllamaHttpClient _client = client.ThrowIfNull();

    /// <inheritdoc/>
    public string Profile => OllamaHttpClient.Profile;

    /// <inheritdoc/>
    public int Dimensions => OllamaHttpClient.Dimensions;

    /// <inheritdoc/>
    public Task<Result<float[]>> EmbedDocumentAsync(string text, CancellationToken ct = default) => _client.EmbedDocumentAsync(text, ct);

    /// <inheritdoc/>
    public Task<Result<float[]>> EmbedQueryAsync(string text, CancellationToken ct = default) => _client.EmbedQueryAsync(text, ct);
}
