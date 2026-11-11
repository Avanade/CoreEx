namespace CoreEx.IO.Json;

/// <summary>Exposes document-local state for EOF reconciliation.</summary>
/// <param name="identity">The immutable definition/source/attempt identity.</param>
/// <param name="mode">The requested processing mode.</param>
/// <param name="errorMode">The content-error continuation policy.</param>
public class JsonFileContext(FileProcessingIdentity identity, FileProcessingMode mode = FileProcessingMode.ValidateOnly, FileErrorMode errorMode = FileErrorMode.FailFast)
    : FileProcessingContext(identity, mode, errorMode)
{
    /// <summary>Gets the final parser line number, or null until input reaches EOF.</summary>
    public long? EndOfFileLineNumber { get; internal set; }
}
