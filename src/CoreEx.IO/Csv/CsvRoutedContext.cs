namespace CoreEx.IO.Csv;

/// <summary>Holds pass-local occurrence counts and caller-defined reconciliation state for unordered routed files.</summary>
/// <remarks>No records or diagnostic history are retained. Create a fresh context for every invocation and replay pass.</remarks>
public class CsvRoutedContext : FileProcessingContext
{
    private readonly Dictionary<string, long> _counts = new(StringComparer.Ordinal);
    private bool _initialized;

    /// <summary>Initializes an invocation-local routed context.</summary>
    /// <param name="identity">The immutable definition/source identity.</param>
    /// <param name="mode">The requested mode.</param>
    /// <param name="errorMode">The default record policy.</param>
    public CsvRoutedContext(FileProcessingIdentity identity, FileProcessingMode mode = FileProcessingMode.ValidateOnly, FileErrorMode errorMode = FileErrorMode.FailFast) : base(identity, mode, errorMode) { }

    /// <summary>Gets the last encountered record's physical start, or one for an empty file.</summary>
    public long EndOfFileLineNumber { get; internal set; } = 1;

    /// <summary>Gets whether content errors have occurred in this pass.</summary>
    public bool HasValidationErrors => Progress.ErrorCount > 0;

    /// <summary>Gets a route's recognized occurrences, including records that failed mapping or validation.</summary>
    /// <param name="key">The exact configured logical key.</param>
    /// <returns>The recognized count, not the accepted record count.</returns>
    public long GetRecordCount(string key) => _counts.TryGetValue(key.ThrowIfNullOrWhiteSpace(), out var count) ? count
        : throw new ArgumentException("The record key is not configured in this file.", nameof(key));

    /// <summary>Initializes exactly one pass, rejecting reuse of mutable reconciliation state.</summary>
    internal void Initialize(IEnumerable<string> keys)
    {
        if (_initialized)
            throw new InvalidOperationException("A routed context cannot be reused across passes or invocations.");

        _initialized = true;
        foreach (var key in keys)
            _counts.Add(key, 0);
    }

    /// <summary>Counts a recognized route independently of field validity.</summary>
    internal void Count(string key) => _counts[key] = checked(_counts[key] + 1);
}
