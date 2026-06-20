namespace CoreEx.Database.Postgres;

/// <summary>
/// Provides the <see href="https://www.postgresql.org/docs/">PostgreSQL</see> metrics.
/// </summary>
public static class PostgresMetrics
{
    /// <summary>
    /// Gets the meter used for the database outbox metrics.
    /// </summary>
    public static Meter Meter { get; } = new("CoreEx.Database.Postgres.Outbox");

    /// <summary>
    /// Gets the counter representing the total number of messages enqueued successfully.
    /// </summary>
    public static Counter<long> OutboxEnqueued { get; } = Meter.CreateCounter<long>("postgres.outbox.enqueue", unit: "{message}", description: "Number of PostgreSQL outbox messages enqueued successfully.");

    /// <summary>
    /// Gets the counter representing the total number of messages (batch) dequeued (relayed) successfully.
    /// </summary>
    public static Counter<long> OutboxRelayBatchSize { get; } = Meter.CreateCounter<long>("postgres.outbox.relay.batch.size", unit: "{message}", description: "Number of PostgreSQL outbox messages (batch) relayed successfully.");

    /// <summary>
    /// Gets the histogram that tracks the oldest lag duration (now - enqueued time of first message in batch), in milliseconds, of successful PostgreSQL outbox relay operations; i.e. end-to-end relay lag.
    /// </summary>
    public static Histogram<double> OutboxRelayOldestLagDuration { get; } = Meter.CreateHistogram<double>("postgres.outbox.batch.oldest_lag", unit: "ms", description: "Oldest lag duration (now - enqueued time of first message in batch) of PostgreSQL outbox relay.");

    /// <summary>
    /// Gets the histogram that tracks the newest lag duration (now - enqueued time of last message in batch), in milliseconds, of successful PostgreSQL outbox relay operations; i.e. end-to-end relay lag.
    /// </summary>
    public static Histogram<double> OutboxRelayNewestLagDuration { get; } = Meter.CreateHistogram<double>("postgres.outbox.batch.newest_lag", unit: "ms", description: "Newest lag duration (now - enqueued time of last message in batch) of PostgreSQL outbox relay.");
}