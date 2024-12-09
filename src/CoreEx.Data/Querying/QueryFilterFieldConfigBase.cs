// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Data.Querying.Expressions;
using CoreEx.Mapping.Converters;
using System;
using System.Text;

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
                Operators = QueryFilterOperator.Equal | QueryFilterOperator.NotEqual;
            else
                Operators = QueryFilterOperator.ComparisonOperators;
        }

        /// <inheritdoc/>
        QueryFilterParser IQueryFilterFieldConfig.Parser => _parser;

        /// <inheritdoc/>
        Type IQueryFilterFieldConfig.Type => Type;

        /// <summary>
        /// Gets the field type.
        /// </summary>
        protected Type Type => _type;

        /// <inheritdoc/>
        string IQueryFilterFieldConfig.Field => Field;

        /// <summary>
        /// Gets the field name.
        /// </summary>
        protected string Field => _field;

        /// <inheritdoc/>
        string? IQueryFilterFieldConfig.Model => Model;

        /// <summary>
        /// Gets the model name to be used for the dynamic LINQ expression.
        /// </summary>
        protected string? Model => _model ?? _field;

        /// <inheritdoc/>
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
        QueryFilterOperator IQueryFilterFieldConfig.Operators => Operators;

        /// <summary>
        /// Gets the supported <see cref="QueryFilterOperator"/>(s).
        /// </summary>
        /// <remarks>Where <see cref="IQueryFilterFieldConfig.IsTypeBoolean"/> defaults to both <see cref="QueryFilterOperator.Equal"/> and <see cref="QueryFilterOperator.NotEqual"/>; otherwise, defaults to <see cref="QueryFilterOperator.ComparisonOperators"/>.</remarks>
        protected QueryFilterOperator Operators { get; set; }

        /// <inheritdoc/>
        bool IQueryFilterFieldConfig.IsToUpper => IsToUpper;

        /// <summary>
        /// Indicates whether the comparison should ignore case or not (default); will use <see cref="string.ToUpper()"/> when selected for comparisons.
        /// </summary>
        /// <remarks>This is only applicable where the <see cref="IQueryFilterFieldConfig.IsTypeString"/>.</remarks>
        protected bool IsToUpper { get; set; } = false;

        /// <inheritdoc/>
        bool IQueryFilterFieldConfig.IsNullable => IsNullable;

        /// <summary>
        /// Indicates whether the field can be <see langword="null"/> or not.
        /// </summary>
        protected bool IsNullable { get; set; } = false;

        /// <inheritdoc/>
        bool IQueryFilterFieldConfig.IsCheckForNotNull => IsCheckForNotNull;

        /// <summary>
        /// Indicates whether a not-<see langword="null"/> check should also be performed before the comparion occurs (defaults to <c>false</c>).
        /// </summary>
        protected bool IsCheckForNotNull { get; set; } = false;

        /// <inheritdoc/>
        Func<QueryStatement>? IQueryFilterFieldConfig.DefaultStatement => DefaultStatement;

        /// <summary>
        /// Gets or sets the default LINQ <see cref="QueryStatement"/> function to be used where no filtering is specified.
        /// </summary>
        protected Func<QueryStatement>? DefaultStatement { get; set; }

        /// <inheritdoc/>
        QueryFilterFieldResultWriter? IQueryFilterFieldConfig.ResultWriter => ResultWriter;

        /// <summary>
        /// Gets or sets the <see cref="QueryFilterFieldResultWriter"/>.
        /// </summary>
        protected QueryFilterFieldResultWriter? ResultWriter { get; set; }

        /// <inheritdoc/>
        string? IQueryFilterFieldConfig.HelpText => HelpText;

        /// <summary>
        /// Gets or sets the additional help text.
        /// </summary>
        protected string? HelpText { get; set; }

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

            if (constant.Kind == QueryFilterTokenKind.Null && !IsNullable)
                throw new QueryFilterParserException($"Field '{field.GetRawToken(filter).ToString()}' constant '{constant.GetValueToken(filter)}' is not supported.");

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

        /// <inheritdoc/>
        public override string ToString() => AppendToString(new StringBuilder()).ToString();

        /// <summary>
        /// Appends the field configuration to the <paramref name="stringBuilder"/>.
        /// </summary>
        /// <param name="stringBuilder">The <see cref="StringBuilder"/>.</param>
        /// <returns>The <paramref name="stringBuilder"/>.</returns>
        public virtual StringBuilder AppendToString(StringBuilder stringBuilder)
        {
            stringBuilder.Append(_field);
            stringBuilder.Append(" (Type: ").Append(_type.Name);
            stringBuilder.Append(", Null: ").Append(IsNullable ? "true" : "false");
            stringBuilder.Append(", Operators: ");

            AppendOperatorsToString(stringBuilder);

            stringBuilder.Append(')');
            if (!string.IsNullOrEmpty(HelpText))
                stringBuilder.Append(" - ").Append(HelpText);

            return stringBuilder;
        }

        /// <summary>
        /// Appends the <see cref="Operators"/> to the <paramref name="stringBuilder"/>.
        /// </summary>
        /// <param name="stringBuilder">The <see cref="StringBuilder"/>.</param>
        /// <returns>The <paramref name="stringBuilder"/>.</returns>
        protected StringBuilder AppendOperatorsToString(StringBuilder stringBuilder)
        {
            var first = true;
#if NET6_0_OR_GREATER
            foreach (var e in Enum.GetValues<QueryFilterOperator>())
#else
            foreach (var e in Enum.GetValues(typeof(QueryFilterOperator)))
#endif
            {
                if (Operators.HasFlag((QueryFilterOperator)e))
                {
                    var op = GetODataOperator((QueryFilterOperator)e);
                    if (op is not null)
                    {
                        if (first)
                            first = false;
                        else
                            stringBuilder.Append(", ");

                        stringBuilder.Append(op);
                    }
                }
            }

            return stringBuilder;
        }

        /// <summary>
        /// Gets the ODATA operator for the specified <paramref name="operator"/>
        /// </summary>
        /// <param name="operator">The <see cref="QueryFilterOperator"/>.</param>
        protected static string? GetODataOperator(QueryFilterOperator @operator) => @operator switch
        {
            QueryFilterOperator.Equal => "EQ",
            QueryFilterOperator.NotEqual => "NE",
            QueryFilterOperator.GreaterThan => "GT",
            QueryFilterOperator.GreaterThanOrEqual => "GE",
            QueryFilterOperator.LessThan => "LT",
            QueryFilterOperator.LessThanOrEqual => "LE",
            QueryFilterOperator.In => "IN",
            QueryFilterOperator.StartsWith => nameof(QueryFilterOperator.StartsWith),
            QueryFilterOperator.EndsWith => nameof(QueryFilterOperator.EndsWith),
            QueryFilterOperator.Contains => nameof(QueryFilterOperator.Contains),
            _ => null
        };
    }
}