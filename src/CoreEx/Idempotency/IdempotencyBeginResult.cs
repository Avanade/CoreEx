namespace CoreEx.Idempotency;

/// <summary>
/// Represents a coherent processing/outcome snapshot.
/// </summary>
/// <param name="Outcome">The decision.</param>
/// <param name="Attempt">The owned attempt only when acquired.</param>
/// <param name="Response">The serialized response only when completed.</param>
/// <param name="Revision">The opaque revision needed for reconciliation.</param>
public sealed record IdempotencyBeginResult(IdempotencyOutcome Outcome, IIdempotencyAttempt? Attempt = null, BinaryData? Response = null, string? Revision = null);
