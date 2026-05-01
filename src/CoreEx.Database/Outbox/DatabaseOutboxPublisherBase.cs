namespace CoreEx.Database.Outbox;

/// <summary>
/// Provides the base <see cref="IEventPublisher"/> to be used as a <see href="https://microservices.io/patterns/data/transactional-outbox.html">transactional outbox</see>.
/// </summary>
/// <param name="database">The <see cref="IDatabase"/>.</param>
/// <param name="destinationProvider">The optional <see cref="IDestinationProvider"/>.</param>
/// <param name="formatter">The optional <see cref="IEventFormatter"/>.</param>
/// <param name="logger">The optional <see cref="ILogger"/>.</param>
public abstract class DatabaseOutboxPublisherBase<TDatabase>(TDatabase database, IDestinationProvider? destinationProvider = null, IEventFormatter? formatter = null, ILogger<DatabaseOutboxPublisherBase<TDatabase>>? logger = null)
    : EventPublisherBase(destinationProvider, formatter, logger) where TDatabase : IDatabase
{
    /// <summary>
    /// Gets the underlying <typeparamref name="TDatabase"/>.
    /// </summary>
    protected TDatabase Database { get; } = database.ThrowIfNull();

    /// <summary>
    /// Gets or sets the <see cref="SqlStatement"/> used to persist each event to the underlying outbox store.
    /// </summary>
    /// <remarks>Defaults to <see cref="SqlStatement.None"/>.</remarks>
    public virtual SqlStatement Statement { get; set; } = SqlStatement.None;

    /// <summary>
    /// Gets or sets the maximum number of statements to include in a single batch operation.
    /// </summary>
    /// <remarks><para>Defaults to '<c>10</c>'.</para>
    /// Increasing the batch size may improve performance by reducing the number of round trips; however, larger batches may require more memory and could result in a timeout or other resource-related issues.
    /// <para>This is only leveraged where the <see cref="Statement"/> is a <see cref="SqlStatement.CommandType"/> of <see cref="CommandType.StoredProcedure"/>; otherwise, they are executed individually.</para></remarks>
    public int StatementBatchSize { get; set => field = value.ThrowIfLessThanOrEqualToZero(); } = 10;

    /// <summary>
    /// Gets or sets the partition size to use when calculating the partition id for each event.
    /// </summary>
    /// <remarks>This is used to ensure that events with the same partition key are stored in the same partition, which guarantees that events are processed in order within a partition.</remarks>
    public int PartitionSize { get; set; } = PartitionKey.DefaultPartitionSize;

    /// <inheritdoc/>
    protected async override Task OnPublishAsync(DestinationEvent[] events, CancellationToken cancellationToken = default)
    {
        var utc = Runtime.UtcNow.UtcDateTime;
        var ec = ExecutionContext.HasCurrent ? ExecutionContext.Current : null;

        // Where the statement is not a stored procedure, we will execute the statement for each.
        if (Statement.CommandType != CommandType.StoredProcedure)
        {
            foreach (var de in events)
            {
                var ce = de.Event;
                var pk = de.Event.GetPartitionKey();
                var partitionId = PartitionKey.GetPartitionId(string.IsNullOrEmpty(pk) ? Guid.NewGuid().ToString() : pk, PartitionSize);

                await Database.Statement(Statement)
                    .ParamWhen(!string.IsNullOrEmpty(ec?.TenantId), Database.NamedColumns.TenantIdName, () => ec!.TenantId)
                    .Param(Database.NamedColumns.PartitionIdName, partitionId)
                    .Param(Database.NamedColumns.OutboxDestinationName, de.Destination)
                    .Param(Database.NamedColumns.OutboxEventName, de.Event.EncodeToJsonElement())
                    .Param(Database.NamedColumns.OutboxEnqueuedUtcName, utc)
                    .NonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            return;
        };

        // For each into batch of up to 'StatementBatchSize' events to persist, to eek out better performance by reducing the number of round trips to the database.
        foreach (var chunk in events.Chunk(StatementBatchSize))
        {
            var sb = new StringBuilder();
            var dpc = new DatabaseParameterCollection(Database);

            for (var i = 0; i < chunk.Length; i++)
            {
                // Add each event to the batch statement and parameters collection.
                var ce = chunk[i].Event;
                var pk = chunk[i].Event.GetPartitionKey();
                var partitionId = PartitionKey.GetPartitionId(string.IsNullOrEmpty(pk) ? Guid.NewGuid().ToString() : pk, PartitionSize);

                sb.Append($"EXEC {Statement.CommandText} ");

                if (!string.IsNullOrEmpty(ec?.TenantId))
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