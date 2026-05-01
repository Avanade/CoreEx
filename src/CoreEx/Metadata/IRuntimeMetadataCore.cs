namespace CoreEx.Metadata;

/// <summary>
/// Enables access to the <see cref="IPropertyRuntimeMetadata"/> for each property.
/// </summary>
public interface IRuntimeMetadataCore
{
    /// <summary>
    /// Gets the <see cref="IPropertyRuntimeMetadata"/> for each property.
    /// </summary>
    IEnumerable<IPropertyRuntimeMetadata> GetPropertyRuntimeMetadata();
}