namespace CoreEx.Hosting.Work;

/// <summary>
/// Describes the outcome of a work-state transition.
/// </summary>
public enum WorkTransitionOutcome
{
    /// <summary>The transition was persisted.</summary>
    Applied,

    /// <summary>The work is already in a terminal state.</summary>
    AlreadyFinished,

    /// <summary>The work does not exist.</summary>
    NotFound,

    /// <summary>The transition is not valid for the current state.</summary>
    Conflict,

    /// <summary>Another worker currently holds the lease.</summary>
    LeaseHeld,

    /// <summary>The supplied lease is no longer current.</summary>
    LeaseLost,

    /// <summary>The work deadline has passed.</summary>
    Expired,

    /// <summary>The optimistic-concurrency retry limit was reached.</summary>
    ConcurrencyExhausted
}
