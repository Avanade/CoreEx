namespace CoreEx.Azure.Storage.Blobs;

/// <summary>Streams file attempt, diagnostic and completion records to a create-only JSONL Blob report.</summary>
public sealed class BlobJsonlFileReportAdapter : IFileProcessingReport
{
    private readonly BlobJsonlReport _report;
    private readonly Func<FileProcessingContext, object> _attempt;
    private readonly Func<FileDiagnosticArgs, object> _diagnostic;
    private readonly Func<FileReportCompletion, object> _completion;

    /// <summary>Creates an invocation-local JSONL adapter with optional application-owned record projections.</summary>
    /// <param name="report">The create-only Blob report.</param>
    /// <param name="attempt">The optional attempt-record projection.</param>
    /// <param name="diagnostic">The optional diagnostic-record projection.</param>
    /// <param name="completion">The optional terminal-record projection.</param>
    public BlobJsonlFileReportAdapter(BlobJsonlReport report, Func<FileProcessingContext, object>? attempt = null,
        Func<FileDiagnosticArgs, object>? diagnostic = null, Func<FileReportCompletion, object>? completion = null)
    {
        _report = report.ThrowIfNull();
        _attempt = attempt ?? (context => new { kind = "attempt", context.Identity });
        _diagnostic = diagnostic ?? (args => new
        {
            kind = "diagnostic", args.Diagnostic.Code, category = args.Diagnostic.Category.ToString(), severity = args.Diagnostic.Severity.ToString(),
            message = args.Diagnostic.Message.ToString(), args.Diagnostic.Location, args.Diagnostic.RecordType, args.Diagnostic.UnitOrdinal
        });
        _completion = completion ?? (result => new
        {
            kind = "outcome", result.Context.Identity, outcome = result.Outcome, result.Progress,
            validationCompleted = result.Summary?.ValidationCompleted, failureType = result.Error?.GetType().Name
        });
    }

    /// <inheritdoc/>
    public async Task StartAsync(FileProcessingContext context, CancellationToken cancellationToken = default)
    {
        await _report.StartAsync(cancellationToken).ConfigureAwait(false);
        await _report.WriteAsync(_attempt(context), cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public Task WriteDiagnosticAsync(FileDiagnosticArgs args, CancellationToken cancellationToken = default) => _report.WriteAsync(_diagnostic(args), cancellationToken);

    /// <inheritdoc/>
    public Task CompleteAsync(FileReportCompletion completion, CancellationToken cancellationToken = default)
        => _report.CompleteAsync(_completion(completion), completion.Outcome, cancellationToken);
}
