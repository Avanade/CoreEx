namespace CoreEx.Database.SqlServer.Outbox;

/// <summary>
/// Provides the <see href="https://learn.microsoft.com/en-us/sql/">SQL Server</see> <see cref="IEventPublisher"/> to be used as a <see href="https://microservices.io/patterns/data/transactional-outbox.html">transactional outbox</see>.
/// </summary>
/// <remarks>As the <see cref="SqlServerDatabase"/> is used, the <see cref="SqlServerOutboxPublisher"/> should participate in the same transaction. It is the responsibility of the caller to manage this transaction.</remarks>
public class SqlServerOutboxPublisher : DatabaseOutboxPublisherBase<SqlServerDatabase>
{
    /// <summary>
    /// Gets the default service key used when registering the service.
    /// </summary>
    /// <remarks>See related <see cref="CoreExSqlServerExtensions.AddSqlServerOutboxPublisher(IServiceCollection, Action{IServiceProvider, SqlServerOutboxPublisher}?, bool, string)"/>.</remarks>
    public const string DefaultServiceKey = "SqlServerOutbox";

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlServerOutboxPublisher"/> class.
    /// </summary>
    /// <param name="database">The <see cref="SqlServerDatabase"/>.</param>
    /// <param name="destinationProvider">The optional <see cref="IDestinationProvider"/>.</param>
    /// <param name="formatter">The optional <see cref="IEventFormatter"/>.</param>
    /// <param name="logger">The optional <see cref="ILogger"/>.</param>
    public SqlServerOutboxPublisher(SqlServerDatabase database, IDestinationProvider? destinationProvider = null, IEventFormatter? formatter = null, ILogger<SqlServerOutboxPublisher>? logger = null)
        : base(database, destinationProvider, formatter, logger)
    {
        // Attempt to automatically set the statement by convention, if possible.
        var schema = ExecutionContext.GetService<IHostSettings>()?.DomainName;
        if (schema is not null)
            Statement = SqlStatement.StoredProcedure($"[{schema}].[spOutboxEnqueue]");
    }
}