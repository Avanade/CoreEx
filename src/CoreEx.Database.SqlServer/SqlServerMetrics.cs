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
    /// Gets the counter representing the total number of messages (batch) dequeued (relayed) successfully.
    /// </summary>
    public static Counter<long> OutboxRelayBatchSize { get; } = Meter.CreateCounter<long>("sqlserver.outbox.relay.batch.size", unit: "{message}", description: "Number of SQL Server outbox messages (batch) relayed successfully.");

    /// <summary>
    /// Gets the histogram that tracks the oldest lag duration (now - enqueued time of first message in batch), in milliseconds, of successful SQL Server outbox relay operations; i.e. end-to-end relay lag.
    /// </summary>
    public static Histogram<double> OutboxRelayOldestLagDuration { get; } = Meter.CreateHistogram<double>("sqlserver.outbox.batch.oldest_lag", unit: "ms", description: "Oldest lag duration (now - enqueued time of first message in batch) of SQL Server outbox relay.");

    /// <summary>
    /// Gets the histogram that tracks the newest lag duration (now - enqueued time of last message in batch), in milliseconds, of successful SQL Server outbox relay operations; i.e. end-to-end relay lag.
    /// </summary>
    public static Histogram<double> OutboxRelayNewestLagDuration { get; } = Meter.CreateHistogram<double>("sqlserver.outbox.batch.newest_lag", unit: "ms", description: "Newest lag duration (now - enqueued time of last message in batch) of SQL Server outbox relay.");
}