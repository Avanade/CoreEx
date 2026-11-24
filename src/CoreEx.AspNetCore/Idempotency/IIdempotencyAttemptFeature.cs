namespace CoreEx.AspNetCore.Idempotency;

/// <summary>
/// Allows an owned HTTP idempotency endpoint to request release after a response proves no work was dispatched.
/// </summary>
public interface IIdempotencyAttemptFeature
{
    /// <summary>
    /// Requests release of this attempt after the response has been captured.
    /// </summary>
    void ReleaseForRetry();
}
