﻿// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using System;

namespace CoreEx.Data.Querying
{
    /// <summary>
    /// Provides the <see cref="QueryArgs"/> configuration.
    /// </summary>
    public class QueryArgsConfig
    {
        private QueryFilterParser? _filterParser;
        private QueryOrderByParser? _orderByParser;

        /// <summary>
        /// Creates a new <see cref="QueryArgsConfig"/>.
        /// </summary>
        /// <returns>The <see cref="QueryArgsConfig"/>.</returns>
        public static QueryArgsConfig Create() => new();

        /// <summary>
        /// Gets the <see cref="QueryFilterParser"/>.
        /// </summary>
        public QueryFilterParser FilterParser => _filterParser ??= new QueryFilterParser();

        /// <summary>
        /// Indicates whether there is a <see cref="FilterParser"/>.
        /// </summary>
        public bool HasFilterParser => _filterParser is not null;

        /// <summary>
        /// Gets the <see cref="QueryOrderByParser"/>.
        /// </summary>
        public QueryOrderByParser OrderByParser => _orderByParser ??= new QueryOrderByParser();

        /// <summary>
        /// Indicates whether there is an <see cref="OrderByParser"/>.
        /// </summary>
        public bool HasOrderByParser => _orderByParser is not null;

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
        /// Enables fluent-style method-chaining configuration for the <see cref="OrderByParser"/>.
        /// </summary>
        /// <param name="orderBy">The <see cref="OrderByParser"/>.</param>
        /// <returns>The <see cref="QueryArgsConfig"/> instance to support fluent-style method-chaining.</returns>
        public QueryArgsConfig WithOrderBy(Action<QueryOrderByParser> orderBy)
        {
            orderBy.ThrowIfNull(nameof(orderBy))(OrderByParser);
            return this;
        }
    }
}