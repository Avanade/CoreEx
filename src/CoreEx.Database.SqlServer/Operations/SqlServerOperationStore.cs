namespace CoreEx.Database.SqlServer.Operations;

/// <summary>
/// Provides the <see href="https://learn.microsoft.com/en-us/sql/sql-server/">SQL Server</see> <see cref="IOperationStore"/> backed by a single operations table.
/// </summary>
/// <remarks>The schema defaults to the <see cref="IHostSettings.DomainName"/> (consistent with the outbox convention) and the table to <see cref="DefaultTableName"/>; provision the table using the exported <c>ScriptOperations_sql</c> migration.
/// <para>The independent claim scope uses a clone of the ambient <see cref="SqlConnection"/> (preserving its connection string and credentials) unless an <see cref="DatabaseOperationStoreBase{TDatabase}.OperationDatabaseFactory"/> is specified.</para></remarks>
/// <param name="database">The ambient <see cref="SqlServerDatabase"/>.</param>
/// <param name="logger">The <see cref="ILogger{TCategoryName}"/>; required so that every expired operation is reported.</param>
/// <param name="settings">The <see cref="IHostSettings"/>.</param>
/// <param name="options">The optional <see cref="OperationStoreOptions"/>.</param>
/// <param name="schema">The optional schema name; defaults to the <see cref="IHostSettings.DomainName"/>, or <see cref="DefaultSchema"/> where not available.</param>
public class SqlServerOperationStore(SqlServerDatabase database, ILogger<SqlServerOperationStore> logger, IHostSettings settings, OperationStoreOptions? options = null, string? schema = null) : DatabaseOperationStoreBase<SqlServerDatabase>(database, logger, settings, options, schema ?? (string.IsNullOrEmpty(settings.ThrowIfNull().DomainName) ? DefaultSchema : settings.DomainName), DefaultTableName)
{
    /// <summary>
    /// Gets the default table name.
    /// </summary>
    public const string DefaultTableName = "Operation";

    /// <summary>
    /// Gets the default schema name where the <see cref="IHostSettings.DomainName"/> is not available.
    /// </summary>
    public const string DefaultSchema = "dbo";

    private static readonly string[] _columns =
    [
        "OperationId", "DomainName", "TenantId", "OperationName", "InstanceId", "Status", "InitiatorId", "Attempt", "ClaimOwner", "ClaimExpiresOn", "CancelRequestedOn", "Progress",
        "ErrorCode", "ErrorMessage", "ResultLocation", "Data", "CreatedOn", "UpdatedOn", "StartedOn", "CompletedOn", "AbandonAfter", "PurgeAfter", "RowVersion"
    ];

    /// <inheritdoc/>
    protected override IReadOnlyList<string> StateColumnNames => _columns;

    /// <inheritdoc/>
    protected override string LoadStatementText(string name) => SqlStatement.FromResource<SqlServerOperationStore>($"Operations.Operation{name}.sql").CommandText;

    /// <inheritdoc/>
    protected override SqlServerDatabase CreateOperationDatabase()
    {
        var connection = (SqlConnection)((ICloneable)((IDatabase)Database).Connection).Clone();
        return new SqlServerDatabase(connection, Database.JsonSerializerOptions);
    }

    /// <inheritdoc/>
    protected override void ReleaseOperationDatabase(SqlServerDatabase database)
    {
        var connection = ((IDatabase)database).Connection;
        try
        {
            base.ReleaseOperationDatabase(database);
        }
        finally
        {
            connection.Dispose();
        }
    }
}
