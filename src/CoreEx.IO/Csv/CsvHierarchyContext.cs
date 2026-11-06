namespace CoreEx.IO.Csv;

/// <summary>Holds pass-local recognized counts, accepted envelope records, and caller-defined reconciliation state.</summary>
/// <remarks>Only the accepted header/trailer are retained file-wide; root groups and diagnostics are not retained. Derive for typed accessors and accumulators, and override replay-context creation for a derived context.</remarks>
public class CsvHierarchyContext : FileProcessingContext
{
    private readonly Dictionary<string, long> _counts = new(StringComparer.Ordinal);
    private object? _header;
    private object? _trailer;
    private bool _initialized;

    /// <summary>Initializes an invocation-local hierarchical context.</summary>
    /// <param name="identity">The immutable definition/source identity.</param>
    /// <param name="mode">The requested mode.</param>
    /// <param name="errorMode">The default record policy.</param>
    public CsvHierarchyContext(FileProcessingIdentity identity, FileProcessingMode mode = FileProcessingMode.ValidateOnly, FileErrorMode errorMode = FileErrorMode.FailFast) : base(identity, mode, errorMode) { }

    /// <summary>Gets whether a header passed mapping, structure, and field validation.</summary>
    public bool HeaderAvailable => _header is not null;
    /// <summary>Gets whether a trailer passed mapping, structure, and field validation.</summary>
    public bool TrailerAvailable => _trailer is not null;
    /// <summary>Gets the accepted header's physical starting line.</summary>
    public long? HeaderLineNumber { get; private set; }

    /// <summary>Gets the accepted trailer's physical starting line.</summary>
    public long? TrailerLineNumber { get; private set; }

    /// <summary>Gets the last encountered record's physical start, or one for an empty file.</summary>
    public long EndOfFileLineNumber { get; internal set; } = 1;

    /// <summary>Gets whether content errors have occurred in this pass.</summary>
    public bool HasValidationErrors { get; internal set; }

    /// <summary>Gets a recognized route's occurrences, including invalid recognized records.</summary>
    /// <param name="key">The exact configured logical key.</param>
    /// <returns>The recognized count, not the accepted record count.</returns>
    public long GetRecordCount(string key) => _counts.TryGetValue(key.ThrowIfNullOrWhiteSpace(), out var count) ? count
        : throw new ArgumentException("The record key is not configured in this file.", nameof(key));

    /// <summary>Gets an accepted typed header, or null when unavailable; incorrect type requests fail explicitly.</summary>
    /// <typeparam name="THeader">The declared header type.</typeparam>
    /// <returns>The accepted header, when available.</returns>
    public THeader? GetHeader<THeader>() where THeader : class => _header is null ? null : _header as THeader
        ?? throw new InvalidOperationException("The requested header type does not match the definition.");

    /// <summary>Gets an accepted typed trailer, or null when unavailable; incorrect type requests fail explicitly.</summary>
    /// <typeparam name="TTrailer">The declared trailer type.</typeparam>
    /// <returns>The accepted trailer, when available.</returns>
    public TTrailer? GetTrailer<TTrailer>() where TTrailer : class => _trailer is null ? null : _trailer as TTrailer
        ?? throw new InvalidOperationException("The requested trailer type does not match the definition.");

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

    /// <summary>Publishes the first accepted envelope record without replacing it with a duplicate.</summary>
    internal void AcceptEnvelope(bool header, object record, long line)
    {
        if (header && _header is null)
            (_header, HeaderLineNumber) = (record, line);
        else if (!header && _trailer is null)
            (_trailer, TrailerLineNumber) = (record, line);
    }
}
