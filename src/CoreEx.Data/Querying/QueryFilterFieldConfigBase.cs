// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Mapping.Converters;
using System;

namespace CoreEx.Data.Querying
{
    /// <summary>
    /// Provides the base <see cref="QueryFilterParser"/> field configuration.
    /// </summary>
    public abstract class QueryFilterFieldConfigBase : IQueryFilterFieldConfig
    {
        private readonly QueryFilterParser _parser;
        private readonly Type _type;
        private readonly string _field;
        private readonly string? _model;

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryFilterFieldConfigBase{TSelf}"/> class.
        /// </summary>
        /// <param name="parser">The owning <see cref="QueryFilterParser"/>.</param>
        /// <param name="type">The field type.</param>
        /// <param name="field">The field name.</param>
        /// <param name="model">The model name (defaults to <paramref name="field"/>.</param>
        public QueryFilterFieldConfigBase(QueryFilterParser parser, Type type, string field, string? model)
        {
            _parser = parser.ThrowIfNull(nameof(parser));
            _type = type.ThrowIfNull(nameof(type));
            _field = field.ThrowIfNullOrEmpty(nameof(field));
            _model = model;

            IsTypeString = type == typeof(string);
            IsTypeBoolean = type == typeof(bool);

            if (IsTypeBoolean)
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
        string? IQueryFilterFieldConfig.Model => _model ?? _field;

        bool IQueryFilterFieldConfig.IsTypeString => IsTypeString;

        /// <summary>
        /// Indicates whether the field type is a <see cref="string"/>.
        /// </summary>
        protected bool IsTypeString { get; set; }

        /// <summary>
        /// Indicates whether the field type is a <see cref="bool"/>.
        /// </summary>
        bool IQueryFilterFieldConfig.IsTypeBoolean => IsTypeBoolean;

        /// <summary>
        /// Indicates whether the field type is a <see cref="string"/>.
        /// </summary>
        protected bool IsTypeBoolean { get; set; }

        /// <inheritdoc/>
        QueryFilterTokenKind IQueryFilterFieldConfig.SupportedKinds => SupportedKinds;

        /// <summary>
        /// Gets the supported kinds.
        /// </summary>
        /// <remarks>Where <see cref="IQueryFilterFieldConfig.IsTypeBoolean"/> defaults to both <see cref="QueryFilterTokenKind.Equal"/> and <see cref="QueryFilterTokenKind.NotEqual"/>; otherwise, defaults to <see cref="QueryFilterTokenKind.Operator"/>.</remarks>
        protected QueryFilterTokenKind SupportedKinds { get; set; }

        /// <inheritdoc/>
        bool IQueryFilterFieldConfig.IsToUpper => IsToUpper;

        /// <summary>
        /// Indicates whether the comparison should ignore case or not (default); will use <see cref="string.ToUpper()"/> when selected for comparisons.
        /// </summary>
        /// <remarks>This is only applicable where the <see cref="IQueryFilterFieldConfig.IsTypeString"/>.</remarks>
        protected bool IsToUpper { get; set; } = false;

        /// <inheritdoc/>
        bool IQueryFilterFieldConfig.IsCheckForNotNull => IsCheckForNotNull;

        /// <summary>
        /// Indicates whether a not-<see langword="null"/> check should also be performed before the comparion occurs (defaults to <c>false</c>).
        /// </summary>
        protected bool IsCheckForNotNull { get; set; } = false;

        /// <inheritdoc/>
        QueryStatement? IQueryFilterFieldConfig.DefaultStatement => DefaultStatement;

        /// <summary>
        /// Gets the default LINQ <see cref="QueryStatement"/> to be used where no filtering is specified.
        /// </summary>
        protected QueryStatement? DefaultStatement { get; set; }

        /// <inheritdoc/>
        object? IQueryFilterFieldConfig.ConvertToValue(QueryFilterToken operation, QueryFilterToken field, string filter) => ConvertToValue(operation, field, filter);

        /// <summary>
        /// Converts <paramref name="field"/> to the destination type using the <see cref="Converter"/> configurations where specified.
        /// </summary>
        /// <param name="operation">The operation <see cref="QueryFilterToken"/> being performed on the <paramref name="operation"/>.</param>
        /// <param name="field">The field <see cref="QueryFilterToken"/>.</param>
        /// <param name="filter">The query filter.</param>
        /// <returns>The converted value.</returns>
        /// <remarks>Note: A converted value of <see langword="null"/> is considered invalid and will result in an <see cref="InvalidOperationException"/>.</remarks>
        protected abstract object ConvertToValue(QueryFilterToken operation, QueryFilterToken field, string filter);

        /// <summary>
        /// Validate the <paramref name="constant"/> token against the field configuration.
        /// </summary>
        /// <param name="field">The field <see cref="QueryFilterToken"/>.</param>
        /// <param name="constant">The constant <see cref="QueryFilterToken"/>.</param>
        /// <param name="filter">The query filter.</param>
        void IQueryFilterFieldConfig.ValidateConstant(QueryFilterToken field, QueryFilterToken constant, string filter)
        {
            if (!QueryFilterTokenKind.Constant.HasFlag(constant.Kind))
                throw new QueryFilterParserException($"Field '{field.GetRawToken(filter).ToString()}' constant '{constant.GetValueToken(filter)}' is not considered valid.");

            if (IsTypeString)
            {
                if (!(constant.Kind == QueryFilterTokenKind.Literal || constant.Kind == QueryFilterTokenKind.Null))
                    throw new QueryFilterParserException($"Field '{field.GetRawToken(filter).ToString()}' constant '{constant.GetValueToken(filter)}' must be specified as a {QueryFilterTokenKind.Literal} where the underlying type is a string.");
            }
            else if (IsTypeBoolean)
            {
                if (!(constant.Kind == QueryFilterTokenKind.True || constant.Kind == QueryFilterTokenKind.False || constant.Kind == QueryFilterTokenKind.Null))
                    throw new QueryFilterParserException($"Field '{field.GetRawToken(filter).ToString()}' constant '{constant.GetValueToken(filter)}' is not considered a valid boolean.");
            }
            else
            {
                if (!(constant.Kind == QueryFilterTokenKind.Value || constant.Kind == QueryFilterTokenKind.Null))
                    throw new QueryFilterParserException($"Field '{field.GetRawToken(filter).ToString()}' constant '{constant.GetValueToken(filter)}' must not be specified as a {QueryFilterTokenKind.Literal} where the underlying type is not a string.");
            }
        }
    }
}