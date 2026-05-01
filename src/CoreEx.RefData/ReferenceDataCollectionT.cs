namespace CoreEx.RefData;

/// <summary>
/// Represents a generic <see cref="IReferenceDataCollection{TId, TRef}"/> implementation where the <see cref="IReferenceData.Id"/> <see cref="Type"/> is a <see cref="string"/>.
/// </summary>
/// <typeparam name="TRef">The <see cref="IReferenceData{TId}"/> <see cref="Type"/>.</typeparam>
/// <remarks>This class leverages dictionaries internally to manage the items and as such there is no implied order when using the likes of the <see cref="IEnumerator"/>; use 
/// <see cref="ReferenceDataCollectionCore{TId, TRef}.GetItems(CoreEx.RefData.ReferenceDataSortOrder?, bool?, bool?)"/> to achieve desired ordering where applicable.</remarks>
public class ReferenceDataCollection<TRef> : ReferenceDataCollectionCore<string, TRef>, ICollection<TRef> where TRef : class, IReferenceData<string>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReferenceDataCollection{TItem, TId}"/> class.
    /// </summary>
    /// <param name="sortOrder">The <see cref="ReferenceDataSortOrder"/>. Defaults to <see cref="ReferenceDataSortOrder.SortOrder"/>.</param>
    /// <param name="codeComparer">The <see cref="StringComparer"/> for <see cref="IReferenceData.Code"/> comparisons. Defaults to <see cref="StringComparer.OrdinalIgnoreCase"/>.</param>
    public ReferenceDataCollection(ReferenceDataSortOrder sortOrder = ReferenceDataSortOrder.SortOrder, StringComparer? codeComparer = null) : base(sortOrder, codeComparer) => OnInitialization();

    /// <summary>
    /// Provides an opportunity to extend initialization when the object is constructed.
    /// </summary>
    protected virtual void OnInitialization() { }
}