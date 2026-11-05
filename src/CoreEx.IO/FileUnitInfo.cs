namespace CoreEx.IO;

/// <summary>Provides location metadata for one fully validated processing unit.</summary>
public sealed class FileUnitInfo
{
    /// <summary>Initializes metadata for a flat row or assembled business unit.</summary>
    /// <param name="location">The physical or logical location of the unit.</param>
    /// <param name="recordType">The route or root-record key, when applicable.</param>
    public FileUnitInfo(FileLocation location, string? recordType = null)
    {
        Location = location.ThrowIfNull();
        if (recordType is not null)
            recordType.ThrowIfNullOrWhiteSpace();

        RecordType = recordType;
    }

    /// <summary>Gets the physical or logical location of the unit.</summary>
    public FileLocation Location { get; }

    /// <summary>Gets the route or root-record key, when applicable.</summary>
    public string? RecordType { get; }
}
