namespace CoreEx.Database.Outbox;

/// <summary>
/// Provides configuration shared by relational outbox publishers and relays.
/// </summary>
public static class DatabaseOutboxConfiguration
{
    /// <summary>
    /// Gets the configuration key for the partition size shared by relational outbox publishers and relays.
    /// </summary>
    public const string PartitionSizeConfigurationKey = "CoreEx:Host:Outbox:PartitionSize";

    /// <summary>
    /// Gets the configured relational outbox partition size, or the default when not configured.
    /// </summary>
    /// <param name="configuration">The optional configuration.</param>
    /// <returns>The validated partition size.</returns>
    public static int GetPartitionSize(IConfiguration? configuration = null)
        => PartitionKey.ValidatePartitionSize(Internal.GetConfigurationValue(PartitionSizeConfigurationKey, PartitionKey.DefaultPartitionSize, configuration));
}
