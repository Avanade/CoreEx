// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using System;

namespace CoreEx.Data
{
    /// <summary>
    /// Provides the <see cref="QueryArgs"/> configuration.
    /// </summary>
    public class QueryArgsConfig
    {
        private readonly Lazy<QueryFilterParser> _filterParser = new(() => new QueryFilterParser());
        private readonly Lazy<QueryOrderByParser> _orderByParser = new(() => new QueryOrderByParser());

        /// <summary>
        /// Creates a new <see cref="QueryArgsConfig"/>.
        /// </summary>
        /// <returns>The <see cref="QueryArgsConfig"/>.</returns>
        public static QueryArgsConfig Create() => new();

        /// <summary>
        /// Gets the <see cref="QueryFilterParser"/>.
        /// </summary>
        public QueryFilterParser FilterParser => _filterParser.Value;

        /// <summary>
        /// Indicates whether there is a <see cref="FilterParser"/> and it has at least one field configured.
        /// </summary>
        public bool HasFilterParser => _filterParser.IsValueCreated && FilterParser.HasFields;

        /// <summary>
        /// Enables fluent-style method-chaining configuration for the <see cref="FilterParser"/>.
        /// </summary>
        /// <param name="filter">The <see cref="FilterParser"/>.</param>
        /// <returns>The <see cref="QueryArgsConfig"/> instance to support fluent-style method-chaining.</returns>
        public QueryArgsConfig WithFilter(Action<QueryFilterParser> filter)
        {
            filter.ThrowIfNull(nameof(filter))(FilterParser);
            return this;
        }

        /// <summary>
        /// Gets the <see cref="QueryOrderByParser"/>.
        /// </summary>
        public QueryOrderByParser OrderByParser => _orderByParser.Value;

        /// <summary>
        /// Indicates whether there is an <see cref="OrderByParser"/> and it has at least one field configured.
        /// </summary>
        public bool HasOrderByParser => _orderByParser.IsValueCreated && OrderByParser.HasFields;

        /// <summary>
        /// Enables fluent-style method-chaining configuration for the <see cref="OrderByParser"/>.
        /// </summary>
        /// <param name="orderBy">The <see cref="OrderByParser"/>.</param>
        /// <returns>The <see cref="QueryArgsConfig"/> instance to support fluent-style method-chaining.</returns>
        public QueryArgsConfig WithOrderBy(Action<QueryOrderByParser> orderBy)
        {
            orderBy.ThrowIfNull(nameof(orderBy))(OrderByParser);
            return this;
        }

        /// <summary>
        /// Gets the default order by statement expressed as dynamic LINQ.
        /// </summary>
        public string? DefaultOrderBy { get; private set; }

        /// <summary>
        /// Sets (overrides) the <see cref="DefaultOrderBy"/>.
        /// </summary>
        /// <param name="orderBy">The default order by statement expressed as dynamic LINQ.</param>
        /// <returns>The <see cref="QueryArgsConfig"/> instance to support fluent-style method-chaining.</returns>
        public QueryArgsConfig WithDefaultOrderBy(string orderBy)
        {
            DefaultOrderBy = orderBy;
            return this;
        }
    }
}