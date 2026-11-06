namespace CoreEx.IO;

/// <summary>Holds invocation-local identity and reconciliation state, not ambient DI or authentication state.</summary>
/// <remarks>Derive to add typed accumulators. Create fresh pass-local contexts for validation and execution using the same identity; do not retain scoped services, tracked entities, or diagnostic history here.</remarks>
public class FileProcessingContext
{
    /// <summary>Initializes a file context with explicit identity and processing policy.</summary>
    /// <param name="identity">The immutable definition/source/attempt identity.</param>
    /// <param name="mode">The requested processing mode.</param>
    /// <param name="errorMode">The file's default content-error continuation policy.</param>
    public FileProcessingContext(FileProcessingIdentity identity, FileProcessingMode mode = FileProcessingMode.ValidateOnly, FileErrorMode errorMode = FileErrorMode.FailFast)
    {
        identity.ThrowIfNull();
        if (!Enum.IsDefined(mode))
            throw new ArgumentOutOfRangeException(nameof(mode));

        if (!Enum.IsDefined(errorMode))
            throw new ArgumentOutOfRangeException(nameof(errorMode));

        Identity = identity;
        Mode = mode;
        ErrorMode = errorMode;
    }

    /// <summary>Gets the identity preserved across validation and execution passes.</summary>
    public FileProcessingIdentity Identity { get; }

    /// <summary>Gets the requested processing mode.</summary>
    public FileProcessingMode Mode { get; }

    /// <summary>Gets the default continuation policy for expected content errors.</summary>
    public FileErrorMode ErrorMode { get; }

    /// <summary>Gets the latest immutable progress snapshot, including partial progress before an operational fault.</summary>
    public FileProcessingProgress Progress { get; internal set; } = new();
}
