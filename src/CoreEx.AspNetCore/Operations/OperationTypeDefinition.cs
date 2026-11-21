namespace CoreEx.AspNetCore.Operations;

/// <summary>
/// Holds the registered policies for an operation message type.
/// </summary>
/// <param name="TypeName">The persisted message type name.</param>
/// <param name="CancellationEnabled">Whether cooperative cancellation is enabled.</param>
/// <param name="AuthorizeAsync">The optional type-specific authorization callback.</param>
/// <param name="ReadResultAsync">The optional successful-result reader.</param>
/// <param name="MapError">The optional safe error mapper.</param>
/// <param name="RetryAfter">The optional retry-delay policy.</param>
internal sealed record OperationTypeDefinition(
    string TypeName,
    bool CancellationEnabled,
    Func<ExecutionContext, WorkState, CancellationToken, Task<bool>>? AuthorizeAsync,
    Func<ExecutionContext, WorkState, CancellationToken, Task<object?>>? ReadResultAsync,
    Func<WorkError, OperationError>? MapError,
    Func<WorkState, TimeSpan>? RetryAfter);
