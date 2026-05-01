namespace CoreEx.Data.Querying;

/// <summary>
/// Represents the schema type for a <see cref="QueryFilterFieldType"/>.
/// </summary>
/// <remarks>This is used to define the schema data type of the field in the query filter similar to the <see href="https://swagger.io/docs/specification/v3_0/data-models/data-types/">OpenAPI specification</see>.</remarks>
public enum QueryFilterSchemaType
{
    /// <summary>
    /// Indicates a <see cref="string"/>.
    /// </summary>
    String,

    /// <summary>
    /// Indicates a <see cref="double"/> or <see cref="decimal"/>.
    /// </summary>
    Number,
    
    /// <summary>
    /// Indicates a <see cref="int"/> or <see cref="long"/>.
    /// </summary>
    Integer,

    /// <summary>
    /// Indicates a <see cref="bool"/>.
    /// </summary>
    Boolean,

    /// <summary>
    /// Indicates an object; as distinct from primitive types.
    /// </summary>
    /// <remarks>This is a special case specifically for <see cref="QueryFilterNullFieldConfig"/>.</remarks>
    Object
}