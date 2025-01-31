// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using CoreEx.Mapping;
using CoreEx.Results;
using Microsoft.EntityFrameworkCore;

namespace CoreEx.EntityFrameworkCore
{
    /// <summary>
    /// Encapsulates an Entity Framework query enabling all select-like capabilities on a specified <typeparamref name="TModel"/> automatically mapping to the resultant <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
    /// <typeparam name="TModel">The entity framework model <see cref="Type"/>.</typeparam>
    /// <remarks>Queried entities by default are <see cref="EntityFrameworkQueryableExtensions.AsNoTracking{TEntity}(IQueryable{TEntity})">not tracked</see>; this behavior can be overridden using <see cref="EfDbArgs.QueryNoTracking"/>.
    /// <para>Reminder: leverage <see cref="EntityFrameworkQueryableExtensions.IgnoreAutoIncludes{TEntity}(IQueryable{TEntity})"/> and then explictly include to improve performance where applicable.</para></remarks>
    public class EfDbQuery<T, TModel> where T : class, new() where TModel : class, new()
    {
        private readonly EfDbQuery<TModel> _query;

        /// <summary>
        /// Initializes a new instance of the <see cref="EfDbQuery{T, TModel}"/> struct.
        /// </summary>
        /// <param name="efdb">The <see cref="IEfDb"/>.</param>
        /// <param name="args">The <see cref="EfDbArgs"/>.</param>
        /// <param name="query">A function to modify the underlying <see cref="IQueryable{TModel}"/>.</param>
        internal EfDbQuery(IEfDb efdb, EfDbArgs args, Func<IQueryable<TModel>, IQueryable<TModel>>? query = null) => _query = new EfDbQuery<TModel>(efdb, args, query);

        /// <summary>
        /// Gets the <see cref="IEfDb"/>.
        /// </summary>
        public IEfDb EfDb => _query.EfDb;

        /// <summary>
        /// Gets the <see cref="EfDbArgs"/>.
        /// </summary>
        public EfDbArgs Args => _query.Args;

        /// <summary>
        /// Gets the <see cref="IMapper"/>.
        /// </summary>
        public IMapper Mapper => EfDb.Mapper;

        /// <summary>
        /// Gets the <see cref="PagingResult"/>.
        /// </summary>
        public PagingResult? Paging => _query.Paging;

        /// <summary>
        /// Adds <see cref="PagingArgs"/> to the query.
        /// </summary>
        /// <param name="paging">The <see cref="PagingArgs"/>.</param>
        /// <returns>The <see cref="EfDbQuery{T, TModel}"/> to suport fluent-style method-chaining.</returns>
        public EfDbQuery<T, TModel> WithPaging(PagingArgs? paging)
        {
            _query.WithPaging(paging);
            return this;
        }

        /// <summary>
        /// Adds <see cref="PagingArgs"/> to the query.
        /// </summary>
        /// <param name="skip">The specified number of elements in a sequence to bypass.</param>
        /// <param name="take">The specified number of contiguous elements from the start of a sequence.</param>
        /// <returns>The <see cref="EfDbQuery{T, TModel}"/> to suport fluent-style method-chaining.</returns>
        public EfDbQuery<T, TModel> WithPaging(long skip, long? take = null) => WithPaging(PagingArgs.CreateSkipAndTake(skip, take));

        /// <summary>
        /// Executes the query and maps.
        /// </summary>
        private async Task<Result<TResult>> ExecuteQueryAndMapAsync<TResult, TModelResult>(Func<CancellationToken, Task<Result<TModelResult>>> executeAsync, CancellationToken cancellationToken)
        {
            var result = await executeAsync(cancellationToken).ConfigureAwait(false);
            if (result.IsFailure)
                return Result<TResult>.Fail(result.Error);

            var val = result.Value == null ? default! : Mapper.Map<TResult>(result.Value, Mapping.OperationTypes.Get);
            return Args.CleanUpResult ? Cleaner.Clean(val) : val;
        }

        /// <summary>
        /// Selects a single item.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The single item.</returns>
        public async Task<T> SelectSingleAsync(CancellationToken cancellationToken = default)
            => (await SelectSingleWithResultAsync(cancellationToken).ConfigureAwait(false)).Value;

