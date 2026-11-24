namespace CoreEx.Idempotency;

/// <summary>
/// Adds explicit owner-checked release for a request proven not to have dispatched business work.
/// </summary>
public interface IIdempotencyAttemptRelease
{
    /// <summary>
    /// Releases the active attempt so the same key may begin a new attempt.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task ReleaseForRetryAsync(CancellationToken cancellationToken = default);
}
