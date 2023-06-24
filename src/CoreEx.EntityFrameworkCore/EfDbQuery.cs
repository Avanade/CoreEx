// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using CoreEx.Mapping;
using CoreEx.Results;
using Microsoft.EntityFrameworkCore;

namespace CoreEx.EntityFrameworkCore
{
    /// <summary>
    /// Encapsulates an Entity Framework query enabling all select-like capabilities.
    /// </summary>
    /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
    /// <typeparam name="TModel">The entity framework model <see cref="Type"/>.</typeparam>
    /// <remarks>Queried entities by default are <see cref="EntityFrameworkQueryableExtensions.AsNoTracking{TEntity}(IQueryable{TEntity})">not tracked</see>; this behavior can be overridden using <see cref="EfDbArgs.QueryNoTracking"/>.
    /// <para>Reminder: leverage <see cref="EntityFrameworkQueryableExtensions.IgnoreAutoIncludes{TEntity}(IQueryable{TEntity})"/> and then explictly include to improve performance where applicable.</para></remarks>
    public struct EfDbQuery<T, TModel> where T : class, new() where TModel : class, new()
    {
        private readonly Func<IQueryable<TModel>, IQueryable<TModel>>? _query;

        /// <summary>
        /// Initializes a new instance of the <see cref="EfDbQuery{T, TModel}"/> struct.
        /// </summary>
        /// <param name="efdb">The <see cref="IEfDb"/>.</param>
        /// <param name="args">The <see cref="EfDbArgs"/>.</param>
        /// <param name="query">A function to modify the underlying <see cref="IQueryable{TModel}"/>.</param>
        internal EfDbQuery(IEfDb efdb, EfDbArgs args, Func<IQueryable<TModel>, IQueryable<TModel>>? query = null)
        {
            EfDb = efdb ?? throw new ArgumentNullException(nameof(efdb));
            Args = args;
            _query = query;
            Paging = null;
        }

        /// <summary>
        /// Gets the <see cref="IEfDb"/>.
        /// </summary>
        public IEfDb EfDb { get; }

        /// <summary>
        /// Gets the <see cref="EfDbArgs"/>.
        /// </summary>
        public EfDbArgs Args { get; }

        /// <summary>
        /// Gets the <see cref="IMapper"/>.
        /// </summary>
        public IMapper Mapper => EfDb.Mapper;

        /// <summary>
        /// Gets the <see cref="PagingResult"/>.
        /// </summary>
        public PagingResult? Paging { get; private set; }

