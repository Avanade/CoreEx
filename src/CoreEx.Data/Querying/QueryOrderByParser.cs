﻿// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Results;
using System;
using System.Collections.Generic;
using System.Text;

namespace CoreEx.Data.Querying
{
    /// <summary>
    /// Represents a basic query sort order by parser and LINQ translator with explicitly defined field support.
    /// </summary>
    /// <remarks>This is <b>not</b> intended to be a replacement for OData, GraphQL, etc. but to provide a limited, explicitly supported, dynamic capability to sort an underlying query.</remarks>
    /// <param name="owner">The owning <see cref="QueryArgsConfig"/>.</param>
    public sealed class QueryOrderByParser(QueryArgsConfig owner)
    {
        private readonly Dictionary<string, QueryOrderByFieldConfig> _fields = new(StringComparer.OrdinalIgnoreCase);
        private Action<string[]>? _validator;
        private string? _helpText;

        /// <summary>
        /// Gets the owning <see cref="QueryArgsConfig"/>.
        /// </summary>
        public QueryArgsConfig Owner => owner.ThrowIfNull(nameof(owner));

        /// <summary>
        /// Gets the default order-by dynamic LINQ statement.
        /// </summary>
        /// <remarks>To avoid unnecessary parsing this should be specified as a valid dynamic LINQ statement.</remarks>
        public string? DefaultOrderBy { get; private set; }

        /// <summary>
        /// Indicates that at least a single field has been configured.
        /// </summary>
        public bool HasFields => _fields.Count > 0;

        /// <summary>
        /// Adds a <see cref="QueryOrderByFieldConfig"/> to the parser for the specified <paramref name="field"/> as-is.
        /// </summary>
        /// <param name="field">The field name used in the order by specified with the correct casing.</param>
        /// <param name="configure">The optional action enabling further field configuration.</param>
        /// <returns>The <see cref="QueryOrderByParser"/> to support fluent-style method-chaining.</returns>
        /// <remarks>To avoid unnecessary parsing this should be the valid dynamic LINQ statement.</remarks>
        public QueryOrderByParser AddField(string field, Action<QueryOrderByFieldConfig>? configure = null) => AddField(field, null, configure);

        /// <summary>
        /// Adds a <see cref="QueryOrderByFieldConfig"/> to the parser using the specified <paramref name="field"/> and <paramref name="model"/>.
        /// </summary>
        /// <param name="field">The field name used in the query filter.</param>
        /// <param name="model">The model name (defaults to <paramref name="field"/>.</param>
        /// <param name="configure">The optional action enabling further field configuration.</param>
        /// <returns>The <see cref="QueryOrderByParser"/> to support fluent-style method-chaining.</returns>
        public QueryOrderByParser AddField(string field, string? model, Action<QueryOrderByFieldConfig>? configure = null)
        {
            var config = new QueryOrderByFieldConfig(this, field, model);
            configure?.Invoke(config);
            _fields.Add(field, config);
            return this;
        }

        /// <summary>
        /// Sets (overrides) the default order-by dynamic LINQ statement.
        /// </summary>
        /// <param name="defaultOrderBy">The default order-by statement used where not explicitly specified (see <see cref="CoreEx.Entities.QueryArgs.OrderBy"/>.).</param>
        /// <returns>The <see cref="QueryOrderByParser"/> to support fluent-style method-chaining.</returns>
        /// <remarks>To avoid unnecessary parsing this should be specified as a valid dynamic LINQ statement.</remarks>
        public QueryOrderByParser WithDefault(string? defaultOrderBy)
        {
            DefaultOrderBy = defaultOrderBy.ThrowIfEmpty(nameof(defaultOrderBy));
            return this;
        }

        /// <summary>
        /// Sets (override) the additional help text.
        /// </summary>
        /// <param name="text">The additional help text.</param>
        /// <returns>The <see cref="QueryOrderByParser"/> to support fluent-style method-chaining.</returns>
        public QueryOrderByParser WithHelpText(string text)
        {
            _helpText = text;
            return this;
        }

