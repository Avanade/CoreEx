namespace CoreEx.Operations;

/// <summary>
/// Represents the outcome of an <see cref="IOperationStore.CleanupAsync"/> pass.
/// </summary>
/// <param name="Expired">The number of abandoned operations transitioned to <see cref="OperationStatus.Expired"/>.</param>
/// <param name="Purged">The number of finished operations deleted.</param>
public sealed record OperationCleanupResult(int Expired, int Purged);
