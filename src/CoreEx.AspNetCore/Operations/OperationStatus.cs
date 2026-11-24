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
    /// Gets the public lifecycle value: <c>accepted</c>, <c>running</c>, <c>retrying</c>, <c>succeeded</c>, <c>failed</c>, <c>canceled</c>, or <c>expired</c>.
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
    /// Gets the current inactivity deadline for unfinished work; otherwise, <see langword="null"/>.
    /// </summary>
    /// <remarks>This is not a fixed processing deadline or the terminal retention/purge time.</remarks>
    public DateTimeOffset? ExpiryOn { get; init; }

    /// <summary>
    /// Gets whether cooperative cancellation has been requested.
    /// </summary>
    public bool CancelRequested { get; init; }

    /// <summary>
    /// Gets the latest reported completion percentage; currently <see langword="null"/> for operation-store-backed work.
    /// </summary>
    public int? PercentComplete { get; init; }

    /// <summary>
    /// Gets the latest user-safe progress message.
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// Gets a safe public error, when applicable.
    /// </summary>
    public OperationError? Error { get; init; }

    /// <summary>
    /// Gets the result resource location, when one exists.
    /// </summary>
    public string? ResultLocation { get; init; }
}
