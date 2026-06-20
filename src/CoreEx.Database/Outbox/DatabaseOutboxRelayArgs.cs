namespace CoreEx.Database.Outbox;

/// <summary>
/// Provides the <see cref="IDatabaseOutboxRelay.RelayAsync"/> arguments.
/// </summary>
public class DatabaseOutboxRelayArgs
{
    /// <summary>
    /// Gets the <see cref="Data.PartitionPicker"/>.
    /// </summary>
    public required PartitionPicker PartitionPicker { get; init; }

    /// <summary>
    /// Gets the batch size.
    /// </summary>
    public int BatchSize { get; init => field = value <= 0 ? 1 : value; }

    /// <summary>
    /// Gets the lease duration used to lock when claiming a batch.
    /// </summary>
    public TimeSpan LeaseDuration { get; init; }

    /// <summary>
    /// Gets the backoff duration used to push out availability of the underlying event within the outbox when cancelling a batch.
    /// </summary>
    public TimeSpan BackOffDuration { get; init; }
}