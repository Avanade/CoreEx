// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Mapping.Converters;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Database.SqlServer
{
    /// <summary>
    /// Provides <see href="https://learn.microsoft.com/en-us/sql/?view=sql-server-ver16">SQL Server</see> database access functionality.
    /// </summary>
    public class SqlServerDatabase : Database<SqlConnection>
    {
        private static readonly SqlServerDatabaseColumns _defaultColumns = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlServerDatabase"/> class.
        /// </summary>
        /// <param name="create">The function to create the <see cref="SqlConnection"/>.</param>
        /// <param name="logger">The optional <see cref="ILogger"/>.</param>
        /// <param name="invoker">The optional <see cref="DatabaseInvoker"/>.</param>
        public SqlServerDatabase(Func<SqlConnection> create, ILogger<SqlServerDatabase>? logger = null, DatabaseInvoker? invoker = null)
            : base(create, SqlClientFactory.Instance, logger, invoker) { }

        /// <summary>
        /// Gets or sets the names of the pre-configured <see cref="SqlServerDatabaseColumns"/>.
        /// </summary>
        /// <remarks>Do not update the default properties directly as a shared static instance is used (unless this is the desired behaviour); create a new <see cref="Extended.DatabaseColumns"/> instance for overridding.</remarks>
        public new SqlServerDatabaseColumns DatabaseColumns { get; set; } = _defaultColumns;

        /// <inheritdoc/>
        public override IConverter RowVersionConverter => StringToBase64Converter.Default;

        /// <summary>
        /// Gets or sets the stored procedure name used by <see cref="SetSqlSessionContextAsync(string, DateTime?, string?, string?, CancellationToken)"/>.
        /// </summary>
        /// <remarks>Defaults to '<c>[dbo].[spSetSessionContext]</c>'.</remarks>
        public string SessionContextStoredProcedure { get; set; } = "[dbo].[spSetSessionContext]";

        /// <summary>
        /// Indicates whether to transform the <see cref="SqlException"/> into an <see cref="Abstractions.IExtendedException"/> equivalent based on the <see cref="SqlException.Number"/>.
        /// </summary>
        /// <remarks>Transforms and throws the <see cref="Abstractions.IExtendedException"/> equivalent from the <see cref="SqlException"/> known list.</remarks>
        public bool ThrowTransformedException { get; set; } = true;

        /// <summary>
        /// Indicates whether to check the <see cref="SqlDuplicateErrorNumbers"/> when catching the <see cref="SqlException"/>.
        /// </summary>
        public bool CheckSqlDuplicateErrorNumbers { get; set; } = true;

        /// <summary>
        /// Gets or sets the list of known <see cref="SqlException.Number"/> values that are considered a duplicate error.
        /// </summary>
        /// <remarks>See <see href="https://docs.microsoft.com/en-us/sql/relational-databases/errors-events/database-engine-events-and-errors"/>
        /// and <see href="https://docs.microsoft.com/en-us/azure/sql-database/sql-database-develop-error-messages"/>.</remarks>
        public List<int> SqlDuplicateErrorNumbers { get; set; } = new List<int>(new int[] { 2601, 2627 });

        /// <summary>
        /// Sets the SQL session context using the specified values by invoking the <see cref="SessionContextStoredProcedure"/> using parameters named <see cref="SqlServerDatabaseColumns.SessionContextUsernameName"/>, 
        /// <see cref="SqlServerDatabaseColumns.SessionContextTimestampName"/>, <see cref="SqlServerDatabaseColumns.SessionContextTenantIdName"/> and <see cref="SqlServerDatabaseColumns.SessionContextUserIdName"/>.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="timestamp">The timestamp <see cref="DateTime"/> (where <c>null</c> the value will default to <see cref="DateTime.UtcNow"/>).</param>
        /// <param name="tenantId">The tenant identifer (where <c>null</c> the value will not be used).</param>
        /// <param name="userId">The unique user identifier (where <c>null</c> the value will not be used).</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <remarks>See <see href="https://docs.microsoft.com/en-us/sql/relational-databases/system-stored-procedures/sp-set-session-context-transact-sql"/>.</remarks>
        public Task SetSqlSessionContextAsync(string username, DateTime? timestamp, string? tenantId = null, string? userId = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(SessionContextStoredProcedure))
                throw new InvalidOperationException("The SessionContextStoredProcedure property must have a value.");

            return Invoker.InvokeAsync(this, username, timestamp, tenantId, userId, async (username, timestamp, tenantId, userId, ct) =>
            {
                return await StoredProcedure(SessionContextStoredProcedure)
                    .Param($"@{DatabaseColumns.SessionContextUsernameName}", username ?? ExecutionContext.EnvironmentUserName)
                    .Param($"@{DatabaseColumns.SessionContextTimestampName}", timestamp ?? Entities.Cleaner.Clean(DateTime.UtcNow))
                    .ParamWith(tenantId, $"@{DatabaseColumns.SessionContextTenantIdName}")
                    .ParamWith(userId, $"@{DatabaseColumns.SessionContextUserIdName}")
                    .NonQueryAsync(ct).ConfigureAwait(false);
            }, cancellationToken);
        }

        /// <summary>
        /// Sets the SQL session context using the <see cref="ExecutionContext"/>.
        /// </summary>
        /// <param name="executionContext">The <see cref="ExecutionContext"/>. Defaults to <see cref="ExecutionContext.Current"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <remarks>See <see cref="SetSqlSessionContextAsync(string, DateTime?, string?, string?, CancellationToken)"/> for more information.</remarks>
        public Task SetSqlSessionContextAsync(ExecutionContext? executionContext = null, CancellationToken cancellationToken = default)
        {
            var ec = executionContext ?? (ExecutionContext.HasCurrent ? ExecutionContext.Current : null);
            return (ec == null)
                ? SetSqlSessionContextAsync(null!, null, cancellationToken: cancellationToken)
                : SetSqlSessionContextAsync(ec.UserName, ec.Timestamp, ec.TenantId, ec.UserId, cancellationToken);
        }

        /// <inheritdoc/>
        protected override void OnDbException(DbException dbex)
        {
            if (!ThrowTransformedException)
            {
                base.OnDbException(dbex);
                return;
            }

            if (dbex is SqlException sex)
            {
                var msg = sex.Message?.TrimEnd();
                if (string.IsNullOrEmpty(msg))
                    msg = null;

                switch (sex.Number)
                {
                    case 56001: throw new ValidationException(msg, sex);
                    case 56002: throw new BusinessException(msg, sex);
                    case 56003: throw new AuthorizationException(msg, sex);
                    case 56004: throw new ConcurrencyException(msg, sex);
                    case 56005: throw new NotFoundException(msg, sex);
                    case 56006: throw new ConflictException(msg, sex);
                    case 56007: throw new DuplicateException(msg, sex);

                    default:
                        if (CheckSqlDuplicateErrorNumbers && SqlDuplicateErrorNumbers.Contains(sex.Number))
                            throw new DuplicateException(null, sex);

                        break;
                }
            }

            base.OnDbException(dbex);
        }
    }
}