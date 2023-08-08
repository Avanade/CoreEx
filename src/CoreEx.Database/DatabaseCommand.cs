// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using CoreEx.Results;
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
        public async Task SelectMultiSetAsync(params IMultiSetArgs[] multiSetArgs) 
            => (await SelectMultiSetWithResultInternalAsync(multiSetArgs, nameof(SelectMultiSetAsync), default).ConfigureAwait(false)).ThrowOnError();

        /// <summary>
        /// Executes a multi-dataset query command with one or more <see cref="IMultiSetArgs"/> with a <see cref="Result{T}"/>.
        /// </summary>
        /// <param name="multiSetArgs">One or more <see cref="IMultiSetArgs"/>.</param>
        /// <remarks>The number of <see cref="IMultiSetArgs"/> specified must match the number of returned datasets. A null dataset indicates to ignore (skip) a dataset.</remarks>
        public Task<Result> SelectMultiSetWithResultAsync(params IMultiSetArgs[] multiSetArgs) 
            => SelectMultiSetWithResultInternalAsync(multiSetArgs, nameof(SelectMultiSetWithResultAsync), default);

        /// <summary>
        /// Executes a multi-dataset query command with one or more <see cref="IMultiSetArgs"/>.
        /// </summary>
        /// <param name="multiSetArgs">One or more <see cref="IMultiSetArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <remarks>The number of <see cref="IMultiSetArgs"/> specified must match the number of returned datasets. A null dataset indicates to ignore (skip) a dataset.</remarks>
        public async Task SelectMultiSetAsync(IEnumerable<IMultiSetArgs> multiSetArgs, CancellationToken cancellationToken = default)
            => (await SelectMultiSetWithResultInternalAsync(multiSetArgs, nameof(SelectMultiSetAsync), cancellationToken).ConfigureAwait(false)).ThrowOnError();

        /// <summary>
        /// Executes a multi-dataset query command with one or more <see cref="IMultiSetArgs"/> with a <see cref="Result{T}"/>.
        /// </summary>
        /// <param name="multiSetArgs">One or more <see cref="IMultiSetArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <remarks>The number of <see cref="IMultiSetArgs"/> specified must match the number of returned datasets. A null dataset indicates to ignore (skip) a dataset.</remarks>
        public Task<Result> SelectMultiSetWithResultAsync(IEnumerable<IMultiSetArgs> multiSetArgs, CancellationToken cancellationToken = default)
            => SelectMultiSetWithResultInternalAsync(multiSetArgs, nameof(SelectMultiSetWithResultAsync), cancellationToken);

        /// <summary>
        /// Executes a multi-dataset query command with one or more <see cref="IMultiSetArgs"/> with a <see cref="Result{T}"/> internal.
        /// </summary>
        private Task<Result> SelectMultiSetWithResultInternalAsync(IEnumerable<IMultiSetArgs> multiSetArgs, string memberName, CancellationToken cancellationToken = default)
        {
            var multiSetList = multiSetArgs?.ToList() ?? null;
            if (multiSetList == null || multiSetList.Count == 0)
                throw new ArgumentException($"At least one {nameof(IMultiSetArgs)} must be supplied.", nameof(multiSetArgs));

            return Database.Invoker.InvokeAsync(Database, multiSetArgs, multiSetList, async (_, multiSetArgs, multiSetList, ct) =>
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
                        return Result.Fail(new InvalidOperationException($"{nameof(SelectMultiSetAsync)} has returned more record sets than expected ({multiSetList.Count})."));

                    if (multiSetList[index] != null)
                    {
                        records = 0;
                        multiSetArg = multiSetList[index];
                        while (await dr.ReadAsync(ct).ConfigureAwait(false))
                        {
                            records++;
                            if (multiSetArg.MaxRows.HasValue && records > multiSetArg.MaxRows.Value)
                                return Result.Fail(new InvalidOperationException($"{nameof(SelectMultiSetAsync)} (msa[{index}]) has returned more records than expected ({multiSetArg.MaxRows.Value})."));

                            multiSetArg.DatasetRecord(new DatabaseRecord(Database, dr));
                        }

                        if (records < multiSetArg.MinRows)
                            return Result.Fail(new InvalidOperationException($"{nameof(SelectMultiSetAsync)} (msa[{index}]) has returned less records ({records}) than expected ({multiSetArg.MinRows})."));

                        if (records == 0 && multiSetArg.StopOnNull)
                            return Result.Success;

                        multiSetArg.InvokeResult();
                    }

                    index++;
                } while (dr.NextResult());

                return index < multiSetList.Count && !multiSetList[index].StopOnNull 
                    ? Result.Fail(new InvalidOperationException($"{nameof(SelectMultiSetAsync)} has returned less ({index}) record sets than expected ({multiSetList.Count})."))
                    : Result.Success;
            }, cancellationToken, memberName);
        }

        /// <summary>
        /// Executes a multi-dataset query command with one or more <see cref="IMultiSetArgs"/> that supports <paramref name="paging"/>.
        /// </summary>
        /// <param name="paging">The <see cref="PagingArgs"/> or <see cref="PagingResult"/> to add to the <see cref="Parameters"/>.</param>
        /// <param name="multiSetArgs">One or more <see cref="IMultiSetArgs"/>.</param>
        /// <remarks>The number of <see cref="IMultiSetArgs"/> specified must match the number of returned datasets. A null dataset indicates to ignore (skip) a dataset.</remarks>
        public async Task SelectMultiSetAsync(PagingArgs? paging, params IMultiSetArgs[] multiSetArgs) 
            => (await SelectMultiSetWithResultInternalAsync(paging, multiSetArgs, nameof(SelectMultiSetAsync), default).ConfigureAwait(false)).ThrowOnError();

        /// <summary>
        /// Executes a multi-dataset query command with one or more <see cref="IMultiSetArgs"/> that supports <paramref name="paging"/> with a <see cref="Result"/>.
        /// </summary>
        /// <param name="paging">The <see cref="PagingArgs"/> or <see cref="PagingResult"/> to add to the <see cref="Parameters"/>.</param>
        /// <param name="multiSetArgs">One or more <see cref="IMultiSetArgs"/>.</param>
        /// <remarks>The number of <see cref="IMultiSetArgs"/> specified must match the number of returned datasets. A null dataset indicates to ignore (skip) a dataset.</remarks>
        public Task<Result> SelectMultiSetWithResultAsync(PagingArgs? paging, params IMultiSetArgs[] multiSetArgs) 
            => SelectMultiSetWithResultInternalAsync(paging, multiSetArgs, nameof(SelectMultiSetWithResultAsync), default);

        /// <summary>
        /// Executes a multi-dataset query command with one or more <see cref="IMultiSetArgs"/> that supports <paramref name="paging"/>.
        /// </summary>
        /// <param name="paging">The <see cref="PagingArgs"/> or <see cref="PagingResult"/> to add to the <see cref="Parameters"/>.</param>
        /// <param name="multiSetArgs">One or more <see cref="IMultiSetArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <remarks>The number of <see cref="IMultiSetArgs"/> specified must match the number of returned datasets. A null dataset indicates to ignore (skip) a dataset.</remarks>
        public async Task SelectMultiSetAsync(PagingArgs? paging, IEnumerable<IMultiSetArgs> multiSetArgs, CancellationToken cancellationToken = default)
            => (await SelectMultiSetWithResultInternalAsync(paging, multiSetArgs, nameof(SelectMultiSetAsync), cancellationToken).ConfigureAwait(false)).ThrowOnError();

        /// <summary>
        /// Executes a multi-dataset query command with one or more <see cref="IMultiSetArgs"/> that supports <paramref name="paging"/> with a <see cref="Result"/>.
        /// </summary>
        /// <param name="paging">The <see cref="PagingArgs"/> or <see cref="PagingResult"/> to add to the <see cref="Parameters"/>.</param>
        /// <param name="multiSetArgs">One or more <see cref="IMultiSetArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <remarks>The number of <see cref="IMultiSetArgs"/> specified must match the number of returned datasets. A null dataset indicates to ignore (skip) a dataset.</remarks>
        public Task<Result> SelectMultiSetWithResultAsync(PagingArgs? paging, IEnumerable<IMultiSetArgs> multiSetArgs, CancellationToken cancellationToken = default)
            => SelectMultiSetWithResultInternalAsync(paging, multiSetArgs, nameof(SelectMultiSetWithResultAsync), cancellationToken);

        /// <summary>
        /// Executes a multi-dataset query command with one or more <see cref="IMultiSetArgs"/> that supports <paramref name="paging"/> with a <see cref="Result"/> internal.
        /// </summary>
        private async Task<Result> SelectMultiSetWithResultInternalAsync(PagingArgs? paging, IEnumerable<IMultiSetArgs> multiSetArgs, string memberName, CancellationToken cancellationToken)
        {
            Parameters.PagingParams(paging);

            var result = await SelectMultiSetWithValueResultInternalAsync(multiSetArgs, memberName, cancellationToken).ConfigureAwait(false);
            return result.ThenAs(rv =>
            {
                if (paging is PagingResult pr && pr.IsGetCount && rv >= 0)
                    pr.TotalCount = rv;
            });
        }

        /// <summary>
        /// Executes a multi-dataset query command with one or more <see cref="IMultiSetArgs"/>; whilst also outputing the resulting <see cref="int"/> <see cref="ParameterDirection.ReturnValue"/>.
        /// </summary>
        /// <param name="multiSetArgs">One or more <see cref="IMultiSetArgs"/>.</param>
        /// <returns>The resultant return value.</returns>
        /// <remarks>The number of <see cref="IMultiSetArgs"/> specified must match the number of returned datasets. A null dataset indicates to ignore (skip) a dataset.</remarks>
        public async Task<int> SelectMultiSetWithValueAsync(params IMultiSetArgs[] multiSetArgs)
            => await SelectMultiSetWithValueResultInternalAsync(multiSetArgs, nameof(SelectMultiSetWithValueAsync), default).ConfigureAwait(false);

        /// <summary>
        /// Executes a multi-dataset query command with one or more <see cref="IMultiSetArgs"/>; whilst also outputing the resulting <see cref="int"/> <see cref="ParameterDirection.ReturnValue"/> with a <see cref="Result{T}"/>.
        /// </summary>
        /// <param name="multiSetArgs">One or more <see cref="IMultiSetArgs"/>.</param>
        /// <returns>The resultant return value.</returns>
        /// <remarks>The number of <see cref="IMultiSetArgs"/> specified must match the number of returned datasets. A null dataset indicates to ignore (skip) a dataset.</remarks>
        public Task<Result<int>> SelectMultiSetWithValueResultAsync(params IMultiSetArgs[] multiSetArgs)
            => SelectMultiSetWithValueResultInternalAsync(multiSetArgs, nameof(SelectMultiSetWithValueResultAsync), default);

        /// <summary>
        /// Executes a multi-dataset query command with one or more <see cref="IMultiSetArgs"/>; whilst also outputing the resulting <see cref="int"/> <see cref="ParameterDirection.ReturnValue"/>.
        /// </summary>
        /// <param name="multiSetArgs">One or more <see cref="IMultiSetArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The resultant return value.</returns>
        /// <remarks>The number of <see cref="IMultiSetArgs"/> specified must match the number of returned datasets. A null dataset indicates to ignore (skip) a dataset.</remarks>
        public async Task<int> SelectMultiSetWithValueAsync(IEnumerable<IMultiSetArgs> multiSetArgs, CancellationToken cancellationToken = default)
            => await SelectMultiSetWithValueResultInternalAsync(multiSetArgs, nameof(SelectMultiSetWithValueAsync), cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Executes a multi-dataset query command with one or more <see cref="IMultiSetArgs"/>; whilst also outputing the resulting <see cref="int"/> <see cref="ParameterDirection.ReturnValue"/> with a <see cref="Result{T}"/>.
        /// </summary>
        /// <param name="multiSetArgs">One or more <see cref="IMultiSetArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The resultant return value.</returns>
        /// <remarks>The number of <see cref="IMultiSetArgs"/> specified must match the number of returned datasets. A null dataset indicates to ignore (skip) a dataset.</remarks>
        public Task<Result<int>> SelectMultiSetWithValueResultAsync(IEnumerable<IMultiSetArgs> multiSetArgs, CancellationToken cancellationToken = default)
            => SelectMultiSetWithValueResultInternalAsync(multiSetArgs, nameof(SelectMultiSetWithValueResultAsync), cancellationToken);

        /// <summary>
        /// Executes a multi-dataset query command with one or more <see cref="IMultiSetArgs"/>; whilst also outputing the resulting <see cref="int"/> <see cref="ParameterDirection.ReturnValue"/> with a <see cref="Result{T}"/> internal.
        /// </summary>
        private async Task<Result<int>> SelectMultiSetWithValueResultInternalAsync(IEnumerable<IMultiSetArgs> multiSetArgs, string memberName, CancellationToken cancellationToken)
        {
            var rvp = Parameters.AddReturnValueParameter();
            var result = await SelectMultiSetWithResultInternalAsync(multiSetArgs, memberName, cancellationToken).ConfigureAwait(false);
            return result.ThenAs(() => rvp.Value == null ? -1 : (int)rvp.Value);
        }

        /// <summary>
        /// Executes a non-query command.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The number of rows affected.</returns>
        public async Task<int> NonQueryAsync(CancellationToken cancellationToken = default) 
            => await NonQueryWithResultInternalAsync(null, nameof(NonQueryAsync), cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Executes a non-query command with a <see cref="Result{T}"/>.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The number of rows affected.</returns>
        public Task<Result<int>> NonQueryWithResultAsync(CancellationToken cancellationToken = default) 
            => NonQueryWithResultInternalAsync(null, nameof(NonQueryWithResultAsync), cancellationToken);

        /// <summary>
        /// Executes a non-query command.
        /// </summary>
        /// <param name="parameters">The post-execution delegate to enable parameter access.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The number of rows affected.</returns>
        public async Task<int> NonQueryAsync(Action<DbParameterCollection>? parameters, CancellationToken cancellationToken = default) 
            => await NonQueryWithResultInternalAsync(parameters, nameof(NonQueryAsync), cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Executes a non-query command with a <see cref="Result{T}"/>.
        /// </summary>
        /// <param name="parameters">The post-execution delegate to enable parameter access.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The number of rows affected.</returns>
        public Task<Result<int>> NonQueryWithResultAsync(Action<DbParameterCollection>? parameters, CancellationToken cancellationToken = default)
            => NonQueryWithResultInternalAsync(parameters, nameof(NonQueryWithResultAsync), cancellationToken);

        /// <summary>
        /// Executes a non-query command with a <see cref="Result{T}"/> internal.
        /// </summary>
        private Task<Result<int>> NonQueryWithResultInternalAsync(Action<DbParameterCollection>? parameters, string memberName, CancellationToken cancellationToken = default) => Database.Invoker.InvokeAsync(Database, parameters, async (_, parameters, ct) =>
        {
            using var cmd = await CreateDbCommandAsync(ct).ConfigureAwait(false);
            parameters?.Invoke(cmd.Parameters);
            var result = await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
            return Result.Ok(result);
        }, cancellationToken, memberName);

        /// <summary>
        /// Executes the query and returns the first column of the first row in the result set returned by the query.
        /// </summary>
        /// <typeparam name="T">The result <see cref="Type"/>.</typeparam>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The value of the first column of the first row in the result set.</returns>
        public async Task<T> ScalarAsync<T>(CancellationToken cancellationToken = default) 
            => await ScalarWithResultInternalAsync<T>(null, nameof(ScalarAsync), cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Executes the query and returns the first column of the first row in the result set returned by the query with a <see cref="Result{T}"/>.
        /// </summary>
        /// <typeparam name="T">The result <see cref="Type"/>.</typeparam>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The value of the first column of the first row in the result set.</returns>
        public Task<Result<T>> ScalarWithResultAsync<T>(CancellationToken cancellationToken = default) 
            => ScalarWithResultInternalAsync<T>(null, nameof(ScalarWithResultAsync), cancellationToken);

        /// <summary>
        /// Executes the query and returns the first column of the first row in the result set returned by the query.
        /// </summary>
        /// <typeparam name="T">The result <see cref="Type"/>.</typeparam>
        /// <param name="parameters">The post-execution delegate to enable parameter access.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The value of the first column of the first row in the result set.</returns>
        public async Task<T> ScalarAsync<T>(Action<DbParameterCollection>? parameters, CancellationToken cancellationToken = default)
            => await ScalarWithResultInternalAsync<T>(parameters, nameof(ScalarAsync), cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Executes the query and returns the first column of the first row in the result set returned by the query with a <see cref="Result{T}"/>.
        /// </summary>
        /// <typeparam name="T">The result <see cref="Type"/>.</typeparam>
        /// <param name="parameters">The post-execution delegate to enable parameter access.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The value of the first column of the first row in the result set.</returns>
        public Task<Result<T>> ScalarWithResultAsync<T>(Action<DbParameterCollection>? parameters, CancellationToken cancellationToken = default)
            => ScalarWithResultInternalAsync<T>(parameters, nameof(ScalarWithResultAsync), cancellationToken);

        /// <summary>
        /// Executes the query and returns the first column of the first row in the result set returned by the query with a <see cref="Result{T}"/> internal.
        /// </summary>
        private Task<Result<T>> ScalarWithResultInternalAsync<T>(Action<DbParameterCollection>? parameters, string memberName, CancellationToken cancellationToken = default) => Database.Invoker.InvokeAsync(Database, parameters, async (_, parameters, ct) =>
        {
            using var cmd = await CreateDbCommandAsync(ct).ConfigureAwait(false);
            parameters?.Invoke(cmd.Parameters);
            var result = await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false);
            T value = result is null ? default! : result is DBNull ? default! : (T)result;
            return Result.Ok(value);
        }, cancellationToken, memberName);

        /// <summary>
        /// Selects none or more items from the first result set using a <paramref name="mapper"/>.
        /// </summary>
        /// <typeparam name="T">The item <see cref="Type"/>.</typeparam>
        /// <param name="mapper">The <see cref="IDatabaseMapper{T}"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The item sequence.</returns>
        public async Task<IEnumerable<T>> SelectQueryAsync<T>(IDatabaseMapper<T> mapper, CancellationToken cancellationToken = default)
            => (await SelectQueryWithResultInternalAsync<T>(mapper, nameof(SelectQueryAsync), cancellationToken).ConfigureAwait(false)).Value;

        /// <summary>
        /// Selects none or more items from the first result set using a <paramref name="mapper"/> with a <see cref="Result{T}"/>.
        /// </summary>
        /// <typeparam name="T">The item <see cref="Type"/>.</typeparam>
        /// <param name="mapper">The <see cref="IDatabaseMapper{T}"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The item sequence.</returns>
        public Task<Result<IEnumerable<T>>> SelectQueryWithResultAsync<T>(IDatabaseMapper<T> mapper, CancellationToken cancellationToken = default)
            => SelectQueryWithResultInternalAsync(mapper, nameof(SelectQueryWithResultAsync), cancellationToken);

        /// <summary>
        /// Selects none or more items from the first result set using a <paramref name="mapper"/> with a <see cref="Result{T}"/> internal.
        /// </summary>
        private async Task<Result<IEnumerable<T>>> SelectQueryWithResultInternalAsync<T>(IDatabaseMapper<T> mapper, string memberName, CancellationToken cancellationToken)
        {
            var coll = new List<T>();
            var result = await SelectQueryWithResultInternalAsync(coll, mapper, memberName, cancellationToken).ConfigureAwait(false);
            return result.ThenAs(() => (IEnumerable<T>)coll);
        }

        /// <summary>
        /// Selects none or more items from the first result set.
        /// </summary>
        /// <typeparam name="T">The item <see cref="Type"/>.</typeparam>
        /// <param name="func">The <see cref="DatabaseRecord"/> mapping function.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The resulting set.</returns>
        public async Task<IEnumerable<T>> SelectQueryAsync<T>(Func<DatabaseRecord, T> func, CancellationToken cancellationToken = default)
            => (await SelectQueryWithResultInternalAsync<T>(func, nameof(SelectQueryAsync), cancellationToken).ConfigureAwait(false)).Value;

        /// <summary>
        /// Selects none or more items from the first result set with a <see cref="Result{T}"/>.
        /// </summary>
        /// <typeparam name="T">The item <see cref="Type"/>.</typeparam>
        /// <param name="func">The <see cref="DatabaseRecord"/> mapping function.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The resulting set.</returns>
        public Task<Result<IEnumerable<T>>> SelectQueryWithResultAsync<T>(Func<DatabaseRecord, T> func, CancellationToken cancellationToken = default)
            => SelectQueryWithResultInternalAsync(func, nameof(SelectQueryWithResultAsync), cancellationToken);

        /// <summary>
        /// Selects none or more items from the first result set with a <see cref="Result{T}"/> internal.
        /// </summary>
        private async Task<Result<IEnumerable<T>>> SelectQueryWithResultInternalAsync<T>(Func<DatabaseRecord, T> func, string memberName, CancellationToken cancellationToken = default)
        {
            var coll = new List<T>();
            var result = await SelectInternalAsync(coll, new DatabaseRecordMapper<T>(func), false, false, memberName, cancellationToken).ConfigureAwait(false);
            return result.ThenAs(() => (IEnumerable<T>)coll);
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
            => (await SelectQueryWithResultInternalAsync(collection, mapper, nameof(SelectQueryAsync), cancellationToken).ConfigureAwait(false)).ThrowOnError();

        /// <summary>
        /// Selects none or more items from the first result set and adds to the <paramref name="collection"/> with a <see cref="Result{T}"/>.
        /// </summary>
        /// <typeparam name="T">The item <see cref="Type"/>.</typeparam>
        /// <typeparam name="TColl">The collection <see cref="Type"/>.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="mapper">The <see cref="IDatabaseMapper{T}"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public async Task<Result> SelectQueryWithResultAsync<T, TColl>(TColl collection, IDatabaseMapper<T> mapper, CancellationToken cancellationToken = default) where TColl : ICollection<T>
            => await SelectQueryWithResultInternalAsync(collection, mapper, nameof(SelectQueryWithResultAsync), cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Selects none or more items from the first result set and adds to the <paramref name="collection"/> with a <see cref="Result{T}"/> internal.
        /// </summary>
        private async Task<Result> SelectQueryWithResultInternalAsync<T, TColl>(TColl collection, IDatabaseMapper<T> mapper, string memberName, CancellationToken cancellationToken) where TColl : ICollection<T>
            => await SelectInternalAsync(collection, mapper, false, false, memberName, cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Selects none or more items from the first result set and adds to the <paramref name="collection"/>.
        /// </summary>
        /// <typeparam name="T">The item <see cref="Type"/>.</typeparam>
        /// <typeparam name="TColl">The collection <see cref="Type"/>.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="func">The <see cref="DatabaseRecord"/> mapping function.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public async Task SelectQueryAsync<T, TColl>(TColl collection, Func<DatabaseRecord, T> func, CancellationToken cancellationToken = default) where TColl : ICollection<T>
            => (await SelectInternalAsync(collection, new DatabaseRecordMapper<T>(func), false, false, nameof(SelectQueryAsync), cancellationToken)).ThrowOnError();

        /// <summary>
        /// Selects none or more items from the first result set and adds to the <paramref name="collection"/> with a <see cref="Result{T}"/>.
        /// </summary>
        /// <typeparam name="T">The item <see cref="Type"/>.</typeparam>
        /// <typeparam name="TColl">The collection <see cref="Type"/>.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="func">The <see cref="DatabaseRecord"/> mapping function.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public Task<Result> SelectQueryWithResultAsync<T, TColl>(TColl collection, Func<DatabaseRecord, T> func, CancellationToken cancellationToken = default) where TColl : ICollection<T>
            => SelectInternalAsync(collection, new DatabaseRecordMapper<T>(func), false, false, nameof(SelectQueryWithResultAsync), cancellationToken);

        /// <summary>
        /// Selects a single item.
        /// </summary>
        /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
        /// <param name="mapper">The <see cref="IDatabaseMapper{T}"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The single item.</returns>
        public async Task<T> SelectSingleAsync<T>(IDatabaseMapper<T> mapper, CancellationToken cancellationToken = default)
            => (await SelectSingleWithResultInternalAsync(mapper, nameof(SelectSingleAsync), cancellationToken).ConfigureAwait(false)).Value;

        /// <summary>
        /// Selects a single item with a <see cref="Result{T}"/>.
        /// </summary>
        /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
        /// <param name="mapper">The <see cref="IDatabaseMapper{T}"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The single item.</returns>
        public Task<Result<T>> SelectSingleWithResultAsync<T>(IDatabaseMapper<T> mapper, CancellationToken cancellationToken = default)
            => SelectSingleWithResultInternalAsync(mapper, nameof(SelectSingleWithResultAsync), cancellationToken);

        /// <summary>
        /// Selects a single item with a <see cref="Result{T}"/> internal.
        /// </summary>
        private async Task<Result<T>> SelectSingleWithResultInternalAsync<T>(IDatabaseMapper<T> mapper, string memberName, CancellationToken cancellationToken)
        {
            var result = await SelectSingleFirstWithResultInternalAsync(mapper, true, memberName, cancellationToken).ConfigureAwait(false);
            return result.When(item => Comparer<T>.Default.Compare(item, default!) == 0, _ => Result<T>.Fail(new InvalidOperationException("SelectSingle request has not returned a row.")));
        }

        /// <summary>
        /// Selects a single item or default.
        /// </summary>
        /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
        /// <param name="mapper">The <see cref="IDatabaseMapper{T}"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The single item or default.</returns>
        public async Task<T?> SelectSingleOrDefaultAsync<T>(IDatabaseMapper<T> mapper, CancellationToken cancellationToken = default)
            => await SelectSingleFirstWithResultInternalAsync(mapper, false, nameof(SelectSingleOrDefaultAsync), cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Selects a single item or default with a <see cref="Result{T}"/>.
        /// </summary>
        /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
        /// <param name="mapper">The <see cref="IDatabaseMapper{T}"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The single item or default.</returns>
        public async Task<Result<T?>> SelectSingleOrDefaultWithResultAsync<T>(IDatabaseMapper<T> mapper, CancellationToken cancellationToken = default)
            => await SelectSingleFirstWithResultInternalAsync(mapper, false, nameof(SelectSingleOrDefaultWithResultAsync), cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Selects first item.
        /// </summary>
        /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
        /// <param name="mapper">The <see cref="IDatabaseMapper{T}"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The single item.</returns>
        public async Task<T> SelectFirstAsync<T>(IDatabaseMapper<T> mapper, CancellationToken cancellationToken = default)
            => await SelectFirstWithResultInternalAsync(mapper, nameof(SelectFirstAsync), cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Selects first item with a <see cref="Result{T}"/>.
        /// </summary>
        /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
        /// <param name="mapper">The <see cref="IDatabaseMapper{T}"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The single item.</returns>
        public Task<Result<T>> SelectFirstWithResultAsync<T>(IDatabaseMapper<T> mapper, CancellationToken cancellationToken = default)
            => SelectFirstWithResultInternalAsync(mapper, nameof(SelectFirstWithResultAsync), cancellationToken);

        /// <summary>
        /// Selects first item with a <see cref="Result{T}"/> internal.
        /// </summary>
        private async Task<Result<T>> SelectFirstWithResultInternalAsync<T>(IDatabaseMapper<T> mapper, string memberName, CancellationToken cancellationToken = default)
        {
            var result = await SelectSingleFirstWithResultInternalAsync(mapper, false, memberName, cancellationToken).ConfigureAwait(false);
            return result.When(item => Comparer<T>.Default.Compare(item, default!) == 0, _ => new InvalidOperationException("SelectFirst request has not returned a row."));
        }

        /// <summary>
        /// Selects first item or default.
        /// </summary>
        /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
        /// <param name="mapper">The <see cref="IDatabaseMapper{T}"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The single item or default.</returns>
        public async Task<T?> SelectFirstOrDefaultAsync<T>(IDatabaseMapper<T> mapper, CancellationToken cancellationToken = default)
            => await SelectSingleFirstWithResultInternalAsync(mapper, false, nameof(SelectFirstOrDefaultAsync), cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Selects first item or default with a <see cref="Result{T}"/>.
        /// </summary>
        /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
        /// <param name="mapper">The <see cref="IDatabaseMapper{T}"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The single item or default.</returns>
        public async Task<Result<T?>> SelectFirstOrDefaultWithResultAsync<T>(IDatabaseMapper<T> mapper, CancellationToken cancellationToken = default) 
            => await SelectSingleFirstWithResultInternalAsync(mapper, false, nameof(SelectFirstOrDefaultWithResultAsync), cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Select first row result only (where exists) internal.
        /// </summary>
        private async Task<Result<T>> SelectSingleFirstWithResultInternalAsync<T>(IDatabaseMapper<T> mapper, bool throwWhereMulti, string memberName, CancellationToken cancellationToken)
        {
            var coll = new List<T>();
            var result = await SelectInternalAsync(coll, mapper, throwWhereMulti, true, memberName, cancellationToken).ConfigureAwait(false);
            return result.ThenAs(() => coll.Count == 0 ? default! : coll[0]);
        }

        /// <summary>
        /// Select the rows from the query internal.
        /// </summary>
        private async Task<Result> SelectInternalAsync<T, TColl>(TColl coll, IDatabaseMapper<T> mapper, bool throwWhereMulti, bool stopAfterOneRow, string memberName, CancellationToken cancellationToken) where TColl : ICollection<T>
        {
            if (mapper == null)
                throw new ArgumentNullException(nameof(mapper));

            return await Database.Invoker.InvokeAsync(Database, mapper, throwWhereMulti, stopAfterOneRow, async (_, mapper, throwWhereMulti, stopAfterOneRow, ct) =>
            {
                int i = 0;

                using var cmd = await CreateDbCommandAsync(ct).ConfigureAwait(false);
                using var dr = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);

                while (await dr.ReadAsync(ct).ConfigureAwait(false))
                {
                    if (++i == 2)
                    {
                        if (throwWhereMulti)
                            Result.Fail(new InvalidOperationException("SelectSingle request has returned more than one row."));

                        if (stopAfterOneRow)
                            return Result.Success;
                    }

                    var val = mapper.MapFromDb(new DatabaseRecord(Database, dr));
                    if (val == null)
                        return Result.Fail(new InvalidOperationException("A null must not be returned from the mapper."));

                    coll.Add(val);
                    if (!throwWhereMulti && stopAfterOneRow)
                        return Result.Success;
                }

                return Result.Success;
            }, cancellationToken, memberName).ConfigureAwait(false);
        }
    }
}