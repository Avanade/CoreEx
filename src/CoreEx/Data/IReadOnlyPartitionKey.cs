namespace CoreEx.Data;

/// <summary>
/// Enables a read-only <see cref="PartitionKey"/>.
/// </summary>
public interface IReadOnlyPartitionKey
{
    /// <summary>
    /// Gets the partition key.
    /// </summary>
    string? PartitionKey { get; }
}