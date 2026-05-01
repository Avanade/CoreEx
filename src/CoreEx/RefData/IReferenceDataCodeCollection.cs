namespace CoreEx.RefData;

/// <summary>
/// Enables a special purpose <see cref="IReferenceData"/> collection specifically for managing a referenced list of <i>serialization identifiers</i>, being the underlying <see cref="IReferenceData.Code"/>.
/// </summary>
public interface IReferenceDataCodeCollection
{
    /// <summary>
    /// Indicates whether the collection contains invalid items (i.e. not <see cref="IReferenceData.IsValid"/>).
    /// </summary>
    /// <returns><see langword="true"/> indicates that invalid items exist; otherwise, <see langword="false"/>.</returns>
    bool HasInvalidItems { get; }

    /// <summary>
    /// Indicates whether the collection contains inactive items (i.e. <see cref="IReferenceData.IsInactive"/>).
    /// </summary>
    /// <returns><see langword="true"/> indicates that inactive items exist; otherwise, <see langword="false"/>.</returns>
    bool HasInactiveItems { get; }

    /// <summary>
    /// Gets the number of items in the underlying collection.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Gets the underlying <see cref="IReferenceData"/> entries.
    /// </summary>
    /// <returns>The underlying <see cref="IReferenceData"/> entries.</returns>
    IEnumerable<IReferenceData> ToRefDataList();

    /// <summary>
    /// Gets the underlying <see cref="IReferenceData.Code"/> collection.
    /// </summary>
    /// <returns>The underlying <see cref="IReferenceData.Code"/> entries as a <see cref="List{T}"/>.</returns>
    List<string?> ToCodeList();
}