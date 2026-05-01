namespace CoreEx.Data;

/// <summary>
/// Enables a mutable <see cref="PartitionKey"/>.
/// </summary>
public interface IPartitionKey : IReadOnlyPartitionKey
{
    /// <inheritdoc/>
    string? IReadOnlyPartitionKey.PartitionKey => PartitionKey;

    /// <summary>
    /// Gets or sets the partition key.
    /// </summary>
    new string? PartitionKey { get; set; }
}