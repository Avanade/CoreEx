namespace CoreEx.AspNetCore.Operations;

/// <summary>
/// Holds the registered policies for an operation message type.
/// </summary>
/// <param name="OperationName">The stable persisted operation name.</param>
/// <param name="MessageType">The initiating message type.</param>
/// <param name="CancellationEnabled">Whether cooperative cancellation is enabled.</param>
/// <param name="AuthorizeAsync">The optional type-specific authorization callback.</param>
/// <param name="ReadResultAsync">The optional successful-result reader.</param>
/// <param name="MapError">The optional safe error mapper.</param>
/// <param name="RetryAfter">The optional retry-delay policy.</param>
internal sealed record OperationTypeDefinition(
    string OperationName,
    Type MessageType,
    bool CancellationEnabled,
    Func<ExecutionContext, OperationState, CancellationToken, Task<bool>>? AuthorizeAsync,
    Func<ExecutionContext, OperationState, CancellationToken, Task<object?>>? ReadResultAsync,
    Func<OperationState, OperationError>? MapError,
    Func<OperationState, TimeSpan>? RetryAfter);
