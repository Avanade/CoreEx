namespace CoreEx.AspNetCore.Operations;

/// <summary>
/// Configures behavior for one registered asynchronous operation type.
/// </summary>
/// <typeparam name="TMessage">The message type.</typeparam>
public sealed class OperationTypeOptions<TMessage>
{
    /// <summary>
    /// Gets or sets whether clients may request cooperative cancellation.
    /// </summary>
    public bool CancellationEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the optional type-specific authorization callback; invoked with the current <see cref="ExecutionContext"/> after the owner check succeeds.
    /// </summary>
    public Func<ExecutionContext, WorkState, CancellationToken, Task<bool>>? AuthorizeAsync { get; set; }

    /// <summary>
    /// Gets or sets the optional result reader for successful operations; invoked with the current <see cref="ExecutionContext"/>.
    /// </summary>
    public Func<ExecutionContext, WorkState, CancellationToken, Task<object?>>? ReadResultAsync { get; set; }

    /// <summary>
    /// Gets or sets the optional safe error mapper.
    /// </summary>
    public Func<WorkError, OperationError>? MapError { get; set; }

    /// <summary>
    /// Gets or sets the optional type-specific retry-delay policy for non-terminal status responses.
    /// </summary>
    public Func<WorkState, TimeSpan>? RetryAfter { get; set; }
}
