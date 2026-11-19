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
    /// Begins processing or reads the existing outcome without extending its retention.
    /// </summary>
    /// <param name="key">The logical key qualified by the registered key provider.</param>
    /// <param name="fingerprint">The request fingerprint.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The coherent current result.</returns>
    Task<IdempotencyBeginResult> TryBeginAsync(string key, string fingerprint, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves an expired/indeterminate attempt after external verification that its old work cannot commit.
    /// </summary>
    /// <param name="key">The logical key.</param>
    /// <param name="revision">The expected attempt revision.</param>
    /// <param name="response">The non-empty verified response, or null for a non-replayable outcome; ignored when <paramref name="allowRetry"/> is <see langword="true"/>.</param>
    /// <param name="allowRetry">Whether external reconciliation established that retry is safe.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Whether the expected expired/indeterminate attempt was resolved.</returns>
    Task<bool> TryResolveAsync(string key, string revision, BinaryData? response, bool allowRetry = false, CancellationToken cancellationToken = default);
}
