namespace CoreEx.AspNetCore.Operations;

/// <summary>
/// Represents the public status of an asynchronous operation.
/// </summary>
public sealed record OperationStatus
{
    /// <summary>
    /// Gets the public operation identifier.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the stable public status value: <c>not-started</c>, <c>running</c>, <c>succeeded</c>, <c>failed</c>, or <c>canceled</c>.
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// Gets the time the operation was accepted.
    /// </summary>
    public DateTimeOffset CreatedOn { get; init; }

    /// <summary>
    /// Gets the time the operation was last updated.
    /// </summary>
    public DateTimeOffset UpdatedOn { get; init; }

    /// <summary>
    /// Gets the logical operation expiry time.
    /// </summary>
    public DateTimeOffset ExpiryOn { get; init; }

    /// <summary>
    /// Gets whether cooperative cancellation has been requested.
    /// </summary>
    public bool CancelRequested { get; init; }

    /// <summary>
    /// Gets the latest reported completion percentage.
    /// </summary>
    public int? PercentComplete { get; init; }

    /// <summary>
    /// Gets the latest progress or indeterminate message.
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// Gets the safe public error, when the operation failed or was canceled.
    /// </summary>
    public OperationError? Error { get; init; }

    /// <summary>
    /// Gets the result resource location, when one exists.
    /// </summary>
    public string? ResultLocation { get; init; }
}
