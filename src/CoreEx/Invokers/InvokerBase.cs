namespace CoreEx.Invokers;

/// <summary>
/// Enables standard functionality to be added to an invocation including tracing (and logging). 
/// </summary>
public abstract class InvokerBase : IInvoker
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvokerBase{TSelf}"/> class.
    /// </summary>
    /// <param name="serviceProvider">The optional <i>root</i> <see cref="IServiceProvider"/> needed where there is no <see cref="ExecutionContext.Current"/> <see cref="ExecutionContext.ServiceProvider"/>.</param>
    public InvokerBase(IServiceProvider? serviceProvider = null)
    {
        Type = GetType();
        Name = InvokerNameAttribute.GetName(Type);
        ServiceProvider = serviceProvider;
        Logger = serviceProvider?.GetService<ILoggerFactory>()?.CreateLogger(Type);
        Configuration = serviceProvider?.GetService<IConfiguration>();
    }

    /// <inheritdoc/>
    public Type Type { get; }

    /// <inheritdoc/>
    public string Name { get; }

    /// <summary>
    /// Gets the optional <see cref="IServiceProvider"/>.
    /// </summary>
    public IServiceProvider? ServiceProvider { get; }

    /// <inheritdoc/>
    public ILogger? Logger { get; }

    /// <inheritdoc/>
    public IConfiguration? Configuration { get; }

    /// <inheritdoc/>
    /// <remarks>Defaults to <see cref="ActivityKind.Internal"/>.</remarks>
    public virtual ActivityKind ActivityKind => ActivityKind.Internal;

    /// <inheritdoc/>
    public virtual bool IsTracingDisabled => false;

    /// <inheritdoc/>
    public virtual bool IsLoggingDisabled => false;

    /// <inheritdoc/>
    void IInvoker.OnActivityStart(InvokerTracer args) => OnActivityStart(args);

    /// <inheritdoc/>
    void IInvoker.OnActivityException(InvokerTracer args, Exception exception) => OnActivityException(args, exception);

    /// <inheritdoc/>
    void IInvoker.OnActivityComplete(InvokerTracer args) => OnActivityComplete(args);

    /// <summary>
    /// Invoked on <see cref="Activity"/> start.
    /// </summary>
    /// <param name="args">The <see cref="InvokerTracer"/>.</param>
    /// <remarks>Where overriding the base <see cref="OnActivityStart"/> <b>must</b> be invoked.</remarks>
    protected virtual void OnActivityStart(InvokerTracer args) { }

    /// <summary>
    /// Invoked where the invocation resulted in an <paramref name="exception"/> for an <see cref="Activity"/>.
    /// </summary>
    /// <remarks>Where overriding the base <see cref="OnActivityException"/> <b>must</b> be invoked.
    /// <para><see cref="OnActivityComplete(InvokerTracer)"/> and <see cref="OnActivityException(InvokerTracer, Exception)"/> are mutually exclusive.</para></remarks>
    protected virtual void OnActivityException(InvokerTracer args, Exception exception) { }

    /// <summary>
    /// Invoked where the invocation completes successfully for an <see cref="Activity"/>.
    /// </summary>
    /// <remarks>Where overriding the base <see cref="OnActivityComplete"/> <b>must</b> be invoked.
    /// <para><see cref="OnActivityComplete(InvokerTracer)"/> and <see cref="OnActivityException(InvokerTracer, Exception)"/> are mutually exclusive.</para></remarks>
    protected virtual void OnActivityComplete(InvokerTracer args) { }
}