namespace CoreEx.AspNetCore.Idempotency;

/// <summary>
/// Respresents the status of the request (server-side) with respect to idempotency.
/// </summary>
public enum IdempotencyStatus
{
    /// <summary>
    /// Indicates that the operation is currently being processed.
    /// </summary>
    InProgress,

    /// <summary>
    /// Indicates that the operation has completed and can be replayed if necessary.
    /// </summary>
    CompletedAndReplayable,

    /// <summary>
    /// Indicates that the completed operation cannot be replayed because its size exceeds the allowable limit.
    /// </summary>
    CompletedTooLargeToReplay
}