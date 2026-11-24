namespace CoreEx.AspNetCore.Operations;

/// <summary>
/// Identifies the kind of generic asynchronous-operation endpoint.
/// </summary>
public enum OperationsEndpointKind
{
    /// <summary>
    /// Gets an operation status.
    /// </summary>
    Status,

    /// <summary>
    /// Gets an operation result.
    /// </summary>
    Result,

    /// <summary>
    /// Requests operation cancellation.
    /// </summary>
    Cancel
}
