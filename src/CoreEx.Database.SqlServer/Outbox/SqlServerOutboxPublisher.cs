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

    /// <inheritdoc/>
    protected async override Task OnPublishAsync(DestinationEvent[] events, CancellationToken cancellationToken = default)
    {
        var utc = Runtime.UtcNow.UtcDateTime;
        var ec = ExecutionContext.HasCurrent ? ExecutionContext.Current : null;
        var hasTenantId = !string.IsNullOrEmpty(ec?.TenantId);

        // The statement must be a stored procedure.
        if (Statement.CommandType != CommandType.StoredProcedure)
            throw new InvalidOperationException($"The {nameof(Statement)}.{nameof(Statement.CommandType)} must be {nameof(CommandType.StoredProcedure)}.");

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

                sb.Append($"EXEC {Statement.CommandText} ");

                if (hasTenantId)
                    AddParameter(sb, dpc, Database.NamedColumns.TenantIdName, ec!.TenantId, i);

                AddParameter(sb, dpc, Database.NamedColumns.PartitionIdName, partitionId, i);
                AddParameter(sb, dpc, Database.NamedColumns.OutboxDestinationName, chunk[i].Destination, i);
                AddParameter(sb, dpc, Database.NamedColumns.OutboxEventName, chunk[i].Event.EncodeToJsonElement(), i);
                AddParameter(sb, dpc, Database.NamedColumns.OutboxEnqueuedUtcName, utc, i, false);
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
    private static void AddParameter(StringBuilder sb, DatabaseParameterCollection dpc, string name, object? value, int index, bool appendComma = true)
    {
        sb.Append(DatabaseParameterCollection.ParameterizeName(name)).Append(" = ").Append(DatabaseParameterCollection.ParameterizeName($"{name}_{index}"));
        if (appendComma)
            sb.Append(", ");
        else
            sb.AppendLine(";");

        dpc.AddParameter($"{name}_{index}", value);
    }
}