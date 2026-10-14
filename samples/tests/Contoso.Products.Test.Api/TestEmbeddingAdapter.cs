namespace Contoso.Products.Test.Api;

internal sealed class TestEmbeddingAdapter : IProductEmbeddingAdapter
{
    public string Profile => "tests:controlled-vectors:768";
    public int Dimensions => OllamaHttpClient.Dimensions;
    public int? FailDocumentOnCall { get; set; }
    public int DocumentCallCount { get; set; }
    public int QueryCallCount { get; set; }
    public float[]? QueryVector { get; set; }
    public bool FailQuery { get; set; }
    public Func<CancellationToken, Task>? BeforeDocument { get; set; }

    public async Task<Result<float[]>> EmbedDocumentAsync(string text, CancellationToken ct = default)
    {
        if (BeforeDocument is not null)
            await BeforeDocument(ct).ConfigureAwait(false);
        if (++DocumentCallCount == FailDocumentOnCall)
            throw new HttpRequestException("Controlled embedding failure.");
        return await EmbedAsync(text, ct).ConfigureAwait(false);
    }
    public Task<Result<float[]>> EmbedQueryAsync(string text, CancellationToken ct = default)
    {
        QueryCallCount++;
        if (FailQuery)
            return Task.FromException<Result<float[]>>(new HttpRequestException("Controlled query embedding failure."));
        return QueryVector is null ? EmbedAsync(text, ct) : Task.FromResult(Result.Ok(QueryVector));
    }

    private static Task<Result<float[]>> EmbedAsync(string text, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var vector = new float[OllamaHttpClient.Dimensions];
        vector[0] = 0.1f;
        if (text.Contains("derailleur", StringComparison.OrdinalIgnoreCase) || text.Contains("changing gears", StringComparison.OrdinalIgnoreCase))
            vector[1] = 1;
        else if (text.Contains("sram-xx1", StringComparison.OrdinalIgnoreCase))
            vector[2] = 1;
        else if (text.Contains("cleaner", StringComparison.OrdinalIgnoreCase) || text.Contains("clean mud", StringComparison.OrdinalIgnoreCase))
            vector[3] = 1;
        else
            vector[4] = 1;
        return Task.FromResult(Result.Ok(vector));
    }
}
