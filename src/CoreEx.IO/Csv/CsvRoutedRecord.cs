namespace CoreEx.IO.Csv;

/// <summary>Defines an exact unordered route and its file-level occurrence, position, and error policy.</summary>
/// <typeparam name="TUnit">The common business-unit type produced by typed route projections.</typeparam>
public abstract class CsvRoutedRecord<TUnit> where TUnit : class
{
    /// <summary>Initializes immutable route metadata.</summary>
    private protected CsvRoutedRecord(string key, string match, CsvRecordCardinality cardinality, CsvRecordPosition position, FileErrorMode? errorMode)
    {
        Key = key.ThrowIfNullOrWhiteSpace();
        Match = match.ThrowIfNullOrWhiteSpace();
        if (!Enum.IsDefined(cardinality))
            throw new ArgumentOutOfRangeException(nameof(cardinality));

        if (!Enum.IsDefined(position))
            throw new ArgumentOutOfRangeException(nameof(position));

        if (errorMode.HasValue && !Enum.IsDefined(errorMode.Value))
            throw new ArgumentOutOfRangeException(nameof(errorMode));

        if (position != CsvRecordPosition.Any && cardinality is not (CsvRecordCardinality.One or CsvRecordCardinality.Optional))
            throw new ArgumentException("First and Last positions require One or Optional cardinality.", nameof(cardinality));

        Cardinality = cardinality;
        Position = position;
        ErrorMode = errorMode;
    }

    /// <summary>Gets the logical key, unique ignoring case within a definition.</summary>
    public string Key { get; }

    /// <summary>Gets the exact case-sensitive discriminator, without trimming.</summary>
    public string Match { get; }

    /// <summary>Gets the recognized occurrences allowed per file.</summary>
    public CsvRecordCardinality Cardinality { get; }

    /// <summary>Gets the optional file-boundary restriction.</summary>
    public CsvRecordPosition Position { get; }

    /// <summary>Gets the override for content failures and structural rules owned by this route.</summary>
    public FileErrorMode? ErrorMode { get; }

    /// <summary>Creates a fresh positional mapper and typed validation dispatch for one pass.</summary>
    internal abstract Func<CsvReader, CsvRoutedInput<TUnit>> CreateMapping();
}
