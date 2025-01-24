// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using System;
using System.Linq;
using System.Linq.Dynamic.Core;

namespace CoreEx.Cosmos
{
    /// <summary>
    /// Adds additional extension methods to <see cref="IQueryable{T}"/> for where <i>T</i> is <see cref="CosmosDbValue{TModel}"/>.
    /// </summary>
    public static class CosmosDbValueQueryableExtensions
    {
        /// <summary>
        /// Filters a sequence of values based on the <see cref="CosmosDbValue{TModel}.Type"/> equalling the <paramref name="typeName"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Type"/> being queried.</typeparam>
        /// <param name="query">The query.</param>
        /// <param name="typeName">The <see cref="CosmosDbValue{TModel}.Type"/> name.</param>
        /// <returns>The query.</returns>
        public static IQueryable<CosmosDbValue<T>> WhereType<T>(this IQueryable<CosmosDbValue<T>> query, string? typeName = null) where T : class, IEntityKey, new() => query.Where("type = @0", string.IsNullOrEmpty(typeName) ? typeof(T).Name : typeName);
    }
}