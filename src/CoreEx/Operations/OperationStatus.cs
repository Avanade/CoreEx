namespace CoreEx.Operations;

/// <summary>
/// Defines the persisted lifecycle status of a tracked operation.
/// </summary>
public enum OperationStatus
{
    /// <summary>
    /// The operation has been accepted and has not yet been claimed for execution.
    /// </summary>
    Accepted = 1,

    /// <summary>
    /// The operation is claimed by a worker for execution.
    /// </summary>
    /// <remarks>A worker that crashes cannot update its status; a running operation whose claim has expired (compared with store time) is stalled and can be taken over by a redelivery.</remarks>
    Running = 2,

    /// <summary>
    /// A previous execution attempt released its claim after a retryable error; the operation is waiting for a redelivery to claim it again.
    /// </summary>
    Retrying = 3,

    /// <summary>
    /// The operation finished successfully.
    /// </summary>
    Succeeded = 4,

    /// <summary>
    /// The operation finished unsuccessfully and will not be retried.
    /// </summary>
    Failed = 5,

    /// <summary>
    /// The operation was cancelled before it started, while waiting to retry, or because its handler honored a cancellation request.
    /// </summary>
    Cancelled = 6,

    /// <summary>
    /// The operation was abandoned: it remained unfinished with no activity past its <see cref="OperationState.AbandonAfter"/> (for example, its message was dead-lettered or lost, or its worker stalled) and was expired by cleanup.
    /// </summary>
    /// <remarks>Expiry does not prove that no business effects occurred; an earlier attempt's commit outcome may be unknown.</remarks>
    Expired = 7
}
