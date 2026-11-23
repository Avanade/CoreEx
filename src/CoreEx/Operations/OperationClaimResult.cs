namespace CoreEx.Operations;

/// <summary>
/// Represents the result of an <see cref="IOperationStore.ClaimAsync"/>.
/// </summary>
/// <param name="Outcome">The <see cref="OperationClaimOutcome"/>.</param>
/// <param name="State">The resulting or current <see cref="OperationState"/>; <see langword="null"/> where <see cref="OperationClaimOutcome.NotFound"/>.</param>
/// <param name="Claim">The acquired <see cref="OperationClaim"/> where <see cref="OperationClaimOutcome.Claimed"/>.</param>
public sealed record OperationClaimResult(OperationClaimOutcome Outcome, OperationState? State, OperationClaim? Claim);
