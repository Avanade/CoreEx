namespace CoreEx.IO.JsonLines;

/// <summary>Exposes pass-local recognized route counts without retaining units or diagnostics.</summary>
/// <param name="identity">The immutable definition/source/attempt identity.</param>
/// <param name="mode">The requested processing mode.</param>
/// <param name="errorMode">The default content-error continuation policy.</param>
public class JsonLinesContext(FileProcessingIdentity identity, FileProcessingMode mode = FileProcessingMode.ValidateOnly, FileErrorMode errorMode = FileErrorMode.FailFast) : FileProcessingContext(identity, mode, errorMode)
{
    private readonly Dictionary<string, long> _counts = new(StringComparer.Ordinal);
    private bool _initialized;

    /// <summary>Gets the last non-skipped physical line, or null for empty input.</summary>
    public long? EndOfFileLineNumber { get; internal set; }

    /// <summary>Gets whether content errors have been emitted.</summary>
    public bool HasValidationErrors => Progress.ErrorCount > 0;

    /// <summary>Gets plain candidate counts or recognized route occurrences; unroutable lines have no route count.</summary>
    /// <param name="recordType">The configured logical record type.</param>
    /// <returns>The number of candidates or recognized occurrences for that type.</returns>
    public long GetRecordCount(string recordType) => _counts.TryGetValue(recordType.ThrowIfNullOrWhiteSpace(), out var count)
        ? count : throw new ArgumentException("The record type is not configured in this file.", nameof(recordType));

    /// <summary>Initializes one fresh pass before input is opened.</summary>
    /// <param name="recordTypes">The distinct logical record types counted by this pass.</param>
    internal void Initialize(IEnumerable<string> recordTypes)
    {
        if (_initialized)
            throw new InvalidOperationException("Each JSONL pass requires a fresh context.");

        _initialized = true;
        foreach (var recordType in recordTypes)
            _counts.Add(recordType, 0);
    }

    /// <summary>Counts a recognized root without retaining its object.</summary>
    /// <param name="recordType">The configured logical record type to increment.</param>
    internal void Count(string recordType) => _counts[recordType] = checked(_counts[recordType] + 1);
}
