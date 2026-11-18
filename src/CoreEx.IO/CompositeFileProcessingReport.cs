namespace CoreEx.IO;

/// <summary>Writes an invocation to multiple reports in the supplied order.</summary>
/// <remarks>Operations are sequential and stop at the first failure. Reports are not atomic; earlier reports may already contain a write when a later report fails.</remarks>
public sealed class CompositeFileProcessingReport : IFileProcessingReport
{
    private readonly IFileProcessingReport[] _reports;

    /// <summary>Creates a composite from distinct, invocation-local reports.</summary>
    /// <param name="reports">The reports to write in order.</param>
    public CompositeFileProcessingReport(params IFileProcessingReport[] reports)
    {
        ArgumentNullException.ThrowIfNull(reports);
        if (reports.Length == 0 || reports.Any(x => x is null) || reports.Distinct(ReferenceEqualityComparer.Instance).Count() != reports.Length)
            throw new ArgumentException("At least one distinct, non-null report is required.", nameof(reports));

        _reports = [.. reports];
    }

    /// <inheritdoc/>
    public async Task StartAsync(FileProcessingContext context, CancellationToken cancellationToken = default)
    {
        foreach (var report in _reports)
            await report.StartAsync(context, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task WriteDiagnosticAsync(FileDiagnosticArgs args, CancellationToken cancellationToken = default)
    {
        foreach (var report in _reports)
            await report.WriteDiagnosticAsync(args, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task CompleteAsync(FileReportCompletion completion, CancellationToken cancellationToken = default)
    {
        foreach (var report in _reports)
            await report.CompleteAsync(completion, cancellationToken).ConfigureAwait(false);
    }
}
