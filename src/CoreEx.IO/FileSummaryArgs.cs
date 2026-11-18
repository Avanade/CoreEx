namespace CoreEx.IO;

/// <summary>Provides the final summary and original context to successful or content-failure finalization.</summary>
public sealed class FileSummaryArgs
{
    /// <summary>Initializes framework-owned finalization arguments.</summary>
    internal FileSummaryArgs(FileProcessingContext context, FileProcessingSummary summary)
        => (Context, Summary) = (context.ThrowIfNull(), summary.ThrowIfNull());

    /// <summary>Gets the original file invocation context.</summary>
    public FileProcessingContext Context { get; }

    /// <summary>Gets the final processing summary.</summary>
    public FileProcessingSummary Summary { get; }
}
