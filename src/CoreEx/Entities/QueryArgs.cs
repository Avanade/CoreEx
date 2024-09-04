// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Data;
using System.IO.MemoryMappedFiles;

namespace CoreEx.Entities
{
    /// <summary>
    /// Represents basic dynamic query <see cref="Filter"/> and <see cref="OrderBy"/> arguments.
    /// </summary>
    /// <remarks>This is <b>not</b> intended to be a replacement for OData, GraphQL, etc. but to provide a limited, explicitly supported, dynamic capability to filter and order an underlying query.</remarks>
    public class QueryArgs : IInitial
    {
        /// <summary>
        /// Create a new <see cref="QueryArgs"/>.
        /// </summary>
        /// <param name="filter">The basic dynamic <i>OData-esque</i> <c>$filter</c> statement.</param>
        /// <param name="orderBy">The basic dynamic <i>OData-esque</i> <c>$orderby</c> statement.</param>
        /// <returns>The <see cref="QueryArgsConfig"/>.</returns>
        public static QueryArgs Create(string? filter = null, string? orderBy = null) => new() { Filter = filter, OrderBy = orderBy  };

        /// <summary>
        /// Gets or sets the basic dynamic <i>OData-esque</i> <c>$filter</c> statement.
        /// </summary>
        /// <remarks>Functionality is enabled by the <see cref="Data.QueryArgsConfig"/> and related <see cref="Data.QueryFilterParser"/>.</remarks>
        public string? Filter { get; set; }

        /// <summary>
        /// Gets or sets the basic dynamic <i>OData-esque</i> <c>$orderby</c> statement.
        /// </summary>
        /// <remarks>Functionality is enabled by the <see cref="Data.QueryArgsConfig"/> and related <see cref="Data.QueryOrderByParser"/>.</remarks>
        public string? OrderBy { get; set; }

        /// <inheritdoc/>
        public bool IsInitial => string.IsNullOrEmpty(Filter) && string.IsNullOrEmpty(OrderBy);
    }
}