// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Mapping.Converters;
using CoreEx.Results;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Linq;
using System.Data.Common;
using System.Threading.Tasks;
using System.Threading;

namespace CoreEx.Database.Postgres
{
    /// <summary>
    /// Provides <see href="https://www.npgsql.org/">Npgsql (PostgreSQL)</see> database access functionality.
    /// </summary>
    /// <param name="create">The function to create the <see cref="NpgsqlConnection"/>.</param>
    /// <param name="logger">The optional <see cref="ILogger"/>.</param>
    /// <param name="invoker">The optional <see cref="DatabaseInvoker"/>.</param>
    public class PostgresDatabase(Func<NpgsqlConnection> create, ILogger<PostgresDatabase>? logger = null, DatabaseInvoker? invoker = null) : Database<NpgsqlConnection>(create, NpgsqlFactory.Instance, logger, invoker)
    {
        private static readonly PostgresDatabaseColumns _defaultColumns = new();

        /// <summary>
        /// Gets the default <see cref="DuplicateErrorNumbers"/>.
        /// </summary>
        /// <remarks>See <see href="https://dev.Npgsql.com/doc/Npgsql-errors/8.0/en/server-error-reference.html"/>.</remarks>
        public static string[] DefaultDuplicateErrorNumbers { get; } = ["23505"];

        /// <summary>
        /// Gets or sets the names of the pre-configured <see cref="PostgresDatabaseColumns"/>.
        /// </summary>
        /// <remarks>Do not update the default properties directly as a shared static instance is used (unless this is the desired behaviour); create a new <see cref="PostgresDatabaseColumns"/> instance for overridding.</remarks>
        public new PostgresDatabaseColumns DatabaseColumns { get; set; } = _defaultColumns;

        /// <summary>
        /// Gets or sets the stored procedure name used by <see cref="SetPostgresSessionContextAsync(string?, DateTime?, string?, string?, CancellationToken)"/>.
        /// </summary>
        /// <remarks>Defaults to '<c>"public"."sp_set_session_context"</c>'.</remarks>
        public string SessionContextStoredProcedure { get; set; } = "\"public\".\"sp_set_session_context\"";

        /// <inheritdoc/>
        public override IConverter RowVersionConverter => EncodedStringToUInt32Converter.Default;

        /// <summary>
        /// Indicates whether to transform the <see cref="PostgresException"/> into an <see cref="Abstractions.IExtendedException"/> equivalent based on the <see cref="PostgresException.SqlState"/>.
        /// </summary>
        /// <remarks>Transforms and throws the <see cref="Abstractions.IExtendedException"/> equivalent from the <see cref="PostgresException"/> known list.</remarks>
        public bool ThrowTransformedException { get; set; } = true;

        /// <summary>
        /// Indicates whether to check the <see cref="DuplicateErrorNumbers"/> when catching the <see cref="PostgresException"/>.
        /// </summary>
        public bool CheckDuplicateErrorNumbers { get; set; } = true;

        /// <summary>
        /// Gets or sets the list of known <see cref="PostgresException.SqlState"/> values that are considered a duplicate error.
        /// </summary>
        /// <remarks>Overrides the <see cref="DefaultDuplicateErrorNumbers"/>.</remarks>
        public string[]? DuplicateErrorNumbers { get; set; }

        /// <summary>
        /// Sets the PostgreSQL context using the specified values by invoking the <see cref="SessionContextStoredProcedure"/> using parameters named <see cref="PostgresDatabaseColumns.SessionContextUsernameName"/>, 
        /// <see cref="PostgresDatabaseColumns.SessionContextTimestampName"/>, <see cref="PostgresDatabaseColumns.SessionContextTenantIdName"/> and <see cref="PostgresDatabaseColumns.SessionContextUserIdName"/>.
        /// </summary>
        /// <param name="username">The username (where <c>null</c> the value will default to <see cref="ExecutionContext.EnvironmentUserName"/>).</param>
        /// <param name="timestamp">The timestamp <see cref="DateTime"/> (where <c>null</c> the value will default to <see cref="SystemTime.Timestamp"/>).</param>
        /// <param name="tenantId">The tenant identifer (where <c>null</c> the value will not be used).</param>
        /// <param name="userId">The unique user identifier (where <c>null</c> the value will not be used).</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <remarks>See <see href="https://docs.microsoft.com/en-us/sql/relational-databases/system-stored-procedures/sp-set-session-context-transact-sql"/>.</remarks>
        public Task SetPostgresSessionContextAsync(string? username, DateTime? timestamp, string? tenantId = null, string? userId = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(SessionContextStoredProcedure))
                throw new InvalidOperationException("The SessionContextStoredProcedure property must have a value.");

            return Invoker.InvokeAsync(this, username, timestamp, tenantId, userId, async (_, username, timestamp, tenantId, userId, ct) =>
            {
                return await StoredProcedure(SessionContextStoredProcedure)
                    .Param($"@{DatabaseColumns.SessionContextUsernameName}", username ?? ExecutionContext.EnvironmentUserName)
                    .Param($"@{DatabaseColumns.SessionContextTimestampName}", timestamp ?? SystemTime.Timestamp)
                    .ParamWith(tenantId, $"@{DatabaseColumns.SessionContextTenantIdName}")
                    .ParamWith(userId, $"@{DatabaseColumns.SessionContextUserIdName}")
                    .NonQueryAsync(ct).ConfigureAwait(false);
            }, cancellationToken, nameof(SetPostgresSessionContextAsync));
        }

        /// <summary>
        /// Sets the PostgreSQL session context using the <see cref="ExecutionContext"/>.
        /// </summary>
        /// <param name="executionContext">The <see cref="ExecutionContext"/>. Defaults to <see cref="ExecutionContext.Current"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <remarks>See <see cref="SetPostgresSessionContextAsync(string, DateTime?, string?, string?, CancellationToken)"/> for more information.</remarks>
        public Task SetPostgresSessionContextAsync(ExecutionContext? executionContext = null, CancellationToken cancellationToken = default)
        {
            var ec = executionContext ?? (ExecutionContext.HasCurrent ? ExecutionContext.Current : null);
            return (ec == null)
                ? SetPostgresSessionContextAsync(null!, null, cancellationToken: cancellationToken)
                : SetPostgresSessionContextAsync(ec.UserName, ec.Timestamp, ec.TenantId, ec.UserId, cancellationToken);
        }

        /// <inheritdoc/>
        protected override Result? OnDbException(DbException dbex)
        {
            if (ThrowTransformedException && dbex is PostgresException pex)
            {
                var msg = pex.MessageText?.TrimEnd();
                if (string.IsNullOrEmpty(msg))
                    msg = null;

                switch (pex.SqlState)
                {
                    case "56001": return Result.Fail(new ValidationException(msg, pex));
                    case "56002": return Result.Fail(new BusinessException(msg, pex));
                    case "56003": return Result.Fail(new AuthorizationException(msg, pex));
                    case "56004": return Result.Fail(new ConcurrencyException(msg, pex));
                    case "56005": return Result.Fail(new NotFoundException(msg, pex));
                    case "56006": return Result.Fail(new ConflictException(msg, pex));
                    case "56007": return Result.Fail(new DuplicateException(msg, pex));
                    case "56010": return Result.Fail(new DataConsistencyException(msg, pex));

                    default:
                        if (CheckDuplicateErrorNumbers && (DuplicateErrorNumbers ?? DefaultDuplicateErrorNumbers).Contains(pex.SqlState))
                            return Result.Fail(new DuplicateException(null, pex));

                        break;
                }
            }

            return base.OnDbException(dbex);
        }
    }
}