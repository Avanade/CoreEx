namespace CoreEx.IO.Csv;

/// <summary>Holds pass-local recognized counts, metadata locations, and caller-defined reconciliation state.</summary>
/// <remarks>Only declared metadata is retained file-wide; root groups and diagnostics are not retained. Derive for typed accessors and accumulators, and override replay-context creation for a derived context.</remarks>
/// <param name="identity">The immutable definition/source identity.</param>
/// <param name="mode">The requested mode.</param>
/// <param name="errorMode">The default record policy.</param>
public class CsvHierarchyContext(FileProcessingIdentity identity, FileProcessingMode mode = FileProcessingMode.ValidateOnly, FileErrorMode errorMode = FileErrorMode.FailFast) : FileProcessingContext(identity, mode, errorMode)
{
    private readonly Dictionary<string, long> _counts = new(StringComparer.Ordinal);
    private readonly Dictionary<string, long> _metadataLines = new(StringComparer.Ordinal);
    private bool _initialized;

    /// <summary>Gets the last encountered record's physical start, or one for an empty file.</summary>
    public long EndOfFileLineNumber { get; internal set; } = 1;

    /// <summary>Gets whether content errors have occurred in this pass.</summary>
    public bool HasValidationErrors { get; internal set; }

    /// <summary>Gets a recognized route's occurrences, including invalid recognized records.</summary>
    /// <param name="key">The exact configured logical key.</param>
    /// <returns>The recognized count, not the accepted record count.</returns>
    public long GetRecordCount(string key) => _counts.TryGetValue(key.ThrowIfNullOrWhiteSpace(), out var count) ? count
        : throw new ArgumentException("The record key is not configured in this file.", nameof(key));

    /// <summary>Gets the physical start line of an encountered metadata record.</summary>
    /// <param name="name">The exact configured metadata name.</param>
    public long? GetMetadataLineNumber(string name) => _metadataLines.TryGetValue(name.ThrowIfNullOrWhiteSpace(), out var line) ? line : null;

    /// <summary>Initializes exactly one pass, rejecting accidental reuse of mutable context state.</summary>
    internal void Initialize(IEnumerable<string> keys)
    {
        if (_initialized)
            throw new InvalidOperationException("A hierarchy context cannot be reused across passes or invocations.");

        _initialized = true;
        foreach (var key in keys)
            _counts.Add(key, 0);
    }

    /// <summary>Counts a recognized route independently of field validity.</summary>
    internal void Count(string key) => _counts[key] = checked(_counts[key] + 1);

    /// <summary>Stores an encountered metadata value and its physical source line.</summary>
    internal void SetMetadata(string name, object value, long line)
    {
        base.SetMetadata(name, value);
        _metadataLines[name] = line;
    }
}
