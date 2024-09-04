// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Mapping.Converters;
using System;

namespace CoreEx.Data
{
    /// <summary>
    /// Provides the base <see cref="QueryFilterParser"/> field configuration.
    /// </summary>
    public abstract class QueryFilterFieldConfigBase : IQueryFilterFieldConfig
    {
        private readonly QueryFilterParser _parser;
        private readonly Type _type;
        private readonly string _field;
        private readonly string? _overrideName;

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryFilterFieldConfigBase{TSelf}"/> class.
        /// </summary>
        /// <param name="parser">The owning <see cref="QueryFilterParser"/>.</param>
        /// <param name="type">The field type.</param>
        /// <param name="field">The field name.</param>
        /// <param name="overrideName">The field name override.</param>
        public QueryFilterFieldConfigBase(QueryFilterParser parser, Type type, string field, string? overrideName)
        {
            _parser = parser.ThrowIfNull(nameof(parser));
            _type = type.ThrowIfNull(nameof(type));
            _field = field.ThrowIfNullOrEmpty(nameof(field));
            _overrideName = overrideName;

            var iface = this as IQueryFilterFieldConfig;
            if (iface.IsTypeBoolean)
                SupportedKinds = QueryFilterTokenKind.Equal | QueryFilterTokenKind.NotEqual;
            else
                SupportedKinds = QueryFilterTokenKind.Operator;
        }

        /// <inheritdoc/>
        QueryFilterParser IQueryFilterFieldConfig.Parser => _parser;

        /// <inheritdoc/>
        Type IQueryFilterFieldConfig.Type => _type;

        /// <inheritdoc/>
        string IQueryFilterFieldConfig.Field => _field;

        /// <inheritdoc/>
        string? IQueryFilterFieldConfig.OverrideName => _overrideName;

        /// <inheritdoc/>
        QueryFilterTokenKind IQueryFilterFieldConfig.SupportedKinds => SupportedKinds;

        /// <summary>
        /// Gets the supported kinds.
        /// </summary>
        /// <remarks>Where <see cref="IQueryFilterFieldConfig.IsTypeBoolean"/> defaults to both <see cref="QueryFilterTokenKind.Equal"/> and <see cref="QueryFilterTokenKind.NotEqual"/>; otherwise, defaults to <see cref="QueryFilterTokenKind.Operator"/>.</remarks>
        protected QueryFilterTokenKind SupportedKinds { get; set; }

        /// <inheritdoc/>
        bool IQueryFilterFieldConfig.IsIgnoreCase => IsIgnoreCase;

        /// <summary>
        /// Indicates whether the comparison should ignore case or not (default); will use <see cref="string.ToUpper()"/> when selected for comparisons.
        /// </summary>
        /// <remarks>This is only applicable where the <see cref="IQueryFilterFieldConfig.IsTypeString"/>.</remarks>
        protected bool IsIgnoreCase { get; set; } = false;

        /// <inheritdoc/>
        bool IQueryFilterFieldConfig.IsCheckForNotNull => IsCheckForNotNull;

        /// <summary>
        /// Indicates whether a not-<see langword="null"/> check should also be performed before the comparion occurs (defaults to <c>false</c>).
        /// </summary>
        protected bool IsCheckForNotNull { get; set; } = false;

        /// <summary>
        /// Converts <paramref name="text"/> to the destination type using the <see cref="Converter"/> and <see cref="IsIgnoreCase"/> configurations where specified.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns>The converted value.</returns>
        object? IQueryFilterFieldConfig.ConvertToValue(string text) => ConvertToValue(text);

        /// <summary>
        /// Converts <paramref name="text"/> to the destination type using the <see cref="Converter"/> and <see cref="IsIgnoreCase"/> configurations where specified.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns>The converted value.</returns>
        protected abstract object? ConvertToValue(string text);

        /// <summary>
        /// Validate the <paramref name="constant"/> token against the field configuration.
        /// </summary>
        /// <param name="field">The field <see cref="QueryFilterToken"/>.</param>
        /// <param name="constant">The constant <see cref="QueryFilterToken"/>.</param>
        /// <param name="filter">The query filter.</param>
        void IQueryFilterFieldConfig.ValidateConstant(QueryFilterToken field, QueryFilterToken constant, string filter)
        {
            if (!QueryFilterTokenKind.Constant.HasFlag(constant.Kind))
                throw new QueryFilterParserException($"Filter is invalid: Field '{field.GetRawToken(filter).ToString()}' constant '{constant.GetValueToken(filter)}' is not considered valid.");

            var iface = this as IQueryFilterFieldConfig;

            if (iface.IsTypeString)
            {
                if (!(constant.Kind == QueryFilterTokenKind.Literal || constant.Kind == QueryFilterTokenKind.Null))
                    throw new QueryFilterParserException($"Filter is invalid: Field '{field.GetRawToken(filter).ToString()}' constant '{constant.GetValueToken(filter)}' must be specified as a {QueryFilterTokenKind.Literal} where the underlying type is a string.");
            }
            else if (iface.IsTypeBoolean)
            {
                if (!(constant.Kind == QueryFilterTokenKind.True || constant.Kind == QueryFilterTokenKind.False || constant.Kind == QueryFilterTokenKind.Null))
                    throw new QueryFilterParserException($"Filter is invalid: Field '{field.GetRawToken(filter).ToString()}' constant '{constant.GetValueToken(filter)}' is not considered a valid boolean.");
            }
            else
            {
                if (!(constant.Kind == QueryFilterTokenKind.Value || constant.Kind == QueryFilterTokenKind.Null))
                    throw new QueryFilterParserException($"Filter is invalid: Field '{field.GetRawToken(filter).ToString()}' constant '{constant.GetValueToken(filter)}' must not be specified as a {QueryFilterTokenKind.Literal} where the underlying type is not a string.");
            }
        }
    }
}