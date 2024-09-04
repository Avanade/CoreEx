// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Collections.Generic;
using System.Text;

namespace CoreEx.Data
{
    /// <summary>
    /// Represents a basic query sort order by parser with explicitly defined field support.
    /// </summary>
    /// <remarks>This is <b>not</b> intended to be a replacement for OData, GraphQL, etc. but to provide a limited, explicitly supported, dynamic capability to sort an underlying query.</remarks>
    public class QueryOrderByParser
    {
        private static readonly string _orderByAscending = "ascending";
        private static readonly string _orderByDescending = "descending";

        private readonly Dictionary<string, QueryOrderByFieldConfig> _fields = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Adds a <see cref="QueryOrderByFieldConfig"/> to the parser for the specified <paramref name="name"/> as-is.
        /// </summary>
        /// <param name="name">The field name used in the query filter specified with the correct casing.</param>
        /// <param name="configure">The optional action enabling further field configuration.</param>
        /// <returns>The <see cref="QueryOrderByParser"/> to support fluent-style method-chaining.</returns>
        public QueryOrderByParser AddField(string name, Action<QueryOrderByFieldConfig>? configure = null) => AddField(name, null, configure);

        /// <summary>
        /// Adds a <see cref="QueryOrderByFieldConfig"/> to the parser using the specified <paramref name="name"/> and <paramref name="overrideName"/>.
        /// </summary>
        /// <param name="name">The field name used in the query filter.</param>
        /// <param name="overrideName">The field name override.</param>

        /// <param name="configure">The optional action enabling further field configuration.</param>
        /// <returns>The <see cref="QueryOrderByParser"/> to support fluent-style method-chaining.</returns>
        public QueryOrderByParser AddField(string name, string? overrideName, Action<QueryOrderByFieldConfig>? configure = null)
        {
            var config = new QueryOrderByFieldConfig(this, name) { OverrideField = overrideName };
            configure?.Invoke(config);
            _fields.Add(name, config);
            return this;
        }

        /// <summary>
        /// Indicates that at least a single field has been configured.
        /// </summary>
        public bool HasFields => _fields.Count > 0;

        /// <summary>
        /// Gets or sets the default order by to use when none is specified.
        /// </summary>
        public string? Default { get; set; }

        /// <summary>
        /// Parses and converts the <paramref name="orderBy"/> to dynamic LINQ.
        /// </summary>
        /// <param name="orderBy">The query order-by.</param>
        /// <returns>The dynamic LINQ equivalent.</returns>
        [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(orderBy))]
        public string? Parse(string? orderBy)
        {
            if (string.IsNullOrEmpty(orderBy))
                return null;

            var sb = new StringBuilder();

#if NET6_0_OR_GREATER
            foreach (var sort in orderBy.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var parts = sort.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
#else
            foreach (var sort in orderBy.Split(',', StringSplitOptions.RemoveEmptyEntries ))
            {
                var parts = sort.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
#endif
                if (parts.Length == 0)
                    continue;
                else if (parts.Length > 2)
                    throw new QueryFilterParserException("Order By is invalid: invalid syntax.");

#if NET6_0_OR_GREATER
                var field = parts[0];
#else
                var field = parts[0].Trim();
#endif
                var config = _fields.TryGetValue(field, out var fc) ? fc : throw new QueryFilterParserException($"Order By is invalid: Field '{field}' is not supported.");

                if (sb.Length > 0)
                    sb.Append(", ");

                sb.Append(config.OverrideField ?? config.Field);

                var dir = parts.Length == 2 ? parts[1].Trim() : null;
                if (dir is not null)
                {
                    if (dir.Length > 2 && _orderByAscending.StartsWith(dir, StringComparison.OrdinalIgnoreCase))
                        sb.Append(" asc");
                    else if (dir.Length > 3 && _orderByDescending.StartsWith(dir, StringComparison.OrdinalIgnoreCase))
                        sb.Append(" desc");
                    else
                        throw new QueryFilterParserException($"Order By is invalid: Direction '{dir}' must be either 'asc' (ascending) or 'desc' (descending).");
                }
            }

            return sb.ToString();
        }
    }
}