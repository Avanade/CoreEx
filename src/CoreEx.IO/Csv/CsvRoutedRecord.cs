namespace CoreEx.IO.Csv;

/// <summary>Defines an exact unordered route and its file-level occurrence and position.</summary>
/// <typeparam name="TUnit">The common business-unit type produced by typed route projections.</typeparam>
public abstract class CsvRoutedRecord<TUnit> where TUnit : class
{
    /// <summary>Initializes immutable route metadata.</summary>
    /// <param name="key">The logical record key.</param>
    /// <param name="match">The exact discriminator value.</param>
    /// <param name="cardinality">The file-level occurrence restriction.</param>
    /// <param name="position">The optional file-boundary restriction.</param>
    private protected CsvRoutedRecord(string key, string match, FileCardinality cardinality, FileRecordPosition position)
    {
        Key = key.ThrowIfNullOrWhiteSpace();
        Match = match.ThrowIfNullOrWhiteSpace();
        if (!Enum.IsDefined(cardinality))
            throw new ArgumentOutOfRangeException(nameof(cardinality));

        if (!Enum.IsDefined(position))
            throw new ArgumentOutOfRangeException(nameof(position));

        if (position != FileRecordPosition.Any && cardinality is not (FileCardinality.One or FileCardinality.Optional))
            throw new ArgumentException("First and Last positions require One or Optional cardinality.", nameof(cardinality));

        Cardinality = cardinality;
        Position = position;
    }

    /// <summary>Gets the logical key, unique ignoring case within a definition.</summary>
    public string Key { get; }

    /// <summary>Gets the exact case-sensitive discriminator, without trimming.</summary>
    public string Match { get; }

    /// <summary>Gets the recognized occurrences allowed per file.</summary>
    public FileCardinality Cardinality { get; }

    /// <summary>Gets the optional file-boundary restriction.</summary>
    public FileRecordPosition Position { get; }

    /// <summary>Creates a fresh positional mapper and typed validation dispatch for one pass.</summary>
    /// <returns>The mapper and validation dispatcher for the current pass.</returns>
    internal abstract Func<CsvReader, CsvRoutedInput<TUnit>> CreateMapping();
}