        /// <summary>
        /// Sets (overrides) a <paramref name="validator"/> that can be used to further validate the fields specified in the order by.
        /// </summary>
        /// <param name="validator">The validator action.</param>
        /// <returns>The <see cref="QueryOrderByParser"/> to support fluent-style method-chaining.</returns>
        /// <remarks>Throw a <see cref="QueryOrderByParserException"/> to have the validation message formatted correctly and consistently.
        /// <para>The <c>string[]</c> passed into the validator will contain the parsed fields (names) in the order in which they were specified.</para></remarks>
        public QueryOrderByParser WithValidator(Action<string[]>? validator)
        {
            _validator = validator;
            return this;
        }

        /// <summary>
        /// Parses and converts the <paramref name="orderBy"/> to dynamic LINQ.
        /// </summary>
        /// <param name="orderBy">The query order-by.</param>
        /// <returns>The <see cref="QueryOrderByParserResult"/>.</returns>
        public Result<QueryOrderByParserResult> Parse(string? orderBy)
        {
            if (!string.IsNullOrEmpty(orderBy) && orderBy.Equals("help", StringComparison.OrdinalIgnoreCase))
                return new QueryOrderByParserException(ToString());

            if (string.IsNullOrEmpty(orderBy))
                return new QueryOrderByParserResult(DefaultOrderBy);

            var fields = new List<string>();
            var sb = new StringBuilder();

#if NET6_0_OR_GREATER
            foreach (var sort in orderBy.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var parts = sort.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
#else
            foreach (var sort in orderBy.ThrowIfNullOrEmpty(nameof(orderBy)).Split(',', StringSplitOptions.RemoveEmptyEntries ))
            {
                var parts = sort.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
#endif
                if (parts.Length == 0)
                    continue;
                else if (parts.Length > 2)
                    throw new QueryOrderByParserException("Statement is syntactically incorrect.");

#if NET6_0_OR_GREATER
                var field = parts[0];
#else
                var field = parts[0].Trim();
#endif
                var config = _fields.TryGetValue(field, out var fc) ? fc : throw new QueryOrderByParserException($"Field '{field}' is not supported.");

                if (sb.Length > 0)
                    sb.Append(", ");

                sb.Append(config.Model);

                var dir = parts.Length == 2 ? parts[1].Trim() : null;
                if (dir is not null)
                {
                    var direction = QueryOrderByDirection.Ascending;
                    if (dir.Length > 3 && nameof(QueryOrderByDirection.Descending).StartsWith(dir, StringComparison.OrdinalIgnoreCase))
                    {
                        sb.Append(" desc");
                        direction = QueryOrderByDirection.Descending;
                    }
                    else if (!(dir.Length > 2 && nameof(QueryOrderByDirection.Ascending).StartsWith(dir, StringComparison.OrdinalIgnoreCase)))
                        return new QueryOrderByParserException($"Field '{field}' direction '{dir}' is invalid; must be either 'asc' (ascending) or 'desc' (descending).");

                    if (!config.Direction.HasFlag(direction))
                        return new QueryOrderByParserException($"Field '{field}' direction '{dir}' is invalid; not supported.");
                }

                if (fields.Contains(config.Field))
                    return new QueryOrderByParserException($"Field '{field}' must not be specified more than once.");

                fields.Add(config.Field);
            }

            try
            {
                _validator?.Invoke([.. fields]);
            }
            catch (QueryOrderByParserException qobpex)
            {
                return qobpex;
            }

            return new QueryOrderByParserResult(sb.ToString());
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            if (!HasFields)
                return "Order-By statement is not currently supported.";

            var sb = new StringBuilder("Order-by field(s) are as follows:");
            foreach (var field in _fields)
            {
                sb.AppendLine().Append(field.Key).Append(" (Direction: ").Append(field.Value.Direction).Append(')');
            }

            if (!string.IsNullOrEmpty(_helpText))
                sb.AppendLine().Append(_helpText);

            return sb.ToString();
        }
    }
}