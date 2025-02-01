// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using CoreEx.Results;
using Microsoft.EntityFrameworkCore;

namespace CoreEx.EntityFrameworkCore
{
    /// <summary>
    /// Encapsulates an Entity Framework query enabling all select-like capabilities on a specified <typeparamref name="TModel"/>.
    /// </summary>
    /// <typeparam name="TModel">The entity framework model <see cref="Type"/>.</typeparam>
    /// <remarks>Queried entities by default are <see cref="EntityFrameworkQueryableExtensions.AsNoTracking{TEntity}(IQueryable{TEntity})">not tracked</see>; this behavior can be overridden using <see cref="EfDbArgs.QueryNoTracking"/>.
    /// <para>Reminder: leverage <see cref="EntityFrameworkQueryableExtensions.IgnoreAutoIncludes{TEntity}(IQueryable{TEntity})"/> and then explictly include to improve performance where applicable.</para>
    /// <para>Automatic filtering is applied using the <see cref="EfDbArgs.WhereModelValid"/>.</para></remarks>
    public class EfDbQuery<TModel> where TModel : class, new()
    {
        private readonly Func<IQueryable<TModel>, IQueryable<TModel>>? _query;

        /// <summary>
        /// Initializes a new instance of the <see cref="EfDbQuery{T, TModel}"/> class.
        /// </summary>
        /// <param name="efdb">The <see cref="IEfDb"/>.</param>
        /// <param name="args">The <see cref="EfDbArgs"/>.</param>
        /// <param name="query">A function to modify the underlying <see cref="IQueryable{TModel}"/>.</param>
        internal EfDbQuery(IEfDb efdb, EfDbArgs args, Func<IQueryable<TModel>, IQueryable<TModel>>? query = null)
        {
            EfDb = efdb.ThrowIfNull(nameof(efdb));
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
        /// Gets the <see cref="PagingResult"/>.
        /// </summary>
        public PagingResult? Paging { get; private set; }

        /// <summary>
        /// Adds <see cref="PagingArgs"/> to the query.
        /// </summary>
        /// <param name="paging">The <see cref="PagingArgs"/>.</param>
        /// <returns>The <see cref="EfDbQuery{TModel}"/> to suport fluent-style method-chaining.</returns>
        public EfDbQuery<TModel> WithPaging(PagingArgs? paging)
        {
            Paging = paging == null ? null : (paging is PagingResult pr ? pr : new PagingResult(paging));
            return this;
        }

        /// <summary>
        /// Adds <see cref="PagingArgs"/> to the query.
        /// </summary>
        /// <param name="skip">The specified number of elements in a sequence to bypass.</param>
        /// <param name="take">The specified number of contiguous elements from the start of a sequence.</param>
        /// <returns>The <see cref="EfDbQuery{TModel}"/> to suport fluent-style method-chaining.</returns>
        public EfDbQuery<TModel> WithPaging(long skip, long? take = null) => WithPaging(PagingArgs.CreateSkipAndTake(skip, take));

        /// <summary>
        /// Manages the DbContext and underlying query construction and lifetime.
        /// </summary>
        private async Task<Result<TResult?>> ExecuteQueryAsync<TResult>(Func<IQueryable<TModel>, CancellationToken, Task<TResult?>> executeAsync, string memberName, CancellationToken cancellationToken) => await EfDb.Invoker.InvokeAsync(EfDb, EfDb, _query, Args, async (_, efdb, query, args, ct) =>
        {
            var dbSet = args.WhereModelValid(args.QueryNoTracking ? efdb.DbContext.Set<TModel>().AsNoTracking() : efdb.DbContext.Set<TModel>());
            return Result<TResult?>.Ok(await executeAsync((query == null) ? dbSet : query(dbSet), ct).ConfigureAwait(false));
        }, cancellationToken, memberName).ConfigureAwait(false);

        /// <summary>
        /// Executes the query and cleans.
        /// </summary>
        private async Task<Result<TModel?>> ExecuteQueryInternalAsync<TResult>(Func<IQueryable<TModel>, CancellationToken, Task<TResult?>> executeAsync, string memberName, CancellationToken cancellationToken)
        {
            var result = await ExecuteQueryAsync(executeAsync, memberName, cancellationToken).ConfigureAwait(false);
            if (result.IsFailure)
                return Result<TModel?>.Fail(result.Error);

            return result.Value is TModel model ? model : default;
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
        public async Task<TModel> SelectSingleAsync(CancellationToken cancellationToken = default)
            => (await ExecuteQueryInternalAsync(async (q, ct) => await q.SingleAsync(ct).ConfigureAwait(false), nameof(SelectSingleAsync), cancellationToken).ConfigureAwait(false))!;

        /// <summary>
        /// Selects a single item with a <see cref="Result{T}"/>.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The single item.</returns>
        public async Task<Result<TModel>> SelectSingleWithResultAsync(CancellationToken cancellationToken = default)
            => (await ExecuteQueryInternalAsync(async (q, ct) => await q.SingleAsync(ct).ConfigureAwait(false), nameof(SelectSingleWithResultAsync), cancellationToken).ConfigureAwait(false))!;

        /// <summary>
        /// Selects a single item or default.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The single item or default.</returns>
        public async Task<TModel?> SelectSingleOrDefaultAsync(CancellationToken cancellationToken = default)
            => await ExecuteQueryInternalAsync((q, ct) => q.SingleOrDefaultAsync(ct), nameof(SelectSingleOrDefaultAsync), cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Selects a single item or default with a <see cref="Result{T}"/>.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The single item or default.</returns>
        public Task<Result<TModel?>> SelectSingleOrDefaultWithResultAsync(CancellationToken cancellationToken = default)
            => ExecuteQueryInternalAsync((q, ct) => q.SingleOrDefaultAsync(ct), nameof(SelectSingleOrDefaultWithResultAsync), cancellationToken);

        /// <summary>
        /// Selects first item.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The first item.</returns>
        public async Task<TModel> SelectFirstAsync(CancellationToken cancellationToken = default)
            => (await ExecuteQueryInternalAsync(async (q, ct) => await q.FirstAsync(ct).ConfigureAwait(false), nameof(SelectFirstAsync), cancellationToken).ConfigureAwait(false))!;

        /// <summary>
        /// Selects first item with a <see cref="Result{T}"/>.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The first item.</returns>
        public async Task<Result<TModel>> SelectFirstWithResultAsync(CancellationToken cancellationToken = default)
            => (await ExecuteQueryInternalAsync(async (q, ct) => await q.FirstAsync(ct).ConfigureAwait(false), nameof(SelectFirstWithResultAsync), cancellationToken).ConfigureAwait(false))!;

        /// <summary>
        /// Selects first item or default.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The single item or default.</returns>
        public async Task<TModel?> SelectFirstOrDefaultAsync(CancellationToken cancellationToken = default)
            => await ExecuteQueryInternalAsync((q, ct) => q.FirstOrDefaultAsync(ct), nameof(SelectFirstOrDefaultAsync), cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Selects first item or default with a <see cref="Result{T}"/>.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The single item or default.</returns>
        public Task<Result<TModel?>> SelectFirstOrDefaultWithResultAsync(CancellationToken cancellationToken = default)
            => ExecuteQueryInternalAsync((q, ct) => q.FirstOrDefaultAsync(ct), nameof(SelectFirstOrDefaultWithResultAsync), cancellationToken);

        /// <summary>
        /// Executes the query command creating a <typeparamref name="TCollResult"/>.
        /// </summary>
        /// <typeparam name="TCollResult">The <see cref="ICollectionResult{TColl, TItem}"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="TColl">The <see cref="ICollection{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The resulting <typeparamref name="TCollResult"/>.</returns>
        public async Task<TCollResult> SelectResultAsync<TCollResult, TColl>(CancellationToken cancellationToken = default) where TCollResult : ICollectionResult<TColl, TModel>, new() where TColl : ICollection<TModel>, new() => new TCollResult
        {
            Paging = Paging,
            Items = (await SelectQueryWithResultInternalAsync<TColl>(nameof(SelectResultAsync), cancellationToken).ConfigureAwait(false)).Value
        };

        /// <summary>
        /// Executes the query command creating a <typeparamref name="TCollResult"/> with a <see cref="Result{T}"/>.
        /// </summary>
        /// <typeparam name="TCollResult">The <see cref="ICollectionResult{TColl, TItem}"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="TColl">The <see cref="ICollection{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The resulting <typeparamref name="TCollResult"/>.</returns>
        public async Task<Result<TCollResult>> SelectResultWithResultAsync<TCollResult, TColl>(CancellationToken cancellationToken = default) where TCollResult : ICollectionResult<TColl, TModel>, new() where TColl : ICollection<TModel>, new() => new TCollResult
        {
            Paging = Paging,
            Items = (await SelectQueryWithResultInternalAsync<TColl>(nameof(SelectResultWithResultAsync), cancellationToken).ConfigureAwait(false)).Value
        };

        /// <summary>
        /// Executes the query command creating a resultant collection.
        /// </summary>
        /// <typeparam name="TColl">The collection <see cref="Type"/>.</typeparam>
        /// <returns>A resultant collection.</returns>
        public async Task<TColl> SelectQueryAsync<TColl>(CancellationToken cancellationToken = default) where TColl : ICollection<TModel>, new()
            => (await SelectQueryWithResultInternalAsync<TColl>(nameof(SelectQueryAsync), cancellationToken).ConfigureAwait(false)).Value;

        /// <summary>
        /// Executes the query command creating a resultant collection with a <see cref="Result{T}"/>.
        /// </summary>
        /// <typeparam name="TColl">The collection <see cref="Type"/>.</typeparam>
        /// <returns>A resultant collection.</returns>
        public Task<Result<TColl>> SelectQueryWithResultAsync<TColl>(CancellationToken cancellationToken = default) where TColl : ICollection<TModel>, new()
            => SelectQueryWithResultInternalAsync<TColl>(nameof(SelectQueryWithResultAsync), cancellationToken);

        /// <summary>
        /// Executes the query command creating a resultant collection with a <see cref="Result{T}"/> internal.
        /// </summary>
        private async Task<Result<TColl>> SelectQueryWithResultInternalAsync<TColl>(string memberName, CancellationToken cancellationToken) where TColl : ICollection<TModel>, new()
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
        public async Task<TColl> SelectQueryAsync<TColl>(TColl collection, CancellationToken cancellationToken = default) where TColl : ICollection<TModel>
            => (await SelectQueryWithResultInternalAsync(collection, nameof(SelectQueryAsync), cancellationToken).ConfigureAwait(false)).Value;

        /// <summary>
        /// Executes a query adding to the passed collection with a <see cref="Result{T}"/>.
        /// </summary>
        /// <typeparam name="TColl">The collection <see cref="Type"/>.</typeparam>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <param name="collection">The collection to add items to.</param>
        /// <returns>The <paramref name="collection"/>.</returns>
        public Task<Result<TColl>> SelectQueryWithResultAsync<TColl>(TColl collection, CancellationToken cancellationToken = default) where TColl : ICollection<TModel>
            => SelectQueryWithResultInternalAsync(collection, nameof(SelectQueryWithResultAsync), cancellationToken);

        /// <summary>
        /// Executes a query adding to the passed collection with a <see cref="Result{T}"/> internal.
        /// </summary>
        private async Task<Result<TColl>> SelectQueryWithResultInternalAsync<TColl>(TColl collection, string memberName, CancellationToken cancellationToken) where TColl : ICollection<TModel>
        {
            collection.ThrowIfNull(nameof(collection));

            var result = await SelectQueryWithResultAsync(item =>
            {
                collection.Add(item);
                return true;
            }, memberName, cancellationToken).ConfigureAwait(false);

            return result.ThenAs(() => collection);
        }

        /// <summary>
        /// Executes a query with per <paramref name="item"/> processing
        /// </summary>
        /// <param name="item">The item function.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <remarks>The <paramref name="item"/> returns a <see cref="bool"/> which indicates whether to continue enumerating. A <c>true</c> indicates
        /// to continue, whilst a <c>false</c> indicates a stop. Where <see cref="Result{T}.IsFailure"/> then the error will be returned as-is.</remarks>
        public async Task SelectQueryAsync(Func<TModel, bool> item, CancellationToken cancellationToken = default)
            => (await SelectQueryWithResultAsync(model => item.ThrowIfNull(nameof(item))(model), nameof(SelectQueryAsync), cancellationToken).ConfigureAwait(false)).ThrowOnError();

        /// <summary>
        /// Executes a query with per <paramref name="item"/> processing with a <see cref="Result{T}"/>.
        /// </summary>
        /// <param name="item">The item function.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="Result"/>.</returns>
        /// <remarks>The <paramref name="item"/> returns a <see cref="Result{T}"/> where the corresponding <see cref="Result{T}.Value"/> <see cref="bool"/> indicates whether to continue enumerating. A <c>true</c> indicates
        /// to continue, whilst a <c>false</c> indicates a stop. Where <see cref="Result{T}.IsFailure"/> then the error will be returned as-is.</remarks>
        public Task<Result> SelectQueryWithResultAsync(Func<TModel, Result<bool>> item, CancellationToken cancellationToken = default)
            => SelectQueryWithResultAsync(item.ThrowIfNull(nameof(item)), nameof(SelectQueryWithResultAsync), cancellationToken);

        /// <summary>
        /// Executes a query with per <paramref name="item"/> processing.
        /// </summary>
        /// <param name="item">The item function.</param>
        /// <param name="memberName">The member name.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="Result"/>.</returns>
        /// <remarks>The <paramref name="item"/> returns a <see cref="Result{T}"/> where the corresponding <see cref="Result{T}.Value"/> <see cref="bool"/> indicates whether to continue enumerating. A <c>true</c> indicates
        /// to continue, whilst a <c>false</c> indicates a stop. Where <see cref="Result{T}.IsFailure"/> then the error will be returned as-is.</remarks>
        internal async Task<Result> SelectQueryWithResultAsync(Func<TModel, Result<bool>> item, string memberName, CancellationToken cancellationToken)
        {
            var result = await ExecuteQueryAsync<bool>(async (query, ct) =>
            {
                var q = SetPaging(query, Paging);
                Result<bool> r = Result<bool>.None;

                await foreach (var model in q.AsAsyncEnumerable().WithCancellation(ct))
                {
                    r = item(model);
                    if (r.IsFailure || !r.Value)
                        break;
                }

                if (r.IsSuccess && Paging != null && Paging.IsGetCount)
                    Paging.TotalCount = query.LongCount();

                return r;
            }, memberName, cancellationToken);

            return result.AsResult();
        }
    }
}