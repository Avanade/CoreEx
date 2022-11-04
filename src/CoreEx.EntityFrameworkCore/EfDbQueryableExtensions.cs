// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;

namespace Microsoft.EntityFrameworkCore
{
    /// <summary>
    /// Adds additional extension methods to <see cref="IQueryable{T}"/>.
    /// </summary>
    public static class EfDbQueryableExtensions
    {
        /// <summary>
        /// Creates a <see cref="ICollectionResult"/> from a <see cref="IQueryable{TItem}"/>.
        /// </summary>
        /// <typeparam name="TCollResult">The <see cref="ICollectionResult"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="TColl">The collection <see cref="Type"/>.</typeparam>
        /// <typeparam name="TItem">The item <see cref="Type"/>.</typeparam>
        /// <param name="query">The <see cref="IEnumerable{TItem}"/>.</param>
        /// <param name="paging">The <see cref="PagingArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>A new collection that contains the elements from the input sequence.</returns>
        public static async Task<TCollResult> ToCollectionResultAsync<TCollResult, TColl, TItem>(this IQueryable<TItem> query, PagingArgs? paging = null, CancellationToken cancellationToken = default)
            where TCollResult : ICollectionResult<TItem>, new()
            where TColl : ICollection<TItem>, new()
        {
            var result = new TCollResult { Paging = new PagingResult(paging) };

            await foreach (var item in query.WithPaging(paging).AsAsyncEnumerable().WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                result.Items.Add(item);
            }

            if (result.Paging.IsGetCount)
                result.Paging.TotalCount = await query.LongCountAsync(cancellationToken).ConfigureAwait(false);

            return result;
        }
    }
}