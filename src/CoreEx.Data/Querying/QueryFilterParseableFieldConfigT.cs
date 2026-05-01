namespace CoreEx.Data.Querying;

/// <summary>
/// Provides the <see cref="QueryFilterParser"/> <see cref="IParsable{TSelf}"/> field configuration.
/// </summary>
/// <typeparam name="T">The field type.</typeparam>
/// <param name="parser">The owning <see cref="QueryFilterParser"/>.</param>
/// <param name="field">The field name.</param>
/// <param name="model">The model name (defaults to <paramref name="field"/>).</param>
public class QueryFilterParseableFieldConfig<T>(QueryFilterParser parser, string field, string? model) 
    : QueryFilterFieldConfigBase<QueryFilterParseableFieldConfig<T>>(parser, typeof(T), field, model) where T : notnull, IParsable<T>
{
    private Func<string, T>? _converterFunc;
    private Func<T, object>? _valueFunc;

    /// <summary>
    /// Sets (overrides) the operator <see cref="QueryFilterFieldConfigBase.Operators"/>.
    /// </summary>
    /// <param name="operators">The supported <see cref="QueryFilterOperator"/>(s).</param>
    /// <returns>The <see cref="QueryFilterParseableFieldConfig{T}"/> to support fluent-style method-chaining.</returns>
    /// <remarks>The defaults are:
    /// <list type="bullet">
    /// <item>Where <see cref="QueryFilterFieldConfigBase.FieldType"/> is <see cref="QueryFilterFieldType.Boolean"/> then defaults to <see cref="QueryFilterOperator.BooleanEqualityOperators"/>.</item>
    /// <item>Where <see cref="QueryFilterFieldConfigBase.FieldType"/> is <see cref="QueryFilterFieldType.String"/> then defaults to <see cref="QueryFilterOperator.ComparisonOperators"/>.</item>
    /// <item>Otherwise, <see cref="QueryFilterFieldConfigBase.FieldType"/> is <see cref="QueryFilterFieldType.Other"/> then defaults to <see cref="QueryFilterOperator.ComparisonOperators"/>.</item>
    /// </list></remarks>
    public QueryFilterParseableFieldConfig<T> WithOperators(QueryFilterOperator operators)
    {
        if (((IQueryFilterFieldConfig)this).FieldType == QueryFilterFieldType.Boolean)
            throw new NotSupportedException($"{nameof(WithOperators)} is not supported where {nameof(QueryFilterFieldType.Boolean)}.");

        Operators = operators;
        return this;
    }

    /// <summary>
    /// Indicates that the operation should ignore case by performing an explicit <see cref="string.ToUpper()"/> comparison and value conversion.
    /// </summary>
    /// <returns>The <see cref="QueryFilterParseableFieldConfig{T}"/> to support fluent-style method-chaining.</returns>
    /// <remarks>Sets the <see cref="QueryFilterFieldConfigBase.IsToUpper"/> to <see langword="true"/>.</remarks>
    public QueryFilterParseableFieldConfig<T> AsUpperCase()
    {
        if (((IQueryFilterFieldConfig)this).FieldType != QueryFilterFieldType.String)
            throw new ArgumentException($"A {nameof(AsUpperCase)} can only be specified where the field type is a string.");

        IsToUpper = true;
        return this;
    }

    /// <summary>
    /// Indicates that the operation should ignore case by performing an explicit <see cref="string.ToLower()"/> comparison and value conversion.
    /// </summary>
    /// <returns>The <see cref="QueryFilterParseableFieldConfig{T}"/> to support fluent-style method-chaining.</returns>
    /// <remarks>Sets the <see cref="QueryFilterFieldConfigBase.IsToUpper"/> to <see langword="false"/>.</remarks>
    public QueryFilterParseableFieldConfig<T> AsLowerCase()
    {
        if (((IQueryFilterFieldConfig)this).FieldType != QueryFilterFieldType.String)
            throw new ArgumentException($"A {nameof(AsLowerCase)} can only be specified where the field type is a string.");

        IsToUpper = false;
        return this;
    }

    /// <summary>
    /// Sets (overrides) the <paramref name="converter"/> to convert the field value from a <see cref="string"/> to the field type <typeparamref name="T"/>.
    /// </summary>
    /// <param name="converter">The converter function.</param>
    /// <returns>The <see cref="QueryFilterParseableFieldConfig{T}"/> to support fluent-style method-chaining.</returns>
    /// <remarks>The <paramref name="converter"/> is invoked before the <see cref="WithValue(Func{T, object}?)"/> as the resulting value is passed through to enable further conversion and/or validation where applicable.</remarks>
    public QueryFilterParseableFieldConfig<T> WithConverter(Func<string, T>? converter)
    {
        _converterFunc = converter;
        return this;
    }

    /// <summary>
    /// Sets (overrides) the <paramref name="value"/> function to, a) further convert the field <typeparamref name="T"/> value to the final <see cref="object"/> value that will be used in the LINQ query; and/or, b) to provide additional validation.
    /// </summary>
    /// <param name="value">The value function.</param>
    /// <returns>The <see cref="QueryFilterParseableFieldConfig{T}"/> to support fluent-style method-chaining.</returns>
    /// <remarks>This is an opportunity to further validate the query as needed. Throw a <see cref="FormatException"/> to have the exception message formatted correctly and consistently.
    /// <para>This in invoked after the <see cref="WithConverter(Func{string, T}?)"/> has been invoked.</para></remarks>
    public QueryFilterParseableFieldConfig<T> WithValue(Func<T, object>? value)
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
            value = val is null ? default! : T.Parse(val, null);
        else
            value = _converterFunc(val);

        // Convert from string to the underlying type and consider the casing requirements.
        if (typeof(T) == typeof(string))
        {
            var str = value?.ToString();
            if (str is null)
                return null!;

            if (IsToUpper.HasValue)
            {
                if (IsToUpper.Value)
                    value = T.Parse(str!.ToUpperInvariant(), null)!;
                else
                    value = T.Parse(str!.ToLowerInvariant(), null)!;
            }
                
            return _valueFunc?.Invoke(value!) ?? value!;
        }

        // Convert the underlying type to the final value.
        return _valueFunc?.Invoke(value) ?? value!;
    }
}