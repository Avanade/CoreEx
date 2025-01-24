// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using CoreEx.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Cosmos.Model
{
    /// <summary>
    /// Enables the common <b>CosmosDb</b> model-only query capabilities.
    /// </summary>
    /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
    /// <typeparam name="TSelf">The <see cref="Type"/> itself.</typeparam>
    /// <param name="container">The <see cref="CosmosDbContainer"/>.</param>
    /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
    public abstract class CosmosDbModelQueryBase<TModel, TSelf>(CosmosDbContainer container, CosmosDbArgs dbArgs) where TModel : new() where TSelf : CosmosDbModelQueryBase<TModel, TSelf>
    {
        /// <summary>
        /// Gets the <see cref="CosmosDbContainer"/>.
        /// </summary>
        public CosmosDbContainer Container { get; } = container.ThrowIfNull(nameof(container));

        /// <summary>
        /// Gets the <see cref="CosmosDbArgs"/>.
        /// </summary>
        public CosmosDbArgs QueryArgs = dbArgs;

        /// <summary>
        /// Gets the <see cref="PagingResult"/>.
        /// </summary>
        public PagingResult? Paging { get; protected set; }

        /// <summary>
        /// Adds <see cref="PagingArgs"/> to the query.
        /// </summary>
        /// <param name="paging">The <see cref="PagingArgs"/>.</param>
        /// <returns>The <typeparamref name="TSelf"/> instance to suport fluent-style method-chaining.</returns>
        public TSelf WithPaging(PagingArgs? paging)
        {
            Paging = paging == null ? null : paging is PagingResult pr ? pr : new PagingResult(paging);
            return (TSelf)this;
        }

        /// <summary>
        /// Adds <see cref="PagingArgs"/> to the query.
        /// </summary>
        /// <param name="skip">The specified number of elements in a sequence to bypass.</param>
        /// <param name="take">The specified number of contiguous elements from the start of a sequence.</param>
        /// <returns>The <typeparamref name="TSelf"/> instance to suport fluent-style method-chaining.</returns>
        public TSelf WithPaging(long skip, long? take = null) => WithPaging(PagingArgs.CreateSkipAndTake(skip, take));

        /// <summary>
        /// Selects a single item.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The single item.</returns>
        /// <remarks><see cref="Paging"/> is not supported for this operation.</remarks>
        public async Task<TModel> SelectSingleAsync(CancellationToken cancellationToken = default) => await SelectSingleWithResultAsync(cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Selects a single item with a <see cref="Result{T}"/>.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The single item.</returns>
        /// <remarks><see cref="Paging"/> is not supported for this operation.</remarks>
        public async Task<Result<TModel>> SelectSingleWithResultAsync(CancellationToken cancellationToken = default)
        {
            var result = await SelectArrayWithResultAsync(nameof(SelectSingleAsync), 2, cancellationToken).ConfigureAwait(false);
            return result.ThenAs(coll => coll.Single());
        }

        /// <summary>
        /// Selects a single item or default.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The single item or default.</returns>
        /// <remarks><see cref="Paging"/> is not supported for this operation.</remarks>
        public async Task<TModel?> SelectSingleOrDefaultAsync(CancellationToken cancellationToken = default) => await SelectSingleOrDefaultWithResultAsync(cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Selects a single item or default with a <see cref="Result{T}"/>.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The single item or default.</returns>
        /// <remarks><see cref="Paging"/> is not supported for this operation.</remarks>
        public async Task<Result<TModel?>> SelectSingleOrDefaultWithResultAsync(CancellationToken cancellationToken = default)
        {
            var result = await SelectArrayWithResultAsync(nameof(SelectSingleOrDefaultAsync), 2, cancellationToken).ConfigureAwait(false);
            return result.ThenAs(coll => Result<TModel?>.Ok(coll.SingleOrDefault()));
        }

        /// <summary>
        /// Selects first item.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The first item.</returns>
        /// <remarks><see cref="Paging"/> is not supported for this operation.</remarks>
        public async Task<TModel> SelectFirstAsync(CancellationToken cancellationToken = default) => await SelectFirstWithResultAsync(cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Selects first item with a <see cref="Result{T}"/>.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The first item.</returns>
        /// <remarks><see cref="Paging"/> is not supported for this operation.</remarks>
        public async Task<Result<TModel>> SelectFirstWithResultAsync(CancellationToken cancellationToken = default)
        {
            var result = await SelectArrayWithResultAsync(nameof(SelectFirstAsync), 1, cancellationToken).ConfigureAwait(false);
            return result.ThenAs(coll => coll.First());
        }

        /// <summary>
        /// Selects first item or default.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The single item or default.</returns>
        /// <remarks><see cref="Paging"/> is not supported for this operation.</remarks>
        public async Task<TModel?> SelectFirstOrDefaultAsync(CancellationToken cancellationToken = default) => await SelectFirstOrDefaultWithResultAsync(cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Selects first item or default with a <see cref="Result{T}"/>.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The single item or default.</returns>
        /// <remarks><see cref="Paging"/> is not supported for this operation.</remarks>
        public async Task<Result<TModel?>> SelectFirstOrDefaultWithResultAsync(CancellationToken cancellationToken = default)
        {
            var result = await SelectArrayWithResultAsync(nameof(SelectFirstOrDefaultAsync), 1, cancellationToken).ConfigureAwait(false);
            return result.ThenAs(coll => Result<TModel?>.Ok(coll.FirstOrDefault()));
        }

        /// <summary>
        /// Selects an array by limiting the data retrieved.
        /// </summary>
        private Task<Result<TModel[]>> SelectArrayWithResultAsync(string caller, long take, CancellationToken cancellationToken)
        {
            if (Paging != null)
                throw new InvalidOperationException($"The {nameof(Paging)} must be null for a {caller}; internally applied paging is needed to limit unnecessary data retrieval.");

            WithPaging(0, take);
            return ToArrayWithResultAsync(cancellationToken);
        }

        /// <summary>
        /// Executes the query command creating a resultant collection.
        /// </summary>
        /// <typeparam name="TColl">The collection <see cref="Type"/>.</typeparam>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>A resultant collection.</returns>
        /// <remarks>The <see cref="Paging"/> is also applied, including <see cref="PagingArgs.IsGetCount"/> where requested.</remarks>
        public async Task<TColl> SelectQueryAsync<TColl>(CancellationToken cancellationToken = default) where TColl : ICollection<TModel>, new() => await SelectQueryWithResultAsync<TColl>(cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Executes the query command creating a resultant collection with a <see cref="Result{T}"/>.
        /// </summary>
        /// <typeparam name="TColl">The collection <see cref="Type"/>.</typeparam>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>A resultant collection.</returns>
        /// <remarks>The <see cref="Paging"/> is also applied, including <see cref="PagingArgs.IsGetCount"/> where requested.</remarks>
        public async Task<Result<TColl>> SelectQueryWithResultAsync<TColl>(CancellationToken cancellationToken = default) where TColl : ICollection<TModel>, new()
        {
            var coll = new TColl();
            var result = await SelectQueryWithResultAsync(coll, cancellationToken).ConfigureAwait(false);
            return result.ThenAs(() => coll);
        }

        /// <summary>
        /// Executes the query adding to the passed collection.
        /// </summary>
        /// <typeparam name="TColl">The collection <see cref="Type"/>.</typeparam>
        /// <param name="coll">The collection to add items to.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <remarks>The <see cref="Paging"/> is also applied, including <see cref="PagingArgs.IsGetCount"/> where requested.</remarks>
        public async Task SelectQueryAsync<TColl>(TColl coll, CancellationToken cancellationToken = default) where TColl : ICollection<TModel> => (await SelectQueryWithResultAsync(coll, cancellationToken).ConfigureAwait(false)).ThrowOnError();

        /// <summary>
        /// Executes the query adding to the passed collection with a <see cref="Result{T}"/>.
        /// </summary>
        /// <typeparam name="TColl">The collection <see cref="Type"/>.</typeparam>
        /// <param name="coll">The collection to add items to.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <remarks>The <see cref="Paging"/> is also applied, including <see cref="PagingArgs.IsGetCount"/> where requested.</remarks>
        public abstract Task<Result> SelectQueryWithResultAsync<TColl>(TColl coll, CancellationToken cancellationToken = default) where TColl : ICollection<TModel>;

        /// <summary>
        /// Executes the query command creating a resultant array.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>A resultant array.</returns>
        /// <remarks>The <see cref="Paging"/> is also applied, including <see cref="PagingArgs.IsGetCount"/> where requested.</remarks>
        public async Task<TModel[]> ToArrayAsync(CancellationToken cancellationToken = default) => await ToArrayWithResultAsync(cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Executes the query command creating a resultant array with a <see cref="Result{T}"/>.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>A resultant array.</returns>
        /// <remarks>The <see cref="Paging"/> is also applied, including <see cref="PagingArgs.IsGetCount"/> where requested.</remarks>
        public async Task<Result<TModel[]>> ToArrayWithResultAsync(CancellationToken cancellationToken = default)
        {
            var list = new List<TModel>();
            var result = await SelectQueryWithResultAsync(list, cancellationToken).ConfigureAwait(false);
            return result.ThenAs(() => list.ToArray());
        }

        /// <summary>
        /// Executes the query command creating a <typeparamref name="TCollResult"/>.
        /// </summary>
        /// <typeparam name="TCollResult">The <see cref="ICollectionResult{TColl, TItem}"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="TColl">The <see cref="ICollection{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The resulting <typeparamref name="TCollResult"/>.</returns>
        /// <remarks>The <see cref="Paging"/> is also applied, including <see cref="PagingArgs.IsGetCount"/> where requested.</remarks>
        public async Task<TCollResult> SelectResultAsync<TCollResult, TColl>(CancellationToken cancellationToken = default) where TCollResult : ICollectionResult<TColl, TModel>, new() where TColl : ICollection<TModel>, new()
            => await SelectResultWithResultAsync<TCollResult, TColl>(cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Executes the query command creating a <typeparamref name="TCollResult"/> with a <see cref="Result{T}"/>.
        /// </summary>
        /// <typeparam name="TCollResult">The <see cref="ICollectionResult{TColl, TItem}"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="TColl">The <see cref="ICollection{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The resulting <typeparamref name="TCollResult"/>.</returns>
        /// <remarks>The <see cref="Paging"/> is also applied, including <see cref="PagingArgs.IsGetCount"/> where requested.</remarks>
        public async Task<Result<TCollResult>> SelectResultWithResultAsync<TCollResult, TColl>(CancellationToken cancellationToken = default) where TCollResult : ICollectionResult<TColl, TModel>, new() where TColl : ICollection<TModel>, new()
        {
            var result = await SelectQueryWithResultAsync<TColl>(cancellationToken).ConfigureAwait(false);
            return result.ThenAs(coll => new TCollResult { Items = coll, Paging = Paging ?? new PagingResult() });
        }
    }
}