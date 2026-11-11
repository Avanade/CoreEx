namespace CoreEx.IO.JsonLines;

/// <summary>Defines the single business object type and optional explicitly routed metadata for one pass.</summary>
/// <typeparam name="TUnit">The natural nested business wire type.</typeparam>
public sealed class JsonLinesDefinition<TUnit> where TUnit : class, new()
{
    /// <summary>Initializes fresh mappings and exact, disjoint route declarations.</summary>
    /// <param name="recordType">The logical business record type.</param>
    /// <param name="mapping">The business object's declared field mapping.</param>
    /// <param name="cardinality">The required number of business-unit occurrences.</param>
    /// <param name="match">The exact business discriminator value, when routing is enabled.</param>
    /// <param name="metadata">The optional disjoint metadata routes.</param>
    public JsonLinesDefinition(string recordType, CoreEx.IO.Json.JsonUnitMapping<TUnit> mapping, FileCardinality cardinality = FileCardinality.Many,
        string? match = null, IEnumerable<JsonLinesMetadata>? metadata = null)
    {
        RecordType = recordType.ThrowIfNullOrWhiteSpace();
        Mapping = mapping.ThrowIfNull();
        if (!Enum.IsDefined(cardinality))
            throw new ArgumentOutOfRangeException(nameof(cardinality));

        Cardinality = cardinality;
        Match = match;
        Metadata = Array.AsReadOnly((metadata ?? []).ToArray());

        if (Metadata.Any(x => x is null) || Metadata.Select(x => x.Name).Distinct(StringComparer.Ordinal).Count() != Metadata.Count
            || Metadata.Select(x => x.Match).Append(match).Distinct(StringComparer.Ordinal).Count() != Metadata.Count + 1
            || Metadata.Count(x => x.Position == FileRecordPosition.First) > 1 || Metadata.Count(x => x.Position == FileRecordPosition.Last) > 1)
            throw new ArgumentException("Metadata names/matches must be unique and disjoint from the business route, with at most one First and Last.", nameof(metadata));
    }

    /// <summary>Gets the logical root type.</summary>
    public string RecordType { get; }

    /// <summary>Gets the business mapping.</summary>
    public CoreEx.IO.Json.JsonUnitMapping<TUnit> Mapping { get; }

    /// <summary>Gets the business-unit occurrence requirement.</summary>
    public FileCardinality Cardinality { get; }

    /// <summary>Gets the exact business discriminator value, when routing is enabled.</summary>
    public string? Match { get; }

    /// <summary>Gets the declared metadata routes.</summary>
    public IReadOnlyList<JsonLinesMetadata> Metadata { get; }
}
