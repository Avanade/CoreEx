// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System.Collections.Generic;

namespace CoreEx.Entities
{
    /// <summary>
    /// Represents basic dynamic query arguments.
    /// </summary>
    /// <remarks>This is <b>not</b> intended to be a replacement for OData, GraphQL, etc. but to provide a limited, explicitly supported, dynamic capability to filter and order an underlying query.</remarks>
    public class QueryArgs
    {
        /// <summary>
        /// Create a new <see cref="QueryArgs"/>.
        /// </summary>
        /// <param name="filter">The basic dynamic <i>OData-like</i> <c>$filter</c> statement.</param>
        /// <param name="orderBy">The basic dynamic <i>OData-like</i> <c>$orderby</c> statement.</param>
        public static QueryArgs Create(string? filter = null, string? orderBy = null) => new() { Filter = filter, OrderBy = orderBy  };

        /// <summary>
        /// Gets or sets the basic dynamic <i>OData-like</i> <c>$filter</c> statement.
        /// </summary>
        public string? Filter { get; set; }

        /// <summary>
        /// Gets or sets the basic dynamic <i>OData-like</i> <c>$orderby</c> statement.
        /// </summary>
        public string? OrderBy { get; set; }

        /// <summary>
        /// Gets or sets the list of <b>included</b> fields.
        /// </summary>
        /// <remarks>Currently these are <b>only</b> used within <i>CoreEx</i> for JSON serialization filtering (see <see cref="CoreEx.Json.IJsonSerializer.TryApplyFilter{T}(T, IEnumerable{string}?, out object, Json.JsonPropertyFilter, System.StringComparison, System.Action{Json.IJsonPreFilterInspector}?)"/>).</remarks>
        public List<string>? IncludeFields { get; set; }

        /// <summary>
        /// Gets or sets the list of <b>excluded</b> fields.
        /// </summary>
        /// <remarks>Currently these are <b>only</b> used within <i>CoreEx</i> for JSON serialization filtering (see <see cref="CoreEx.Json.IJsonSerializer.TryApplyFilter{T}(T, IEnumerable{string}?, out object, Json.JsonPropertyFilter, System.StringComparison, System.Action{Json.IJsonPreFilterInspector}?)"/>).</remarks>
        public List<string>? ExcludeFields { get; set; }

        /// <summary>
        /// Appends the <paramref name="fields"/> to the <see cref="IncludeFields"/>.
        /// </summary>
        /// <param name="fields">The fields to append.</param>
        /// <returns>The <see cref="QueryArgs"/> to support fluent-style method-chaining.</returns>
        public QueryArgs Include(params string[] fields)
        {
            (IncludeFields ??= []).AddRange(fields);
            return this;
        }

        /// <summary>
        /// Appends the <paramref name="fields"/> to the <see cref="ExcludeFields"/>.
        /// </summary>
        /// <param name="fields">The fields to append.</param>
        /// <returns>The <see cref="QueryArgs"/> to support fluent-style method-chaining.</returns>
        public QueryArgs Exclude(params string[] fields)
        {
            (ExcludeFields ??= []).AddRange(fields);
            return this;
        }
    }
}