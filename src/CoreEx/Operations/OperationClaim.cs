namespace CoreEx.Operations;

/// <summary>
/// Represents a held execution claim; its <see cref="Owner"/> and <see cref="Attempt"/> fence later renewal and completion.
/// </summary>
/// <param name="OperationId">The operation identifier.</param>
/// <param name="Owner">The unique owner token supplied by the claimant.</param>
/// <param name="Attempt">The execution attempt (fencing token).</param>
/// <param name="ExpiresOn">The store time at which the claim expires unless renewed.</param>
/// <param name="CancelRequested">Indicates whether cancellation has been requested; honoring it is cooperative.</param>
public sealed record OperationClaim(Guid OperationId, string Owner, int Attempt, DateTimeOffset ExpiresOn, bool CancelRequested);
