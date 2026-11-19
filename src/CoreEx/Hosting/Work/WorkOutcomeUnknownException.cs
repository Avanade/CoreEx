namespace CoreEx.Hosting.Work;

/// <summary>
/// Indicates that a work-store write may have been applied, but its outcome could not be confirmed.
/// </summary>
/// <param name="workId">The affected work identifier.</param>
/// <param name="operation">The attempted operation.</param>
/// <param name="innerException">The exception that caused the unknown outcome.</param>
public sealed class WorkOutcomeUnknownException(string workId, string operation, Exception? innerException = null)
    : Exception($"The outcome of work operation '{operation}' for '{workId}' could not be confirmed.", innerException)
{
    /// <summary>Gets the affected work identifier.</summary>
    public string WorkId { get; } = workId.ThrowIfNullOrEmpty(nameof(workId));

    /// <summary>Gets the attempted operation.</summary>
    public string Operation { get; } = operation.ThrowIfNullOrEmpty(nameof(operation));
}
