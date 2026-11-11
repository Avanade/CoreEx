namespace CoreEx.IO;

/// <summary>Provides completed traversal state and an awaited diagnostic sink for EOF reconciliation.</summary>
public sealed class FileValidationArgs
{
    /// <summary>Initializes framework-owned file-validation arguments.</summary>
    internal FileValidationArgs(FileProcessingContext context, Func<FileDiagnostic, CancellationToken, Task> reportDiagnosticAsync)
        => (Context, ReportDiagnosticAsync) = (context.ThrowIfNull(), reportDiagnosticAsync.ThrowIfNull());

    /// <summary>Gets the validation-pass context containing reconciliation state.</summary>
    public FileProcessingContext Context { get; }

    /// <summary>Gets the diagnostic sink; await every emission and forward its cancellation token.</summary>
    public Func<FileDiagnostic, CancellationToken, Task> ReportDiagnosticAsync { get; }
}
