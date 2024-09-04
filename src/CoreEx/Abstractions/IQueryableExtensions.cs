// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx;
using CoreEx.Data;
using CoreEx.Entities;
using CoreEx.Wildcards;
using System.Collections.Generic;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;

namespace System.Linq
{
    /// <summary>
    /// Adds additional extension methods to the <see cref="IQueryable{T}"/>.
    /// </summary>
    public static class IQueryableExtensions
    {
        /// <summary>
        /// Adds paging to the query.
        /// </summary>
        /// <typeparam name="T">The <see cref="Type"/> being queried.</typeparam>
        /// <param name="query">The query.</param>
        /// <param name="paging">The <see cref="PagingArgs"/>.</param>
        /// <returns>The query.</returns>
        public static IQueryable<T> WithPaging<T>(this IQueryable<T> query, PagingArgs? paging)
        {
            if (paging is null)
                return query.WithPaging(0, null);

            if (paging.Option == PagingOption.TokenAndTake)
                throw new ArgumentException("PagingArgs.Option must not be PagingOption.TokenAndTake.", nameof(paging));

            return query.WithPaging(paging.Skip!.Value, paging.Take);
        }

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
        /// Creates a collection from a <see cref="IQueryable{TItem}"/>.
        /// </summary>
        /// <typeparam name="TColl">The collection <see cref="Type"/>.</typeparam>
        /// <typeparam name="TItem">The item <see cref="Type"/>.</typeparam>
        /// <param name="query">The <see cref="IEnumerable{TItem}"/>.</param>
        /// <returns>A new collection that contains the elements from the input sequence.</returns>
        public static TColl ToCollection<TColl, TItem>(this IQueryable<TItem> query)
            where TColl : ICollection<TItem>, new()
        {
            var coll = new TColl();
            ToCollection(query, coll);
            return coll;
        }

        /// <summary>
        /// Creates a collection from a <see cref="IQueryable{TElement}"/> mapping each element to a corresponding item.
        /// </summary>
        /// <typeparam name="TColl">The collection <see cref="Type"/>.</typeparam>
        /// <typeparam name="TItem">The item <see cref="Type"/>.</typeparam>
        /// <typeparam name="TElement">The element <see cref="Type"/>.</typeparam>
        /// <param name="query">>The <see cref="IQueryable{TElement}"/>.</param>
        /// <param name="mapToItem">The mapping function invoked for each element.</param>
        /// <returns>A new collection that contains the elements from the input sequence.</returns>
        public static TColl ToCollection<TColl, TItem, TElement>(this IQueryable<TElement> query, Func<TElement, TItem> mapToItem)
            where TColl : ICollection<TItem>, new()
        {
            var coll = new TColl();
            ToCollection(query, mapToItem, coll);
            return coll;
        }

        /// <summary>
        /// Add to a collection from a <see cref="IQueryable{TItem}"/>.
        /// </summary>
        /// <typeparam name="TColl">The collection <see cref="Type"/>.</typeparam>
        /// <typeparam name="TItem">The item <see cref="Type"/>.</typeparam>
        /// <param name="query">The <see cref="IQueryable{TItem}"/>.</param>
        /// <param name="coll">The collection to add the elements from the input sequence.</param>
        public static void ToCollection<TColl, TItem>(this IQueryable<TItem> query, TColl coll)
            where TColl : ICollection<TItem>
        {
            coll.ThrowIfNull(nameof(coll));

            foreach (var item in query.ThrowIfNull(nameof(query)))
            {
                coll.Add(item);
            }
        }

