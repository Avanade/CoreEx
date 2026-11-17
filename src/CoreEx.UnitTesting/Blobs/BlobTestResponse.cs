namespace CoreEx.UnitTesting.Blobs;

/// <summary>Provides raw status information for SDK-level test response models without simulating HTTP headers.</summary>
internal sealed class BlobTestResponse(int status) : global::Azure.Response
{
    /// <inheritdoc/>
    public override int Status => status;

    /// <inheritdoc/>
    public override string ReasonPhrase => string.Empty;

    /// <inheritdoc/>
    public override Stream? ContentStream { get; set; }

    /// <inheritdoc/>
    public override string ClientRequestId { get; set; } = string.Empty;

    /// <inheritdoc/>
    public override void Dispose() => ContentStream?.Dispose();

    /// <inheritdoc/>
    protected override bool ContainsHeader(string name) => false;

    /// <inheritdoc/>
    protected override IEnumerable<global::Azure.Core.HttpHeader> EnumerateHeaders() => [];

    /// <inheritdoc/>
    protected override bool TryGetHeader(string name, [NotNullWhen(true)] out string? value)
    {
        value = null;
        return false;
    }

    /// <inheritdoc/>
    protected override bool TryGetHeaderValues(string name, [NotNullWhen(true)] out IEnumerable<string>? values)
    {
        values = null;
        return false;
    }
}
