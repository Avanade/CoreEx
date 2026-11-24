namespace CoreEx.Database.Postgres.Operations;

/// <summary>
/// Provides the <see href="https://www.postgresql.org/docs/">PostgreSQL</see> <see cref="IOperationStore"/> backed by a single operations table.
/// </summary>
/// <remarks>The schema defaults to the <see cref="IHostSettings.DomainName"/> converted to <c>snake_case</c> (consistent with the outbox convention) and the table to <see cref="DefaultTableName"/>; provision the table using the exported <c>ScriptOperations_pgsql</c> migration.
/// <para>Fenced commands observe <c>clock_timestamp()</c> after acquiring the row lock; other statements use database time rather than transaction-start time. The ETag is the system <c>xmin</c> column (via the <see cref="PostgresDatabase.RowVersionConverter"/>). Writes within the same transaction can share it, and transaction IDs can wrap around; it is not guaranteed to change on every write or recreation. Use the operation identifier to distinguish incarnations and the claim fields for fencing.</para>
/// <para>The independent claim scope opens its own connection from the <see cref="NpgsqlDataSource"/> unless an <see cref="DatabaseOperationStoreBase{TDatabase}.OperationDatabaseFactory"/> is specified.</para></remarks>
/// <param name="database">The ambient <see cref="PostgresDatabase"/>.</param>
/// <param name="dataSource">The <see cref="NpgsqlDataSource"/> used to open independent connections.</param>
/// <param name="logger">The <see cref="ILogger{TCategoryName}"/>; required so that every expired operation is reported.</param>
/// <param name="settings">The <see cref="IHostSettings"/>.</param>
/// <param name="options">The optional <see cref="OperationStoreOptions"/>.</param>
/// <param name="schema">The optional schema name; defaults to the <see cref="IHostSettings.DomainName"/> converted to <c>snake_case</c>, or <see cref="DefaultSchema"/> where not available.</param>
public class PostgresOperationStore(PostgresDatabase database, NpgsqlDataSource dataSource, ILogger<PostgresOperationStore> logger, IHostSettings settings, OperationStoreOptions? options = null, string? schema = null) : DatabaseOperationStoreBase<PostgresDatabase>(database, logger, settings, options, schema ?? (string.IsNullOrEmpty(settings.ThrowIfNull().DomainName) ? DefaultSchema : SentenceCase.ToSnakeCase(settings.DomainName)!), DefaultTableName)
{
    /// <summary>
    /// Gets the default table name.
    /// </summary>
    public const string DefaultTableName = "operation";

    /// <summary>
    /// Gets the default schema name where the <see cref="IHostSettings.DomainName"/> is not available.
    /// </summary>
    public const string DefaultSchema = "public";

    private static readonly string[] _columns =
    [
        "operation_id", "domain_name", "tenant_id", "operation_name", "instance_id", "status", "initiated_by", "attempt", "claim_owner", "claim_expires_on", "cancel_requested_on", "progress",
        "error_code", "error_message", "result_location", "data", "created_on", "updated_on", "started_on", "completed_on", "abandon_after", "purge_after", "inactivity_ms", "retention_ms", "xmin"
    ];

    private readonly NpgsqlDataSource _dataSource = dataSource.ThrowIfNull();

    /// <inheritdoc/>
    protected override IReadOnlyList<string> StateColumnNames => _columns;

    /// <inheritdoc/>
    protected override string LoadStatementText(string name) => SqlStatement.FromResource<PostgresOperationStore>($"Operations.Operation{name}.pgsql").CommandText;

    /// <inheritdoc/>
    protected override PostgresDatabase CreateOperationDatabase() => new(_dataSource, Database.JsonSerializerOptions);
}
