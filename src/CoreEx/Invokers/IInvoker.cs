namespace CoreEx.Invokers;

/// <summary>
/// Enables standardized invocation capabilities.
/// </summary>
public interface IInvoker
{
    /// <summary>
    /// Gets the invoker <see cref="System.Type"/>.
    /// </summary>
    Type Type { get; }

    /// <summary>
    /// Gets the invoker name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the optional <see cref="IServiceProvider"/>.
    /// </summary>
    public IServiceProvider? ServiceProvider { get; }

    /// <summary>
    /// Gets the optional <see cref="ILogger"/>.
    /// </summary>
    ILogger? Logger { get; }

    /// <summary>
    /// Gets the optional <see cref="IConfiguration"/>.
    /// </summary>
    IConfiguration? Configuration { get; }

    /// <summary>
    /// Gets the <see cref="System.Diagnostics.ActivityKind"/> associated with the invoker.
    /// </summary>
    ActivityKind ActivityKind { get; }

    /// <summary>
    /// Indicates whether the tracing is explicitly disabled for the invoker; i.e. will never happen regardless of configuration or other factors.
    /// </summary>
    bool IsTracingDisabled { get; }

    /// <summary>
    /// Indicates whether the logging is explicitly disabled for the invoker; i.e. will never happen regardless of configuration or other factors.
    /// </summary>
    bool IsLoggingDisabled { get; }

    /// <summary>
    /// Invoked on <see cref="Activity"/> start.
    /// </summary>
    /// <param name="tracer">The <see cref="InvokerTracer"/>.</param>
    void OnActivityStart(InvokerTracer tracer);

    /// <summary>
    /// Invoked where the invocation resulted in an <paramref name="exception"/> for an <see cref="Activity"/>.
    /// </summary>
    /// <remarks><see cref="OnActivityComplete(InvokerTracer)"/> and <see cref="OnActivityException(InvokerTracer, Exception)"/> are mutually exclusive.</remarks>
    void OnActivityException(InvokerTracer tracer, Exception exception);

    /// <summary>
    /// Invoked where the invocation completes successfully for an <see cref="Activity"/>.
    /// </summary>
    /// <remarks><see cref="OnActivityComplete(InvokerTracer)"/> and <see cref="OnActivityException(InvokerTracer, Exception)"/> are mutually exclusive.</remarks>
    void OnActivityComplete(InvokerTracer tracer);
}