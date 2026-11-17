namespace CoreEx.Azure.Storage.Blobs;

/// <summary>Streams projected file diagnostics as fixed-column CSV rows to a create-only Blob report.</summary>
/// <remarks>CSV completion sets outcome metadata only; it does not add an out-of-schema summary row.</remarks>
public sealed class BlobCsvFileReportAdapter(BlobCsvReport report, Func<FileDiagnosticArgs, IReadOnlyList<object?>> projectDiagnostic) : IFileProcessingReport
{
    private readonly BlobCsvReport _report = report.ThrowIfNull();
    private readonly Func<FileDiagnosticArgs, IReadOnlyList<object?>> _projectDiagnostic = projectDiagnostic.ThrowIfNull();

    /// <inheritdoc/>
    public Task StartAsync(FileProcessingContext context, CancellationToken cancellationToken = default) => _report.StartAsync(cancellationToken);

    /// <inheritdoc/>
    public Task WriteDiagnosticAsync(FileDiagnosticArgs args, CancellationToken cancellationToken = default) => _report.WriteAsync(_projectDiagnostic(args), cancellationToken);

    /// <inheritdoc/>
    public Task CompleteAsync(FileReportCompletion completion, CancellationToken cancellationToken = default) => _report.CompleteAsync(completion.Outcome, cancellationToken);
}
