namespace CoreEx.Cosmos;

/// <summary>
/// Provides the <see href="https://learn.microsoft.com/en-us/azure/cosmos-db/">Azure Cosmos DB</see> metrics.
/// </summary>
/// <remarks>Naming is harmonized with <c>SqlServerMetrics</c>/<c>PostgresMetrics</c> (<c>Outbox{Relay}Xxx</c>) so the same metric concept has the same name across all three outbox relay implementations.</remarks>
public static class CosmosMetrics
{
    /// <summary>
    /// Gets the tag name used to identify which container a metric relates to; a single relay host can run against multiple containers (see <c>AddCosmosDbOutboxRelayHostedService</c>), so this distinguishes them.
    /// </summary>
    public const string ContainerTagName = "cosmos.container";

    /// <summary>
    /// Gets the meter used for the Cosmos DB outbox metrics.
    /// </summary>
    public static Meter Meter { get; } = new("CoreEx.Cosmos.Outbox");

    /// <summary>
    /// Gets the counter representing the total number of outbox event documents enqueued successfully.
    /// </summary>
    public static Counter<long> OutboxEnqueued { get; } = Meter.CreateCounter<long>("cosmos.outbox.enqueue", unit: "{message}", description: "Number of Cosmos DB outbox event documents enqueued successfully.");

    /// <summary>
    /// Gets the counter representing the total number of outbox event documents (batch) relayed (published) successfully.
    /// </summary>
    public static Counter<long> OutboxRelayPublished { get; } = Meter.CreateCounter<long>("cosmos.outbox.relay.publish", unit: "{message}", description: "Number of Cosmos DB outbox event documents successfully published to their destination.");

    /// <summary>
    /// Gets the counter representing the total number of outbox event documents (batch) that failed to relay (publish).
    /// </summary>
    /// <remarks>Feeds the relay's circuit breaker - a sustained run of these is what trips it.</remarks>
    public static Counter<long> OutboxRelayPublishFailed { get; } = Meter.CreateCounter<long>("cosmos.outbox.relay.publish.failed", unit: "{message}", description: "Number of Cosmos DB outbox event documents that failed to publish to their destination.");

    /// <summary>
    /// Gets the histogram that tracks the oldest lag duration (now - <c>CloudEvent.Time</c> of the oldest event in the batch), in milliseconds, of successful Cosmos DB outbox relay operations; i.e.
    /// end-to-end relay lag.
    /// </summary>
    public static Histogram<double> OutboxRelayOldestLagDuration { get; } = Meter.CreateHistogram<double>("cosmos.outbox.relay.oldest_lag", unit: "ms", description: "Oldest lag duration (now - enqueued time of oldest event in batch) of Cosmos DB outbox relay.");

    /// <summary>
    /// Gets the histogram that tracks the newest lag duration (now - <c>CloudEvent.Time</c> of the newest event in the batch), in milliseconds, of successful Cosmos DB outbox relay operations; i.e.
    /// end-to-end relay lag.
    /// </summary>
    public static Histogram<double> OutboxRelayNewestLagDuration { get; } = Meter.CreateHistogram<double>("cosmos.outbox.relay.newest_lag", unit: "ms", description: "Newest lag duration (now - enqueued time of newest event in batch) of Cosmos DB outbox relay.");

    /// <summary>
    /// Gets the counter representing the total number of outbox event documents successfully deleted after a successful publish.
    /// </summary>
    public static Counter<long> OutboxRelayCleanupDeleted { get; } = Meter.CreateCounter<long>("cosmos.outbox.relay.cleanup.deleted", unit: "{message}", description: "Number of Cosmos DB outbox event documents successfully deleted after a successful publish.");

    /// <summary>
    /// Gets the counter representing the total number of outbox event documents that failed to delete after a successful publish.
    /// </summary>
    /// <remarks>Never feeds the relay's circuit breaker - the event was already published, so the only consequence is the document sitting until its time-to-live expires; a bounded, self-healing cost, not lost work.</remarks>
    public static Counter<long> OutboxRelayCleanupFailed { get; } = Meter.CreateCounter<long>("cosmos.outbox.relay.cleanup.failed", unit: "{message}", description: "Number of Cosmos DB outbox event documents that failed to delete after a successful publish.");
}