        /// <summary>
        /// Adds <see cref="PagingArgs"/> to the query.
        /// </summary>
        /// <param name="paging">The <see cref="PagingArgs"/>.</param>
        /// <returns>The <see cref="EfDbQuery{T, TModel}"/> to suport fluent-style method-chaining.</returns>
        public EfDbQuery<T, TModel> WithPaging(PagingArgs? paging)
        {
            Paging = paging == null ? null : (paging is PagingResult pr ? pr : new PagingResult(paging));
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
        /// Manages the DbContext and underlying query construction and lifetime.
        /// </summary>
        private async Task<Result<TResult?>> ExecuteQueryAsync<TResult>(Func<IQueryable<TModel>, CancellationToken, Task<TResult?>> executeAsync, CancellationToken cancellationToken) => await EfDb.Invoker.InvokeAsync(EfDb, EfDb, _query, Args, async (efdb, query, args, ct) =>
        {
            var dbSet = args.QueryNoTracking ? efdb.DbContext.Set<TModel>().AsNoTracking() : efdb.DbContext.Set<TModel>();
            return Result<TResult?>.Ok(await executeAsync((query == null) ? dbSet : query(dbSet), ct).ConfigureAwait(false));
        }, cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Executes the query and maps.
        /// </summary>
        private async Task<Result<T?>> ExecuteQueryAndMapAsync<TResult>(Func<IQueryable<TModel>, CancellationToken, Task<TResult?>> executeAsync, CancellationToken cancellationToken)
        {
            var result = await ExecuteQueryAsync(executeAsync, cancellationToken).ConfigureAwait(false);
            if (result.IsFailure)
                return Result<T?>.Fail(result.Error);

            var val = result.Value == null ? default! : Mapper.Map<T>(result.Value, Mapping.OperationTypes.Get);
            return Args.CleanUpResult ? Cleaner.Clean(val) : val;
        }

        /// <summary>
        /// Sets the paging from the <see cref="PagingArgs"/>.
        /// </summary>
        private static IQueryable<TModel> SetPaging(IQueryable<TModel> query, PagingArgs? paging)
        {
            if (paging == null)
                return query;

            var q = query;
            if (paging.Skip > 0)
                q = q.Skip((int)paging.Skip);

            return q.Take((int)(paging == null ? PagingArgs.DefaultTake : paging.Take));
        }

        /// <summary>
        /// Selects a single item.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The single item.</returns>
        public async Task<T> SelectSingleAsync(CancellationToken cancellationToken = default) => await SelectSingleWithResultAsync(cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Selects a single item with a <see cref="Result{T}"/>.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The single item.</returns>
        public async Task<Result<T>> SelectSingleWithResultAsync(CancellationToken cancellationToken = default) => (await ExecuteQueryAndMapAsync(async (q, ct) => await q.SingleAsync(ct).ConfigureAwait(false), cancellationToken).ConfigureAwait(false))!;

        /// <summary>
        /// Selects a single item or default.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The single item or default.</returns>
        public async Task<T?> SelectSingleOrDefaultAsync(CancellationToken cancellationToken = default) => await SelectSingleOrDefaultWithResultAsync(cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Selects a single item or default with a <see cref="Result{T}"/>.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The single item or default.</returns>
        public Task<Result<T?>> SelectSingleOrDefaultWithResultAsync(CancellationToken cancellationToken = default) => ExecuteQueryAndMapAsync((q, ct) => q.SingleOrDefaultAsync(ct), cancellationToken);

        /// <summary>
        /// Selects first item.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The first item.</returns>
        public async Task<T> SelectFirstAsync(CancellationToken cancellationToken = default) => await SelectFirstWithResultAsync(cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Selects first item with a <see cref="Result{T}"/>.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The first item.</returns>
        public async Task<Result<T>> SelectFirstWithResultAsync(CancellationToken cancellationToken = default) => (await ExecuteQueryAndMapAsync(async (q, ct) => await q.FirstAsync(ct).ConfigureAwait(false), cancellationToken).ConfigureAwait(false))!;

        /// <summary>
        /// Selects first item or default.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The single item or default.</returns>
        public async Task<T?> SelectFirstOrDefaultAsync(CancellationToken cancellationToken = default) => await SelectFirstOrDefaultWithResultAsync(cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Selects first item or default with a <see cref="Result{T}"/>.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The single item or default.</returns>
        public Task<Result<T?>> SelectFirstOrDefaultWithResultAsync(CancellationToken cancellationToken = default) => ExecuteQueryAndMapAsync((q, ct) => q.FirstOrDefaultAsync(ct), cancellationToken);

        /// <summary>
        /// Executes the query command creating a <typeparamref name="TCollResult"/>.
        /// </summary>
        /// <typeparam name="TCollResult">The <see cref="ICollectionResult{TColl, TItem}"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="TColl">The <see cref="ICollection{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The resulting <typeparamref name="TCollResult"/>.</returns>
        public async Task<TCollResult> SelectResultAsync<TCollResult, TColl>(CancellationToken cancellationToken = default) where TCollResult : ICollectionResult<TColl, T>, new() where TColl : ICollection<T>, new()
            => await SelectResultWithResultAsync<TCollResult, TColl>(cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Executes the query command creating a <typeparamref name="TCollResult"/> with a <see cref="Result{T}"/>.
        /// </summary>
        /// <typeparam name="TCollResult">The <see cref="ICollectionResult{TColl, TItem}"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="TColl">The <see cref="ICollection{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The resulting <typeparamref name="TCollResult"/>.</returns>
        public async Task<Result<TCollResult>> SelectResultWithResultAsync<TCollResult, TColl>(CancellationToken cancellationToken = default) where TCollResult : ICollectionResult<TColl, T>, new() where TColl : ICollection<T>, new() => new TCollResult
        {
            Paging = Paging,
            Items = await SelectQueryAsync<TColl>(cancellationToken).ConfigureAwait(false)
        };

        /// <summary>
        /// Executes the query command creating a resultant collection.
        /// </summary>
        /// <typeparam name="TColl">The collection <see cref="Type"/>.</typeparam>
        /// <returns>A resultant collection.</returns>
        public async Task<TColl> SelectQueryAsync<TColl>(CancellationToken cancellationToken = default) where TColl : ICollection<T>, new()
            => (await SelectQueryWithResultAsync<TColl>(cancellationToken).ConfigureAwait(false)).Value;

        /// <summary>
        /// Executes the query command creating a resultant collection with a <see cref="Result{T}"/>.
        /// </summary>
        /// <typeparam name="TColl">The collection <see cref="Type"/>.</typeparam>
        /// <returns>A resultant collection.</returns>
        public async Task<Result<TColl>> SelectQueryWithResultAsync<TColl>(CancellationToken cancellationToken = default) where TColl : ICollection<T>, new()
        {
            var coll = new TColl();
            return await SelectQueryWithResultAsync(coll, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes a query adding to the passed collection.
        /// </summary>
        /// <typeparam name="TColl">The collection <see cref="Type"/>.</typeparam>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <param name="collection">The collection to add items to.</param>
        /// <returns>The <paramref name="collection"/>.</returns>
        public async Task<TColl> SelectQueryAsync<TColl>(TColl collection, CancellationToken cancellationToken = default) where TColl : ICollection<T>
            => (await SelectQueryWithResultAsync(collection, cancellationToken).ConfigureAwait(false)).Value;

        /// <summary>
        /// Executes a query adding to the passed collection with a <see cref="Result{T}"/>.
        /// </summary>
        /// <typeparam name="TColl">The collection <see cref="Type"/>.</typeparam>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <param name="collection">The collection to add items to.</param>
        /// <returns>The <paramref name="collection"/>.</returns>
        public async Task<Result<TColl>> SelectQueryWithResultAsync<TColl>(TColl collection, CancellationToken cancellationToken = default) where TColl : ICollection<T>
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));

            var paging = Paging;
            var mapper = Mapper;
            var args = Args;

            return await ExecuteQueryAsync(async (query, ct) =>
            {
                var q = SetPaging(query, paging);

                await foreach (var item in q.AsAsyncEnumerable().WithCancellation(ct))
                {
                    var val = mapper.Map<TModel, T>(item, OperationTypes.Get) ?? throw new InvalidOperationException("Mapping from the EF model must not result in a null value.");
                    collection.Add(args.CleanUpResult ? Cleaner.Clean(val) : val);
                }

                if (paging != null && paging.IsGetCount)
                    paging.TotalCount = query.LongCount();

                return Result<TColl>.Ok(collection);
            }, cancellationToken);
        }
    }
}