namespace CoreEx.Database.SqlServer;

/// <summary>
/// Provides the <see href="https://learn.microsoft.com/en-us/sql/">SQL Server</see> <see cref="IDatabase"/> functionality.
/// </summary>
/// <remarks>The <see cref="OnDbException(DbException)"/> override implements transformation of pre-determined SQL Server error codes, as follows:
///  <list type="bullet">
///   <item><c>56001</c> -> <see cref="ValidationException"/>.</item>
///   <item><c>56002</c> -> <see cref="BusinessException"/>.</item>
///   <item><c>56003</c> -> <see cref="AuthorizationException"/>.</item>
///   <item><c>56004</c> -> <see cref="ConcurrencyException"/>.</item>
///   <item><c>56005</c> -> <see cref="NotFoundException"/>.</item>
///   <item><c>56006</c> -> <see cref="ConflictException"/>.</item>
///   <item><c>56007</c> -> <see cref="DuplicateException"/>.</item>
///   <item><c>56010</c> -> <see cref="DataConsistencyException"/>.</item>
///  </list>
///  <para>This is in addition to the <see cref="CheckDuplicateErrorNumbers"/> with the corresponding <see cref="DuplicateErrorNumbers"/> that will also result in a <see cref="DuplicateException"/>.</para>
///  <para>The <see cref="OnIsTransientException(Exception)"/> override additionally classifies deadlock, lock timeout, schema-recompile, and Azure SQL Database throttling/failover/connectivity error codes as transient (retryable).</para>
///  <para>This class also implements the <see cref="IUnitOfWork"/> including a <see href="https://microservices.io/patterns/data/transactional-outbox.html">transactional outbox</see>. The <see cref="IUnitOfWork"/> 
///  functionality is enabled by the <see cref="SqlServerUnitOfWorkInvoker"/>; note, this is not thread-safe.</para>
/// </remarks>
/// <param name="connection">The <see cref="SqlConnection"/>.</param>
/// <param name="jsonSerializerOptions">The optional <see cref="JsonSerializerOptions"/>.</param>
/// <param name="logger">The optional <see cref="ILogger"/>.</param>
public partial class SqlServerDatabase(SqlConnection connection, JsonSerializerOptions? jsonSerializerOptions = null, ILogger<SqlServerDatabase>? logger = null) : Database<SqlConnection, SqlServerCommand, SqlServerDatabaseArgs, SqlServerDatabaseColumns>(connection, SqlServerInvoker.Default, SqlServerDatabaseColumns.Default, jsonSerializerOptions, logger)
{
    /// <summary>
    /// Gets the default <see cref="DuplicateErrorNumbers"/>.
    /// </summary>
    /// <remarks>See <see href="https://docs.microsoft.com/en-us/sql/relational-databases/errors-events/database-engine-events-and-errors"/>
    /// and <see href="https://docs.microsoft.com/en-us/azure/sql-database/sql-database-develop-error-messages"/>.</remarks>
    public static int[] DefaultDuplicateErrorNumbers { get; } = [2601, 2627];

    /// <inheritdoc/>
    public override ISourceConverter<string?> RowVersionConverter => StringBase64Converter.Default;

    /// <summary>
    /// Indicates whether to check the <see cref="DuplicateErrorNumbers"/> when catching the <see cref="SqlException"/>.
    /// </summary>
    public bool CheckDuplicateErrorNumbers { get; set; } = true;

    /// <summary>
    /// Gets or sets the list of known <see cref="SqlException.Number"/> values that are considered a duplicate error.
    /// </summary>
    /// <remarks>Overrides the <see cref="DefaultDuplicateErrorNumbers"/>.</remarks>
    public int[]? DuplicateErrorNumbers { get; set; }

    /// <inheritdoc/>
    public override SqlServerCommand Statement(SqlStatement statement) => new(this, statement);

    /// <inheritdoc/>
    public override DbParameter CreateParameter() => new SqlParameter();

    /// <inheritdoc/>
    protected override Exception? OnDbException(DbException dbex)
    {
        if (dbex is SqlException sex)
        {
            var msg = sex.Message?.TrimEnd();
            if (string.IsNullOrEmpty(msg))
                msg = null;

            switch (sex.Number)
            {
                case 56001: return new ValidationException(msg, sex);
                case 56002: return new BusinessException(msg, sex);
                case 56003: return new AuthorizationException(msg, sex);
                case 56004: return new ConcurrencyException(msg, sex);
                case 56005: return new NotFoundException(msg, sex);
                case 56006: return new ConflictException(msg, sex);
                case 56007: return new DuplicateException(msg, sex);
                case 56010: return new DataConsistencyException(msg, sex);

                default:
                    if (CheckDuplicateErrorNumbers && (DuplicateErrorNumbers ?? DefaultDuplicateErrorNumbers).Contains(sex.Number))
                        return new DuplicateException(null, sex);

                    break;
            }
        }

        return base.OnDbException(dbex);
    }

    /// <inheritdoc/>
    /// <remarks>Includes well-known concurrency errors (deadlock, lock timeout, a stored procedure recompiled mid-flight by a migration), plus the well-known
    /// <see href="https://learn.microsoft.com/en-us/azure/azure-sql/database/troubleshoot-common-errors-issues">Azure SQL Database throttling/failover/connectivity error codes</see> - relevant
    /// because Azure SQL Database is a supported production target.</remarks>
    protected override bool OnIsTransientException(Exception exception)
    {
        if (exception is SqlException sex && sex.Errors.Count > 0)
        {
            switch (sex.Errors[0].Number)
            {
                case 1205: return true;   // Deadlock: https://learn.microsoft.com/en-us/sql/relational-databases/errors-events/mssqlserver-1205-database-engine-error
                case 1222: return true;   // Lock request time out period exceeded.
                case 2801: return true;   // The definition of object '...' has changed since it was compiled: a migration recompiled the object while this connection had a stale plan.

                // Azure SQL Database throttling/failover (resource governance, DTU/vCore limits, elastic pool, failover group).
                case 4060: return true;   // Cannot open database requested by the login (e.g. transient during failover).
                case 4221: return true;   // Login to read-secondary failed due to long wait on 'HADR_DATABASE_WAIT_FOR_TRANSITION_TO_VERSIONING'.
                case 10928: return true;  // Resource ID: Not enough resources to process request.
                case 10929: return true;  // Resource ID: The database is currently too busy.
                case 40197: return true;  // The service has encountered an error processing your request.
                case 40501: return true;  // The service is currently busy. Retry the request after 10 seconds.
                case 40613: return true;  // Database on server is not currently available.
                case 49918: return true;  // Not enough resources to process request.
                case 49919: return true;  // Too many create/update operations in progress for subscription.
                case 49920: return true;  // Too many operations in progress for subscription.

                // Network/connectivity transients.
                case 233: return true;    // The client was unable to establish a connection.
                case 64: return true;     // A connection was successfully established but then an error occurred during login.
                case 20: return true;     // The instance of SQL Server does not support encryption (transient during connection negotiation).
                case -2: return true;     // Client-side command/connection timeout.
                case 10053: return true;  // A transport-level error occurred: connection aborted.
                case 10054: return true;  // A transport-level error occurred: connection forcibly closed.
                case 10060: return true;  // A network-related or instance-specific error: connection attempt timed out.
            }
        }

        return base.OnIsTransientException(exception);
    }
}