namespace CoreEx.IO.Csv;

/// <summary>Declares unordered heterogeneous records, each an independent business unit.</summary>
/// <typeparam name="TUnit">The common unit type used by the scoped handler.</typeparam>
public sealed class CsvRoutedDefinition<TUnit> where TUnit : class
{
    /// <summary>Initializes a validated exact route table with optional file-boundary records.</summary>
    /// <param name="discriminatorIndex">The zero-based discriminator field index.</param>
    /// <param name="records">Typed record routes; at most one First and one Last route.</param>
    public CsvRoutedDefinition(int discriminatorIndex, IEnumerable<CsvRoutedRecord<TUnit>> records)
    {
        DiscriminatorIndex = discriminatorIndex.ThrowIfLessThanZero();
        var list = records.ThrowIfNull().ToArray();
        if (list.Length == 0)
            throw new ArgumentException("At least one route is required.", nameof(records));

        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var matches = new HashSet<string>(StringComparer.Ordinal);
        foreach (var record in list)
        {
            record.ThrowIfNull();
            if (!keys.Add(record.Key) || !matches.Add(record.Match))
                throw new ArgumentException("Route keys must be unique ignoring case and discriminator matches must be exactly unique.", nameof(records));
        }

        if (list.Count(x => x.Position == CsvRecordPosition.First) > 1 || list.Count(x => x.Position == CsvRecordPosition.Last) > 1)
            throw new ArgumentException("Only one First and one Last route may be configured.", nameof(records));

        Records = Array.AsReadOnly(list);
    }

    /// <summary>Gets the zero-based discriminator index.</summary>
    public int DiscriminatorIndex { get; }

    /// <summary>Gets the validated immutable route table.</summary>
    public IReadOnlyList<CsvRoutedRecord<TUnit>> Records { get; }
}
