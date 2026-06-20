namespace CoreEx.Database.SqlServer;

public partial class SqlServerDatabase
{
    /// <summary>
    /// Gets or sets the <see cref="SqlStatement"/> used by <see cref="SetSqlSessionContextAsync(ExecutionContext?, CancellationToken)"/>.
    /// </summary>
    /// <remarks>Defaults to <see cref="SqlStatement.StoredProcedure(string)"/> to invoke '<c>[dbo].[spSetSessionContext]</c>'.</remarks>
    public SqlStatement SessionContextStatement { get; set => field = value.ThrowIfNull(); } = SqlStatement.StoredProcedure("[dbo].[spSetSessionContext]");

    /// <summary>
    /// Sets the SQL session context using the specified values by invoking the <see cref="SessionContextStatement"/> using parameters named <see cref="SqlServerDatabaseColumns.SessionContextUsernameName"/>, 
    /// <see cref="SqlServerDatabaseColumns.SessionContextTimestampName"/>, <see cref="SqlServerDatabaseColumns.SessionContextTenantIdName"/> and <see cref="SqlServerDatabaseColumns.SessionContextUserIdName"/>.
    /// </summary>
    /// <param name="username">The username (where <see langword="null"/> the value will default to <see cref="ExecutionContext.User"/> <see cref="AuthenticationUser.UserName"/>).</param>
    /// <param name="timestamp">The timestamp <see cref="DateTimeOffset"/> (where <see langword="null"/> the value will default to <see cref="Runtime.UtcNow"/>).</param>
    /// <param name="tenantId">The tenant identifer (where <see langword="null"/> the value will not be used).</param>
    /// <param name="userId">The unique user identifier (where <see langword="null"/> the value will not be used).</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <remarks>See <see href="https://docs.microsoft.com/en-us/sql/relational-databases/system-stored-procedures/sp-set-session-context-transact-sql"/>.</remarks>
    public Task SetSqlSessionContextAsync(string? username, DateTimeOffset? timestamp, string? tenantId = null, string? userId = null, CancellationToken cancellationToken = default)
    {
        return Invoker.InvokeAsync(this, DbArgs, async (_, _, cancellationToken) =>
        {
            var r = await Statement(SessionContextStatement)
                .Param(NamedColumns.SessionContextUsernameName, username ?? AuthenticationUser.EnvironmentUser.UserName)
                .Param(NamedColumns.SessionContextTimestampName, timestamp ?? Runtime.UtcNow)
                .ParamWith(tenantId, NamedColumns.SessionContextTenantIdName)
                .ParamWith(userId, NamedColumns.SessionContextUserIdName)
                .NonQueryAsync(cancellationToken).ConfigureAwait(false);
        }, cancellationToken, nameof(SetSqlSessionContextAsync));
    }

    /// <summary>
    /// Sets the SQL session context using the <see cref="ExecutionContext"/>.
    /// </summary>
    /// <param name="executionContext">The <see cref="ExecutionContext"/>. Defaults to <see cref="ExecutionContext.Current"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <remarks>See <see cref="SetSqlSessionContextAsync(string?, DateTimeOffset?, string?, string?, CancellationToken)"/> for more information.</remarks>
    public async Task SetSqlSessionContextAsync(ExecutionContext? executionContext = null, CancellationToken cancellationToken = default)
    {
        if (executionContext is not null || ExecutionContext.TryGetCurrent(out executionContext))
            await SetSqlSessionContextAsync(executionContext.User?.UserName, executionContext.Timestamp, executionContext.TenantId, executionContext.User?.Id, cancellationToken).ConfigureAwait(false);
        else
            await SetSqlSessionContextAsync(null, null, cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}