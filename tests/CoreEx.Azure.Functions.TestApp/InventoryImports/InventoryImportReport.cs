namespace CoreEx.Azure.Functions.TestApp.InventoryImports;

/// <summary>Streams one attempt to an append Blob; only explicit terminal outcomes finalize its metadata.</summary>
/// <remarks>The report container is provisioned outside execution. In-progress reports remain observable after faults or cancellation.</remarks>
public sealed class InventoryImportReport(BlobServiceClient storage, InventoryImportOptions options)
{
    private BlobJsonlReport? _report;

    /// <summary>Creates a unique attempt report without overwriting an earlier attempt.</summary>
    public async Task StartAsync(FileProcessingIdentity identity, CancellationToken cancellationToken = default)
    {
        if (_report is not null)
            throw new InvalidOperationException("Each invocation requires a new report scope.");

        _report = new BlobJsonlReport(storage.GetBlobContainerClient(options.ReportContainer).GetAppendBlobClient(InventoryImportIdentity.ReportName(identity)));
        await _report.StartAsync(cancellationToken).ConfigureAwait(false);
        await WriteAsync(new { kind = "attempt", identity }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Appends one awaited JSON record without retaining file-wide diagnostics.</summary>
    public Task WriteAsync(object value, CancellationToken cancellationToken = default)
        => (_report ?? throw new InvalidOperationException("The attempt report has not started.")).WriteAsync(value, cancellationToken);

    /// <summary>Writes a diagnostic consistently across the generated format processors.</summary>
    public Task WriteDiagnosticAsync(FileDiagnosticArgs args, CancellationToken cancellationToken = default)
        => WriteAsync(new
        {
            kind = "diagnostic", args.Context.Identity, args.Diagnostic.Code, args.Diagnostic.Category,
            message = args.Diagnostic.Message.ToString(), args.Diagnostic.Location
        }, cancellationToken);

    /// <summary>Writes the summary before making the explicit outcome visible to report consumers.</summary>
    public Task FinishAsync(FileProcessingSummary summary, CancellationToken cancellationToken = default)
        => (_report ?? throw new InvalidOperationException("The attempt report has not started.")).CompleteAsync(new
        {
            kind = "summary", summary.Identity, outcome = summary.Outcome.ToString(), summary.Progress,
            summary.ValidationCompleted, executionErrorType = summary.ExecutionResult.IsFailure ? summary.ExecutionResult.Error.GetType().Name : null
        }, summary.Outcome.ToString(), cancellationToken);
}
