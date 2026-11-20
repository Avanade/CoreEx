namespace CoreEx.Hosting.Work;

/// <summary>
/// Indicates that a work-store write may have been applied, but its outcome could not be confirmed.
/// </summary>
/// <param name="workKey">The complete identity of the affected operation.</param>
/// <param name="operation">The attempted operation.</param>
/// <param name="innerException">The exception that caused the unknown outcome.</param>
public sealed class WorkOutcomeUnknownException(OperationKey workKey, string operation, Exception? innerException = null)
    : Exception($"The outcome of work operation '{operation}' for '{workKey.ThrowIfNull(nameof(workKey)).InstanceId}' could not be confirmed.", innerException)
{
    /// <summary>Gets the complete identity of the affected operation.</summary>
    public OperationKey WorkKey { get; } = workKey.ThrowIfNull(nameof(workKey));

    /// <summary>Gets the attempted operation.</summary>
    public string Operation { get; } = operation.ThrowIfNullOrEmpty(nameof(operation));
}
