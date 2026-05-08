namespace CoreEx.Database.Postgres.Outbox;

/// <summary>
/// Provides the <see href="https://www.postgresql.org/docs/">PostgreSQL</see> <see cref="IEventPublisher"/> to be used as a <see href="https://microservices.io/patterns/data/transactional-outbox.html">transactional outbox</see>.
/// </summary>
/// <remarks>As the <see cref="PostgresDatabase"/> is used, the <see cref="PostgresOutboxPublisher"/> should participate in the same transaction. It is the responsibility of the caller to manage this transaction.
/// <para>Note: The <see cref="DatabaseOutboxPublisherBase{TDatabase}.Statement"/> should be set to a SQL function that enqueues the outbox events; the function must accept parameters (positionally, in the order specified) for the partition ID, destination, event data, enqueued UTC, and optional tenant.</para></remarks>
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
            Statement = SqlStatement.FromText($"SELECT \"{SentenceCase.ToSnakeCase(schema)}\".\"fn_outbox_enqueue\"");
    }

    /// <inheritdoc/>
    protected async override Task OnPublishAsync(DestinationEvent[] events, CancellationToken cancellationToken = default)
    {
        var utc = Runtime.UtcNow.UtcDateTime;
        var ec = ExecutionContext.HasCurrent ? ExecutionContext.Current : null;
        var hasTenantId = !string.IsNullOrEmpty(ec?.TenantId);

        // The statement must be SQL text; which in turn should (assumes) executes a function.
        if (Statement.CommandType != CommandType.Text)
            throw new InvalidOperationException($"The {nameof(Statement)}.{nameof(Statement.CommandType)} must be {nameof(CommandType.Text)}.");

        // For each into batch of up to 'StatementBatchSize' events to persist, to eek out better performance by reducing the number of round trips to the database.
        foreach (var chunk in events.Chunk(StatementBatchSize))
        {
            var sb = new StringBuilder();
            var dpc = new DatabaseParameterCollection(Database);

            for (var i = 0; i < chunk.Length; i++)
            {
                // Add each event to the batch statement and parameters collection.
                var pk = chunk[i].Event.GetPartitionKey();
                var partitionId = PartitionKey.GetPartitionId(string.IsNullOrEmpty(pk) ? Guid.NewGuid().ToString() : pk, PartitionSize);

                sb.Append($"{Statement.CommandText}(");

                // Parameters in PostgreSQL are positional; so we need to emit in the correct order and with the correct types.
                AddParameter(sb, dpc, Database.NamedColumns.PartitionIdName, partitionId, NpgsqlDbType.Integer, i);
                AddParameter(sb, dpc, Database.NamedColumns.OutboxDestinationName, chunk[i].Destination, NpgsqlDbType.Varchar, i);
                AddParameter(sb, dpc, Database.NamedColumns.OutboxEventName, chunk[i].Event.EncodeToJsonElement(), NpgsqlDbType.Text, i);
                AddParameter(sb, dpc, Database.NamedColumns.OutboxEnqueuedUtcName, utc, NpgsqlDbType.TimestampTz, i, hasTenantId);

                if (hasTenantId)
                    AddParameter(sb, dpc, Database.NamedColumns.TenantIdName, ec!.TenantId, NpgsqlDbType.Varchar, i, false);
            }

            if (Logger?.IsEnabled(LogLevel.Debug) is true)
                Logger.LogDebug("Executing batch statement to persist {Count} event(s) as a single database command.", chunk.Length);

            // Execute the batch statement with the parameters collection.
            var ds = Database.Statement(new SqlStatement(CommandType.Text, sb.ToString()));
            ds.Parameters.AddRange(dpc);
            await ds.NonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Adds the parameter to the <see cref="StringBuilder"/> and <see cref="DatabaseParameterCollection"/>.
    /// </summary>
    private static void AddParameter(StringBuilder sb, DatabaseParameterCollection dpc, string name, object? value, NpgsqlDbType dbType, int index, bool appendComma = true)
    {
        sb.Append(DatabaseParameterCollection.ParameterizeName($"{name}_{index}"));
        if (appendComma)
            sb.Append(", ");
        else
            sb.AppendLine(");");

        dpc.AddParameter($"{name}_{index}", value, dbType);
    }
}