namespace CoreEx.Azure.Storage.Blobs.Test.Unit;

internal sealed class StubResponse(int status = 200, string etag = "\"selected\"") : Response
{
    public override int Status => status;
    public override string ReasonPhrase => "OK";
    public override Stream? ContentStream { get; set; }
    public override string ClientRequestId { get; set; } = "";
    public override void Dispose() { }
    protected override bool ContainsHeader(string name) => name.Equals("ETag", StringComparison.OrdinalIgnoreCase);
    protected override IEnumerable<HttpHeader> EnumerateHeaders() => [new("ETag", etag)];

    protected override bool TryGetHeader(string name, [NotNullWhen(true)] out string? value)
    {
        value = ContainsHeader(name) ? etag : null;
        return value is not null;
    }

    protected override bool TryGetHeaderValues(string name, [NotNullWhen(true)] out IEnumerable<string>? values)
    {
        values = ContainsHeader(name) ? [etag] : null;
        return values is not null;
    }
}
