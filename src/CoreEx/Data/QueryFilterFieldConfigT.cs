// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Mapping.Converters;
using System;

namespace CoreEx.Data
{
    /// <summary>
    /// Provides the <see cref="QueryFilterParser"/> field configuration.
    /// </summary>
    /// <typeparam name="T">The field type.</typeparam>
    /// <param name="parser">The owning <see cref="QueryFilterParser"/>.</param>
    /// <param name="field">The field name.</param>
    /// <param name="overrideName">The field name override.</param>
    public sealed class QueryFilterFieldConfig<T>(QueryFilterParser parser, string field, string? overrideName) : QueryFilterFieldConfigBase<QueryFilterFieldConfig<T>>(parser, typeof(T), field, overrideName)
    {
        private IConverter<string, T> _converter = StringToTypeConverter<T>.Default;
        private Func<T, object>? _converterFunc;

        /// <summary>
        /// Sets the <paramref name="converter"/> to convert the field value from a <see cref="string"/> to the field type <typeparamref name="T"/>.
        /// </summary>
        /// <param name="converter">The <see cref="IConverter{TSource, TDestination}"/>.</param>
        /// <returns>The <see cref="QueryFilterFieldConfig{T}"/> to support fluent-style method-chaining.</returns>
        /// <remarks>The <paramref name="converter"/> is invoked before the <see cref="WithConverter(Func{T, object}?)"/> as the resultung value is passed through to enable further conversion and/or where applicable.</remarks>
        public QueryFilterFieldConfig<T> WithConverter(IConverter<string, T> converter)
        {
            _converter = converter.ThrowIfNull(nameof(converter));
            return this;
        }

        /// <summary>
        /// Sets the <paramref name="converter"/> to convert the field <typeparamref name="T"/> value to the final <see cref="object"/> value that will be used in the LINQ query.
        /// </summary>
        /// <param name="converter">The conversion query.</param>
        /// <returns>The final <see cref="object"/> value that will be used in the LINQ query.</returns>
        public QueryFilterFieldConfig<T> WithConverter(Func<T, object>? converter)
        {
            _converterFunc = converter;
            return this;
        }

        /// <inheritdoc/>
        protected override object? ConvertToValue(string text)
        {
            // Convert from string to the underlying type and consider the upper case requirements.
            T value = _converter.ConvertToDestination(text);
            if (typeof(T) == typeof(string))
            {
                var str = value?.ToString();
                if (str is null)
                    return null;

                if (IsIgnoreCase)
                    str = str?.ToUpper(System.Globalization.CultureInfo.CurrentCulture);
                
                value = _converter.ConvertToDestination(str!);
                return _converterFunc?.Invoke(value) ?? value;
            }

            // Convert the underlying type to the final value.
            return _converterFunc?.Invoke(value) ?? value;
        }
    }
}