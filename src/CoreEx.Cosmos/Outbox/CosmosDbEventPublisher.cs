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
    /// Gets the default service key used for the underlying <see cref="IEventPublisher"/> registration.
    /// </summary>
    /// <remarks>See related <c>CoreExCosmosExtensions.AddCosmosDbEventPublisher(IServiceCollection, Action{IServiceProvider, CosmosDbEventPublisher}?, bool, string)</c>.</remarks>
    public const string DefaultServiceKey = "CosmosOutbox";

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

    // CosmosDbOutboxEvent always serializes its partition key under the fixed JSON property name "partitionKey" (matching CosmosDbModelBase's convention). That is only correct if the container's actual,
    // physical partition-key path (a container-creation-time setting, wholly independent of any C# property/JsonPropertyName) is literally "/partitionKey" - e.g. a model implementing IPartitionKey
    // directly with a different [JsonPropertyName] to match a container whose real path is "/tenantId" would silently produce an outbox document with no value at that path, and TransactionalBatch
    // (which requires every enlisted operation to agree on the exact same partition key) would then fail with an undiagnosable BadRequest. Rather than let that happen silently, the first outbox publish
    // against a given container validates (and thereafter caches, since a container's partition-key path is immutable for its lifetime) that its actual path is "/partitionKey", failing fast with a clear,
    // actionable exception otherwise. Keyed by (Database.Id, Container.Id) rather than the CosmosDb instance, since this reflects a physical, permanent fact about the container itself, safely shared
    // process-wide regardless of how many CosmosDb/CosmosDbEventPublisher instances (e.g. one per request) ever touch it.
    private static readonly ConcurrentDictionary<(string DatabaseId, string ContainerId), bool> _validatedOutboxContainers = new();

    /// <inheritdoc/>
    /// <exception cref="InvalidOperationException">Thrown where there is no active <see cref="CosmosDbUnitOfWork"/> <see cref="IUnitOfWork.TransactionAsync(Func{CancellationToken, Task}, CancellationToken)"/>
    /// scope, where no business mutation has yet been enlisted within it (an outbox event has no container/partition key to bind to otherwise — see <see cref="CosmosDbTransaction.BoundContainer"/>/
    /// <see cref="CosmosDbTransaction.BoundPartitionKeyValue"/>), or where the bound container's actual partition-key path is not <c>/partitionKey</c> (see remarks).</exception>
    /// <remarks><see cref="CosmosDbOutboxEvent"/> always serializes its partition key under the fixed JSON property name <c>partitionKey</c>; this is only correct where the container's actual,
    /// physical partition-key path is <c>/partitionKey</c> - see the container-level check performed here (once per container, cached thereafter) for what happens otherwise.</remarks>
    protected override async Task OnPublishAsync(DestinationEvent[] events, CancellationToken cancellationToken = default)
    {
        var txn = CosmosDb.CurrentTransaction
            ?? throw new InvalidOperationException($"{nameof(CosmosDbEventPublisher)} can only publish within an active {nameof(CosmosDbUnitOfWork)} ({nameof(IUnitOfWork.TransactionAsync)}) scope.");

        if (!txn.HasOperations)
            throw new InvalidOperationException($"{nameof(CosmosDbEventPublisher)} requires at least one business mutation to already be enlisted in the current unit-of-work; an outbox event document has no container/partition key to bind to otherwise.");

        var container = txn.BoundContainer!;
        await EnsureOutboxPartitionKeyPathAsync(container, cancellationToken).ConfigureAwait(false);

        // BoundPartitionKeyValue is null where the enlisted business mutation's own partition key resolved to PartitionKey.None - a real, valid single logical partition (not an error; the simplest
        // possible container shape), so the outbox event document is co-located there too, exactly the same as any other partition key value.
        var partitionKeyValue = txn.BoundPartitionKeyValue;
        var partitionKey = partitionKeyValue is null ? PartitionKey.None : new PartitionKey(partitionKeyValue);

        foreach (var de in events)
        {
            var outboxEvent = new CosmosDbOutboxEvent
            {
                Id = CompositeKey.Create(CosmosDbOutboxEvent.OutboxKeyPrefix, Runtime.NewGuid()).ToString()!,
                PartitionKey = partitionKeyValue,
                Destination = de.Destination,
                Event = de.Event.EncodeToJsonElement(),
                TimeToLive = OutboxTimeToLiveSeconds
            };

            txn.Enlist(container, partitionKey, partitionKeyValue, CompositeKey.Create(outboxEvent.Id), b => b.CreateItem(outboxEvent));
        }
    }

    /// <summary>
    /// Validates (once per container, then caches the result for the remaining process lifetime) that <paramref name="container"/>'s actual, physical partition-key path is <c>/partitionKey</c> -
    /// see the remarks on <see cref="_validatedOutboxContainers"/>/<see cref="OnPublishAsync(DestinationEvent[], CancellationToken)"/> for why this matters and why caching is safe.
    /// </summary>
    private static async Task EnsureOutboxPartitionKeyPathAsync(Container container, CancellationToken cancellationToken)
    {
        var key = (container.Database.Id, container.Id);
        if (_validatedOutboxContainers.ContainsKey(key))
            return;

        var response = await container.ReadContainerAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        var paths = response.Resource.PartitionKeyPaths;

        if (paths.Count != 1 || paths[0] != "/partitionKey")
        {
            throw new InvalidOperationException(
                $"{nameof(CosmosDbEventPublisher)} requires container '{container.Id}' to use the partition key path '/partitionKey' (matching {nameof(CosmosDbOutboxEvent)}'s fixed JSON property " +
                $"name), but it is actually configured with partition key path(s) '{string.Join(", ", paths)}'. The transactional outbox document has no way to carry a value at a different, " +
                "application-chosen path, so a TransactionalBatch enlisting both the business mutation and the outbox event would fail. Either recreate the container with '/partitionKey' as its " +
                $"partition key path, or do not use {nameof(CosmosDbEventPublisher)}/{nameof(CosmosDbUnitOfWork)} against this container.");
        }

        _validatedOutboxContainers[key] = true;
    }
}
