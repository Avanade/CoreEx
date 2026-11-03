namespace CoreEx.Azure.Functions;

/// <summary>
/// Initializes the scoped CoreEx execution context within the Functions worker pipeline.
/// </summary>
/// <param name="configure">The optional invocation-specific context configuration callback.</param>
public sealed class FunctionsExecutionContextMiddleware(Func<FunctionContext, ExecutionContext, CancellationToken, Task>? configure = null) : IFunctionsWorkerMiddleware
{
    private readonly Func<FunctionContext, ExecutionContext, CancellationToken, Task>? _configure = configure;

    /// <inheritdoc/>
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        context.ThrowIfNull();
        next.ThrowIfNull();

        var ec = context.InstanceServices.GetRequiredService<ExecutionContext>();
        if (!ExecutionContext.TryGetCurrent(out var current) || !ReferenceEquals(current, ec))
        {
            ExecutionContext.Reset();
            ExecutionContext.SetCurrent(ec);
        }

        try
        {
            if (_configure is not null)
                await _configure(context, ec, context.CancellationToken).ConfigureAwait(false);

            var httpContext = context.GetHttpContext();
            if (httpContext is not null)
            {
                // IActionResult rendering can occur after the function returns; add headers immediately before the response starts.
                httpContext.Response.OnStarting(() =>
                {
                    ExecutionContextMiddleware.AddMessagesHeader(httpContext, ec);
                    return Task.CompletedTask;
                });
            }

            await next(context).ConfigureAwait(false);
        }
        finally
        {
            ExecutionContext.Reset();
        }
    }
}
