// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Database.Extended
{
    /// <summary>
    /// Encapsulates a SQL query enabling select-like capabilities.
    /// </summary>
    /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
    public class DatabaseQuery<T> : IDatabaseParameters<DatabaseQuery<T>> where T : class, new()
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseQuery{T}"/> class.
        /// </summary>
        /// <param name="command">The <see cref="DatabaseCommand"/>.</param>
        /// <param name="args">The <see cref="DatabaseArgs"/>.</param>
        /// <param name="queryParams">The query <see cref="DatabaseParameterCollection"/> action to enable additional filtering.</param>
        internal DatabaseQuery(DatabaseCommand command, DatabaseArgs args, Action<DatabaseParameterCollection>? queryParams)
        {
            Command = command ?? throw new ArgumentNullException(nameof(command));
            Parameters = new DatabaseParameterCollection(Database);
            QueryArgs = args;
            Mapper = (IDatabaseMapper<T>)args.Mapper;

            queryParams?.Invoke(Parameters);
        }

        /// <summary>
        /// Gets the <see cref="DatabaseCommand"/>.
        /// </summary>
        public DatabaseCommand Command { get; }

        /// <inheritdoc/>
        public IDatabase Database => Command.Database;

        /// <inheritdoc/>
        public DatabaseParameterCollection Parameters { get; }

        /// <summary>
        /// Gets the <see cref="DatabaseArgs"/>.
        /// </summary>
        public DatabaseArgs QueryArgs { get; }

        /// <summary>
        /// Gets the <see cref="IDatabaseMapper{TSource}"/>.
        /// </summary>
        public IDatabaseMapper<T> Mapper { get; }

        /// <summary>
        /// Gets the <see cref="PagingResult"/>.
        /// </summary>
        public PagingResult? Paging { get; private set; }

        /// <summary>
        /// Adds <see cref="PagingArgs"/> to the query.
        /// </summary>
        /// <param name="paging">The <see cref="PagingArgs"/>.</param>
        /// <returns>The <see cref="DatabaseQuery{T}"/> to suport fluent-style method-chaining.</returns>
        public DatabaseQuery<T> WithPaging(PagingArgs paging)
        {
            Paging = paging == null ? null : (paging is PagingResult pr ? pr : new PagingResult(paging));
            return this;
        }

        /// <summary>
        /// Adds <see cref="PagingArgs"/> to the query.
        /// </summary>
        /// <param name="skip">The specified number of elements in a sequence to bypass.</param>
        /// <param name="take">The specified number of contiguous elements from the start of a sequence.</param>
        /// <returns>The <see cref="DatabaseQuery{T}"/> to suport fluent-style method-chaining.</returns>
        public DatabaseQuery<T> WithPaging(long skip, long? take = null) => WithPaging(PagingArgs.CreateSkipAndTake(skip, take));

        /// <summary>
        /// Selects a single item.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The single item.</returns>
        public Task<T> SelectSingleAsync(CancellationToken cancellationToken = default) => SelectWrapperAsync((cmd, ct) => cmd.SelectSingleAsync(Mapper, ct), cancellationToken);

        /// <summary>
        /// Selects a single item or default.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The single item or default.</returns>
        public Task<T?> SelectSingleOrDefaultAsync(CancellationToken cancellationToken = default) => SelectWrapperAsync((cmd, ct) => cmd.SelectSingleOrDefaultAsync(Mapper, ct), cancellationToken);

        /// <summary>
        /// Selects first item.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The first item.</returns>
        public Task<T> SelectFirstAsync(CancellationToken cancellationToken = default) => SelectWrapperAsync((cmd, ct) => cmd.SelectFirstAsync(Mapper, ct), cancellationToken);

        /// <summary>
        /// Selects first item or default.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The single item or default.</returns>
        public Task<T?> SelectFirstOrDefaultAsync(CancellationToken cancellationToken = default) => SelectWrapperAsync((cmd, ct) => cmd.SelectFirstOrDefaultAsync(Mapper, ct), cancellationToken);

        /// <summary>
        /// Executes the query command creating a <typeparamref name="TCollResult"/>.
        /// </summary>
        /// <typeparam name="TCollResult">The <see cref="ICollectionResult{TColl, TItem}"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="TColl">The <see cref="ICollection{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The resulting <typeparamref name="TCollResult"/>.</returns>
        public async Task<TCollResult> SelectResultAsync<TCollResult, TColl>(CancellationToken cancellationToken = default) where TCollResult : ICollectionResult<TColl, T>, new() where TColl : ICollection<T>, new() => new TCollResult
        {
            Paging = Paging,
            Collection = await SelectQueryAsync<TColl>(cancellationToken).ConfigureAwait(false)
        };

        /// <summary>
        /// Executes the query command creating a resultant collection.
        /// </summary>
        /// <typeparam name="TColl">The collection <see cref="Type"/>.</typeparam>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>A resultant collection.</returns>
        public async Task<TColl> SelectQueryAsync<TColl>(CancellationToken cancellationToken = default) where TColl : ICollection<T>, new()
        {
            var coll = new TColl();
            return await SelectWrapperAsync(async (cmd, ct) =>
            {
                await cmd.SelectQueryAsync(coll, Mapper, ct).ConfigureAwait(false);
                return coll;
            }, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes a query adding to the passed collection.
        /// </summary>
        /// <typeparam name="TColl">The collection <see cref="Type"/>.</typeparam>
        /// <param name="coll">The collection to add items to.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public Task SelectQueryAsync<TColl>(TColl coll, CancellationToken cancellationToken = default) where TColl : ICollection<T>
        {
            return SelectWrapperAsync(async (cmd, ct) =>
            {
                await cmd.SelectQueryAsync(coll, Mapper, ct).ConfigureAwait(false);
                return coll;
            }, cancellationToken);
        }

        /// <summary>
        /// Wraps the select query to perform standard logic.
        /// </summary>
        private async Task<TResult> SelectWrapperAsync<TResult>(Func<DatabaseCommand, CancellationToken, Task<TResult>> func, CancellationToken cancellationToken)
        {
            var rvp = Paging != null && Paging.IsGetCount ? Parameters.AddReturnValueParameter() : null;
            var cmd = Command.Params(Parameters).PagingParam(Paging);

            var res = await func(cmd, cancellationToken).ConfigureAwait(false);

            if (rvp != null && rvp.Value != null)
                Paging!.TotalCount = (long)rvp.Value;

            return res;
        }
    }
}