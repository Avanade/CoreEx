namespace CoreEx.IO;

/// <summary>Provides one diagnostic under the original file-level ambient context.</summary>
public sealed class FileDiagnosticArgs
{
    /// <summary>Initializes framework-owned diagnostic arguments.</summary>
    internal FileDiagnosticArgs(FileProcessingContext context, FileDiagnostic diagnostic)
        => (Context, Diagnostic) = (context.ThrowIfNull(), diagnostic.ThrowIfNull());

    /// <summary>Gets the file invocation context, not a unit's scoped context.</summary>
    public FileProcessingContext Context { get; }

    /// <summary>Gets the emitted finding without raw input values by default.</summary>
    public FileDiagnostic Diagnostic { get; }
}
