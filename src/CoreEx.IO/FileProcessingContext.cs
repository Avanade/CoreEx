namespace CoreEx.IO;

/// <summary>Holds invocation-local identity and reconciliation state, not ambient DI or authentication state.</summary>
/// <remarks>Derive to add typed accumulators. Create fresh pass-local contexts for validation and execution using the same identity; do not retain scoped services, tracked entities, or diagnostic history here.</remarks>
public class FileProcessingContext
{
    private readonly Dictionary<string, object> _metadata = new(StringComparer.Ordinal);

    /// <summary>Initializes a file context with explicit identity and processing policy.</summary>
    /// <param name="identity">The immutable definition/source/attempt identity.</param>
    /// <param name="mode">The requested processing mode.</param>
    /// <param name="errorMode">The file's default content-error continuation policy.</param>
    public FileProcessingContext(FileProcessingIdentity identity, FileProcessingMode mode = FileProcessingMode.ValidateOnly, FileErrorMode errorMode = FileErrorMode.FailFast)
    {
        if (!Enum.IsDefined(mode))
            throw new ArgumentOutOfRangeException(nameof(mode));

        if (!Enum.IsDefined(errorMode))
            throw new ArgumentOutOfRangeException(nameof(errorMode));

        Identity = identity.ThrowIfNull();
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

    /// <summary>Gets the immutable accepted-file metadata snapshot, or null until the whole file is accepted.</summary>
    /// <remarks>All source formats use System.Text.Json values to isolate accepted metadata from mutable wire models. Metadata types must support default System.Text.Json serialization and deserialization.</remarks>
    public IReadOnlyDictionary<string, JsonElement>? AcceptedMetadata { get; private set; }

    /// <summary>Gets a typed pass-local metadata record by its configured logical name.</summary>
    /// <typeparam name="TMetadata">The metadata record type.</typeparam>
    /// <param name="name">The exact configured logical name.</param>
    /// <returns>The most recently accepted pass-local value, or null when it has not been encountered.</returns>
    public TMetadata? GetMetadata<TMetadata>(string name) where TMetadata : class
        => _metadata.TryGetValue(name.ThrowIfNullOrWhiteSpace(), out var value) ? value as TMetadata
            ?? throw new InvalidOperationException("The requested metadata type does not match the definition.") : null;

    /// <summary>Deserializes a fresh typed value from the immutable accepted-file metadata snapshot.</summary>
    /// <typeparam name="TMetadata">The metadata record type.</typeparam>
    /// <param name="name">The exact configured logical name.</param>
    /// <returns>A fresh typed metadata value, or null when the name is not declared.</returns>
    /// <remarks>Each call deserializes a separate copy using default System.Text.Json behavior; changing it does not change the accepted snapshot.</remarks>
    public TMetadata? GetAcceptedMetadata<TMetadata>(string name) where TMetadata : class
        => AcceptedMetadata is not null && AcceptedMetadata.TryGetValue(name.ThrowIfNullOrWhiteSpace(), out var value)
            ? value.Deserialize<TMetadata>() ?? throw new InvalidOperationException("The accepted metadata value could not be deserialized.") : null;

    /// <summary>Stores an encountered validated metadata value in this pass-local context.</summary>
    internal void SetMetadata(string name, object value) => _metadata[name.ThrowIfNullOrWhiteSpace()] = value.ThrowIfNull();

    /// <summary>Freezes pass-local metadata into a deeply immutable JSON value snapshot.</summary>
    internal void FreezeAcceptedMetadata()
    {
        var snapshot = _metadata.ToDictionary(x => x.Key, x => JsonSerializer.SerializeToElement(x.Value).Clone(), StringComparer.Ordinal);
        AcceptedMetadata = new System.Collections.ObjectModel.ReadOnlyDictionary<string, JsonElement>(snapshot);
    }

    /// <summary>Copies an already frozen snapshot into a fresh execution context.</summary>
    internal void SetAcceptedMetadata(IReadOnlyDictionary<string, JsonElement>? metadata)
        => AcceptedMetadata = metadata is null ? null : new System.Collections.ObjectModel.ReadOnlyDictionary<string, JsonElement>(
            metadata.ToDictionary(x => x.Key, x => x.Value.Clone(), StringComparer.Ordinal));
}
