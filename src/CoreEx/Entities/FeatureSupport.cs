namespace CoreEx.Entities;

/// <summary>
/// Represents the feature support.
/// </summary>
public enum FeatureSupport
{
    /// <summary>
    /// Indicates that the feature is not supported.
    /// </summary>
    NotSupported,

    /// <summary>
    /// Indicates that the feature is partially supported; i.e. is read-only.
    /// </summary>
    ReadOnly,

    /// <summary>
    /// Indicates that the feature is fully supported; i.e. is mutable.
    /// </summary>
    Mutable
}