namespace CoreEx.Data;

/// <summary>
/// Enables a read-only <see cref="PartitionKey"/>.
/// </summary>
/// <remarks>A partition key's intended meaning is layer/purpose-specific, not invariant - e.g. a Cosmos DB physical partition/shard key (chosen for storage distribution and RU throughput) and an
/// event's ordering/session key (chosen so related events are processed in order) are routinely different values for the same logical entity. Because of this, <see cref="Mapping.Mapper"/>'s
/// standard property mapping deliberately does <b>not</b> auto-copy <see cref="PartitionKey"/> between a source and destination (unlike, say, <see cref="ITenantId.TenantId"/>) - set it
/// deliberately at each destination instead (e.g. <c>CosmosDbModelOptions{TModel}.WithPartitionKey</c>/<c>WithFixedPartitionKey</c> for Cosmos, or explicitly on the contract/<c>EventData</c> for
/// events) rather than implementing this interface on a shared type and relying on it flowing through automatically.</remarks>
public interface IReadOnlyPartitionKey
{
    /// <summary>
    /// Gets the partition key.
    /// </summary>
    string? PartitionKey { get; }
}