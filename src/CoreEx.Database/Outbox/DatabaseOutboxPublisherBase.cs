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
}