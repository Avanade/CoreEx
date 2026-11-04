namespace CoreEx.Azure.Functions.TestApp.InventoryImports;

/// <summary>Streams one attempt to an append Blob; only explicit terminal outcomes finalize its metadata.</summary>
/// <remarks>The report container is provisioned outside execution. In-progress reports remain observable after faults or cancellation.</remarks>
public sealed class InventoryImportReport(BlobServiceClient storage, InventoryImportOptions options)
{
    private AppendBlobClient? _blob;

    /// <summary>Creates a unique attempt report without overwriting an earlier attempt.</summary>
    public async Task StartAsync(FileProcessingIdentity identity, CancellationToken cancellationToken = default)
    {
        if (_blob is not null)
            throw new InvalidOperationException("Each invocation requires a new report scope.");

        _blob = storage.GetBlobContainerClient(options.ReportContainer).GetAppendBlobClient(InventoryImportIdentity.ReportName(identity));
        await _blob.CreateAsync(new AppendBlobCreateOptions
        {
            Conditions = new AppendBlobRequestConditions { IfNoneMatch = ETag.All },
            Metadata = new Dictionary<string, string> { ["outcome"] = "InProgress" }
        }, cancellationToken).ConfigureAwait(false);
        await WriteAsync(new { kind = "attempt", identity }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Appends one awaited JSON record without retaining file-wide diagnostics.</summary>
    public async Task WriteAsync(object value, CancellationToken cancellationToken = default)
    {
        var blob = _blob ?? throw new InvalidOperationException("The attempt report has not started.");
        await using var content = new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value, JsonDefaults.SerializerOptions) + "\n"));
        await blob.AppendBlockAsync(content, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Writes the summary before making the explicit outcome visible to report consumers.</summary>
    public async Task FinishAsync(FileProcessingSummary summary, CancellationToken cancellationToken = default)
    {
        await WriteAsync(new
        {
            kind = "summary", summary.Identity, outcome = summary.Outcome.ToString(), summary.Progress,
            summary.ValidationCompleted, executionErrorType = summary.ExecutionResult.IsFailure ? summary.ExecutionResult.Error.GetType().Name : null
        }, cancellationToken).ConfigureAwait(false);
        await (_blob ?? throw new InvalidOperationException("The attempt report has not started."))
            .SetMetadataAsync(new Dictionary<string, string> { ["outcome"] = summary.Outcome.ToString() }, cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}
