namespace CoreEx.Cosmos.Outbox;

/// <summary>
/// Provides the pure filter/decode/publish/cleanup-delete batch logic for a <see cref="CosmosDbOutboxRelay"/>, with no knowledge of the underlying Change Feed Processor SDK - directly unit-testable by handing
/// it an <see cref="IReadOnlyCollection{T}"/> of <see cref="CosmosDbOutboxEvent"/> without any live Cosmos DB dependency for the publish path.
/// </summary>
/// <param name="serviceProvider">The root <see cref="IServiceProvider"/> - a new scope is created per <see cref="ProcessBatchAsync(IReadOnlyCollection{CosmosDbOutboxEvent}, CancellationToken)"/> call.</param>
/// <param name="containerId">The <see cref="Microsoft.Azure.Cosmos.Container"/> identifier being relayed (used for the cleanup-delete container lookup and metric tagging).</param>
/// <param name="logger">The <see cref="ILogger"/>.</param>
/// <param name="invoker">The optional <see cref="CosmosDbOutboxRelayInvoker"/>; defaults to <see cref="CosmosDbOutboxRelayInvoker.Default"/>.</param>
/// <remarks>Mirrors the role of <c>DatabaseOutboxRelayBase</c>'s per-partition relay body (filter/decode → publish → cleanup), minus the claim/lease-partition machinery SQL needs and Cosmos DB's Change Feed
/// Processor already handles via its own checkpointing.
/// <para>A fresh <see cref="IServiceScope"/> is created per batch to resolve <see cref="IEventPublisher"/> and <see cref="ICosmosDb"/> (both registered scoped) - the Change Feed Processor can invoke concurrent
/// batches for different leases, so nothing scoped can be safely captured once at construction; this also means no shared mutable publisher state exists across concurrent batches at all.</para></remarks>
public class CosmosDbOutboxRelayProcessor(IServiceProvider serviceProvider, string containerId, ILogger<CosmosDbOutboxRelayProcessor> logger, CosmosDbOutboxRelayInvoker? invoker = null)
{
    /// <summary>
    /// Gets the root <see cref="IServiceProvider"/>.
    /// </summary>
    protected IServiceProvider ServiceProvider { get; } = serviceProvider.ThrowIfNull();

    /// <summary>
    /// Gets the <see cref="Microsoft.Azure.Cosmos.Container"/> identifier being relayed.
    /// </summary>
    public string ContainerId { get; } = containerId.ThrowIfNullOrEmpty();

    /// <summary>
    /// Gets the <see cref="ILogger"/>.
    /// </summary>
    protected ILogger Logger { get; } = logger.ThrowIfNull();

    /// <summary>
    /// Gets the <see cref="CosmosDbOutboxRelayInvoker"/>.
    /// </summary>
    protected CosmosDbOutboxRelayInvoker Invoker { get; } = invoker ?? CosmosDbOutboxRelayInvoker.Default;

