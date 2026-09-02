namespace CoreEx.Cosmos.Outbox;

/// <summary>
/// Provides the <see href="https://learn.microsoft.com/en-us/azure/cosmos-db/">Azure Cosmos DB</see> <see cref="IEventPublisher"/> to be used as a
/// <see href="https://microservices.io/patterns/data/transactional-outbox.html">transactional outbox</see>, in conjunction with a <see cref="CosmosDbUnitOfWork"/>.
/// </summary>
/// <param name="cosmosDb">The <see cref="ICosmosDb"/>.</param>
/// <param name="destinationProvider">The optional <see cref="IDestinationProvider"/>.</param>
/// <param name="formatter">The optional <see cref="IEventFormatter"/>.</param>
/// <param name="logger">The optional <see cref="ILogger"/>.</param>
/// <remarks>Unlike a relational outbox (a dedicated table, inserted into within the same database transaction), an outbox event document here is written into the <i>same</i> container/partition as the
/// paired business mutation, in the <i>same</i> <see cref="TransactionalBatch"/> — Cosmos DB's only atomic multi-operation primitive supports a single container only, so a dedicated outbox container is not
/// possible while preserving atomicity. See <see cref="CosmosDbUnitOfWork"/> for the full orchestration, and <see cref="CosmosDbModelOptions{TModel}.ApplyFilters(CosmosDbArgs, IQueryable{TModel}, ExecutionContext)"/>
/// for how these co-located documents are automatically kept invisible to ordinary business queries.</remarks>
public class CosmosDbEventPublisher(ICosmosDb cosmosDb, IDestinationProvider? destinationProvider = null, IEventFormatter? formatter = null, ILogger<CosmosDbEventPublisher>? logger = null)
    : EventPublisherBase(destinationProvider, formatter, logger)
{
    /// <summary>
    /// Gets the default outbox event time-to-live, in seconds (7 days).
    /// </summary>
    /// <remarks>See <see cref="OutboxTimeToLiveSeconds"/> for the trade-off this default represents.</remarks>
    public const int DefaultOutboxTimeToLiveSeconds = 60 * 60 * 24 * 7;

    /// <summary>
    /// Gets the <see cref="ICosmosDb"/>.
    /// </summary>
    protected ICosmosDb CosmosDb { get; } = cosmosDb.ThrowIfNull();

    /// <summary>
    /// Gets or sets the time-to-live (in seconds) applied to every outbox event document (see <see cref="CosmosDbOutboxEvent.TimeToLive"/>).
    /// </summary>
    /// <remarks>Defaults to <see cref="DefaultOutboxTimeToLiveSeconds"/> (7 days). This is a real trade-off, not a free safety net: without a relay consuming these documents (not part of this package),
    /// they would otherwise accumulate indefinitely (storage/RU cost, forever); a TTL bounds that. But if a future relay outage or bug ever runs longer than this window, the affected events are gone
    /// permanently — Cosmos DB physically deletes expired documents, with no recovery — which is in tension with "guaranteed at-least-once delivery". Tune this once the operational characteristics of
    /// whatever relay eventually consumes these documents are known; it is not a fixed law.</remarks>
    public int OutboxTimeToLiveSeconds { get; set => field = value.ThrowIfLessThanOrEqualToZero(); } = DefaultOutboxTimeToLiveSeconds;

    /// <inheritdoc/>
    /// <exception cref="InvalidOperationException">Thrown where there is no active <see cref="CosmosDbUnitOfWork"/> <see cref="IUnitOfWork.TransactionAsync(Func{CancellationToken, Task}, CancellationToken)"/>
    /// scope, or where no business mutation has yet been enlisted within it (an outbox event has no container/partition key to bind to otherwise — see <see cref="CosmosDbTransaction.BoundContainer"/>/
    /// <see cref="CosmosDbTransaction.BoundPartitionKeyValue"/>).</exception>
    protected override Task OnPublishAsync(DestinationEvent[] events, CancellationToken cancellationToken = default)
    {
        var txn = CosmosDb.CurrentTransaction
            ?? throw new InvalidOperationException($"{nameof(CosmosDbEventPublisher)} can only publish within an active {nameof(CosmosDbUnitOfWork)} ({nameof(IUnitOfWork.TransactionAsync)}) scope.");

        if (!txn.HasOperations)
            throw new InvalidOperationException($"{nameof(CosmosDbEventPublisher)} requires at least one business mutation to already be enlisted in the current unit-of-work; an outbox event document has no container/partition key to bind to otherwise.");

        var container = txn.BoundContainer!;
        var partitionKeyValue = txn.BoundPartitionKeyValue
            ?? throw new InvalidOperationException($"{nameof(CosmosDbEventPublisher)} requires the enlisted business model to expose its partition key value (see CosmosDbModelOptions<TModel>.PartitionKeySupport); it could not be resolved for the current unit-of-work.");

        var partitionKey = new PartitionKey(partitionKeyValue);

        foreach (var de in events)
        {
            var outboxEvent = new CosmosDbOutboxEvent
            {
                Id = CompositeKey.Create(CosmosDbOutboxEvent.OutboxKeyPrefix, Guid.NewGuid()).ToString()!,
                PartitionKey = partitionKeyValue,
                Destination = de.Destination,
                Event = de.Event.EncodeToJsonElement(),
                TimeToLive = OutboxTimeToLiveSeconds
            };

            txn.Enlist(container, partitionKey, partitionKeyValue, CompositeKey.Create(outboxEvent.Id), b => b.CreateItem(outboxEvent));
        }

        return Task.CompletedTask;
    }
}
