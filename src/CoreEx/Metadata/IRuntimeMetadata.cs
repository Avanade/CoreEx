namespace CoreEx.Metadata;

/// <summary>
/// Enables access to the <see cref="IPropertyRuntimeMetadata"/> for each property via the static <see cref="GetStaticPropertyRuntimeMetadata"/>.
/// </summary>
/// <remarks>See also <see cref="IRuntimeMetadataCore"/>.</remarks>
public interface IRuntimeMetadata : IRuntimeMetadataCore
{
    /// <summary>
    /// Gets the <see cref="IPropertyRuntimeMetadata"/> for each property.
    /// </summary>
    static abstract IEnumerable<IPropertyRuntimeMetadata> GetStaticPropertyRuntimeMetadata();
}