        /// <summary>
        /// Selects a single item with a <see cref="Result{T}"/>.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The single item.</returns>
        public async Task<Result<T>> SelectSingleWithResultAsync(CancellationToken cancellationToken = default)
        {
            var q = _query;
            return await ExecuteQueryAndMapAsync<T, TModel>(q.SelectSingleWithResultAsync, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Selects a single item or default.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The single item or default.</returns>
        public async Task<T?> SelectSingleOrDefaultAsync(CancellationToken cancellationToken = default)
            => (await SelectSingleOrDefaultWithResultAsync(cancellationToken).ConfigureAwait(false)).Value;

        /// <summary>
        /// Selects a single item or default with a <see cref="Result{T}"/>.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The single item or default.</returns>
        public async Task<Result<T?>> SelectSingleOrDefaultWithResultAsync(CancellationToken cancellationToken = default)
        {
            var q = _query;
            return await ExecuteQueryAndMapAsync<T?, TModel?>(q.SelectSingleOrDefaultWithResultAsync, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Selects first item.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The first item.</returns>
        public async Task<T> SelectFirstAsync(CancellationToken cancellationToken = default)
            => (await SelectFirstWithResultAsync(cancellationToken).ConfigureAwait(false)).Value;

        /// <summary>
        /// Selects first item with a <see cref="Result{T}"/>.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The first item.</returns>
        public async Task<Result<T>> SelectFirstWithResultAsync(CancellationToken cancellationToken = default)
        {
            var q = _query;
            return await ExecuteQueryAndMapAsync<T, TModel>(q.SelectFirstWithResultAsync, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Selects first item or default.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The single item or default.</returns>
        public async Task<T?> SelectFirstOrDefaultAsync(CancellationToken cancellationToken = default)
            => (await SelectFirstOrDefaultWithResultAsync(cancellationToken).ConfigureAwait(false)).Value;

        /// <summary>
        /// Selects first item or default with a <see cref="Result{T}"/>.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The single item or default.</returns>
        public async Task<Result<T?>> SelectFirstOrDefaultWithResultAsync(CancellationToken cancellationToken = default)
        {
            var q = _query;
            return await ExecuteQueryAndMapAsync<T?, TModel?>(q.SelectFirstOrDefaultWithResultAsync, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the query command creating a <typeparamref name="TCollResult"/>.
        /// </summary>
        /// <typeparam name="TCollResult">The <see cref="ICollectionResult{TColl, TItem}"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="TColl">The <see cref="ICollection{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The resulting <typeparamref name="TCollResult"/>.</returns>
        public async Task<TCollResult> SelectResultAsync<TCollResult, TColl>(CancellationToken cancellationToken = default) where TCollResult : ICollectionResult<TColl, T>, new() where TColl : ICollection<T>, new() => new TCollResult
        {
            Paging = _query.Paging,
            Items = (await SelectQueryWithResultInternalAsync<TColl>(nameof(SelectResultAsync), cancellationToken).ConfigureAwait(false)).Value
        };

        /// <summary>
        /// Executes the query command creating a <typeparamref name="TCollResult"/> with a <see cref="Result{T}"/>.
        /// </summary>
        /// <typeparam name="TCollResult">The <see cref="ICollectionResult{TColl, TItem}"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="TColl">The <see cref="ICollection{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The resulting <typeparamref name="TCollResult"/>.</returns>
        public async Task<Result<TCollResult>> SelectResultWithResultAsync<TCollResult, TColl>(CancellationToken cancellationToken = default) where TCollResult : ICollectionResult<TColl, T>, new() where TColl : ICollection<T>, new() => new TCollResult
        {
            Paging = _query.Paging,
            Items = (await SelectQueryWithResultInternalAsync<TColl>(nameof(SelectResultWithResultAsync), cancellationToken).ConfigureAwait(false)).Value
        };

        /// <summary>
        /// Executes the query command creating a resultant collection.
        /// </summary>
        /// <typeparam name="TColl">The collection <see cref="Type"/>.</typeparam>
        /// <returns>A resultant collection.</returns>
        public async Task<TColl> SelectQueryAsync<TColl>(CancellationToken cancellationToken = default) where TColl : ICollection<T>, new()
            => (await SelectQueryWithResultInternalAsync<TColl>(nameof(SelectQueryAsync), cancellationToken).ConfigureAwait(false)).Value;

        /// <summary>
        /// Executes the query command creating a resultant collection with a <see cref="Result{T}"/>.
        /// </summary>
        /// <typeparam name="TColl">The collection <see cref="Type"/>.</typeparam>
        /// <returns>A resultant collection.</returns>
        public Task<Result<TColl>> SelectQueryWithResultAsync<TColl>(CancellationToken cancellationToken = default) where TColl : ICollection<T>, new()
            => SelectQueryWithResultInternalAsync<TColl>(nameof(SelectQueryWithResultAsync), cancellationToken);

        /// <summary>
        /// Executes the query command creating a resultant collection with a <see cref="Result{T}"/> internal.
        /// </summary>
        private async Task<Result<TColl>> SelectQueryWithResultInternalAsync<TColl>(string memberName, CancellationToken cancellationToken) where TColl : ICollection<T>, new()
        {
            var coll = new TColl();
            return await SelectQueryWithResultInternalAsync(coll, memberName, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes a query adding to the passed collection.
        /// </summary>
        /// <typeparam name="TColl">The collection <see cref="Type"/>.</typeparam>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <param name="collection">The collection to add items to.</param>
        /// <returns>The <paramref name="collection"/>.</returns>
        public async Task<TColl> SelectQueryAsync<TColl>(TColl collection, CancellationToken cancellationToken = default) where TColl : ICollection<T>
            => (await SelectQueryWithResultInternalAsync(collection, nameof(SelectQueryAsync), cancellationToken).ConfigureAwait(false)).Value;

        /// <summary>
        /// Executes a query adding to the passed collection with a <see cref="Result{T}"/>.
        /// </summary>
        /// <typeparam name="TColl">The collection <see cref="Type"/>.</typeparam>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <param name="collection">The collection to add items to.</param>
        /// <returns>The <paramref name="collection"/>.</returns>
        public Task<Result<TColl>> SelectQueryWithResultAsync<TColl>(TColl collection, CancellationToken cancellationToken = default) where TColl : ICollection<T>
            => SelectQueryWithResultInternalAsync(collection, nameof(SelectQueryWithResultAsync), cancellationToken);

        /// <summary>
        /// Executes a query adding to the passed collection with a <see cref="Result{T}"/> internal.
        /// </summary>
        private async Task<Result<TColl>> SelectQueryWithResultInternalAsync<TColl>(TColl collection, string memberName, CancellationToken cancellationToken) where TColl : ICollection<T>
        {
            collection.ThrowIfNull(nameof(collection));

            var mapper = Mapper;

            var result = await _query.SelectQueryWithResultAsync(item =>
            {
                var val = mapper.Map<TModel, T>(item, OperationTypes.Get);
                if (val is null)
                    return new InvalidOperationException("Mapping from the EF model must not result in a null value.");

                collection.Add(val);
                return true;
            }, memberName, cancellationToken).ConfigureAwait(false);

            return result.ThenAs(() => collection);
        }
    }
}