        /// <summary>
        /// Add to a collection from a <see cref="IQueryable{TElement}"/> mapping each element to a corresponding item.
        /// </summary>
        /// <typeparam name="TColl">The collection <see cref="Type"/>.</typeparam>
        /// <typeparam name="TItem">The item <see cref="Type"/>.</typeparam>
        /// <typeparam name="TElement">The element <see cref="Type"/>.</typeparam>
        /// <param name="query">>The <see cref="IQueryable{TElement}"/>.</param>
        /// <param name="mapToItem">The mapping function invoked for each element.</param>
        /// <param name="coll">The collection to add the elements from the input sequence.</param>
        public static void ToCollection<TColl, TItem, TElement>(this IQueryable<TElement> query, Func<TElement, TItem> mapToItem, TColl coll)
            where TColl : ICollection<TItem>
        {
            mapToItem.ThrowIfNull(nameof(mapToItem));
            coll.ThrowIfNull(nameof(coll));

            foreach (var element in query.ThrowIfNull(nameof(query)))
            {
                coll.Add(mapToItem(element));
            }
        }

        /// <summary>
        /// Filters a sequence of values based on a <paramref name="predicate"/> only <paramref name="when"/> <c>true</c>.
        /// </summary>
        /// <typeparam name="TElement">The element <see cref="Type"/>.</typeparam>
        /// <param name="query">The query.</param>
        /// <param name="when">Indicates to perform an underlying <see cref="Queryable.Where{TElement}(IQueryable{TElement}, Expression{Func{TElement, bool}})"/> only when <c>true</c>;
        /// otherwise, no <b>Where</b> is invoked.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>The resulting query.</returns>
        public static IQueryable<TElement> WhereWhen<TElement>(this IQueryable<TElement> query, bool when, Expression<Func<TElement, bool>> predicate)
        {
            query.ThrowIfNull(nameof(query));

            if (when)
                return query.Where(predicate);
            else
                return query;
        }

        /// <summary>
        /// Filters a sequence of values based on a <paramref name="predicate"/> only when the <paramref name="with"/> is not the default value for the <see cref="Type"/>.
        /// </summary>
        /// <typeparam name="TElement">The element <see cref="Type"/>.</typeparam>
        /// <typeparam name="T">The with value <see cref="Type"/>.</typeparam>
        /// <param name="query">The query.</param>
        /// <param name="with">Indicates to perform an underlying <see cref="Queryable.Where{TElement}(IQueryable{TElement}, Expression{Func{TElement, bool}})"/> only when the with is not the default
        /// value; otherwise, no <b>Where</b> is invoked.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>The resulting query.</returns>
        public static IQueryable<TElement> WhereWith<TElement, T>(this IQueryable<TElement> query, T with, Expression<Func<TElement, bool>> predicate)
        {
            query.ThrowIfNull(nameof(query));

            if (Comparer<T>.Default.Compare(with, default!) != 0 && Comparer<T>.Default.Compare(with, default!) != 0)
            {
                if (with is not string && with is System.Collections.IEnumerable ie && !ie.GetEnumerator().MoveNext())
                    return query;

                return query.Where(predicate);
            }
            else
                return query;
        }

