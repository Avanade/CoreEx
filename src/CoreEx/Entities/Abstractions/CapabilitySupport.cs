namespace CoreEx.Data.Abstractions;

/// <summary>
/// Represents the capability support..
/// </summary>
public enum CapabilitySupport
{
    /// <summary>
    /// Indicates that the capability is not supported.
    /// </summary>
    None,

    /// <summary>
    /// Indicates that the capability is partially supported; i.e. is read-only.
    /// </summary>
    ReadOnly,

    /// <summary>
    /// Indicates that the capability is fully supported; i.e. is mutable.
    /// </summary>
    Mutable
}