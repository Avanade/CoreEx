// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Database.Extended;
using CoreEx.Entities;
using CoreEx.Mapping.Converters;
using CoreEx.Results;
using Microsoft.Extensions.Logging;
using System;
using System.Data.Common;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Database
{
    /// <summary>
    /// Defines the database access.
    /// </summary>
    public interface IDatabase : IAsyncDisposable, IDisposable
    {
        /// <summary>
        /// Gets the <see cref="DbProviderFactory"/>.
        /// </summary>
        DbProviderFactory Provider { get; }

        /// <summary>
        /// Gets the <see cref="ILogger"/>.
        /// </summary>
        ILogger? Logger { get; }

        /// <summary>
        /// Gets the <see cref="DatabaseInvoker"/>.
        /// </summary>
        DatabaseInvoker Invoker { get; }

        /// <summary>
        /// Gets the default <see cref="DatabaseArgs"/> used where not expliticly specified for an operation.
        /// </summary>
        DatabaseArgs DbArgs { get; }

        /// <summary>
        /// Gets the unique database instance identifier.
        /// </summary>
        Guid DatabaseId { get; }

        /// <summary>
        /// Gets or sets the <see cref="Entities.DateTimeTransform"/> to be used when retrieving (see <see cref="DatabaseRecord.GetValue{T}(string)"/>) a <see cref="DateTime"/> value from a <see cref="DatabaseRecord"/>.
        /// </summary>
        DateTimeTransform DateTimeTransform { get; set; }

        /// <summary>
        /// Gets or sets the names of the pre-configured <see cref="Extended.DatabaseColumns"/>.
        /// </summary>
        DatabaseColumns DatabaseColumns { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="DatabaseWildcard"/> to enable wildcard replacement.
        /// </summary>
        DatabaseWildcard Wildcard { get; set; }

        /// <summary>
        /// Indicates whether the <see cref="Mapping.ChangeLogDatabaseMapper.MapToDb(ChangeLog?, DatabaseParameterCollection, CoreEx.Mapping.OperationTypes)"/> and 
        /// <see cref="Mapping.ChangeLogExDatabaseMapper.MapToDb(Entities.Extended.ChangeLogEx?, DatabaseParameterCollection, CoreEx.Mapping.OperationTypes)"/> pass values via parameters.
        /// </summary>
        bool EnableChangeLogMapperToDb { get; }

        /// <summary>
        /// Gets the <see cref="DatabaseColumns.RowVersionName"/> converter.
        /// </summary>
        IConverter RowVersionConverter { get; }

        /// <summary>
        /// Gets the <see cref="DbConnection"/>.
        /// </summary>
        /// <remarks>The connection is created and opened on first use, and closed on <see cref="IAsyncDisposable.DisposeAsync()"/> or <see cref="IDisposable.Dispose()"/>.</remarks>
        DbConnection GetConnection();

        /// <summary>
        /// Gets the <see cref="DbConnection"/>.
        /// </summary>
        /// <remarks>The connection is created and opened on first use, and closed on <see cref="IAsyncDisposable.DisposeAsync()"/> or <see cref="IDisposable.Dispose()"/>.</remarks>
        Task<DbConnection> GetConnectionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a stored procedure <see cref="DatabaseCommand"/>.
        /// </summary>
        /// <param name="storedProcedure">The stored procedure name.</param>
        /// <returns>The <see cref="DatabaseCommand"/>.</returns>
        DatabaseCommand StoredProcedure(string storedProcedure);

        /// <summary>
        /// Creates a SQL statement <see cref="DatabaseCommand"/>.
        /// </summary>
        /// <param name="sqlStatement">The SQL statement.</param>
        /// <returns>The <see cref="DatabaseCommand"/>.</returns>
        DatabaseCommand SqlStatement(string sqlStatement);

        /// <summary>
        /// Creates a SQL statement <see cref="DatabaseCommand"/> from the named embedded resource within the specified <paramref name="assembly"/>.
        /// </summary>
        /// <param name="resourceName">The embedded resource name (matches to the end of the fully qualifed resource name).</param>
        /// <param name="assembly">The <see cref="Assembly"/> that contains the embedded resource; defaults to <see cref="Assembly.GetCallingAssembly"/>.</param>
        /// <returns>The <see cref="DatabaseCommand"/>.</returns>
        DatabaseCommand SqlStatementFromResource(string resourceName, Assembly? assembly = null);

        /// <summary>
        /// Creates a SQL statement <see cref="DatabaseCommand"/> from the named embedded resource within the <see name="Assembly"/> inferred from the <typeparamref name="TResource"/> <see cref="Type"/>.
        /// </summary>
        /// <typeparam name="TResource">The <see cref="Type"/> to infer the <see cref="Assembly"/> that contains the embedded resource.</typeparam>
        /// <param name="resourceName">The embedded resource name (matches to the end of the fully qualifed resource name).</param>
        /// <returns>The <see cref="DatabaseCommand"/>.</returns>
        DatabaseCommand SqlStatementFromResource<TResource>(string resourceName);

        /// <summary>
        /// Invoked where a <see cref="DbException"/> has been thrown.
        /// </summary>
        /// <param name="dbex">The <see cref="DbException"/>.</param>
        /// <returns>The <see cref="Result"/> containing the appropriate <see cref="IResult.Error"/> where handled; otherwise, <c>null</c> indicating that the exception is unexpected and will continue to be thrown as such.</returns>
        /// <remarks>Provides an opportunity to inspect and handle the exception before it is returned. A resulting <see cref="Result"/> that is <see cref="Result.IsSuccess"/> is not considered sensical; therefore, will result in the originating
        /// exception being thrown.</remarks>
        Result? HandleDbException(DbException dbex);
    }
}