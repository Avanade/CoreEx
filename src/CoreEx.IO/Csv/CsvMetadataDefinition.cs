namespace CoreEx.IO.Csv;

/// <summary>Declares one file-level CSV metadata route.</summary>
/// <param name="Name">The logical metadata accessor key.</param>
/// <param name="RecordKey">The configured record route key.</param>
/// <param name="Required">Whether exactly one occurrence is required.</param>
/// <param name="Position">The source-order restriction for this route.</param>
public sealed record CsvMetadataDefinition(string Name, string RecordKey, bool Required = false, FileRecordPosition Position = FileRecordPosition.Any)
{
    /// <summary>Validates a metadata declaration.</summary>
    /// <returns>This validated declaration.</returns>
    internal CsvMetadataDefinition Validate()
    {
        Name.ThrowIfNullOrWhiteSpace();
        RecordKey.ThrowIfNullOrWhiteSpace();
        if (!Enum.IsDefined(Position))
            throw new ArgumentOutOfRangeException(nameof(Position));

        return this;
    }
}
