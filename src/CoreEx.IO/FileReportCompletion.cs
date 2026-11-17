namespace CoreEx.IO;

/// <summary>Describes a returned file outcome or a propagated operational failure.</summary>
public sealed class FileReportCompletion
{
    /// <summary>Creates framework-owned completion data.</summary>
    internal FileReportCompletion(FileProcessingContext context, FileProcessingSummary? summary, Exception? error)
        => (Context, Summary, Error, Progress) = (context, summary, error, summary?.Progress ?? context.Progress);

    /// <summary>Gets the invocation context.</summary>
    public FileProcessingContext Context { get; }

    /// <summary>Gets the returned summary, or null when processing threw.</summary>
    public FileProcessingSummary? Summary { get; }

    /// <summary>Gets the original operational or returned execution failure, if any.</summary>
    public Exception? Error { get; }

    /// <summary>Gets the latest immutable progress snapshot.</summary>
    public FileProcessingProgress Progress { get; }

    /// <summary>Gets the returned outcome or <c>Faulted</c> for an operational exception.</summary>
    public string Outcome => Summary?.Outcome.ToString() ?? "Faulted";
}
