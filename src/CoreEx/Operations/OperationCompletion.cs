namespace CoreEx.Operations;

/// <summary>
/// Represents a validated terminal outcome for <see cref="IOperationStore.CompleteAsync"/>.
/// </summary>
public sealed record OperationCompletion
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OperationCompletion"/> class.
    /// </summary>
    private OperationCompletion(OperationStatus status, string? resultLocation, string? errorCode, string? errorMessage)
    {
        Status = status;
        ResultLocation = resultLocation;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    /// Gets the terminal <see cref="OperationStatus"/>.
    /// </summary>
    public OperationStatus Status { get; }

    /// <summary>
    /// Gets the link to the application-owned result resource.
    /// </summary>
    public string? ResultLocation { get; }

    /// <summary>
    /// Gets the user-safe error code.
    /// </summary>
    public string? ErrorCode { get; }

    /// <summary>
    /// Gets the user-safe error message.
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>
    /// Creates a <see cref="OperationStatus.Succeeded"/> completion.
    /// </summary>
    /// <param name="resultLocation">The optional link to the application-owned result resource.</param>
    /// <returns>The <see cref="OperationCompletion"/>.</returns>
    public static OperationCompletion Succeeded(string? resultLocation = null)
        => new(OperationStatus.Succeeded, Bounded(resultLocation, OperationLimits.MaxResultLocationLength, nameof(resultLocation)), null, null);

    /// <summary>
    /// Creates a <see cref="OperationStatus.Failed"/> completion.
    /// </summary>
    /// <param name="errorCode">The user-safe error code.</param>
    /// <param name="errorMessage">The optional user-safe error message; never include sensitive data.</param>
    /// <returns>The <see cref="OperationCompletion"/>.</returns>
    public static OperationCompletion Failed(string errorCode, string? errorMessage = null)
        => new(OperationStatus.Failed, null, Bounded(errorCode.ThrowIfNullOrWhiteSpace(), OperationLimits.MaxErrorCodeLength, nameof(errorCode)), Bounded(errorMessage, OperationLimits.MaxErrorMessageLength, nameof(errorMessage)));

    /// <summary>
    /// Creates a <see cref="OperationStatus.Cancelled"/> completion where the handler honored a cancellation request.
    /// </summary>
    /// <returns>The <see cref="OperationCompletion"/>.</returns>
    public static OperationCompletion Cancelled() => new(OperationStatus.Cancelled, null, null, null);

    /// <summary>
    /// Validates the maximum length of an optional value.
    /// </summary>
    private static string? Bounded(string? value, int maxLength, string paramName) => value is not null && value.Length > maxLength ? throw new ArgumentException($"The value must not exceed {maxLength} characters.", paramName) : value;
}
