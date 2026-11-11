namespace CoreEx.IO;

/// <summary>Provides location metadata for one fully validated processing unit.</summary>
public sealed class FileUnitInfo
{
    /// <summary>Initializes provenance for one candidate business unit.</summary>
    /// <param name="ordinal">The one-based encounter ordinal, including invalid candidates.</param>
    /// <param name="location">The physical or logical location of the unit.</param>
    /// <param name="recordType">The logical input record or root type.</param>
    /// <param name="sourceByteOffset">The zero-based source byte offset when the reader can report it accurately.</param>
    internal FileUnitInfo(long ordinal, FileLocation location, string? recordType = null, long? sourceByteOffset = null)
    {
        ordinal.ThrowIfLessThanOrEqualToZero();
        Location = location.ThrowIfNull();
        if (recordType is not null)
            recordType.ThrowIfNullOrWhiteSpace();

        if (sourceByteOffset is < 0)
            throw new ArgumentOutOfRangeException(nameof(sourceByteOffset));

        Ordinal = ordinal;
        RecordType = recordType;
        SourceByteOffset = sourceByteOffset;
    }

    /// <summary>Gets the one-based encounter ordinal.</summary>
    public long Ordinal { get; }

    /// <summary>Gets the physical or logical location of the unit.</summary>
    public FileLocation Location { get; }

    /// <summary>Gets the route or root-record key, when applicable.</summary>
    public string? RecordType { get; }

    /// <summary>Gets the zero-based source byte offset, when accurately available.</summary>
    public long? SourceByteOffset { get; }

    /// <summary>Gets the built-in mapper's resolver for pure-validation model paths.</summary>
    internal Func<string?, FileLocation>? ValidationLocation { get; init; }
}
