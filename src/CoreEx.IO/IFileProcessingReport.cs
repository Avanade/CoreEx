namespace CoreEx.IO;

/// <summary>Provides an awaited, invocation-local file report without owning processing or storage policy.</summary>
/// <remarks>Methods are called sequentially. A failure propagates and the same report is not retried. Reports must not be shared by concurrent invocations.</remarks>
public interface IFileProcessingReport
{
    /// <summary>Starts an attempt before the input is opened.</summary>
    /// <param name="context">The invocation context and caller-owned identity.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task StartAsync(FileProcessingContext context, CancellationToken cancellationToken = default);

    /// <summary>Writes one diagnostic with backpressure, without accumulating the file's diagnostics.</summary>
    /// <param name="args">The diagnostic and original file context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task WriteDiagnosticAsync(FileDiagnosticArgs args, CancellationToken cancellationToken = default);

    /// <summary>Finalizes a returned outcome or an operational fault.</summary>
    /// <param name="completion">The outcome, latest progress and optional original failure.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task CompleteAsync(FileReportCompletion completion, CancellationToken cancellationToken = default);
}
