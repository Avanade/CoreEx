// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using System;
using System.Linq;

namespace CoreEx.Cosmos
{
    /// <summary>
    /// Adds additional extension methods to <see cref="IQueryable{T}"/>.
    /// </summary>
    public static class CosmosDbQueryableExtensions
    {
        /// <summary>
        /// Adds paging to the query.
        /// </summary>
        /// <typeparam name="T">The <see cref="Type"/> being queried.</typeparam>
        /// <param name="query">The query.</param>
        /// <param name="paging">The <see cref="PagingArgs"/>.</param>
        /// <returns>The query.</returns>
        public static IQueryable<T> WithPaging<T>(this IQueryable<T> query, PagingArgs? paging) => paging == null ? query.WithPaging(0, null) : query.WithPaging(paging.Skip, paging.Take);

        /// <summary>
        /// Adds paging to the query using the specified <paramref name="skip"/> and <paramref name="take"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Type"/> being queried.</typeparam>
        /// <param name="query">The query.</param>
        /// <param name="skip">The specified number of elements in a sequence to bypass.</param>
        /// <param name="take">The specified number of contiguous elements from the start of a sequence.</param>
        /// <returns>The query.</returns>
        public static IQueryable<T> WithPaging<T>(this IQueryable<T> query, long skip, long? take = null)
        {
            var q = query.Skip(skip <= 0 ? 0 : (int)skip);
            q = q.Take(take == null || take.Value < 1 ? (int)PagingArgs.DefaultTake : (int)take.Value);
            return q;
        }

        /// <summary>
        /// Selects a single item.
        /// </summary>
        /// <typeparam name="T">The <see cref="Type"/> being queried.</typeparam>
        /// <param name="query">The query.</param>
        /// <returns>The single item.</returns>
        public static T SelectSingle<T>(this IQueryable<T> query) => query.WithPaging(CosmosDb.PagingTop2).AsEnumerable().Single();

        /// <summary>
        /// Selects a single item or default.
        /// </summary>
        /// <typeparam name="T">The <see cref="Type"/> being queried.</typeparam>
        /// <param name="query">The query.</param>
        /// <returns>The single item or default.</returns>
        public static T SelectSingleOrDefault<T>(this IQueryable<T> query) => query.WithPaging(CosmosDb.PagingTop2).AsEnumerable().SingleOrDefault();

        /// <summary>
        /// Selects first item.
        /// </summary>
        /// <typeparam name="T">The <see cref="Type"/> being queried.</typeparam>
        /// <param name="query">The query.</param>
        /// <returns>The first item.</returns>
        public static T SelectFirst<T>(this IQueryable<T> query) => query.WithPaging(CosmosDb.PagingTop1).AsEnumerable().First();

        /// <summary>
        /// Selects first item or default.
        /// </summary>
        /// <typeparam name="T">The <see cref="Type"/> being queried.</typeparam>
        /// <param name="query">The query.</param>
        /// <returns>The single item or default.</returns>
        public static T SelectFirstOrDefault<T>(this IQueryable<T> query) => query.WithPaging(CosmosDb.PagingTop1).AsEnumerable().FirstOrDefault();
    }
}