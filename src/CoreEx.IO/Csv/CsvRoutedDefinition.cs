namespace CoreEx.IO.Csv;

/// <summary>Declares unordered heterogeneous records, each an independent business unit.</summary>
/// <typeparam name="TUnit">The common unit type used by the scoped handler.</typeparam>
public sealed class CsvRoutedDefinition<TUnit> where TUnit : class
{
    /// <summary>Initializes a validated exact route table with optional business-unit boundaries and file metadata.</summary>
    /// <param name="discriminatorIndex">The zero-based discriminator field index.</param>
    /// <param name="records">Typed record routes; at most one First and one Last route.</param>
    /// <param name="metadata">Optional metadata declarations referencing routes that are not business units.</param>
    public CsvRoutedDefinition(int discriminatorIndex, IEnumerable<CsvRoutedRecord<TUnit>> records, IEnumerable<CsvMetadataDefinition>? metadata = null)
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

        var exactKeys = list.Select(x => x.Key).ToHashSet(StringComparer.Ordinal);
        var metadataList = (metadata ?? []).Select(x => x.Validate()).ToArray();
        var metadataNames = new HashSet<string>(StringComparer.Ordinal);
        var metadataKeys = new HashSet<string>(StringComparer.Ordinal);

        foreach (var item in metadataList)
        {
            if (!metadataNames.Add(item.Name) || !metadataKeys.Add(item.RecordKey) || !exactKeys.Contains(item.RecordKey))
                throw new ArgumentException("Metadata names and route keys must be unique, and every metadata route must exactly match a route key.", nameof(metadata));

            var route = list.Single(x => x.Key == item.RecordKey);
            if (route.Cardinality != CsvRecordCardinality.Many || route.Position != CsvRecordPosition.Any)
                throw new ArgumentException("Metadata occurrence and position rules belong to the metadata declaration, not its route.", nameof(metadata));
        }

        if (list.Count(x => x.Position == CsvRecordPosition.First && !metadataKeys.Contains(x.Key)) + metadataList.Count(x => x.Position == CsvRecordPosition.First) > 1
            || list.Count(x => x.Position == CsvRecordPosition.Last && !metadataKeys.Contains(x.Key)) + metadataList.Count(x => x.Position == CsvRecordPosition.Last) > 1)
            throw new ArgumentException("Only one First and one Last route may be configured.", nameof(records));

        Records = Array.AsReadOnly(list);
        Metadata = Array.AsReadOnly(metadataList);
    }

    /// <summary>Gets the zero-based discriminator index.</summary>
    public int DiscriminatorIndex { get; }

    /// <summary>Gets the validated immutable route table.</summary>
    public IReadOnlyList<CsvRoutedRecord<TUnit>> Records { get; }

    /// <summary>Gets the declared file-level metadata routes.</summary>
    public IReadOnlyList<CsvMetadataDefinition> Metadata { get; }
}
