// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Mapping.Converters;
using System;

namespace CoreEx.Data.Querying
{
    /// <summary>
    /// Provides the <see cref="QueryFilterParser"/> field configuration.
    /// </summary>
    /// <typeparam name="T">The field type.</typeparam>
    /// <param name="parser">The owning <see cref="QueryFilterParser"/>.</param>
    /// <param name="field">The field name.</param>
    /// <param name="model">The model name (defaults to <paramref name="field"/>.</param>
    public class QueryFilterFieldConfig<T>(QueryFilterParser parser, string field, string? model) : QueryFilterFieldConfigBase<QueryFilterFieldConfig<T>>(parser, typeof(T), field, model)
    {
        private IConverter<string, T> _converter = StringToTypeConverter<T>.Default;
        private Func<T, object>? _valueFunc;

        /// <summary>
        /// Sets (overrides) the operator <see cref="QueryFilterFieldConfigBase.SupportedKinds"/>.
        /// </summary>
        /// <param name="kinds">The supported <see cref="QueryFilterTokenKind"/> flags.</param>
        /// <returns>The <see cref="QueryFilterFieldConfig{T}"/> to support fluent-style method-chaining.</returns>
        /// <remarks>The default is <see cref="QueryFilterTokenKind.Operator"/>.</remarks>
        public QueryFilterFieldConfig<T> Operators(QueryFilterTokenKind kinds)
        {
            if (((IQueryFilterFieldConfig)this).IsTypeBoolean)
                throw new NotSupportedException($"{nameof(Operators)} is not supported where {nameof(IQueryFilterFieldConfig.IsTypeBoolean)}.");

            SupportedKinds = kinds;
            return this;
        }

        /// <summary>
        /// Indicates that the operation should ignore case by performing an explicit <see cref="string.ToUpper()"/> comparison and value conversion.
        /// </summary>
        /// <returns>The <see cref="QueryFilterFieldConfig{T}"/> to support fluent-style method-chaining.</returns>
        /// <remarks>Sets the <see cref="QueryFilterFieldConfigBase.IsToUpper"/> to <see langword="true"/>.</remarks>
        public QueryFilterFieldConfig<T> UseUpperCase()
        {
            if (!((IQueryFilterFieldConfig)this).IsTypeString)
                throw new ArgumentException($"A {nameof(UseUpperCase)} can only be specified where the field type is a string.");

            IsToUpper = true;
            return this;
        }

        /// <summary>
        /// Sets (overrides) the <paramref name="converter"/> to convert the field value from a <see cref="string"/> to the field type <typeparamref name="T"/>.
        /// </summary>
        /// <param name="converter">The <see cref="IConverter{TSource, TDestination}"/>.</param>
        /// <returns>The <see cref="QueryFilterFieldConfig{T}"/> to support fluent-style method-chaining.</returns>
        /// <remarks>The <paramref name="converter"/> is invoked before the <see cref="WithValue(Func{T, object}?)"/> as the resulting value is passed through to enable further conversion and/or validation where applicable.</remarks>
        public QueryFilterFieldConfig<T> WithConverter(IConverter<string, T> converter)
        {
            _converter = converter.ThrowIfNull(nameof(converter));
            return this;
        }

        /// <summary>
        /// Sets (overrides) the <paramref name="value"/> function to, a) further convert the field <typeparamref name="T"/> value to the final <see cref="object"/> value that will be used in the LINQ query; and/or, b) to provide additional validation.
        /// </summary>
        /// <param name="value">The value function.</param>
        /// <returns>The final <see cref="object"/> value that will be used in the LINQ query.</returns>
        /// <remarks>This is an opportunity to further validate the query as needed. Throw a <see cref="FormatException"/> to have the validation message formatted correctly and consistently.
        /// <para>This in invoked after the <see cref="WithConverter(IConverter{string, T})"/> has been invoked.</para></remarks>
        public QueryFilterFieldConfig<T> WithValue(Func<T, object>? value)
        {
            _valueFunc = value;
            return this;
        }

        /// <inheritdoc/>
        protected override object ConvertToValue(QueryFilterToken operation, QueryFilterToken field, string filter)
        {
            // Convert from string to the underlying type and consider the upper case requirements.
            T value = _converter.ConvertToDestination(field.GetValueToken(filter));
            if (typeof(T) == typeof(string))
            {
                var str = value?.ToString();
                if (str is null)
                    return null!;

                if (IsToUpper)
                    str = str?.ToUpper(System.Globalization.CultureInfo.CurrentCulture);
                
                value = _converter.ConvertToDestination(str!);
                return _valueFunc?.Invoke(value) ?? value!;
            }

            // Convert the underlying type to the final value.
            return _valueFunc?.Invoke(value) ?? value!;
        }
    }
}