namespace CoreEx.Idempotency;

/// <summary>
/// Maintains an owned processing attempt and retains uncertain outcomes on disposal.
/// </summary>
/// <param name="options">The coordination options.</param>
/// <param name="started">The monotonic acquisition start.</param>
/// <param name="renew">The conditional renewal.</param>
/// <param name="retain">The conditional indeterminate transition on disposal.</param>
/// <param name="complete">The conditional terminal transition.</param>
/// <param name="releaseForRetry">The conditional owner-checked release.</param>
/// <param name="logger">The logger.</param>
public sealed class IdempotencyAttempt(CoreEx.Coordination.CoordinationOptions options, long started, Func<CancellationToken, Task<bool>> renew, Func<CancellationToken, Task> retain, Func<BinaryData?, CancellationToken, Task<bool>> complete, Func<CancellationToken, Task<bool>> releaseForRetry, ILogger logger) : IIdempotencyAttempt, IIdempotencyAttemptRelease
{
    private readonly CoreEx.Coordination.CoordinationLease _lease = new(options, started, renew, retain, logger);
    private readonly Func<BinaryData?, CancellationToken, Task<bool>> _complete = complete;
    private readonly Func<CancellationToken, Task<bool>> _releaseForRetry = releaseForRetry;

    /// <inheritdoc/>
    public CancellationToken OwnershipLost => _lease.OwnershipLost;

    /// <inheritdoc/>
    public Task CompleteAsync(BinaryData? response, CancellationToken cancellationToken = default)
    {
        ThrowIfEmptyResponse(response);
        return _lease.CompleteAsync(ct => _complete(response, ct), cancellationToken);
    }

    /// <inheritdoc/>
    public Task ReleaseForRetryAsync(CancellationToken cancellationToken = default)
        => _lease.ReleaseForRetryAsync(_releaseForRetry, cancellationToken);

    /// <summary>
    /// Rejects an empty replayable response; a non-replayable outcome must be committed as <see langword="null"/>.
    /// </summary>
    /// <param name="response">The response.</param>
    public static void ThrowIfEmptyResponse(BinaryData? response)
    {
        if (response is not null && response.IsEmpty)
            throw new ArgumentException("A replayable idempotency response must not be empty; use null for an outcome without a response.", nameof(response));
    }

    /// <inheritdoc/>
    public ValueTask DisposeAsync() => _lease.DisposeAsync();
}