    /// <summary>
    /// Processes a single batch of changes as delivered by the Change Feed Processor.
    /// </summary>
    /// <param name="changes">The changed documents (may include co-located business documents - only <see cref="CosmosDbOutboxEvent.OutboxKeyPrefix"/>-prefixed ones are relayed).</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <exception cref="Exception">Any decode or publish failure is allowed to propagate - this is what feeds the owning <see cref="CosmosDbOutboxRelay"/>'s circuit breaker and lets the Change Feed Processor's own
    /// native redelivery/backoff continue. A cleanup-delete failure, by contrast, is always caught and never propagated (see remarks).</exception>
    /// <remarks>A cleanup-delete failure only ever occurs <i>after</i> a successful publish - the event has already reached its destination, so the only consequence of leaving the document behind is it sitting
    /// until its <see cref="ITimeToLive"/> expires (a bounded, self-healing storage/RU cost), not lost work. Letting such a failure propagate and pause the relay over an already-completed delivery would be
    /// wrong, so it is caught, logged, and counted (<see cref="CosmosMetrics.OutboxRelayCleanupFailed"/>) instead.</remarks>
    public virtual async Task ProcessBatchAsync(IReadOnlyCollection<CosmosDbOutboxEvent> changes, CancellationToken cancellationToken)
    {
        var outboxDocs = changes.Where(c => c.Id is not null && c.Id.StartsWith(CosmosDbOutboxEvent.OutboxKeyPrefix, StringComparison.Ordinal)).ToList();
        if (outboxDocs.Count == 0)
            return;

        var tag = new KeyValuePair<string, object?>(CosmosMetrics.ContainerTagName, ContainerId);

        await using var scope = ServiceProvider.CreateAsyncScope();
        var eventPublisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();
        var cosmosDb = scope.ServiceProvider.GetRequiredService<ICosmosDb>();

        eventPublisher.Add(outboxDocs.Select(d => new DestinationEvent(d.Destination, d.Event.DecodeToCloudEvent())));

        try
        {
            await Invoker.InvokeAsync(this, async (tracer, ct) =>
            {
                if (tracer.Activity is not null)
                {
                    tracer.Activity.AddTag("outbox.container", ContainerId);
                    tracer.Activity.AddTag("outbox.events.count", outboxDocs.Count);
                    tracer.Activity.LinkTraceContext(eventPublisher.GetEvents().Select(de => de.Event));
                }

                await eventPublisher.PublishAsync(ct).ConfigureAwait(false);
            }, cancellationToken).ConfigureAwait(false);

            CosmosMetrics.OutboxRelayPublished.Add(outboxDocs.Count, tag);

            // Oldest/newest lag is computed via min/max CloudEvent.Time across the batch rather than by indexing the first/last queued event - unlike SQL Server/Postgres's claim query (which returns rows
            // pre-ordered by enqueue time), a single Change Feed Processor delivery can span multiple logical partition keys with no guaranteed overall time ordering between them.
            var times = eventPublisher.GetEvents().Select(de => de.Event.Time ?? default).ToList();
            var now = DateTimeOffset.UtcNow;
            CosmosMetrics.OutboxRelayOldestLagDuration.Record((now - times.Min()).TotalMilliseconds);
            CosmosMetrics.OutboxRelayNewestLagDuration.Record((now - times.Max()).TotalMilliseconds);
        }
        catch (Exception ex)
        {
            CosmosMetrics.OutboxRelayPublishFailed.Add(outboxDocs.Count, tag);
            if (Logger.IsEnabled(LogLevel.Error))
                Logger.LogError(ex, "Failed to publish {Count} outbox event(s) for container '{ContainerId}': {Error}", outboxDocs.Count, ContainerId, ex.Message);

            throw;
        }
        finally
        {
            eventPublisher.Reset();
        }

        var container = cosmosDb.Container<CosmosDbOutboxEvent>(ContainerId);
        await Task.WhenAll(outboxDocs.Select(d => DeleteOneAsync(container, d, tag, cancellationToken))).ConfigureAwait(false);
    }

    /// <summary>
    /// Deletes a single, already-published outbox event document; never throws (see <see cref="ProcessBatchAsync(IReadOnlyCollection{CosmosDbOutboxEvent}, CancellationToken)"/> remarks).
    /// </summary>
    private async Task DeleteOneAsync(CosmosDbContainer<CosmosDbOutboxEvent> container, CosmosDbOutboxEvent doc, KeyValuePair<string, object?> tag, CancellationToken cancellationToken)
    {
        try
        {
            await container.DeleteAsync(CompositeKey.Create(doc.Id), new PartitionKey(doc.PartitionKey), cancellationToken).ConfigureAwait(false);
            CosmosMetrics.OutboxRelayCleanupDeleted.Add(1, tag);
        }
        catch (Exception ex)
        {
            CosmosMetrics.OutboxRelayCleanupFailed.Add(1, tag);
            if (Logger.IsEnabled(LogLevel.Warning))
                Logger.LogWarning(ex, "Failed to delete outbox event document '{Id}' after successful publish for container '{ContainerId}'; it will be removed automatically once its time-to-live expires: {Error}", doc.Id, ContainerId, ex.Message);
        }
    }
}
