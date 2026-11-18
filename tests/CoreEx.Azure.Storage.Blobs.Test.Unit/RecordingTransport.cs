namespace CoreEx.Azure.Storage.Blobs.Test.Unit;

internal sealed class RecordingTransport : HttpPipelineTransport
{
    public List<string> Blocks { get; } = [];
    public List<string?> AppendPositions { get; } = [];
    public List<string?> MatchConditions { get; } = [];
    public List<string?> Outcomes { get; } = [];
    public string? CreationCondition { get; private set; }
    public string? ContentType { get; private set; }
    public bool FailCreate { get; init; }
    public bool FailAppend { get; set; }
    public bool FailMetadata { get; set; }
    public Func<Task>? OnAppend { get; set; }
    public int Requests { get; private set; }
    public override Request CreateRequest() => HttpClientTransport.Shared.CreateRequest();
    public override void Process(HttpMessage message) => ProcessAsync(message).AsTask().GetAwaiter().GetResult();

    public override async ValueTask ProcessAsync(HttpMessage message)
    {
        Requests++;
        message.CancellationToken.ThrowIfCancellationRequested();
        var query = message.Request.Uri.Query;
        message.Request.Headers.TryGetValue("If-Match", out var match);
        if (query.Contains("comp=appendblock", StringComparison.Ordinal))
        {
            if (OnAppend is not null)
                await OnAppend();

            if (FailAppend)
                throw new RequestFailedException(412, "Ambiguous append or conflicting writer.");

            MatchConditions.Add(match);
            message.Request.Headers.TryGetValue("x-ms-blob-condition-appendpos", out var position);
            AppendPositions.Add(position);
            using var buffer = new MemoryStream();
            if (message.Request.Content is not null)
                await message.Request.Content.WriteToAsync(buffer, message.CancellationToken);

            Blocks.Add(Encoding.UTF8.GetString(buffer.ToArray()));
        }
        else
        {
            if (query.Contains("comp=metadata", StringComparison.Ordinal))
            {
                if (FailMetadata)
                    throw new RequestFailedException(412, "Metadata conflict.");

                MatchConditions.Add(match);
            }
            else
            {
                if (FailCreate)
                    throw new RequestFailedException(409, "Blob already exists.");

                message.Request.Headers.TryGetValue("If-None-Match", out var condition);
                CreationCondition = condition;
                message.Request.Headers.TryGetValue("x-ms-blob-content-type", out var contentType);
                ContentType = contentType;
            }

            message.Request.Headers.TryGetValue("x-ms-meta-outcome", out var outcome);
            Outcomes.Add(outcome);
        }

        message.Response = new StubResponse(query.Contains("comp=metadata", StringComparison.Ordinal) ? 200 : 201, $"\"{Requests}\"");
    }
}
