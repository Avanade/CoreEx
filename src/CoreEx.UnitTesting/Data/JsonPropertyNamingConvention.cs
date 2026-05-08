namespace CoreEx.UnitTesting.Data;

/// <summary>
/// Defines the JSON property naming convention used by the <see cref="JsonDataReader"/> when reading/deserializing JSON data.
/// </summary>
public enum JsonPropertyNamingConvention
{
    /// <summary>
    /// Represents the PascalCase naming convention where the first letter of each word is capitalized.
    /// </summary>
    PascalCase,

    /// <summary>
    /// Represents the camelCase naming convention where the first letter of the first word is lowercase and the first letter of each subsequent word is capitalized.
    /// </summary>
    CamelCase,

    /// <summary>
    /// Represents the snake_case naming convention where words are separated by underscores and all letters are lowercase.
    /// </summary>
    SnakeCase,

    /// <summary>
    /// Represents the kebab-case naming convention where words are separated by hyphens and all letters are lowercase.
    /// </summary>
    KebabCase,

    /// <summary>
    /// Represents no known naming convention; i.e. property names will be used as-is without any transformation.
    /// </summary>
    None
}