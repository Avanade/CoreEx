namespace CoreEx.RefData;

/// <summary>
/// Provides the sort order for the reference data.
/// </summary>
public enum ReferenceDataSortOrder
{
    /// <summary>
    /// Ordered by <see cref="IReferenceData.SortOrder"/>, then <see cref="IReferenceData.Text"/>, and finally <see cref="IReferenceData.Code"/> (default).
    /// </summary>
    SortOrder,

    /// <summary>
    /// Ordered by <see cref="IReferenceData"/> <see cref="IReferenceData.Id"/>.
    /// </summary>
    Id,

    /// <summary>
    /// Ordered by <see cref="IReferenceData.Code"/>.
    /// </summary>
    Code,

    /// <summary>
    /// Ordered by <see cref="IReferenceData.Text"/> and then <see cref="IReferenceData.Code"/>.
    /// </summary>
    Text
}