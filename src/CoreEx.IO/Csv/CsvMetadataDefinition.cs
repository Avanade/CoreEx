namespace CoreEx.IO.Csv;

/// <summary>Declares one file-level CSV metadata route.</summary>
public sealed record CsvMetadataDefinition(string Name, string RecordKey, bool Required = false, CsvRecordPosition Position = CsvRecordPosition.Any)
{
    /// <summary>Validates a metadata declaration.</summary>
    public CsvMetadataDefinition Validate()
    {
        Name.ThrowIfNullOrWhiteSpace();
        RecordKey.ThrowIfNullOrWhiteSpace();
        if (!Enum.IsDefined(Position))
            throw new ArgumentOutOfRangeException(nameof(Position));

        return this;
    }
}
