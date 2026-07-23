namespace CoreEx.Data.Querying;

/// <summary>
/// Represents the base <see cref="QueryFilterParser"/> field configuration.
/// </summary>
public interface IQueryFilterFieldConfig
{
    /// <summary>
    /// Gets the owning <see cref="QueryFilterParser"/>.
    /// </summary>
    QueryFilterParser Parser { get; }

    /// <summary>
    /// Gets the field type.
    /// </summary>
    Type Type { get; }

    /// <summary>
    /// Gets the <see cref="QueryFilterFieldType"/>.
    /// </summary>
    QueryFilterFieldType FieldType { get; }

    /// <summary>
    /// Gets the field name.
    /// </summary>
    string Field { get; }

    /// <summary>
    /// Gets the model name to be used for the dynamic LINQ expression.
    /// </summary>
    /// <remarks>Defaults to the <see cref="Field"/> name.</remarks>
    string Model { get; }

    /// <summary>
    /// Gets the optional prefix to be used where referencing the underlying <see cref="IQueryable{T}"/> model, specifically where a field is being projected.
    /// </summary>
    /// <remarks>This will default from <see cref="QueryFilterParser.DefaultModelPrefix"/> when instantiated.</remarks>
    string? ModelPrefix { get; }

    /// <summary>
    /// Gets the fully-qualified <see cref="Model"/> name (including any <see cref="ModelPrefix"/> where specified).
    /// </summary>
    string FullyQualifiedModelName { get; }

    /// <summary>
    /// Gets the supported kinds.
    /// </summary>
    QueryFilterOperator Operators { get; }

    /// <summary>
    /// Indicates whether the comparison should ignore case or not; will use <see cref="string.ToUpper()"/> (where <see langword="true"/>) or <see cref="string.ToLower()"/> (where <see langword="false"/>) when selected for comparisons.
    /// </summary>
    bool? IsToUpper { get; }

    /// <summary>
    /// Indicates whether the field can be <see langword="null"/> or not.
    /// </summary>
    bool IsNullable { get; }

    /// <summary>
    /// Indicates whether a not-<see langword="null"/> check should also be performed before the comparion occurs.
    /// </summary>
    bool IsCheckForNotNull { get; }

    /// <summary>
    /// Gets the default LINQ <see cref="QueryStatement"/> function to be used where no filtering is specified.
    /// </summary>
    Func<QueryStatement>? DefaultStatement { get; }

    /// <summary>
    /// Gets the <see cref="QueryFilterSchemaType"/>.
    /// </summary>
    QueryFilterSchemaType SchemaType { get; }

    /// <summary>
    /// Gets the corresponding format for the <see cref="SchemaType"/> (where applicable).
    /// </summary>
    /// <remarks></remarks>
    string? SchemaFormat { get; }

    /// <summary>
    /// Gets the additional help text.
    /// </summary>
    string? HelpText { get; }

    /// <summary>
    /// Converts <paramref name="field"/> to the underlying type.
    /// </summary>
    /// <param name="operation">The operation <see cref="QueryFilterTokenKind"/> being performed on the <paramref name="field"/>.</param>
    /// <param name="field">The field <see cref="QueryFilterToken"/>.</param>
    /// <param name="filter">The query filter.</param>
    /// <returns>The converted value.</returns>
    object? ConvertToValue(QueryFilterToken operation, QueryFilterToken field, string filter);

    /// <summary>
    /// Validate the <paramref name="constant"/> token against the field configuration.
    /// </summary>
    /// <param name="field">The field <see cref="QueryFilterToken"/>.</param>
    /// <param name="constant">The constant <see cref="QueryFilterToken"/>.</param>
    /// <param name="filter">The query filter.</param>
    void ValidateConstant(QueryFilterToken field, QueryFilterToken constant, string filter);

    /// <summary>
    /// Gets the <see cref="QueryFilterFieldResultWriter"/>.
    /// </summary>
    QueryFilterFieldResultWriter? ResultWriter { get; }

    /// <summary>
    /// Appends the field configuration to the <paramref name="stringBuilder"/>.
    /// </summary>
    /// <param name="stringBuilder">The <see cref="StringBuilder"/>.</param>
    /// <returns>The <paramref name="stringBuilder"/>.</returns>
    StringBuilder AppendToString(StringBuilder stringBuilder);

    /// <summary>
    /// Returns a dictionary representation of the schema configuration.
    /// </summary>
    IDictionary<string, object?> ToSchemaDictionary();
}
