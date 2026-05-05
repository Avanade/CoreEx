namespace CoreEx.Database.Postgres.Outbox;

/// <summary>
/// Provides the <see href="https://www.postgresql.org/docs/">PostgreSQL</see> <see cref="IEventPublisher"/> to be used as a <see href="https://microservices.io/patterns/data/transactional-outbox.html">transactional outbox</see>.
/// </summary>
/// <remarks>As the <see cref="PostgresDatabase"/> is used, the <see cref="PostgresOutboxPublisher"/> should participate in the same transaction. It is the responsibility of the caller to manage this transaction.</remarks>
public class PostgresOutboxPublisher : DatabaseOutboxPublisherBase<PostgresDatabase>
{
    /// <summary>
    /// Gets the default service key used when registering the service.
    /// </summary>
    /// <remarks>See related <see cref="CoreExPostgresExtensions.AddPostgresOutboxPublisher(IServiceCollection, Action{IServiceProvider, PostgresOutboxPublisher}?, bool, string)"/>.</remarks>
    public const string DefaultServiceKey = "PostgresOutbox";

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgresOutboxPublisher"/> class.
    /// </summary>
    /// <param name="database">The <see cref="PostgresDatabase"/>.</param>
    /// <param name="destinationProvider">The optional <see cref="IDestinationProvider"/>.</param>
    /// <param name="formatter">The optional <see cref="IEventFormatter"/>.</param>
    /// <param name="logger">The optional <see cref="ILogger"/>.</param>
    public PostgresOutboxPublisher(PostgresDatabase database, IDestinationProvider? destinationProvider = null, IEventFormatter? formatter = null, ILogger<PostgresOutboxPublisher>? logger = null)
        : base(database, destinationProvider, formatter, logger)
    {
        // Attempt to automatically set the statement by convention, if possible.
        var schema = ExecutionContext.GetService<IHostSettings>()?.DomainName;
        if (schema is not null)
            Statement = SqlStatement.StoredProcedure($"\"{SentenceCase.ToSnakeCase(schema)}\".\"sp_outbox_enqueue\"");
    }
}