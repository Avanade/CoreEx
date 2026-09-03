namespace CoreEx.Database.SqlServer;

/// <summary>
/// Provides the <see href="https://learn.microsoft.com/en-us/sql/">SQL Server</see> metrics.
/// </summary>
public static class SqlServerMetrics
{
    /// <summary>
    /// Gets the meter used for the database outbox metrics.
    /// </summary>
    public static Meter Meter { get; } = new("CoreEx.Database.SqlServer.Outbox");

    /// <summary>
    /// Gets the counter representing the total number of messages enqueued successfully.
    /// </summary>
    public static Counter<long> OutboxEnqueued { get; } = Meter.CreateCounter<long>("sqlserver.outbox.enqueue", unit: "{message}", description: "Number of SQL Server outbox messages enqueued successfully.");

    /// <summary>
    /// Gets the counter representing the total number of messages (batch) relayed (published) successfully.
    /// </summary>
    public static Counter<long> OutboxRelayPublished { get; } = Meter.CreateCounter<long>("sqlserver.outbox.relay.publish", unit: "{message}", description: "Number of SQL Server outbox messages (batch) relayed successfully.");

    /// <summary>
    /// Gets the counter representing the total number of messages (batch) that failed to relay (publish).
    /// </summary>
    /// <remarks>Recorded for a batch that fails anywhere between claim and complete (publish failure, or a failure completing/cancelling the batch) - the batch is cancelled and made available for retry.</remarks>
    public static Counter<long> OutboxRelayPublishFailed { get; } = Meter.CreateCounter<long>("sqlserver.outbox.relay.publish.failed", unit: "{message}", description: "Number of SQL Server outbox messages (batch) that failed to relay.");

    /// <summary>
    /// Gets the histogram that tracks the oldest lag duration (now - enqueued time of first message in batch), in milliseconds, of a SQL Server outbox relay batch attempt; i.e. end-to-end relay lag.
    /// </summary>
    /// <remarks>Recorded on both a successful and a failed publish attempt, so this keeps climbing (rather than going silent) for as long as a batch keeps failing - a stuck relay is visible as an
    /// ever-increasing oldest lag, not an absent metric.</remarks>
    public static Histogram<double> OutboxRelayOldestLagDuration { get; } = Meter.CreateHistogram<double>("sqlserver.outbox.relay.oldest_lag", unit: "ms", description: "Oldest lag duration (now - enqueued time of first message in batch) of SQL Server outbox relay.");

    /// <summary>
    /// Gets the histogram that tracks the newest lag duration (now - enqueued time of last message in batch), in milliseconds, of a SQL Server outbox relay batch attempt; i.e. end-to-end relay lag.
    /// </summary>
    /// <remarks>Recorded on both a successful and a failed publish attempt; see <see cref="OutboxRelayOldestLagDuration"/>.</remarks>
    public static Histogram<double> OutboxRelayNewestLagDuration { get; } = Meter.CreateHistogram<double>("sqlserver.outbox.relay.newest_lag", unit: "ms", description: "Newest lag duration (now - enqueued time of last message in batch) of SQL Server outbox relay.");
}
