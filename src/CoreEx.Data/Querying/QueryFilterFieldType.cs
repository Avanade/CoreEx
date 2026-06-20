namespace CoreEx.Data.Querying;

/// <summary>
/// Represents the field type for a <see cref="QueryFilterFieldConfigBase"/>.
/// </summary>
public enum QueryFilterFieldType
{
    /// <summary>
    /// Indicates a <see cref="string"/>.
    /// </summary>
    String,

    /// <summary>
    /// Indicates a <see cref="Boolean"/>.
    /// </summary>
    Boolean,

    /// <summary>
    /// Indicates a <see cref="System.Enum"/>.
    /// </summary>
    Enum,

    /// <summary>
    /// Indicates another type.
    /// </summary>
    Other
}