#pragma warning disable IDE0130 // Namespace does not match folder structure; by design.
namespace UnitTestEx;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public static partial class UnitTestExExtensions
{
    /// <summary>
    /// Extends the reference data collection with additional items for testing purposes.
    /// </summary>
    /// <typeparam name="TRefColl">The <see cref="IReferenceDataCollection"/> <see cref="Type"/>.</typeparam>
    /// <param name="referenceDataCollection">The reference data collection to extend.</param>
    /// <param name="additionalItems">The additional items to add to the collection.</param>
    /// <returns>The extended reference data collection.</returns>
    public static TRefColl ExtendForTesting<TRefColl>(this TRefColl referenceDataCollection, IEnumerable<IReferenceData> additionalItems) where TRefColl : IReferenceDataCollection
    {
        referenceDataCollection.AddRange(additionalItems);
        return referenceDataCollection;
    }
}