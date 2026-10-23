namespace Contoso.Products.Application.Adapters.Embeddings;

/// <summary>
/// Provides compatible document and query embeddings for product retrieval.
/// </summary>
public interface IProductEmbeddingAdapter
{
    /// <summary>Gets the provider/model profile that must match stored document embeddings.</summary>
    string Profile { get; }

    /// <summary>Gets the required vector dimensions.</summary>
    int Dimensions { get; }

    /// <summary>Embeds a canonical product search document.</summary>
    Task<Result<float[]>> EmbedDocumentAsync(string text, CancellationToken ct = default);

    /// <summary>Embeds user search text using the model's retrieval-query instructions.</summary>
    Task<Result<float[]>> EmbedQueryAsync(string text, CancellationToken ct = default);
}
