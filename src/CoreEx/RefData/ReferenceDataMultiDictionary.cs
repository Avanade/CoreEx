namespace CoreEx.RefData;

/// <summary>
/// Represents a dictionary where the key is the <see cref="IReferenceData"/> <see cref="Type"/> and the value is the corresponding <see cref="IReferenceData"/> items.
/// </summary>
/// <remarks>This is intended for the <see cref="ReferenceDataOrchestrator"/> to enable the <see cref="ReferenceDataOrchestrator.GetNamedAsync"/> to enable correct serialization of <see cref="IReferenceData"/> types.
/// <para>To enable the <see cref="JsonReferenceDataConverter"/> is used; however, note this <i>only</i> supports serialization and not deserialization.</para></remarks>
public class ReferenceDataMultiDictionary : Dictionary<string, IEnumerable<IReferenceData>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReferenceDataMultiDictionary"/> class.
    /// </summary>
    public ReferenceDataMultiDictionary() : base(StringComparer.OrdinalIgnoreCase) { }
}