namespace Contoso.Products.Infrastructure.Clients.Ollama;

/// <summary>
/// Provides validated, full-dimensional Nomic retrieval embeddings through the local Ollama API.
/// </summary>
public sealed class OllamaHttpClient
{
    /// <summary>Gets the versioned model tag used for this sample's embedding profile.</summary>
    public const string ModelName = "nomic-embed-text:v1.5";

    /// <summary>Gets the native embedding dimensions; reduced-dimensional embeddings are not used.</summary>
    public const int Dimensions = 768;

    /// <summary>Gets the profile identifying model, dimensions and task-prefix recipe.</summary>
    public const string Profile = "ollama:nomic-embed-text:v1.5:768:retrieval-prefix-v1";

    private static readonly Validator<ValidatedEmbedResponse> _validator = Validator.Create<ValidatedEmbedResponse>()
        .HasProperty(r => r.Model, c => c.Mandatory().Error("The embedding response model does not match the requested profile.").WhenValue(v => v != ModelName))
        .HasProperty(r => r.Embeddings, c => c.Mandatory().Error("The embedding response must contain one finite, non-zero 768-dimensional vector.")
            .WhenValue(v => v is not [{ Length: Dimensions } vector] || vector.Any(x => !float.IsFinite(x)) || !vector.Any(x => x != 0)));

    private readonly IOllamaApiClient _client;

    /// <summary>Creates the validated wrapper around the Aspire-registered OllamaSharp client.</summary>
    /// <param name="client">The SDK client configured for the Ollama endpoint.</param>
    public OllamaHttpClient(IOllamaApiClient client)
    {
        _client = client.ThrowIfNull();
        if (client is not OllamaApiClient sdk)
            throw new ArgumentException("The embedding wrapper requires the Aspire OllamaSharp client.", nameof(client));

        // The SDK's EmbedResponse omits model; retain it to validate each response against the pinned profile.
        sdk.IncomingJsonSerializerOptions.Converters.Add(new EmbedResponseConverter());
    }

    /// <summary>Embeds product text using Nomic's document instruction.</summary>
    public Task<Result<float[]>> EmbedDocumentAsync(string text, CancellationToken ct = default) => EmbedAsync(text, "search_document: ", ct);

    /// <summary>Embeds search text using Nomic's query instruction.</summary>
    public Task<Result<float[]>> EmbedQueryAsync(string text, CancellationToken ct = default) => EmbedAsync(text, "search_query: ", ct);

    /// <summary>Requests one embedding without silently truncating oversized input.</summary>
    private async Task<Result<float[]>> EmbedAsync(string text, string prefix, CancellationToken ct)
    {
        text = text.ThrowIfNull().Trim().ThrowIfNullOrEmpty(nameof(text));
        try
        {
            var response = await _client.EmbedAsync(new EmbedRequest { Model = ModelName, Input = [prefix + text], Truncate = false, KeepAlive = "5m" }, ct).ConfigureAwait(false);
            if (response is not ValidatedEmbedResponse validated)
                return Result<float[]>.Fail(new HttpRequestException("The embedding response contains no validated model metadata."));

            var validation = await _validator.ValidateAsync(validated, ct).ConfigureAwait(false);
            if (validation.HasErrors)
                return Result<float[]>.Fail(new HttpRequestException("The embedding response is invalid.", validation.ToException()));

            return validated.Embeddings is [{ Length: Dimensions } vector]
                ? Result<float[]>.Ok(vector)
                : Result<float[]>.Fail(new HttpRequestException("The validated embedding response contains no vector."));
        }
        catch (Exception ex) when (ex is HttpRequestException or OllamaException or JsonException)
        {
            return Result<float[]>.Fail(ex is HttpRequestException ? ex : new HttpRequestException("Ollama could not produce a valid embedding response.", ex));
        }
    }

    private sealed class ValidatedEmbedResponse : EmbedResponse
    {
        [JsonPropertyName("model")]
        public string? Model { get; set; }
    }

    private sealed class EmbedResponseConverter : JsonConverter<EmbedResponse>
    {
        public override EmbedResponse? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => JsonSerializer.Deserialize<ValidatedEmbedResponse>(ref reader, options);

        public override void Write(Utf8JsonWriter writer, EmbedResponse value, JsonSerializerOptions options)
            => throw new NotSupportedException("Embedding responses are read-only.");
    }
}
