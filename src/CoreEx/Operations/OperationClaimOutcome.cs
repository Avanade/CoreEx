namespace CoreEx.Operations;

/// <summary>
/// Defines the outcome of an <see cref="IOperationStore.ClaimAsync"/>.
/// </summary>
public enum OperationClaimOutcome
{
    /// <summary>
    /// The claim was acquired; the operation is <see cref="OperationStatus.Running"/> with an incremented attempt.
    /// </summary>
    Claimed = 1,

    /// <summary>
    /// Another owner holds an unexpired claim.
    /// </summary>
    Busy = 2,

    /// <summary>
    /// The operation has already finished.
    /// </summary>
    Finished = 3,

    /// <summary>
    /// The operation does not exist (never accepted, or purged).
    /// </summary>
    NotFound = 4
}
