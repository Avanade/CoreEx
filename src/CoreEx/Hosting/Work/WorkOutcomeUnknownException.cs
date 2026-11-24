namespace CoreEx.Hosting.Work;

/// <summary>
/// Indicates that a work-store write may have been applied, but its outcome could not be confirmed.
/// </summary>
/// <param name="state">The affected <see cref="WorkState"/> providing the complete identity.</param>
/// <param name="operation">The attempted operation.</param>
/// <param name="innerException">The exception that caused the unknown outcome.</param>
public sealed class WorkOutcomeUnknownException(WorkState state, string operation, Exception? innerException = null)
    : Exception($"The outcome of work operation '{operation}' for '{state.ThrowIfNull(nameof(state)).Id}' could not be confirmed.", innerException)
{
    /// <summary>Gets the stable domain name of the affected operation.</summary>
    public string DomainName { get; } = state.DomainName;

    /// <summary>Gets the tenant identifier of the affected operation; otherwise, <see langword="null"/>.</summary>
    public string? TenantId { get; } = state.TenantId;

    /// <summary>Gets the operation name of the affected operation.</summary>
    public string OperationName { get; } = state.OperationName;

    /// <summary>Gets the instance identifier of the affected operation.</summary>
    public string? Id { get; } = state.Id;

    /// <summary>Gets the attempted operation.</summary>
    public string Operation { get; } = operation.ThrowIfNullOrEmpty(nameof(operation));
}
