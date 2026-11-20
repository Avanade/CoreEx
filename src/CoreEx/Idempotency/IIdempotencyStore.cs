namespace CoreEx.Idempotency;

/// <summary>
/// Atomically coordinates processing attempts and retained terminal outcomes.
/// </summary>
public interface IIdempotencyStore
{
    /// <summary>
    /// Gets the maximum serialized terminal response payload size in bytes, or <see langword="null"/> when the store has no advertised limit.
    /// </summary>
    /// <remarks>A non-null limit must be positive. The store must enforce it when committing or resolving a response.</remarks>
    int? MaxPayloadSize => null;

    /// <summary>
    /// Begins processing or reads the existing outcome for an explicitly scoped operation without extending its retention.
    /// </summary>
    /// <param name="key">The complete operation identity.</param>
    /// <param name="fingerprint">The request fingerprint.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The coherent current result.</returns>
    /// <remarks>The key must not be re-qualified from ambient context.</remarks>
    Task<IdempotencyBeginResult> TryBeginAsync(OperationKey key, string fingerprint, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves an expired/indeterminate explicitly scoped attempt after external verification that its old work cannot commit.
    /// </summary>
    /// <param name="key">The complete operation identity used to begin the attempt.</param>
    /// <param name="revision">The expected attempt revision.</param>
    /// <param name="response">The non-empty verified response, or null for a non-replayable outcome; ignored when <paramref name="allowRetry"/> is <see langword="true"/>.</param>
    /// <param name="allowRetry">Whether external reconciliation established that retry is safe.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Whether the expected expired/indeterminate attempt was resolved.</returns>
    Task<bool> TryResolveAsync(OperationKey key, string revision, BinaryData? response, bool allowRetry = false, CancellationToken cancellationToken = default);
}