        /// <summary>
        /// Filters a sequence of values using the specified <paramref name="property"/> and <paramref name="text"/> containing <see cref="Wildcard.MultiBasic"/> supported wildcards.
        /// </summary>
        /// <typeparam name="TElement">The element <see cref="Type"/>.</typeparam>
        /// <param name="query">The query.</param>
        /// <param name="property">The <see cref="MemberExpression"/>.</param>
        /// <param name="text">The text to query.</param>
        /// <param name="ignoreCase">Indicates whether the comparison should ignore case (default) or not; will use <see cref="string.ToUpper()"/> when selected for comparisons.</param>
        /// <param name="checkForNull">Indicates whether a null check should also be performed before the comparion occurs (defaults to <c>true</c>).</param>
        /// <returns>The resulting (updated) query.</returns>
        public static IQueryable<TElement> WhereWildcard<TElement>(this IQueryable<TElement> query, Expression<Func<TElement, string?>> property, string? text, bool ignoreCase = true, bool checkForNull = true)
        {
            query.ThrowIfNull(nameof(query));
            property.ThrowIfNull(nameof(property));

            var wc = Wildcard.MultiBasic;
            var wr = wc.Parse(text).ThrowOnError();

            // Exit stage left where nothing to do.
            if (wr.Selection.HasFlag(WildcardSelection.None) || wr.Selection.HasFlag(WildcardSelection.Single))
                return query;

            // Check the expression.
            if (property.Body is not MemberExpression me)
                throw new ArgumentException("Property expression must be of Type MemberExpression.", nameof(property));

            Expression exp = me;
            var s = wr.GetTextWithoutWildcards();
            if (ignoreCase)
            {
                s = s?.ToUpper(System.Globalization.CultureInfo.CurrentCulture);
                exp = Expression.Call(me, typeof(string).GetMethod("ToUpper", System.Type.EmptyTypes)!)!;
            }

            if (wr.Selection.HasFlag(WildcardSelection.Equal))
                exp = Expression.Equal(exp, Expression.Constant(s));
            else if (wr.Selection.HasFlag(WildcardSelection.EndsWith))
                exp = Expression.Call(exp, "EndsWith", null, Expression.Constant(s));
            else if (wr.Selection.HasFlag(WildcardSelection.StartsWith))
                exp = Expression.Call(exp, "StartsWith", null, Expression.Constant(s));
            else if (wr.Selection.HasFlag(WildcardSelection.Contains))
                exp = Expression.Call(exp, "Contains", null, Expression.Constant(s));
            else
                throw new ArgumentException("Wildcard selection text is not supported.", nameof(text));

            // Add check for not null.
            if (checkForNull)
            {
                var ee = Expression.NotEqual(me, Expression.Constant(null));
                exp = Expression.AndAlso(ee, exp);
            }

            // Create the final lambda expression.
            return query.Where(Expression.Lambda<Func<TElement, bool>>(exp, property.Parameters));
        }

        /// <summary>
        /// Adds a dynamic query (filtering and sorting) as specified by the <paramref name="queryArgs"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Type"/> being queried.</typeparam>
        /// <param name="query">The query.</param>
        /// <param name="queryConfig">The <see cref="QueryArgsConfig"/>.</param>
        /// <param name="queryArgs">The <see cref="QueryArgs"/>.</param>
        /// <returns>The query.</returns>
        public static IQueryable<T> WithQuery<T>(this IQueryable<T> query, QueryArgsConfig queryConfig, QueryArgs? queryArgs)
        {
            queryConfig.ThrowIfNull(nameof(queryConfig));

            if (queryArgs is not null && !string.IsNullOrEmpty(queryArgs.Filter))
            {
                if (!queryConfig.HasFilterParser)
                    throw new QueryFilterParserException("Filter is invalid: is not supported.");

                var filter = queryConfig.FilterParser.Parse(queryArgs.Filter);

                try
                {
                    query = query.Where(filter.FilterBuilder.ToString(), [.. filter.Args]);
                }
                catch
                {
                    throw new QueryFilterParserException("Filter is invalid: there is a syntax error.");
                }
            }

            if (queryArgs is not null && !string.IsNullOrEmpty(queryArgs.OrderBy))
            {
                if (!queryConfig.HasOrderByParser)
                    throw new QueryOrderByParserException("Order By is invalid: is not supported.");

                var orderBy = queryConfig.OrderByParser.Parse(queryArgs.OrderBy);

                try
                {
                    query = query.OrderBy(orderBy);
                }
                catch
                {
                    throw new QueryOrderByParserException("Order By is invalid: there is a syntax error.");
                }
            }
            else if (queryConfig.DefaultOrderBy is not null)
            {
                try
                {
                    query = query.OrderBy(queryConfig.DefaultOrderBy);
                }
                catch
                {
                    throw new QueryOrderByParserException("Order By is invalid: there is a syntax error.");
                }
            }

            return query;
        }
    }
}