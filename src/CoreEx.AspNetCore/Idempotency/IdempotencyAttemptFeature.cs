namespace CoreEx.AspNetCore.Idempotency;

/// <summary>
/// Records an endpoint's request to release its owned idempotency attempt.
/// </summary>
internal sealed class IdempotencyAttemptFeature : IIdempotencyAttemptFeature
{
    /// <summary>
    /// Gets whether release was requested.
    /// </summary>
    public bool IsReleaseRequested { get; private set; }

    /// <inheritdoc/>
    public void ReleaseForRetry() => IsReleaseRequested = true;
}
