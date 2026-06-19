namespace CoreEx.Data.Querying;

/// <summary>
/// Provides the base <see cref="QueryFilterParser"/> field configuration extending <see cref="QueryFilterFieldConfigBase"/> with fluent-style method-chaining capabilities.
/// </summary>
/// <typeparam name="TSelf">The self <see cref="Type"/> for support fluent-style method-chaining.</typeparam>
/// <param name="parser">The owning <see cref="QueryFilterParser"/>.</param>
/// <param name="type">The field type.</param>
/// <param name="field">The field name.</param>
/// <param name="model">The model name (defaults to <paramref name="field"/>).</param>
public abstract class QueryFilterFieldConfigBase<TSelf>(QueryFilterParser parser, Type type, string field, string? model) 
    : QueryFilterFieldConfigBase(parser, type, field, model) where TSelf : QueryFilterFieldConfigBase<TSelf>
{
    /// <summary>
    /// Sets (overrides) the optional <see cref="QueryFilterFieldConfigBase.ModelPrefix"/> to be used where referencing the underlying <see cref="IQueryable{T}"/> model.
    /// </summary>
    /// <param name="modelPrefix">The model prefix.</param>
    /// <returns>The <see cref="QueryFilterParser"/> to support fluent-style method-chaining.</returns>
    public TSelf WithModelPrefix(string? modelPrefix = null)
    {
        ModelPrefix = modelPrefix;
        return (TSelf)this;
    }

    /// <summary>
    /// Indicates that the field can be <see langword="null"/>.
    /// </summary>
    /// <returns>The <typeparamref name="TSelf"/> to support fluent-style method-chaining.</returns>
    /// <remarks>Sets the <see cref="QueryFilterFieldConfigBase.IsNullable"/> to <see langword="true"/>.</remarks>
    public TSelf AsNullable()
    {
        IsNullable = true;
        return (TSelf)this;
    }

    /// <summary>
    /// Indicates that a not-<see langword="null"/> check should also be performed before a comparion occurs.
    /// </summary>
    /// <returns>The <typeparamref name="TSelf"/> to support fluent-style method-chaining.</returns>
    /// <remarks>Sets the <see cref="QueryFilterFieldConfigBase.IsCheckForNotNull"/> and <see cref="QueryFilterFieldConfigBase.IsNullable"/> to <see langword="true"/>.</remarks>
    public TSelf AlsoCheckNotNull()
    {
        IsCheckForNotNull = true;
        IsNullable = true;
        return (TSelf)this;
    }

    /// <summary>
    /// Sets (overrides) the default LINQ statement to be used where no filtering is specified.
    /// </summary>
    /// <param name="statement">The LINQ <see cref="QueryStatement"/>.</param>
    /// <returns>The <typeparamref name="TSelf"/> to support fluent-style method-chaining.</returns>
    /// <remarks>To avoid unnecessary parsing this should be specified as a valid dynamic LINQ statement.
    /// <para>This must be the required expression <b>only</b>. It will be appended as an <i>and</i> to the final LINQ statement.</para></remarks>
    public TSelf WithDefault(QueryStatement? statement) => WithDefault(statement is null ? null : () => statement);

    /// <summary>
    /// Sets (overrides) the default LINQ statement function to be used where no filtering is specified.
    /// </summary>
    /// <param name="statement">The LINQ <see cref="QueryStatement"/>.</param>
    /// <returns>The <typeparamref name="TSelf"/> to support fluent-style method-chaining.</returns>
    /// <remarks>To avoid unnecessary parsing this should be specified as a valid dynamic LINQ statement.
    /// <para>This must be the required expression <b>only</b>. It will be appended as an <i>and</i> to the final LINQ statement.</para></remarks>
    public TSelf WithDefault(Func<QueryStatement>? statement)
    {
        DefaultStatement = statement;
        return (TSelf)this;
    }

    /// <summary>
    /// Sets (overrides) the function that will be used to write the <see cref="IQueryFilterFieldStatementExpression"/> dynamic LINQ statement.
    /// </summary>
    /// <param name="resultWriter">The <see cref="QueryFilterFieldResultWriter"/>.</param>
    /// <returns>The <typeparamref name="TSelf"/> to support fluent-style method-chaining.</returns>
    public TSelf WithResultWriter(QueryFilterFieldResultWriter? resultWriter)
    {
        ResultWriter = resultWriter;
        return (TSelf)this;
    }

    /// <summary>
    /// Sets (overrides) the <see cref="IQueryFilterFieldConfig.SchemaType"/>.
    /// </summary>
    /// <param name="schemaType">The <see cref="QueryFilterSchemaType"/>.</param>
    /// <returns>The <typeparamref name="TSelf"/> to support fluent-style method-chaining.</returns>
    public TSelf WithSchemaType(QueryFilterSchemaType schemaType)
    {
        SchemaType = schemaType;
        return (TSelf)this;
    }

    /// <summary>
    /// Sets (overrides) the <see cref="IQueryFilterFieldConfig.SchemaFormat"/>.
    /// </summary>
    /// <param name="schemaFormat">The schema format.</param>
    /// <returns>The <typeparamref name="TSelf"/> to support fluent-style method-chaining.</returns>
    public TSelf WithSchemaFormat(string? schemaFormat)
    {
        SchemaFormat = schemaFormat;
        return (TSelf)this; 
    }

    /// <summary>
    /// Sets (overrides) any additional help text.
    /// </summary>
    /// <param name="text">The additional help text.</param>
    /// <returns>The <typeparamref name="TSelf"/> to support fluent-style method-chaining.</returns>
    public TSelf WithHelpText(string text)
    {
        HelpText = text.ThrowIfNullOrEmpty();
        return (TSelf)this;
    }
}