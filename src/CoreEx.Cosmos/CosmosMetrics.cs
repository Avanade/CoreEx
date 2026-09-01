namespace CoreEx.Cosmos;

/// <summary>
/// Provides the <see href="https://learn.microsoft.com/en-us/azure/cosmos-db/">Azure Cosmos DB</see> metrics.
/// </summary>
public static class CosmosMetrics
{
    /// <summary>
    /// Gets the meter used for the Cosmos DB outbox metrics.
    /// </summary>
    public static Meter Meter { get; } = new("CoreEx.Cosmos.Outbox");

    /// <summary>
    /// Gets the counter representing the total number of outbox event documents enqueued successfully.
    /// </summary>
    public static Counter<long> OutboxEnqueued { get; } = Meter.CreateCounter<long>("cosmos.outbox.enqueue", unit: "{message}", description: "Number of Cosmos DB outbox event documents enqueued successfully.");
}
