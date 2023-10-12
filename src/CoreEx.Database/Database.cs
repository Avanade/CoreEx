// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Database.Extended;
using CoreEx.Entities;
using CoreEx.Mapping.Converters;
using CoreEx.Results;
using Microsoft.Extensions.Logging;
using System;
using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Database
{
    /// <summary>
    /// Provides the common/base database access functionality.
    /// </summary>
    /// <typeparam name="TConnection">The <see cref="DbConnection"/> <see cref="Type"/>.</typeparam>
    public class Database<TConnection> : IDatabase where TConnection : DbConnection
    {
        private static readonly DatabaseColumns _defaultColumns = new();
        private static readonly DatabaseWildcard _defaultWildcard = new();
        private static DatabaseInvoker? _invoker;

        private readonly Func<TConnection> _dbConnCreate;
        private TConnection? _dbConn;

        /// <summary>
        /// Initializes a new instance of the <see cref="Database{TConn}"/> class.
        /// </summary>
        /// <param name="create">The function to create the <typeparamref name="TConnection"/> <see cref="DbConnection"/>.</param>
        /// <param name="provider">The underlying <see cref="DbProviderFactory"/>.</param>
        /// <param name="logger">The optional <see cref="ILogger"/>.</param>
        /// <param name="invoker">The optional <see cref="DatabaseInvoker"/>.</param>
        public Database(Func<TConnection> create, DbProviderFactory provider, ILogger<Database<TConnection>>? logger = null, DatabaseInvoker? invoker = null)
        {
            _dbConnCreate = create ?? throw new ArgumentNullException(nameof(create));
            Provider = provider ?? throw new ArgumentNullException(nameof(provider));
            Logger = logger ?? ExecutionContext.GetService<ILogger<Database<TConnection>>>();
            Invoker = invoker ?? (_invoker ??= new DatabaseInvoker());
        }

        /// <inheritdoc/>
        public DbProviderFactory Provider { get; }

        /// <inheritdoc/>
        public Guid DatabaseId { get; } = Guid.NewGuid();

        /// <inheritdoc/>
        public ILogger? Logger { get; }

        /// <inheritdoc/>
        public DatabaseInvoker Invoker { get; }

        /// <inheritdoc/>
        public DatabaseArgs DbArgs { get; set; } = new DatabaseArgs();

        /// <inheritdoc/>
        public DateTimeTransform DateTimeTransform { get; set; } = DateTimeTransform.UseDefault;

        /// <inheritdoc/>
        /// <remarks>Do not update the default properties directly as a shared static instance is used (unless this is the desired behaviour); create a new <see cref="Extended.DatabaseColumns"/> instance for overridding.</remarks>
        public DatabaseColumns DatabaseColumns { get; set; } = _defaultColumns;

        /// <summary>
        /// Gets or sets the <see cref="DatabaseWildcard"/> to enable wildcard replacement.
        /// </summary>
        /// <remarks>Do not update the default properties directly as a shared static instance is used (unless this is the desired behaviour); create a new <see cref="DatabaseWildcard"/> instance for overridding.</remarks>
        public DatabaseWildcard Wildcard { get; set; } = _defaultWildcard;

        /// <inheritdoc/>
        public bool EnableChangeLogMapperToDb { get; }

        /// <inheritdoc/>
        public virtual IConverter RowVersionConverter => throw new NotImplementedException();

        /// <inheritdoc/>
        public DbConnection GetConnection() => _dbConn is not null ? _dbConn : Invokers.Invoker.RunSync(() => GetConnectionAsync());

        /// <inheritdoc/>
        public async Task<TConnection> GetConnectionAsync(CancellationToken cancellationToken = default)
        {
            if (_dbConn == null)
            {
                Logger?.LogDebug("Creating and opening the database connection. DatabaseId: {DatabaseId}", DatabaseId);
                _dbConn = _dbConnCreate() ?? throw new InvalidOperationException($"The create function must create a valid {nameof(TConnection)} instance.");
                await OnBeforeConnectionOpenAsync(_dbConn, cancellationToken).ConfigureAwait(false);
                await _dbConn.OpenAsync(cancellationToken).ConfigureAwait(false);
                await OnConnectionOpenAsync(_dbConn, cancellationToken).ConfigureAwait(false);
            }

            return _dbConn;
        }

        /// <summary>
        /// Occurs before a connection is opened.
        /// </summary>
        /// <param name="connection">The <see cref="DbConnection"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        protected virtual Task OnBeforeConnectionOpenAsync(DbConnection connection, CancellationToken cancellationToken) => Task.CompletedTask;

        /// <summary>
        /// Occurs when a connection is opened before any corresponding data access is performed.
        /// </summary>
        /// <param name="connection">The <see cref="DbConnection"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        protected virtual Task OnConnectionOpenAsync(DbConnection connection, CancellationToken cancellationToken) => Task.CompletedTask;

        /// <summary>
        /// Occurs before a connection is closed.
        /// </summary>
        /// <param name="connection">The <see cref="DbConnection"/>.</param>
        protected virtual Task OnConnectionCloseAsync(DbConnection connection) => Task.CompletedTask;

        /// <inheritdoc/>
        async Task<DbConnection> IDatabase.GetConnectionAsync(CancellationToken cancellationToken) => await GetConnectionAsync(cancellationToken).ConfigureAwait(false);

        /// <inheritdoc/>
        public DatabaseCommand StoredProcedure(string storedProcedure)
            => new(this, CommandType.StoredProcedure, storedProcedure ?? throw new ArgumentNullException(nameof(storedProcedure)));

        /// <inheritdoc/>
        public DatabaseCommand SqlStatement(string sqlStatement)
            => new(this, CommandType.Text, sqlStatement ?? throw new ArgumentNullException(nameof(sqlStatement)));

        /// <inheritdoc/>
        public DatabaseCommand SqlFromResource(string resourceName, Assembly? assembly = null)
            => SqlStatement(Abstractions.Resource.GetStreamReader(resourceName, assembly ?? Assembly.GetCallingAssembly()).ReadToEnd());

        /// <inheritdoc/>
        public DatabaseCommand SqlFromResource<TResource>(string resourceName)
            => SqlFromResource(resourceName, typeof(TResource).Assembly);

        /// <inheritdoc/>
        public Result? HandleDbException(DbException dbex)
        {
            var result = OnDbException(dbex);
            return !result.HasValue || result.Value.IsSuccess ? null : result;
        }

        /// <summary>
        /// Provides the <see cref="DbException"/> handling as a result of <see cref="HandleDbException(DbException)"/>.
        /// </summary>
        /// <param name="dbex">The <see cref="DbException"/>.</param>
        /// <returns>The <see cref="Result"/> containing the appropriate <see cref="IResult.Error"/> where handled; otherwise, <c>null</c> indicating that the exception is unexpected and will continue to be thrown as such.</returns>
        /// <remarks>Provides an opportunity to inspect and handle the exception before it is returned. A resulting <see cref="Result"/> that is <see cref="Result.IsSuccess"/> is not considered sensical; therefore, will result in the originating
        /// exception being thrown.
        /// <para>Where overridding and the <see cref="DbException"/> is not specifically handled then invoke the base to ensure any standard handling is executed.</para></remarks>
        protected virtual Result? OnDbException(DbException dbex) => null;

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose of the resources.
        /// </summary>
        /// <param name="disposing">Indicates whether to dispose.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing && _dbConn != null)
            {
                Logger?.LogDebug("Closing and disposing the database connection. DatabaseId: {DatabaseId}", DatabaseId);
                Invokers.Invoker.RunSync(() => OnConnectionCloseAsync(_dbConn));
                _dbConn.Dispose();
                _dbConn = null;
            }
        }

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            await DisposeAsyncCore().ConfigureAwait(false);
            Dispose(false);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose of the resources asynchronously.
        /// </summary>
        public virtual async ValueTask DisposeAsyncCore()
        {
            if (_dbConn != null)
            {
                Logger?.LogDebug("Closing and disposing the database connection. DatabaseId: {DatabaseId}", DatabaseId);
                await OnConnectionCloseAsync(_dbConn).ConfigureAwait(false);
                await _dbConn.DisposeAsync().ConfigureAwait(false);
                _dbConn = null;
            }
        }
    }
}