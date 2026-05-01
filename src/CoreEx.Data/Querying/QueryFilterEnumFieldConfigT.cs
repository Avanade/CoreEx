
namespace CoreEx.Data.Querying;

/// <summary>
/// Provides the <see cref="QueryFilterParser"/> <see cref="Enum"/> field configuration.
/// </summary>
/// <typeparam name="T">The field type.</typeparam>
/// <param name="parser">The owning <see cref="QueryFilterParser"/>.</param>
/// <param name="field">The field name.</param>
/// <param name="model">The model name (defaults to <paramref name="field"/>).</param>
public class QueryFilterEnumFieldConfig<T>(QueryFilterParser parser, string field, string? model) 
    : QueryFilterFieldConfigBase<QueryFilterEnumFieldConfig<T>>(parser, typeof(T), field, model) where T : notnull, Enum
{
    private Func<string, T>? _converterFunc;
    private Func<T, object>? _valueFunc;

    /// <summary>
    /// Sets (overrides) the operator <see cref="QueryFilterFieldConfigBase.Operators"/>.
    /// </summary>
    /// <param name="operators">The supported <see cref="QueryFilterOperator"/>(s).</param>
    /// <returns>The <see cref="QueryFilterEnumFieldConfig{T}"/> to support fluent-style method-chaining.</returns>
    /// <remarks>Defaults to the <see cref="QueryFilterOperator.EqualityOperators"/>.</remarks>
    public QueryFilterEnumFieldConfig<T> WithOperators(QueryFilterOperator operators)
    {
        Operators = operators;
        return this;
    }

    /// <summary>
    /// Sets (overrides) the <paramref name="converter"/> to convert the field value from a <see cref="string"/> to the field type <typeparamref name="T"/>.
    /// </summary>
    /// <param name="converter">The converter function.</param>
    /// <returns>The <see cref="QueryFilterEnumFieldConfig{T}"/> to support fluent-style method-chaining.</returns>
    /// <remarks>The <paramref name="converter"/> is invoked before the <see cref="WithValue(Func{T, object}?)"/> as the resulting value is passed through to enable further conversion and/or validation where applicable.</remarks>
    public QueryFilterEnumFieldConfig<T> WithConverter(Func<string, T>? converter)
    {
        _converterFunc = converter;
        return this;
    }

    /// <summary>
    /// Sets (overrides) the <paramref name="value"/> function to, a) further convert the field <typeparamref name="T"/> value to the final <see cref="object"/> value that will be used in the LINQ query; and/or, b) to provide additional validation.
    /// </summary>
    /// <param name="value">The value function.</param>
    /// <returns>The final <see cref="object"/> value that will be used in the LINQ query.</returns>
    /// <remarks>This is an opportunity to further validate the query as needed. Throw a <see cref="FormatException"/> to have the exception message formatted correctly and consistently.
    /// <para>This in invoked after the <see cref="WithConverter(Func{string, T}?)"/> has been invoked.</para></remarks>
    public QueryFilterEnumFieldConfig<T> WithValue(Func<T, object>? value)
    {
        _valueFunc = value;
        return this;
    }

    /// <inheritdoc/>
    protected override object ConvertToValue(QueryFilterToken operation, QueryFilterToken field, string filter)
    {
        T value = default!;
        var val = field.GetValueToken(filter);

        if (_converterFunc is null)
            value = val is null ? default! : (T)Enum.Parse(typeof(T), val, true);
        else
            value = _converterFunc(val);

        // Convert the underlying type to the final value.
        return _valueFunc?.Invoke(value) ?? value!;
    }

    /// <inheritdoc/>
    public override IDictionary<string, object?> ToSchemaDictionary()
    {
        var dict = base.ToSchemaDictionary();
        dict["enum"] = Enum.GetNames(typeof(T));
        return dict;
    }
}