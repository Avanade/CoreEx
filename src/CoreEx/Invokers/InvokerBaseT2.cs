namespace CoreEx.Invokers;

/// <summary>
/// Wraps an <b>Invoke</b> enabling standard functionality (tracing and logging) to be added to an invocation including <typeparamref name="TArgs"/>. 
/// </summary>
/// <typeparam name="TCaller">The calling (invoking) <see cref="Type"/>.</typeparam>
/// <typeparam name="TArgs">The arguments <see cref="Type"/>.</typeparam>
/// <param name="serviceProvider">The optional <i>root</i> <see cref="IServiceProvider"/> needed where there is no <see cref="ExecutionContext.Current"/> <see cref="ExecutionContext.ServiceProvider"/>.</param>
public abstract class InvokerBase<TCaller, TArgs>(IServiceProvider? serviceProvider = null) : InvokerBase(serviceProvider)
{
    /// <summary>
    /// Invokes a <paramref name="func"/> with a <typeparamref name="TResult"/> asynchronously.
    /// </summary>
    /// <typeparam name="TResult">The result <see cref="Type"/>.</typeparam>
    /// <param name="tracer">The <see cref="InvokerTracer"/>.</param>
    /// <param name="caller">The caller (invoker).</param>
    /// <param name="args">The caller arguments.</param>
    /// <param name="func">The function to invoke.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The result.</returns>
    /// <remarks>Where overriding the base <see cref="OnInvokeAsync"/> <b>must</b> be invoked; do <b>not</b> invoke the <paramref name="func"/> directly.</remarks>
    protected virtual Task<TResult> OnInvokeAsync<TResult>(InvokerTracer tracer, TCaller caller, TArgs args, Func<InvokerTracer, TArgs, CancellationToken, Task<TResult>> func, CancellationToken cancellationToken) 
        => func(tracer, args, cancellationToken);

    /// <summary>
    /// Invoke the <see cref="OnInvokeAsync"/> with tracing.
    /// </summary>
    private async Task<TResult> TraceOnInvokeAsync<TResult>(TCaller caller, TArgs args, Func<InvokerTracer, TArgs, CancellationToken, Task<TResult>> func, string? memberName, CancellationToken cancellationToken)
    {
        var isSuccess = true;
        using var tracer = new InvokerTracer(this, caller, memberName, null);

        try
        {
            return await OnInvokeAsync(tracer, caller, args, func, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            isSuccess = false;
            tracer.TraceException(ex);
            throw;
        }
        finally
        {
            tracer.TraceComplete(isSuccess);
        }
    }

    /// <summary>
    /// Invokes an <paramref name="func"/> asynchronously.
    /// </summary>
    /// <param name="caller">The caller (invoker).</param>
    /// <param name="args">The caller arguments.</param>
    /// <param name="func">The function to invoke.</param>
    /// <param name="memberName">The calling member name (uses <see cref="CallerMemberNameAttribute"/> to default).</param>
    public Task InvokeAsync(TCaller caller, TArgs args, Func<InvokerTracer, TArgs, Task> func, [CallerMemberName] string? memberName = null)
        => TraceOnInvokeAsync(caller.ThrowIfNull(), args, async (tracer, args, _) => { await func(tracer, args).ConfigureAwait(false); return (object?)null!; }, memberName, default);

    /// <summary>
    /// Invokes an <paramref name="func"/> asynchronously.
    /// </summary>
    /// <param name="caller">The caller (invoker).</param>
    /// <param name="args">The caller arguments.</param>
    /// <param name="func">The function to invoke.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <param name="memberName">The calling member name (uses <see cref="CallerMemberNameAttribute"/> to default).</param>
    public Task InvokeAsync(TCaller caller, TArgs args, Func<InvokerTracer, TArgs, CancellationToken, Task> func, CancellationToken cancellationToken, [CallerMemberName] string? memberName = null)
        => TraceOnInvokeAsync(caller.ThrowIfNull(), args, async (tracer, args, cancellationToken) => { await func(tracer, args, cancellationToken).ConfigureAwait(false); return (object?)null!; }, memberName, cancellationToken);

    /// <summary>
    /// Invokes an <paramref name="func"/> asynchronously.
    /// </summary>
    /// <param name="caller">The caller (invoker).</param>
    /// <param name="args">The caller arguments.</param>
    /// <param name="func">The function to invoke.</param>
    /// <param name="memberName">The calling member name (uses <see cref="CallerMemberNameAttribute"/> to default).</param>
    /// <returns>The result.</returns>
    public Task<TResult> InvokeAsync<TResult>(TCaller caller, TArgs args, Func<InvokerTracer, TArgs, Task<TResult>> func, [CallerMemberName] string? memberName = null)
        => TraceOnInvokeAsync(caller.ThrowIfNull(), args, (tracer, args, _) => func(tracer, args), memberName, default);

    /// <summary>
    /// Invokes an <paramref name="func"/> asynchronously.
    /// </summary>
    /// <param name="caller">The caller (invoker).</param>
    /// <param name="args">The caller arguments.</param>
    /// <param name="func">The function to invoke.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <param name="memberName">The calling member name (uses <see cref="CallerMemberNameAttribute"/> to default).</param>
    /// <returns>The result.</returns>
    public Task<TResult> InvokeAsync<TResult>(TCaller caller, TArgs args, Func<InvokerTracer, TArgs, CancellationToken, Task<TResult>> func, CancellationToken cancellationToken = default, [CallerMemberName] string? memberName = null)
        => TraceOnInvokeAsync(caller.ThrowIfNull(), args, (tracer, args, cancellationToken) => func(tracer, args, cancellationToken), memberName, cancellationToken);
}