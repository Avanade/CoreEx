namespace CoreEx.Hosting.Work;

/// <summary>
/// Represents the result of a work-state transition.
/// </summary>
/// <param name="Outcome">The transition outcome.</param>
/// <param name="State">The latest state observed by the transition.</param>
/// <param name="Lease">The lease returned by an applied start or renewal.</param>
/// <param name="IsTakeover">Whether the applied start took over an expired lease.</param>
public sealed record WorkTransitionResult(WorkTransitionOutcome Outcome, WorkState? State, WorkLease? Lease = null, bool IsTakeover = false)
{
    /// <summary>
    /// Gets whether the transition was applied.
    /// </summary>
    public bool IsApplied => Outcome == WorkTransitionOutcome.Applied;

    /// <summary>
    /// Gets the provider time observed while reading the state for this transition.
    /// </summary>
    public DateTimeOffset ObservedOn { get; init; }

    /// <summary>
    /// Converts the transition to a <see cref="Result{T}"/> for result-oriented pipelines.
    /// </summary>
    /// <returns>The state where the transition was applied; otherwise, a matching error result.</returns>
    public Result<WorkState> ToResult() => Outcome switch
    {
        WorkTransitionOutcome.Applied when State is not null => State,
        WorkTransitionOutcome.NotFound => Result.NotFoundError("The work was not found.", ex => ex.ErrorCode = WorkTransitionErrorCodes.NotFound),
        WorkTransitionOutcome.Expired => Result.ConflictError("The work has expired.", ex => ex.ErrorCode = WorkTransitionErrorCodes.Expired),
        WorkTransitionOutcome.LeaseHeld => Result.ConflictError("The work is already leased.", ex => ex.ErrorCode = WorkTransitionErrorCodes.LeaseHeld),
        WorkTransitionOutcome.LeaseLost => Result.ConflictError("The work lease is no longer current.", ex => ex.ErrorCode = WorkTransitionErrorCodes.LeaseLost),
        WorkTransitionOutcome.ConcurrencyExhausted => Result.ConcurrencyError("The work changed concurrently too many times.", ex => ex.ErrorCode = WorkTransitionErrorCodes.ConcurrencyExhausted),
        WorkTransitionOutcome.AlreadyFinished => Result.ConflictError("The work has already finished.", ex => ex.ErrorCode = WorkTransitionErrorCodes.AlreadyFinished),
        _ => Result.ConflictError("The work transition is not valid for its current state.", ex => ex.ErrorCode = WorkTransitionErrorCodes.Conflict)
    };
}
