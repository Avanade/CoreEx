namespace CoreEx.IO;

/// <summary>Provides the first returned business execution failure and its unit provenance.</summary>
public sealed class FileExecutionFailedArgs
{
    /// <summary>Initializes framework-owned execution-failure arguments.</summary>
    internal FileExecutionFailedArgs(FileProcessingContext context, FileUnitInfo info, Result result)
        => (Context, Info, Result) = (context.ThrowIfNull(), info.ThrowIfNull(), result);

    /// <summary>Gets the original file invocation context, including partial progress.</summary>
    public FileProcessingContext Context { get; }

    /// <summary>Gets the failed unit's source provenance.</summary>
    public FileUnitInfo Info { get; }

    /// <summary>Gets the application's original failed result.</summary>
    public Result Result { get; }
}
