// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx;
using CoreEx.Wildcards;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace System.Linq
{
    /// <summary>
    /// Provides additional <see cref="IEnumerable{T}"/> extension methods.
    /// </summary>
    [DebuggerStepThrough]
    public static class IEnumerableExtensions
    {
        /// <summary>
        /// Performs the specified <paramref name="action"/> on each element in the sequence.
        /// </summary>
        /// <typeparam name="TItem">The item <see cref="Type"/>.</typeparam>
        /// <param name="sequence">The sequence to iterate.</param>
        /// <param name="action">The action to perform on each element.</param>
        /// <returns>The sequence.</returns>
        public static IEnumerable<TItem> ForEach<TItem>(this IEnumerable<TItem> sequence, Action<TItem> action)
        {
            if (sequence == null)
                return sequence!;

            action.ThrowIfNull(nameof(action));

            foreach (TItem element in sequence.ThrowIfNull(nameof(sequence)))
            {
                action(element);
            }

            return sequence;
        }

        /// <summary>
        /// Performs the specified <paramref name="action"/> on each element in the sequence asynchronously.
        /// </summary>
        /// <typeparam name="TItem">The item <see cref="Type"/>.</typeparam>
        /// <param name="sequence">The sequence to iterate.</param>
        /// <param name="action">The action to perform on each element.</param>
        /// <returns>The sequence.</returns>
        public static async Task<IEnumerable<TItem>> ForEachAsync<TItem>(this IEnumerable<TItem> sequence, Func<TItem, Task> action)
        {
            if (sequence == null)
                return sequence!;

            action.ThrowIfNull(nameof(action));

            foreach (TItem element in sequence.ThrowIfNull(nameof(sequence)))
            {
                await action(element).ConfigureAwait(false);
            }

            return sequence;
        }

        /// <summary>
        /// Creates a collection from a <see cref="IEnumerable{TItem}"/>.
        /// </summary>
        /// <typeparam name="TColl">The collection <see cref="Type"/>.</typeparam>
        /// <typeparam name="TItem">The item <see cref="Type"/>.</typeparam>
        /// <param name="sequence">The sequence to iterate.</param>
        /// <returns>A new collection that contains the elements from the input sequence.</returns>
        public static TColl ToCollection<TColl, TItem>(this IEnumerable<TItem> sequence)
            where TColl : ICollection<TItem>, new()
        {
            var coll = new TColl();
            ToCollection(sequence, coll);
            return coll;
        }

        /// <summary>
        /// Add to a collection from a <see cref="IEnumerable{TItem}"/>.
        /// </summary>
        /// <typeparam name="TColl">The collection <see cref="Type"/>.</typeparam>
        /// <typeparam name="TItem">The item <see cref="Type"/>.</typeparam>
        /// <param name="sequence">The sequence to iterate.</param>
        /// <param name="coll">The collection to add the elements from the input sequence.</param>
        public static void ToCollection<TColl, TItem>(this IEnumerable<TItem> sequence, TColl coll)
            where TColl : ICollection<TItem>
        {
            coll.ThrowIfNull(nameof(coll));
            sequence.ForEach(coll.Add);
        }

        /// <summary>
        /// Filters a sequence of values based on a <paramref name="predicate"/> only <paramref name="when"/> <c>true</c>.
        /// </summary>
        /// <typeparam name="TElement">The element <see cref="Type"/>.</typeparam>
        /// <param name="query">The query.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="when">Indicates to perform an underlying <see cref="System.Linq.Enumerable.Where{TSource}(IEnumerable{TSource}, Func{TSource, bool})"/> only when <c>true</c>; otherwise, no <i>Where</i> is invoked.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> that contains elements from the input sequence that satisfy the condition.</returns>
        public static IEnumerable<TElement> WhereWhen<TElement>(this IEnumerable<TElement> query, Func<TElement, bool> predicate, bool when) => when ? query.Where(predicate): query;

        /// <summary>
        /// Filters a sequence of values using the specified <paramref name="property"/> and <paramref name="text"/> containing supported wildcards (intended for LINQ to Objects).
        /// </summary>
        /// <typeparam name="TElement">The element <see cref="Type"/>.</typeparam>
        /// <param name="query">The query.</param>
        /// <param name="property">The <see cref="MemberExpression"/>.</param>
        /// <param name="text">The text to query.</param>
        /// <param name="ignoreCase">Indicates whether the comparison should ignore case (default) or not.</param>
        /// <param name="checkForNull">Indicates whether a null check should also be performed before the comparion occurs (defaults to <c>true</c>).</param>
        /// <param name="wildcard">The <see cref="Wildcard"/> configuration to use; where <c>null</c> it will use <see cref="Wildcard.Default"/>.</param>
        /// <returns>The resulting (updated) query.</returns>
        public static IEnumerable<TElement> WhereWildcard<TElement>(this IEnumerable<TElement> query, Func<TElement, string?> property, string? text, bool ignoreCase = true, bool checkForNull = true, Wildcard? wildcard = null)
            where TElement : class
        {
            query.ThrowIfNull(nameof(query));
            property.ThrowIfNull(nameof(property));

            var wc = wildcard ?? Wildcard.Default ?? Wildcard.MultiAll;
            var wr = wc.Parse(text).ThrowOnError();

            // Exit stage left where nothing to do.
            if (wr.Selection.HasFlag(WildcardSelection.None))
                return query;

            // Handle the Equal.
            var sc = ignoreCase ? StringComparison.CurrentCultureIgnoreCase : StringComparison.CurrentCulture;
            if (wr.Selection.HasFlag(WildcardSelection.Equal))
                return query.Where(x =>
                {
                    var v = property.Invoke(x);
                    return checkForNull ? v != null && v.Equals(text, sc) : v!.Equals(text, sc);
                });

            // Handle the easy Contains/StartsWith/Endswith.
            if (!wr.Selection.HasFlag(WildcardSelection.SingleWildcard) && !wr.Selection.HasFlag(WildcardSelection.Embedded))
            {
                if (wr.Selection.HasFlag(WildcardSelection.Single))
                    return query;

                if (wr.Selection.HasFlag(WildcardSelection.Contains))
                    return query.Where(x =>
                    {
                        var v = property.Invoke(x);
                        return checkForNull ? v != null && v.Contains(wr.GetTextWithoutWildcards()!, sc) : v!.Contains(wr.GetTextWithoutWildcards()!, sc);
                    });
                else if (wr.Selection.HasFlag(WildcardSelection.StartsWith))
                    return query.Where(x =>
                    {
                        var v = property.Invoke(x);
                        return checkForNull ? v != null && v.StartsWith(wr.GetTextWithoutWildcards()!, sc) : v!.StartsWith(wr.GetTextWithoutWildcards()!, sc);
                    });
                else if (wr.Selection.HasFlag(WildcardSelection.EndsWith))
                    return query.Where(x =>
                    {
                        var v = property.Invoke(x);
                        return checkForNull ? v != null && v.EndsWith(wr.GetTextWithoutWildcards()!, sc) : v!.EndsWith(wr.GetTextWithoutWildcards()!, sc);
                    });
            }

            // Handle the remainder using a regex.
            var regex = wr.CreateRegex(ignoreCase);
            return query.Where(x =>
            {
                var v = property.Invoke(x);
                return v != null && regex.IsMatch(v);
            });
        }
    }
}