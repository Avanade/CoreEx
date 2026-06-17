namespace CoreEx.Data.Querying;

/// <summary>
/// Provides the <see cref="QueryFilterParser"/> <i>null only</i> comparison field configuration.
/// </summary>
public class QueryFilterNullFieldConfig : QueryFilterFieldConfigBase<QueryFilterNullFieldConfig>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="QueryFilterNullFieldConfig"/> class.
    /// </summary>
    /// <param name="parser">The owning <see cref="QueryFilterParser"/>.</param>
    /// <param name="field">The field name.</param>
    /// <param name="model">The model name (defaults to <paramref name="field"/>.</param>
    public QueryFilterNullFieldConfig(QueryFilterParser parser, string field, string? model) : base(parser, typeof(object), field, model)
    {
        Operators = QueryFilterOperator.Equal | QueryFilterOperator.NotEqual;
        IsNullable = true;
        SchemaType = QueryFilterSchemaType.Object;
        SchemaFormat = null;
    }

    /// <inheritdoc/>
    protected override object ConvertToValue(QueryFilterToken operation, QueryFilterToken field, string filter) => throw new ValidationException("Only null comparisons are supported.");

    /// <summary>
    /// Appends the field configuration to the <paramref name="stringBuilder"/>.
    /// </summary>
    /// <param name="stringBuilder">The <see cref="StringBuilder"/>.</param>
    /// <returns>The <paramref name="stringBuilder"/>.</returns>
    public override StringBuilder AppendToString(StringBuilder stringBuilder)
    {
        stringBuilder.Append(Field);
        stringBuilder.Append(" (Type: <none>");
        stringBuilder.Append(", Null: ").Append(IsNullable ? "true" : "false");
        stringBuilder.Append(", Operators: ");

        AppendOperatorsToString(stringBuilder);

        stringBuilder.Append(')');
        if (!string.IsNullOrEmpty(HelpText))
            stringBuilder.Append(" - ").Append(HelpText);

        return stringBuilder;
    }
}