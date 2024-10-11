// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx;
using CoreEx.Data.Querying;
using CoreEx.Entities;
using System.Linq;
using System.Linq.Dynamic.Core;

namespace System.Linq
{
    /// <summary>
    /// Adds additional extension methods to the <see cref="IQueryable{T}"/>.
    /// </summary>
    public static class QueryFilterExtensions
    {
        /// <summary>
        /// Adds a dynamic query filter as specified by the <paramref name="result"/> (uses the <see cref="QueryArgsParseResult.FilterResult"/> where not <see langword="null"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Type"/> being queried.</typeparam>
        /// <param name="query">The query.</param>
        /// <param name="result">The <see cref="QueryArgsParseResult"/>.</param>
        /// <returns>The query.</returns>
        public static IQueryable<T> Where<T>(this IQueryable<T> query, QueryArgsParseResult result)
        {
            result.ThrowIfNull(nameof(result));
            if (result.FilterResult is not null)
            {
                var linq = result.FilterResult.ToLinqString();
                query = string.IsNullOrEmpty(linq) ? query : query.Where(linq, [.. result.FilterResult.Args]);
            }

            return query;
        }

        /// <summary>
        /// Adds a dynamic query filter as specified by the <paramref name="queryArgs"/> (uses the <see cref="QueryArgs.Filter"/> where not <see langword="null"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Type"/> being queried.</typeparam>
        /// <param name="query">The query.</param>
        /// <param name="queryConfig">The <see cref="QueryArgsConfig"/>.</param>
        /// <param name="queryArgs">The <see cref="QueryArgs"/>.</param>
        /// <returns>The query.</returns>
        public static IQueryable<T> Where<T>(this IQueryable<T> query, QueryArgsConfig queryConfig, QueryArgs? queryArgs) => query.Where(queryConfig, queryArgs?.Filter);

        /// <summary>
        /// Adds a dynamic query <paramref name="filter"/> (basic dynamic <i>OData-like</i> <c>$filter</c> statement).
        /// </summary>
        /// <typeparam name="T">The <see cref="Type"/> being queried.</typeparam>
        /// <param name="query">The query.</param>
        /// <param name="queryConfig">The <see cref="QueryArgsConfig"/>.</param>
        /// <param name="filter">The basic dynamic <i>OData-like</i> <c>$filter</c> statement.</param>
        /// <returns>The query.</returns>
        public static IQueryable<T> Where<T>(this IQueryable<T> query, QueryArgsConfig queryConfig, string? filter)
        {
            queryConfig.ThrowIfNull(nameof(queryConfig));
            if (!queryConfig.HasFilterParser && !string.IsNullOrEmpty(filter))
                throw new QueryFilterParserException("Filter statement is not currently supported.");

            var result = queryConfig.FilterParser.Parse(filter).ThrowOnError();
            var linq = result.Value.ToLinqString();
            return string.IsNullOrEmpty(linq) ? query : query.Where(linq, [.. result.Value.Args]);
        }

        /// <summary>
        /// Adds a dynamic query order by as specified by the <paramref name="result"/> (uses the <see cref="QueryArgsParseResult.OrderByResult"/> where not <see langword="null"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Type"/> being queried.</typeparam>
        /// <param name="query">The query.</param>
        /// <param name="result">The <see cref="QueryArgsParseResult"/>.</param>
        /// <returns>The query.</returns>
        public static IQueryable<T> OrderBy<T>(this IQueryable<T> query, QueryArgsParseResult result)
        {
            result.ThrowIfNull(nameof(result));
            if (result.OrderByResult is not null)
            {
                var linq = result.OrderByResult.ToLinqString();
                query = string.IsNullOrEmpty(linq) ? query : query.OrderBy(linq);
            }

            return query;
        }

        /// <summary>
        /// Adds a dynamic query order by as specified by the <paramref name="queryArgs"/> (uses the <see cref="QueryArgs.OrderBy"/> where not <see langword="null"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Type"/> being queried.</typeparam>
        /// <param name="query">The query.</param>
        /// <param name="queryConfig">The <see cref="QueryArgsConfig"/>.</param>
        /// <param name="queryArgs">The <see cref="QueryArgs"/>.</param>
        /// <returns>The query.</returns>
        /// <remarks>Where the <paramref name="queryArgs"/> is <see langword="null"/> or <see cref="QueryArgs.OrderBy"/> is <see langword="null"/>, then the <see cref="QueryOrderByParser.DefaultOrderBy"/> will be used (where also not <see langword="null"/>).</remarks>
        public static IQueryable<T> OrderBy<T>(this IQueryable<T> query, QueryArgsConfig queryConfig, QueryArgs? queryArgs = null) => query.OrderBy(queryConfig, queryArgs?.OrderBy);

        /// <summary>
        /// Adds a dynamic query order <paramref name="orderby"/> (basic dynamic <i>OData-like</i> <c>$orderby</c> statement).
        /// </summary>
        /// <typeparam name="T">The <see cref="Type"/> being queried.</typeparam>
        /// <param name="query">The query.</param>
        /// <param name="queryConfig">The <see cref="QueryArgsConfig"/>.</param>
        /// <param name="orderby">The basic dynamic <i>OData-like</i> <c>$orderby</c> statement.</param>
        /// <returns>The query.</returns>
        public static IQueryable<T> OrderBy<T>(this IQueryable<T> query, QueryArgsConfig queryConfig, string? orderby)
        {
            queryConfig.ThrowIfNull(nameof(queryConfig));
            if (!queryConfig.HasOrderByParser && !string.IsNullOrEmpty(orderby))
                throw new QueryOrderByParserException("OrderBy statement is not currently supported.");

            var result = queryConfig.OrderByParser.Parse(orderby).ThrowOnError();
            var linq = result.Value.ToLinqString();
            return string.IsNullOrEmpty(linq) ? query : query.OrderBy(linq);
        }
    }
}