// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Database
{
    /// <summary>
    /// Provides extended database command capabilities.
    /// </summary>
    /// <remarks>As the underlying <see cref="DbCommand"/> implements <see cref="IDisposable"/> this is only created (and automatically disposed) where executing the command proper.</remarks>
    public sealed class DatabaseCommand : IDatabaseParameters<DatabaseCommand>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseCommand"/> class.
        /// </summary>
        /// <param name="db">The <see cref="IDatabase"/>.</param>
        /// <param name="commandType">The <see cref="System.Data.CommandType"/>.</param>
        /// <param name="commandText">The command text.</param>
        public DatabaseCommand(IDatabase db, CommandType commandType, string commandText)
        {
            Database = db ?? throw new ArgumentNullException(nameof(db));
            Parameters = new DatabaseParameterCollection(db);
            CommandType = commandType;
            CommandText = commandText ?? throw new ArgumentNullException(nameof(commandText));
        }

        /// <summary>
        /// Gets the underlying <see cref="IDatabase"/>.
        /// </summary>
        public IDatabase Database { get; }

        /// <inheritdoc/>
        public DatabaseParameterCollection Parameters { get; }

        /// <summary>
        /// Gets the <see cref="System.Data.CommandType"/>.
        /// </summary>
        public CommandType CommandType { get; }

        /// <summary>
        /// Gets the command text.
        /// </summary>
        public string CommandText { get; }

        /// <summary>
        /// Creates the corresponding <see cref="DbCommand"/>.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="DbCommand"/>.</returns>
        private async Task<DbCommand> CreateDbCommandAsync(CancellationToken cancellationToken = default)
        {
            var cmd = (await Database.GetConnectionAsync(cancellationToken).ConfigureAwait(false)).CreateCommand();
            cmd.CommandType = CommandType;
            cmd.CommandText = CommandText;
            cmd.Parameters.AddRange(Parameters.ToArray());
            return cmd;
        }

        /// <summary>
        /// Executes a multi-dataset query command with one or more <see cref="IMultiSetArgs"/>.
        /// </summary>
        /// <param name="multiSetArgs">One or more <see cref="IMultiSetArgs"/>.</param>
        /// <remarks>The number of <see cref="IMultiSetArgs"/> specified must match the number of returned datasets. A null dataset indicates to ignore (skip) a dataset.</remarks>
        public Task SelectMultiSetAsync(params IMultiSetArgs[] multiSetArgs) => SelectMultiSetAsync(multiSetArgs, default);

        /// <summary>
        /// Executes a multi-dataset query command with one or more <see cref="IMultiSetArgs"/>.
        /// </summary>
        /// <param name="multiSetArgs">One or more <see cref="IMultiSetArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <remarks>The number of <see cref="IMultiSetArgs"/> specified must match the number of returned datasets. A null dataset indicates to ignore (skip) a dataset.</remarks>
        public Task SelectMultiSetAsync(IEnumerable<IMultiSetArgs> multiSetArgs, CancellationToken cancellationToken = default)
        {
            var multiSetList = multiSetArgs?.ToList() ?? null;
            if (multiSetList == null || multiSetList.Count == 0)
                throw new ArgumentException($"At least one {nameof(IMultiSetArgs)} must be supplied.", nameof(multiSetArgs));

            return Database.Invoker.InvokeAsync(Database, multiSetArgs, multiSetList, async (multiSetArgs, multiSetList, ct) =>
            {
                // Create and execute the command. 
                using var cmd = await CreateDbCommandAsync(ct).ConfigureAwait(false);
                using var dr = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);

                // Iterate through the dataset(s).
                var index = 0;
                var records = 0;
                IMultiSetArgs? multiSetArg = null;
                do
                {
                    if (index >= multiSetList.Count)
                        throw new InvalidOperationException($"{nameof(SelectMultiSetAsync)} has returned more record sets than expected ({multiSetList.Count}).");

                    if (multiSetList[index] != null)
                    {
                        records = 0;
                        multiSetArg = multiSetList[index];
                        while (await dr.ReadAsync(ct).ConfigureAwait(false))
                        {
                            records++;
                            if (multiSetArg.MaxRows.HasValue && records > multiSetArg.MaxRows.Value)
                                throw new InvalidOperationException($"{nameof(SelectMultiSetAsync)} (msa[{index}]) has returned more records than expected ({multiSetArg.MaxRows.Value}).");

                            multiSetArg.DatasetRecord(new DatabaseRecord(Database, dr));
                        }

                        if (records < multiSetArg.MinRows)
                            throw new InvalidOperationException($"{nameof(SelectMultiSetAsync)}  (msa[{index}]) has returned less records ({records}) than expected ({multiSetArg.MinRows}).");

                        if (records == 0 && multiSetArg.StopOnNull)
                            return;

                        multiSetArg.InvokeResult();
                    }

                    index++;
                } while (dr.NextResult());

                if (index < multiSetList.Count && !multiSetList[index].StopOnNull)
                    throw new InvalidOperationException($"{nameof(SelectMultiSetAsync)}  has returned less ({index}) record sets than expected ({multiSetList.Count}).");
            }, cancellationToken);
        }

        /// <summary>
        /// Executes a multi-dataset query command with one or more <see cref="IMultiSetArgs"/> that supports <paramref name="paging"/>.
        /// </summary>
        /// <param name="paging">The <see cref="PagingArgs"/> or <see cref="PagingResult"/> to add to the <see cref="Parameters"/>.</param>
        /// <param name="multiSetArgs">One or more <see cref="IMultiSetArgs"/>.</param>
        /// <remarks>The number of <see cref="IMultiSetArgs"/> specified must match the number of returned datasets. A null dataset indicates to ignore (skip) a dataset.</remarks>
        public Task SelectMultiSetAsync(PagingArgs? paging, params IMultiSetArgs[] multiSetArgs) => SelectMultiSetAsync(paging, multiSetArgs, default);

        /// <summary>
        /// Executes a multi-dataset query command with one or more <see cref="IMultiSetArgs"/> that supports <paramref name="paging"/>.
        /// </summary>
        /// <param name="paging">The <see cref="PagingArgs"/> or <see cref="PagingResult"/> to add to the <see cref="Parameters"/>.</param>
        /// <param name="multiSetArgs">One or more <see cref="IMultiSetArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <remarks>The number of <see cref="IMultiSetArgs"/> specified must match the number of returned datasets. A null dataset indicates to ignore (skip) a dataset.</remarks>
        public async Task SelectMultiSetAsync(PagingArgs? paging, IEnumerable<IMultiSetArgs> multiSetArgs, CancellationToken cancellationToken = default)
        {
            Parameters.PagingParams(paging);

            var rv = await SelectMultiSetWithValueAsync(multiSetArgs, cancellationToken).ConfigureAwait(false);
            if (paging is PagingResult pr && pr.IsGetCount && rv >= 0)
                pr.TotalCount = rv;
        }

        /// <summary>
        /// Executes a multi-dataset query command with one or more <see cref="IMultiSetArgs"/>; whilst also outputing the resulting <see cref="int"/> <see cref="ParameterDirection.ReturnValue"/>.
        /// </summary>
        /// <param name="multiSetArgs">One or more <see cref="IMultiSetArgs"/>.</param>
        /// <returns>The resultant return value.</returns>
        /// <remarks>The number of <see cref="IMultiSetArgs"/> specified must match the number of returned datasets. A null dataset indicates to ignore (skip) a dataset.</remarks>
        public Task<int> SelectMultiSetWithValueAsync(params IMultiSetArgs[] multiSetArgs) => SelectMultiSetWithValueAsync(multiSetArgs, default);

        /// <summary>
        /// Executes a multi-dataset query command with one or more <see cref="IMultiSetArgs"/>; whilst also outputing the resulting <see cref="int"/> <see cref="ParameterDirection.ReturnValue"/>.
        /// </summary>
        /// <param name="multiSetArgs">One or more <see cref="IMultiSetArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The resultant return value.</returns>
        /// <remarks>The number of <see cref="IMultiSetArgs"/> specified must match the number of returned datasets. A null dataset indicates to ignore (skip) a dataset.</remarks>
        public async Task<int> SelectMultiSetWithValueAsync(IEnumerable<IMultiSetArgs> multiSetArgs, CancellationToken cancellationToken = default)
        {
            var rvp = Parameters.AddReturnValueParameter();
            await SelectMultiSetAsync(multiSetArgs, cancellationToken).ConfigureAwait(false);
            return rvp.Value == null ? -1 : (int)rvp.Value;
        }

        /// <summary>
        /// Executes a non-query command.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The number of rows affected.</returns>
        public Task<int> NonQueryAsync(CancellationToken cancellationToken = default) => NonQueryAsync(null, cancellationToken);

        /// <summary>
        /// Executes a non-query command.
        /// </summary>
        /// <param name="parameters">The post-execution delegate to enable parameter access.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The number of rows affected.</returns>
        public Task<int> NonQueryAsync(Action<DbParameterCollection>? parameters, CancellationToken cancellationToken = default) => Database.Invoker.InvokeAsync(Database, parameters, async (parameters, ct) =>
        {
            using var cmd = await CreateDbCommandAsync(ct).ConfigureAwait(false);
            parameters?.Invoke(cmd.Parameters);
            var result = await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
            return result;
        }, cancellationToken);

        /// <summary>
        /// Executes the query and returns the first column of the first row in the result set returned by the query.
        /// </summary>
        /// <typeparam name="T">The result <see cref="Type"/>.</typeparam>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The value of the first column of the first row in the result set.</returns>
        public Task<T> ScalarAsync<T>(CancellationToken cancellationToken = default) => ScalarAsync<T>(null, cancellationToken);

        /// <summary>
        /// Executes the query and returns the first column of the first row in the result set returned by the query.
        /// </summary>
        /// <typeparam name="T">The result <see cref="Type"/>.</typeparam>
        /// <param name="parameters">The post-execution delegate to enable parameter access.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The value of the first column of the first row in the result set.</returns>
        public Task<T> ScalarAsync<T>(Action<DbParameterCollection>? parameters, CancellationToken cancellationToken = default) => Database.Invoker.InvokeAsync(Database, parameters, async (parameters, ct) =>
        {
            using var cmd = await CreateDbCommandAsync(ct).ConfigureAwait(false);
            parameters?.Invoke(cmd.Parameters);
            var result = await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false);
            T value = result is null ? default! : result is DBNull ? default! : (T)result;
            return value;
        }, cancellationToken);

        /// <summary>
        /// Selects none or more items from the first result set using a <paramref name="mapper"/>.
        /// </summary>
        /// <typeparam name="T">The item <see cref="Type"/>.</typeparam>
        /// <param name="mapper">The <see cref="IDatabaseMapper{T}"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The item sequence.</returns>
        public async Task<IEnumerable<T>> SelectQueryAsync<T>(IDatabaseMapper<T> mapper, CancellationToken cancellationToken = default)
        {
            var coll = new List<T>();
            await SelectQueryAsync(coll, mapper, cancellationToken).ConfigureAwait(false);
            return coll;
        }

        /// <summary>
        /// Selects none or more items from the first result set.
        /// </summary>
        /// <typeparam name="T">The item <see cref="Type"/>.</typeparam>
        /// <param name="func">The <see cref="DatabaseRecord"/> mapping function.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The resulting set.</returns>
        public async Task<IEnumerable<T>> SelectQueryAsync<T>(Func<DatabaseRecord, T> func, CancellationToken cancellationToken = default)
        {
            var coll = new List<T>();
            await SelectQueryAsync(coll, func, cancellationToken);
            return coll;
        }

        /// <summary>
        /// Selects none or more items from the first result set and adds to the <paramref name="collection"/>.
        /// </summary>
        /// <typeparam name="T">The item <see cref="Type"/>.</typeparam>
        /// <typeparam name="TColl">The collection <see cref="Type"/>.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="mapper">The <see cref="IDatabaseMapper{T}"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public async Task SelectQueryAsync<T, TColl>(TColl collection, IDatabaseMapper<T> mapper, CancellationToken cancellationToken = default) where TColl : ICollection<T>
            => await SelectInternalAsync(collection, mapper, false, false, cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Selects none or more items from the first result set and adds to the <paramref name="collection"/>.
        /// </summary>
        /// <typeparam name="T">The item <see cref="Type"/>.</typeparam>
        /// <typeparam name="TColl">The collection <see cref="Type"/>.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="func">The <see cref="DatabaseRecord"/> mapping function.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public async Task SelectQueryAsync<T, TColl>(TColl collection, Func<DatabaseRecord, T> func, CancellationToken cancellationToken = default) where TColl : ICollection<T>
            => await SelectInternalAsync(collection, new DatabaseRecordMapper<T>(func), false, false, cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Selects a single item.
        /// </summary>
        /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
        /// <param name="mapper">The <see cref="IDatabaseMapper{T}"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The single item.</returns>
        public async Task<T> SelectSingleAsync<T>(IDatabaseMapper<T> mapper, CancellationToken cancellationToken = default)
        {
            T item = await SelectSingleFirstAsync(mapper, true, cancellationToken).ConfigureAwait(false);
            if (Comparer<T>.Default.Compare(item, default!) == 0)
                throw new InvalidOperationException("SelectSingle request has not returned a row.");

            return item;
        }

        /// <summary>
        /// Selects a single item or default.
        /// </summary>
        /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
        /// <param name="mapper">The <see cref="IDatabaseMapper{T}"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The single item or default.</returns>
        public async Task<T?> SelectSingleOrDefaultAsync<T>(IDatabaseMapper<T> mapper, CancellationToken cancellationToken = default) => await SelectSingleFirstAsync(mapper, false, cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Selects first item.
        /// </summary>
        /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
        /// <param name="mapper">The <see cref="IDatabaseMapper{T}"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The single item.</returns>
        public async Task<T> SelectFirstAsync<T>(IDatabaseMapper<T> mapper, CancellationToken cancellationToken = default)
        {
            T item = await SelectSingleFirstAsync(mapper, false, cancellationToken).ConfigureAwait(false);
            if (Comparer<T>.Default.Compare(item, default!) == 0)
                throw new InvalidOperationException("SelectFirst request has not returned a row.");

            return item;
        }

        /// <summary>
        /// Selects first item or default.
        /// </summary>
        /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
        /// <param name="mapper">The <see cref="IDatabaseMapper{T}"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The single item or default.</returns>
        public async Task<T?> SelectFirstOrDefaultAsync<T>(IDatabaseMapper<T> mapper, CancellationToken cancellationToken = default) => await SelectSingleFirstAsync(mapper, false, cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Select first row result only (where exists).
        /// </summary>
        private async Task<T> SelectSingleFirstAsync<T>(IDatabaseMapper<T> mapper, bool throwWhereMulti, CancellationToken cancellationToken)
        {
            var coll = new List<T>();
            await SelectInternalAsync(coll, mapper, throwWhereMulti, true, cancellationToken).ConfigureAwait(false);
            return coll.Count == 0 ? default! : coll[0];
        }

        /// <summary>
        /// Select the rows from the query.
        /// </summary>
        private async Task SelectInternalAsync<T, TColl>(TColl coll, IDatabaseMapper<T> mapper, bool throwWhereMulti, bool stopAfterOneRow, CancellationToken cancellationToken) where TColl : ICollection<T>
        {
            if (mapper == null)
                throw new ArgumentNullException(nameof(mapper));

            await Database.Invoker.InvokeAsync(Database, mapper, throwWhereMulti, stopAfterOneRow, async (mapper, throwWhereMulti, stopAfterOneRow, ct) =>
            {
                int i = 0;

                using var cmd = await CreateDbCommandAsync(ct).ConfigureAwait(false);
                using var dr = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);

                while (await dr.ReadAsync(ct).ConfigureAwait(false))
                {
                    if (++i == 2)
                    {
                        if (throwWhereMulti)
                            throw new InvalidOperationException("SelectSingle request has returned more than one row.");

                        if (stopAfterOneRow)
                            return;
                    }

                    var val = mapper.MapFromDb(new DatabaseRecord(Database, dr));
                    if (val == null)
                        throw new InvalidOperationException("A null must not be returned from the mapper.");

                    coll.Add(val);
                    if (!throwWhereMulti && stopAfterOneRow)
                        return;
                }
            }, cancellationToken).ConfigureAwait(false);
        }
    }
}