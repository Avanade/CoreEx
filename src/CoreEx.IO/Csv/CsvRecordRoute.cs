namespace CoreEx.IO.Csv;

/// <summary>Defines exact discriminator matching and record-specific mapping, validation, and continuation.</summary>
public abstract class CsvRecordRoute
{
    /// <summary>Initializes immutable routing metadata.</summary>
    /// <param name="key">The logical key, unique ignoring case.</param>
    /// <param name="match">The exact case-sensitive discriminator.</param>
    /// <param name="errorMode">The optional record-specific continuation policy.</param>
    private protected CsvRecordRoute(string key, string match, FileErrorMode? errorMode)
    {
        Key = key.ThrowIfNullOrWhiteSpace();
        Match = match.ThrowIfNullOrWhiteSpace();
        if (errorMode.HasValue && !Enum.IsDefined(errorMode.Value))
            throw new ArgumentOutOfRangeException(nameof(errorMode));

        ErrorMode = errorMode;
    }

    /// <summary>Gets the logical record key.</summary>
    public string Key { get; }

    /// <summary>Gets the exact discriminator value; no case-folding or trimming is applied.</summary>
    public string Match { get; }

    /// <summary>Gets the optional override for this record's content/rule failures.</summary>
    public FileErrorMode? ErrorMode { get; }

    /// <summary>Creates a fresh positional mapper for one pass.</summary>
    internal abstract Func<CsvReader, (object? Value, IReadOnlyList<FileDiagnostic> Diagnostics)> CreateMapping();

    /// <summary>Validates an accepted wire record using non-throwing CoreEx messages.</summary>
    internal abstract Task ValidateAsync(object record, long line, Func<FileDiagnostic, CancellationToken, Task> reportAsync, CancellationToken cancellationToken);
}
