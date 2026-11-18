namespace CoreEx.IO.Json;

/// <summary>Defines one JSON document's selected business units and disjoint metadata objects.</summary>
/// <typeparam name="TUnit">The natural nested business wire type.</typeparam>
public sealed class JsonFileDefinition<TUnit> where TUnit : class, new()
{
    /// <summary>Initializes the unit selector, mapping, cardinality, and optional metadata selectors.</summary>
    /// <param name="recordType">The logical business record type.</param>
    /// <param name="mapping">The business object's declared field mapping.</param>
    /// <param name="kind">The selected business value kind.</param>
    /// <param name="unitPath">The JSON Pointer selecting the business value.</param>
    /// <param name="cardinality">The required number of business-unit occurrences.</param>
    /// <param name="metadata">The optional disjoint object metadata selectors.</param>
    public JsonFileDefinition(string recordType, JsonUnitMapping<TUnit> mapping, JsonDocumentKind kind = JsonDocumentKind.Array, string unitPath = "",
        FileCardinality? cardinality = null, IEnumerable<JsonFileMetadata>? metadata = null)
    {
        RecordType = recordType.ThrowIfNullOrWhiteSpace();
        Mapping = mapping.ThrowIfNull();
        if (!Enum.IsDefined(kind))
            throw new ArgumentOutOfRangeException(nameof(kind));

        Kind = kind;
        UnitPath = unitPath.ThrowIfNull();
        UnitSegments = JsonPointer.Parse(unitPath);
        Cardinality = cardinality ?? (kind == JsonDocumentKind.Object ? FileCardinality.One : FileCardinality.Many);
        if (!Enum.IsDefined(Cardinality))
            throw new ArgumentOutOfRangeException(nameof(cardinality));

        if (kind == JsonDocumentKind.Object && (UnitPath.Length != 0 || Cardinality != FileCardinality.One))
            throw new ArgumentException("Single-object JSON selects only the root and requires unit cardinality One.", nameof(cardinality));

        Metadata = Array.AsReadOnly((metadata ?? []).ToArray());
        if (Metadata.Any(x => x is null) || Metadata.Select(x => x.Name).Distinct(StringComparer.Ordinal).Count() != Metadata.Count)
            throw new ArgumentException("Metadata declarations must be non-null and have unique names.", nameof(metadata));

        var selectors = Metadata.Select(x => x.Segments).Prepend(UnitSegments).ToArray();
        for (var left = 0; left < selectors.Length; left++)
            for (var right = left + 1; right < selectors.Length; right++)
                if (JsonPointer.Overlaps(selectors[left], selectors[right]))
                    throw new ArgumentException("Unit and metadata selectors must be disjoint and non-overlapping.", nameof(metadata));
    }

    /// <summary>Gets the logical business record type.</summary>
    public string RecordType { get; }

    /// <summary>Gets the business object's declared field mapping.</summary>
    public JsonUnitMapping<TUnit> Mapping { get; }

    /// <summary>Gets the expected kind of the selected business value.</summary>
    public JsonDocumentKind Kind { get; }

    /// <summary>Gets the JSON Pointer selecting the business value.</summary>
    public string UnitPath { get; }

    /// <summary>Gets the decoded unit selector segments.</summary>
    internal string[] UnitSegments { get; }

    /// <summary>Gets the required business-unit occurrence count.</summary>
    public FileCardinality Cardinality { get; }

    /// <summary>Gets the declared metadata selectors.</summary>
    public IReadOnlyList<JsonFileMetadata> Metadata { get; }
}
