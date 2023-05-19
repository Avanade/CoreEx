// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using CoreEx.Mapping;
using CoreEx.RefData;
using CoreEx.Results;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Database.Extended
{
    /// <summary>
    /// Provides database extension methods.
    /// </summary>
    public static class DatabaseExtendedExtensions
    {
        /// <summary>
        /// Creates a <see cref="RefDataLoader{TColl, TItem, TId}"/> (for <see cref="IReferenceDataCollection"/> loading).
        /// </summary>
        /// <typeparam name="TColl">The <see cref="IReferenceDataCollection"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="TItem">The <see cref="IReferenceData"/> item <see cref="Type"/>.</typeparam>
        /// <typeparam name="TId">The <see cref="IReferenceData"/> <see cref="IIdentifier.Id"/> <see cref="Type"/>.</typeparam>
        /// <param name="command">The <see cref="DatabaseCommand"/>.</param>
        /// <returns>The <see cref="RefDataLoader{TColl, TItem, TId}"/>.</returns>
        public static RefDataLoader<TColl, TItem, TId> ReferenceData<TColl, TItem, TId>(this DatabaseCommand command)
            where TColl : class, IReferenceDataCollection<TId, TItem>, new()
            where TItem : class, IReferenceData<TId>, new()
            where TId : IComparable<TId>, IEquatable<TId>
            => new(command);

        /// <summary>
        /// Creates a <see cref="RefDataLoader{TColl, TItem, TId}"/> (for <see cref="IReferenceDataCollection"/> loading) using the specified <paramref name="storedProcedure"/>.
        /// </summary>
        /// <typeparam name="TColl">The <see cref="IReferenceDataCollection"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="TItem">The <see cref="IReferenceData"/> item <see cref="Type"/>.</typeparam>
        /// <typeparam name="TId">The <see cref="IReferenceData"/> <see cref="IIdentifier.Id"/> <see cref="Type"/>.</typeparam>
        /// <param name="database">The <see cref="IDatabase"/>.</param>
        /// <param name="storedProcedure">The stored procedure name.</param>
        /// <returns>The <see cref="RefDataLoader{TColl, TItem, TId}"/>.</returns>
        public static RefDataLoader<TColl, TItem, TId> ReferenceData<TColl, TItem, TId>(this IDatabase database, string storedProcedure)
            where TColl : class, IReferenceDataCollection<TId, TItem>, new()
            where TItem : class, IReferenceData<TId>, new()
            where TId : IComparable<TId>, IEquatable<TId>
            => ReferenceData<TColl, TItem, TId>((database ?? throw new ArgumentNullException(nameof(database))).StoredProcedure(storedProcedure));

        /// <summary>
        /// Creates a <see cref="RefDataLoader{TColl, TItem, TId}"/> (for <see cref="IReferenceDataCollection"/> loading) using a '<c>SELECT * FROM [<paramref name="schemaName"/>].[<paramref name="tableName"/>]</c>' SQL statement.
        /// </summary>
        /// <typeparam name="TColl">The <see cref="IReferenceDataCollection"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="TItem">The <see cref="IReferenceData"/> item <see cref="Type"/>.</typeparam>
        /// <typeparam name="TId">The <see cref="IReferenceData"/> <see cref="IIdentifier.Id"/> <see cref="Type"/>.</typeparam>
        /// <param name="database">The <see cref="IDatabase"/>.</param>
        /// <param name="schemaName">The database schema name (optional).</param>
        /// <param name="tableName">The database table name.</param>
        /// <returns>The <see cref="RefDataLoader{TColl, TItem, TId}"/>.</returns>
        /// <remarks>The <paramref name="schemaName"/> and <paramref name="tableName"/> should not be escaped/quoted as this is performed internally to minimize SQL injection opportunity.</remarks>
        public static RefDataLoader<TColl, TItem, TId> ReferenceData<TColl, TItem, TId>(this IDatabase database, string? schemaName, string tableName)
            where TColl : class, IReferenceDataCollection<TId, TItem>, new()
            where TItem : class, IReferenceData<TId>, new()
            where TId : IComparable<TId>, IEquatable<TId>
        {
            if (!database.Provider.CanCreateCommandBuilder)
                throw new NotSupportedException("Database Provider can not CreateCommandBuilder which is required to quote the identifiers to minimize SQL inject possibility.");

            var cb = database.Provider.CreateCommandBuilder();
            if (string.IsNullOrEmpty(schemaName))
                return ReferenceData<TColl, TItem, TId>((database ?? throw new ArgumentNullException(nameof(database)))
                    .SqlStatement($"SELECT * FROM {cb.QuoteIdentifier(tableName ?? throw new ArgumentNullException(nameof(tableName)))}"));
            else
                return ReferenceData<TColl, TItem, TId>((database ?? throw new ArgumentNullException(nameof(database)))
                    .SqlStatement($"SELECT * FROM {cb.QuoteIdentifier(schemaName ?? throw new ArgumentNullException(nameof(schemaName)))}.{cb.QuoteIdentifier(tableName ?? throw new ArgumentNullException(nameof(tableName)))}"));
        }

        /// <summary>
        /// Creates a <see cref="DatabaseQuery{T}"/> to enable select-like capabilities.
        /// </summary>
        /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
        /// <param name="command">The <see cref="DatabaseCommand"/>.</param>
        /// <param name="args">The <see cref="DatabaseArgs"/>.</param>
        /// <param name="queryParams">The query <see cref="DatabaseParameterCollection"/> action to enable additional filtering.</param>
        /// <returns>The <see cref="DatabaseQuery{T}"/></returns>
        public static DatabaseQuery<T> Query<T>(this DatabaseCommand command, DatabaseArgs args, Action<DatabaseParameterCollection>? queryParams = null) where T : class, new() => new(command, args, queryParams);

        /// <summary>
        /// Creates a <see cref="DatabaseQuery{T}"/> to enable select-like capabilities.
        /// </summary>
        /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
        /// <param name="command">The <see cref="DatabaseCommand"/>.</param>
        /// <param name="mapper">The <see cref="IDatabaseMapper{T}"/>.</param>
        /// <param name="queryParams">The query <see cref="DatabaseParameterCollection"/> action to enable additional filtering.</param>
        /// <returns>The <see cref="DatabaseQuery{T}"/></returns>
        public static DatabaseQuery<T> Query<T>(this DatabaseCommand command, IDatabaseMapper<T> mapper, Action<DatabaseParameterCollection>? queryParams = null) where T : class, new() => new(command, new DatabaseArgs(command.Database.DbArgs, mapper), queryParams);

        /// <summary>
        /// Performs the save (create or update) operation.
        /// </summary>
        private static async Task<Result<T>> SaveWithResultAsync<T>(this DatabaseCommand command, DatabaseArgs args, T value, OperationTypes operationType, CancellationToken cancellationToken = default) where T : class, new()
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            if (value == null)
                throw new ArgumentNullException(nameof(value));

            // Set ChangeLog properties where appropriate.
            if (operationType == OperationTypes.Create)
                ChangeLog.PrepareCreated(value);
            else
                ChangeLog.PrepareUpdated(value);

            // Map the parameters.
            var map = (IDatabaseMapper<T>)args.Mapper;
            map.MapToDb(value, command.Parameters, operationType);

            if (args.Refresh)
            {
                var result = await command.ReselectRecordParam().SelectFirstOrDefaultWithResultAsync(map, cancellationToken).ConfigureAwait(false);
                return result.When(v => v is null, () => Result<T>.NotFoundError());
            }

            // NOTE: without refresh, fields like IDs and RowVersion are not automatically updated.
            var nqresult = await command.NonQueryWithResultAsync(cancellationToken).ConfigureAwait(false);
            return nqresult.Then(() => value);
        }

        #region Standard

        /// <summary>
        /// Gets the value for the specified <paramref name="key"/> mapping to <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
        /// <param name="command">The <see cref="DatabaseCommand"/>.</param>
        /// <param name="args">The <see cref="DatabaseArgs"/>.</param>
        /// <param name="key">The key value.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The value where found; otherwise, <c>null</c>.</returns>
        public static Task<T?> GetAsync<T>(this DatabaseCommand command, DatabaseArgs args, object[] key, CancellationToken cancellationToken = default) where T : class, new()
            => GetAsync<T>(command, args, CompositeKey.Create(key), cancellationToken);

        /// <summary>
        /// Gets the value for the specified <paramref name="key"/> mapping to <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
        /// <param name="command">The <see cref="DatabaseCommand"/>.</param>
        /// <param name="mapper">The <see cref="IDatabaseMapper{T}"/>.</param>
        /// <param name="key">The key value.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The value where found; otherwise, <c>null</c>.</returns>
        public static Task<T?> GetAsync<T>(this DatabaseCommand command, IDatabaseMapper<T> mapper, object? key, CancellationToken cancellationToken = default) where T : class, new()
            => GetAsync<T>(command, new DatabaseArgs(command.Database.DbArgs, mapper), CompositeKey.Create(key), cancellationToken);

        /// <summary>
        /// Gets the value for the specified <paramref name="key"/> mapping to <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
        /// <param name="command">The <see cref="DatabaseCommand"/>.</param>
        /// <param name="args">The <see cref="DatabaseArgs"/>.</param>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The value where found; otherwise, <c>null</c>.</returns>
        public static async Task<T?> GetAsync<T>(this DatabaseCommand command, DatabaseArgs args, CompositeKey key, CancellationToken cancellationToken = default) where T : class, new()
            => await GetWithResultAsync<T>(command, args, key, cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Gets the value for the specified <paramref name="key"/> mapping to <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
        /// <param name="command">The <see cref="DatabaseCommand"/>.</param>
        /// <param name="mapper">The <see cref="IDatabaseMapper{T}"/>.</param>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The value where found; otherwise, <c>null</c>.</returns>
        public static Task<T?> GetAsync<T>(this DatabaseCommand command, IDatabaseMapper<T> mapper, CompositeKey key, CancellationToken cancellationToken = default) where T : class, new()
            => GetAsync<T>(command, new DatabaseArgs(command.Database.DbArgs, mapper), key, cancellationToken);

        /// <summary>
        /// Performs a create using the specified stored procedure and value (reselects where specified).
        /// </summary>
        /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
        /// <param name="command">The <see cref="DatabaseCommand"/>.</param>
        /// <param name="args">The <see cref="DatabaseArgs"/>.</param>
        /// <param name="value">The value to insert.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The value (reselected where specified).</returns>
        public static async Task<T> CreateAsync<T>(this DatabaseCommand command, DatabaseArgs args, T value, CancellationToken cancellationToken = default) where T : class, new()
            => await SaveWithResultAsync(command, args, value, OperationTypes.Create, cancellationToken);

        /// <summary>
        /// Performs a create using the specified stored procedure and value (reselects where specified).
        /// </summary>
        /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
        /// <param name="command">The <see cref="DatabaseCommand"/>.</param>
        /// <param name="mapper">The <see cref="IDatabaseMapper{T}"/>.</param>
        /// <param name="value">The value to insert.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The value (reselected where specified).</returns>
        public async static Task<T> CreateAsync<T>(this DatabaseCommand command, IDatabaseMapper<T> mapper, T value, CancellationToken cancellationToken = default) where T : class, new()
            => await SaveWithResultAsync(command, new DatabaseArgs(command.Database.DbArgs, mapper), value, OperationTypes.Create, cancellationToken);

        /// <summary>
        /// Performs an update using the specified stored procedure and value (reselects where specified).
        /// </summary>
        /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
        /// <param name="command">The <see cref="DatabaseCommand"/>.</param>
        /// <param name="args">The <see cref="DatabaseArgs"/>.</param>
        /// <param name="value">The value to insert.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The value (reselected where specified).</returns>
        public static async Task<T> UpdateAsync<T>(this DatabaseCommand command, DatabaseArgs args, T value, CancellationToken cancellationToken = default) where T : class, new()
            => await SaveWithResultAsync(command, args, value, OperationTypes.Update, cancellationToken);

        /// <summary>
        /// Performs an update using the specified stored procedure and value (reselects where specified).
        /// </summary>
        /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
        /// <param name="command">The <see cref="DatabaseCommand"/>.</param>
        /// <param name="mapper">The <see cref="IDatabaseMapper{T}"/>.</param>
        /// <param name="value">The value to insert.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The value (reselected where specified).</returns>
        public static async Task<T> UpdateAsync<T>(this DatabaseCommand command, IDatabaseMapper<T> mapper, T value, CancellationToken cancellationToken = default) where T : class, new()
            => await SaveWithResultAsync(command, new DatabaseArgs(command.Database.DbArgs, mapper), value, OperationTypes.Update, cancellationToken);

        /// <summary>
        /// Performs a delete for the specified <paramref name="key"/>.
        /// </summary>
        /// <param name="command">The <see cref="DatabaseCommand"/>.</param>
        /// <param name="args">The <see cref="DatabaseArgs"/>.</param>
        /// <param name="key">The key value.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public static Task DeleteAsync(this DatabaseCommand command, DatabaseArgs args, object? key, CancellationToken cancellationToken = default)
            => DeleteAsync(command, args, CompositeKey.Create(key), cancellationToken);

        /// <summary>
        /// Performs a delete for the specified <paramref name="key"/>.
        /// </summary>
        /// <param name="command">The <see cref="DatabaseCommand"/>.</param>
        /// <param name="mapper">The <see cref="IDatabaseMapper{T}"/>.</param>
        /// <param name="key">The key values.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public static Task DeleteAsync(this DatabaseCommand command, IDatabaseMapper mapper, object? key, CancellationToken cancellationToken = default)
            => DeleteAsync(command, new DatabaseArgs(command.Database.DbArgs, mapper), CompositeKey.Create(key), cancellationToken);

        /// <summary>
        /// Performs a delete for the specified <paramref name="key"/>.
        /// </summary>
        /// <param name="command">The <see cref="DatabaseCommand"/>.</param>
        /// <param name="args">The <see cref="DatabaseArgs"/>.</param>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public static async Task DeleteAsync(this DatabaseCommand command, DatabaseArgs args, CompositeKey key, CancellationToken cancellationToken = default)
            => (await DeleteWithResultAsync(command, args, key, cancellationToken).ConfigureAwait(false)).ThrowOnError();

        /// <summary>
        /// Performs a delete for the specified <paramref name="key"/>.
        /// </summary>
        /// <param name="command">The <see cref="DatabaseCommand"/>.</param>
        /// <param name="mapper">The <see cref="IDatabaseMapper{T}"/>.</param>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public static Task DeleteAsync(this DatabaseCommand command, IDatabaseMapper mapper, CompositeKey key, CancellationToken cancellationToken = default)
            => DeleteAsync(command, new DatabaseArgs(command.Database.DbArgs, mapper), key, cancellationToken);

        #endregion

        #region WithResult

        /// <summary>
        /// Gets the value for the specified <paramref name="key"/> mapping to <typeparamref name="T"/> with a <see cref="Result{T}"/>.
        /// </summary>
        /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
        /// <param name="command">The <see cref="DatabaseCommand"/>.</param>
        /// <param name="args">The <see cref="DatabaseArgs"/>.</param>
        /// <param name="key">The key value.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The value where found; otherwise, <c>null</c>.</returns>
        public static Task<Result<T?>> GetWithResultAsync<T>(this DatabaseCommand command, DatabaseArgs args, object[] key, CancellationToken cancellationToken = default) where T : class, new()
            => GetWithResultAsync<T>(command, args, CompositeKey.Create(key), cancellationToken);

        /// <summary>
        /// Gets the value for the specified <paramref name="key"/> mapping to <typeparamref name="T"/> with a <see cref="Result{T}"/>.
        /// </summary>
        /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
        /// <param name="command">The <see cref="DatabaseCommand"/>.</param>
        /// <param name="mapper">The <see cref="IDatabaseMapper{T}"/>.</param>
        /// <param name="key">The key value.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The value where found; otherwise, <c>null</c>.</returns>
        public static Task<Result<T?>> GetWithResultAsync<T>(this DatabaseCommand command, IDatabaseMapper<T> mapper, object? key, CancellationToken cancellationToken = default) where T : class, new()
            => GetWithResultAsync<T>(command, new DatabaseArgs(command.Database.DbArgs, mapper), CompositeKey.Create(key), cancellationToken);

        /// <summary>
        /// Gets the value for the specified <paramref name="key"/> mapping to <typeparamref name="T"/> with a <see cref="Result{T}"/>.
        /// </summary>
        /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
        /// <param name="command">The <see cref="DatabaseCommand"/>.</param>
        /// <param name="args">The <see cref="DatabaseArgs"/>.</param>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The value where found; otherwise, <c>null</c>.</returns>
        public static Task<Result<T?>> GetWithResultAsync<T>(this DatabaseCommand command, DatabaseArgs args, CompositeKey key, CancellationToken cancellationToken = default) where T : class, new()
            => (command ?? throw new ArgumentNullException(nameof(command)))
                .Params(p => args.Mapper.MapPrimaryKeyParameters(p, OperationTypes.Get, key))
                .SelectFirstOrDefaultWithResultAsync((IDatabaseMapper<T>)args.Mapper, cancellationToken);

        /// <summary>
        /// Gets the value for the specified <paramref name="key"/> mapping to <typeparamref name="T"/> with a <see cref="Result{T}"/>.
        /// </summary>
        /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
        /// <param name="command">The <see cref="DatabaseCommand"/>.</param>
        /// <param name="mapper">The <see cref="IDatabaseMapper{T}"/>.</param>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The value where found; otherwise, <c>null</c>.</returns>
        public static Task<Result<T?>> GetWithResultAsync<T>(this DatabaseCommand command, IDatabaseMapper<T> mapper, CompositeKey key, CancellationToken cancellationToken = default) where T : class, new()
            => GetWithResultAsync<T>(command, new DatabaseArgs(command.Database.DbArgs, mapper), key, cancellationToken);

        /// <summary>
        /// Performs a create using the specified stored procedure and value (reselects where specified) with a <see cref="Result{T}"/>.
        /// </summary>
        /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
        /// <param name="command">The <see cref="DatabaseCommand"/>.</param>
        /// <param name="args">The <see cref="DatabaseArgs"/>.</param>
        /// <param name="value">The value to insert.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The value (reselected where specified).</returns>
        public static Task<Result<T>> CreateWithResultAsync<T>(this DatabaseCommand command, DatabaseArgs args, T value, CancellationToken cancellationToken = default) where T : class, new()
            => SaveWithResultAsync(command, args, value, OperationTypes.Create, cancellationToken);

        /// <summary>
        /// Performs a create using the specified stored procedure and value (reselects where specified) with a <see cref="Result{T}"/>.
        /// </summary>
        /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
        /// <param name="command">The <see cref="DatabaseCommand"/>.</param>
        /// <param name="mapper">The <see cref="IDatabaseMapper{T}"/>.</param>
        /// <param name="value">The value to insert.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The value (reselected where specified).</returns>
        public static Task<Result<T>> CreateWithResultAsync<T>(this DatabaseCommand command, IDatabaseMapper<T> mapper, T value, CancellationToken cancellationToken = default) where T : class, new()
            => SaveWithResultAsync(command, new DatabaseArgs(command.Database.DbArgs, mapper), value, OperationTypes.Create, cancellationToken);

        /// <summary>
        /// Performs an update using the specified stored procedure and value (reselects where specified) with a <see cref="Result{T}"/>.
        /// </summary>
        /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
        /// <param name="command">The <see cref="DatabaseCommand"/>.</param>
        /// <param name="args">The <see cref="DatabaseArgs"/>.</param>
        /// <param name="value">The value to insert.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The value (reselected where specified).</returns>
        public static Task<Result<T>> UpdateWithResultAsync<T>(this DatabaseCommand command, DatabaseArgs args, T value, CancellationToken cancellationToken = default) where T : class, new()
            => SaveWithResultAsync(command, args, value, OperationTypes.Update, cancellationToken);

        /// <summary>
        /// Performs an update using the specified stored procedure and value (reselects where specified) with a <see cref="Result{T}"/>.
        /// </summary>
        /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
        /// <param name="command">The <see cref="DatabaseCommand"/>.</param>
        /// <param name="mapper">The <see cref="IDatabaseMapper{T}"/>.</param>
        /// <param name="value">The value to insert.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The value (reselected where specified).</returns>
        public static Task<Result<T>> UpdateWithResultAsync<T>(this DatabaseCommand command, IDatabaseMapper<T> mapper, T value, CancellationToken cancellationToken = default) where T : class, new()
            => SaveWithResultAsync(command, new DatabaseArgs(command.Database.DbArgs, mapper), value, OperationTypes.Update, cancellationToken);

        /// <summary>
        /// Performs a delete for the specified <paramref name="key"/> with a <see cref="Result"/>.
        /// </summary>
        /// <param name="command">The <see cref="DatabaseCommand"/>.</param>
        /// <param name="args">The <see cref="DatabaseArgs"/>.</param>
        /// <param name="key">The key value.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public static Task<Result> DeleteWithResultAsync(this DatabaseCommand command, DatabaseArgs args, object? key, CancellationToken cancellationToken = default)
            => DeleteWithResultAsync(command, args, CompositeKey.Create(key), cancellationToken);

        /// <summary>
        /// Performs a delete for the specified <paramref name="key"/> with a <see cref="Result"/>.
        /// </summary>
        /// <param name="command">The <see cref="DatabaseCommand"/>.</param>
        /// <param name="mapper">The <see cref="IDatabaseMapper{T}"/>.</param>
        /// <param name="key">The key values.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public static Task<Result> DeleteWithResultAsync(this DatabaseCommand command, IDatabaseMapper mapper, object? key, CancellationToken cancellationToken = default)
            => DeleteWithResultAsync(command, new DatabaseArgs(command.Database.DbArgs, mapper), CompositeKey.Create(key), cancellationToken);

        /// <summary>
        /// Performs a delete for the specified <paramref name="key"/> with a <see cref="Result"/>.
        /// </summary>
        /// <param name="command">The <see cref="DatabaseCommand"/>.</param>
        /// <param name="args">The <see cref="DatabaseArgs"/>.</param>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public static async Task<Result> DeleteWithResultAsync(this DatabaseCommand command, DatabaseArgs args, CompositeKey key, CancellationToken cancellationToken = default)
        {
            var rowsAffectedResult = await (command ?? throw new ArgumentNullException(nameof(command)))
                .Params(p => args.Mapper.MapPrimaryKeyParameters(p, OperationTypes.Get, key))
                .ScalarWithResultAsync<int>(cancellationToken).ConfigureAwait(false);

            return rowsAffectedResult.When(rowsAffected => rowsAffected < 1, () => Result.NotFoundError());
        }

        /// <summary>
        /// Performs a delete for the specified <paramref name="key"/> with a <see cref="Result"/>.
        /// </summary>
        /// <param name="command">The <see cref="DatabaseCommand"/>.</param>
        /// <param name="mapper">The <see cref="IDatabaseMapper{T}"/>.</param>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public static Task DeleteWithResultAsync(this DatabaseCommand command, IDatabaseMapper mapper, CompositeKey key, CancellationToken cancellationToken = default)
            => DeleteWithResultAsync(command, new DatabaseArgs(command.Database.DbArgs, mapper), key, cancellationToken);

        #endregion
    }
}