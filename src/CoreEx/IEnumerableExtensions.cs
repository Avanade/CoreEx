// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace CoreEx
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

            if (action == null)
                throw new ArgumentNullException(nameof(action));

            foreach (TItem element in sequence ?? throw new ArgumentNullException(nameof(sequence)))
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

            if (action == null)
                throw new ArgumentNullException(nameof(action));

            foreach (TItem element in sequence ?? throw new ArgumentNullException(nameof(sequence)))
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
            if (coll == null)
                throw new ArgumentNullException(nameof(coll));

            sequence.ForEach(item => coll.Add(item));
        }
    }
}