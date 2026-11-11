namespace CoreEx.IO.JsonLines;

/// <summary>Declares one explicitly routed, non-executable metadata object.</summary>
public abstract class JsonLinesMetadata
{
    /// <summary>Initializes an exact metadata route.</summary>
    /// <param name="name">The logical metadata accessor key.</param>
    /// <param name="recordType">The logical metadata record type.</param>
    /// <param name="match">The exact discriminator value selecting this route.</param>
    /// <param name="required">Whether exactly one occurrence is required.</param>
    /// <param name="position">The source-order restriction for this route.</param>
    private protected JsonLinesMetadata(string name, string recordType, string match, bool required, FileRecordPosition position)
    {
        Name = name.ThrowIfNullOrWhiteSpace();
        RecordType = recordType.ThrowIfNullOrWhiteSpace();
        Match = match.ThrowIfNullOrWhiteSpace();
        Required = required;
        if (!Enum.IsDefined(position))
            throw new ArgumentOutOfRangeException(nameof(position));

        Position = position;
    }

    /// <summary>Gets the logical context accessor key.</summary>
    public string Name { get; }

    /// <summary>Gets the declared record type.</summary>
    public string RecordType { get; }

    /// <summary>Gets the exact case-sensitive discriminator value.</summary>
    public string Match { get; }

    /// <summary>Gets whether exactly one occurrence is required.</summary>
    public bool Required { get; }

    /// <summary>Gets the source-order restriction.</summary>
    public FileRecordPosition Position { get; }

    /// <summary>Maps and validates a metadata object without a business scope.</summary>
    /// <param name="element">The routed metadata JSON object.</param>
    /// <param name="context">The pass-local mapping and diagnostic context.</param>
    /// <param name="discriminator">The direct structural discriminator member.</param>
    /// <param name="validate">Whether the metadata validator should run.</param>
    /// <param name="cancellationToken">The processing cancellation token.</param>
    /// <returns>The mapped metadata wire object.</returns>
    internal abstract Task<object?> MapAsync(JsonElement element, CoreEx.IO.Json.JsonMappingContext context, string? discriminator, bool validate, CancellationToken cancellationToken);
}
