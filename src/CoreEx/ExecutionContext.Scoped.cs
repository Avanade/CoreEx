namespace CoreEx;

public partial class ExecutionContext
{
    /// <summary>Executes an operation in a fresh async DI scope without disposing the caller's ambient context.</summary>
    /// <typeparam name="T">The operation's return type, including a Result returned without error conversion.</typeparam>
    /// <param name="serviceProvider">The provider from which to create an independent scope.</param>
    /// <param name="action">The awaited operation; resolve business services from the supplied context's provider.</param>
    /// <param name="configure">The optional configuration of the fresh context using the parent, before business-service resolution. Explicitly select trusted identity/culture/metadata to propagate.</param>
    /// <param name="cancellationToken">The cancellation token forwarded to the operation.</param>
    /// <returns>The original operation result after asynchronous scope disposal.</returns>
    /// <remarks>The registered context factory must return a fresh, scope-owned instance. No parent state is copied automatically. Messages, attributes, operation state, and timestamps remain fresh unless explicitly configured. Do not retain scoped services or start unawaited work. Factory/configuration, operation, and disposal faults propagate; parent restoration occurs on every exit path. Existing AddExecutionContext and host ownership semantics are unchanged.</remarks>
    public static async Task<T> RunScopedAsync<T>(IServiceProvider serviceProvider, Func<ExecutionContext, CancellationToken, Task<T>> action, Action<ExecutionContext?, ExecutionContext>? configure = null, CancellationToken cancellationToken = default)
    {
        serviceProvider.ThrowIfNull();
        action.ThrowIfNull();
        cancellationToken.ThrowIfCancellationRequested();

        var parent = _asyncLocal.Value;
        ExecutionContext? context = null;

        // Detach before resolution: AddExecutionContext resets and disposes any ambient instance.
        _asyncLocal.Value = null;

        try
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            context = scope.ServiceProvider.GetRequiredService<ExecutionContext>();
            _asyncLocal.Value = context;
            using var correlationScope = Correlation.BeginScope(scope.ServiceProvider.GetService<ILoggerFactory>()?.CreateLogger<ExecutionContext>());
            configure?.Invoke(parent, context);
            cancellationToken.ThrowIfCancellationRequested();
            return await action(context, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            try
            {
                // DI disposal can stop before reaching the context if another scoped service throws.
                if (context is not null && !context._disposed)
                    context.Dispose();
            }
            finally
            {
                _asyncLocal.Value = parent;
            }
        }
    }

    /// <summary>Executes an operation without a return value in a fresh async DI scope while preserving the parent context.</summary>
    /// <param name="serviceProvider">The provider from which to create an independent scope.</param>
    /// <param name="action">The awaited operation; resolve business services from the supplied context's provider.</param>
    /// <param name="configure">The optional explicit parent-to-child configuration before resolving business services.</param>
    /// <param name="cancellationToken">The cancellation token forwarded to the operation.</param>
    /// <returns>The asynchronous operation, including scope disposal and parent restoration.</returns>
    /// <remarks>Uses the same ownership and propagation rules as the value-returning overload. Cancellation and faults are not translated into Results.</remarks>
    public static Task RunScopedAsync(IServiceProvider serviceProvider, Func<ExecutionContext, CancellationToken, Task> action, Action<ExecutionContext?, ExecutionContext>? configure = null, CancellationToken cancellationToken = default)
    {
        action.ThrowIfNull();
        return RunScopedAsync(serviceProvider, async (context, ct) =>
        {
            await action(context, ct).ConfigureAwait(false);
            return true;
        }, configure, cancellationToken);
    }
}
