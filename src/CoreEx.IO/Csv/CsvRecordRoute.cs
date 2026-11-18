namespace CoreEx.IO.Csv;

/// <summary>Defines exact discriminator matching and record-specific mapping and validation.</summary>
public abstract class CsvRecordRoute
{
    /// <summary>Initializes immutable routing metadata.</summary>
    /// <param name="key">The logical key, unique ignoring case.</param>
    /// <param name="match">The exact case-sensitive discriminator.</param>
    private protected CsvRecordRoute(string key, string match)
    {
        Key = key.ThrowIfNullOrWhiteSpace();
        Match = match.ThrowIfNullOrWhiteSpace();
    }

    /// <summary>Gets the logical record key.</summary>
    public string Key { get; }

    /// <summary>Gets the exact discriminator value; no case-folding or trimming is applied.</summary>
    public string Match { get; }

    /// <summary>Creates a fresh positional mapper for one pass.</summary>
    /// <returns>The mapper for the current pass.</returns>
    internal abstract Func<CsvReader, (object? Value, IReadOnlyList<FileDiagnostic> Diagnostics)> CreateMapping();

    /// <summary>Validates an accepted wire record using non-throwing CoreEx messages.</summary>
    /// <param name="record">The mapped wire record.</param>
    /// <param name="line">The physical starting line.</param>
    /// <param name="reportAsync">The awaited diagnostic sink.</param>
    /// <param name="cancellationToken">The processing cancellation token.</param>
    /// <returns>The asynchronous validation operation.</returns>
    internal abstract Task ValidateAsync(object record, long line, Func<FileDiagnostic, CancellationToken, Task> reportAsync, CancellationToken cancellationToken);
}
