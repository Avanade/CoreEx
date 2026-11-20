namespace CoreEx.Hosting.Work;

/// <summary>
/// Defines the <see cref="ExtendedException.ErrorCode"/> values used when a rejected <see cref="WorkTransitionResult"/> is converted to a <see cref="Result{T}"/>.
/// </summary>
/// <remarks>These describe why a transition was rejected; they are distinct from the persisted <see cref="WorkErrorCodes"/> describing a work outcome.</remarks>
public static class WorkTransitionErrorCodes
{
    /// <summary>
    /// The work does not exist.
    /// </summary>
    public const string NotFound = "work-not-found";

    /// <summary>
    /// The work is already in a terminal state.
    /// </summary>
    public const string AlreadyFinished = "work-already-finished";

    /// <summary>
    /// The transition is not valid for the current state.
    /// </summary>
    public const string Conflict = "work-conflict";

    /// <summary>
    /// Another worker currently holds the lease.
    /// </summary>
    public const string LeaseHeld = "work-lease-held";

    /// <summary>
    /// The supplied lease is no longer current.
    /// </summary>
    public const string LeaseLost = "work-lease-lost";

    /// <summary>
    /// The work deadline has passed.
    /// </summary>
    public const string Expired = "work-expired";

    /// <summary>
    /// The optimistic-concurrency retry limit was reached.
    /// </summary>
    public const string ConcurrencyExhausted = "work-concurrency-exhausted";
}
