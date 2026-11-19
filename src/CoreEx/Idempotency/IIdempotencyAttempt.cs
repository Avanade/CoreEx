namespace CoreEx.Idempotency;

/// <summary>
/// Represents owned processing; disposal retains an uncertain outcome, never silently reopening it.
/// </summary>
public interface IIdempotencyAttempt : ISynchronizationLease
{
    /// <summary>
    /// Commits the terminal response only while this attempt still owns processing.
    /// </summary>
    /// <param name="response">The non-empty opaque serialized response, or null when too large to replay.</param>
    /// <exception cref="ArgumentException">The <paramref name="response"/> is empty.</exception>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task CompleteAsync(BinaryData? response, CancellationToken cancellationToken = default);
}
