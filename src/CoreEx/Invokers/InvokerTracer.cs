namespace CoreEx.Invokers;

/// <summary>
/// Provides the standardized invocation runtime tracing (and logging) capabilities.
/// </summary>
/// <remarks>This encapsulates the <see cref="Activity"/> to ensure consistency of implementation/usage.</remarks>
public readonly struct InvokerTracer : IDisposable
{
    private const string NullName = "null";
    private const string InvokerResultName = "operation.result";
    private const string InvokerErrorName = "error.type";
    private const string InvokerErrorCodeName = "error.code";
    private const string InvokerErrorMessageName = "error.message";
    private const string InvokerTenantName = "tenant.id";
    private const string StartStateText = "Start";
    private const string CompleteStateText = "Complete";
    private const string ErrorStateText = "Error";
    private const string ExceptionStateText = "Exception";
    private const string ContextStateText = "Context";

    private static readonly ConcurrentDictionary<string, ActivitySource> _activitySources = new();

    /// <summary>
    /// Determines whether tracing is enabled for the <paramref name="invokerType"/>.
    /// </summary>
    private static bool IsTracingEnabled(Type invokerType, IConfiguration? configuration)
        => Internal.GetConfigurationValueWithFallback($"CoreEx:Invokers:{invokerType.FullName}:TracingEnabled", () => "CoreEx:Invokers:TracingEnabled", true, configuration);

    /// <summary>
    /// Determines whether logging is enabled for the <paramref name="invokerType"/>.
    /// </summary>
    private static bool IsLoggingEnabled(Type invokerType, IConfiguration? configuration)
        => Internal.GetConfigurationValueWithFallback($"CoreEx:Invokers:{invokerType.FullName}:LoggingEnabled", () => "CoreEx:Invokers:LoggingEnabled", true, configuration);

    /// <summary>
    /// Gets or sets the <see cref="ICacheEntry.SlidingExpiration"/> <see cref="TimeSpan"/> for <i>tracing</i> and <i>logging</i> enablement determination.
    /// </summary>
    /// <remarks>These are cached to avoid the overhead of repeated configuration lookups and allow for dynamic configuration changes.</remarks>
    public static TimeSpan SlidingExpirationTimeSpan { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Initializes a new instance of the <see cref="InvokerTracer"/> struct as a no-op; not intended for general use.
    /// </summary>
    /// <remarks>This constructor leverages the <see cref="Invokers.Invoker.Default"/> which is essentially a no-op; as in no tracing and/or logging is ever performed.
    /// <para>This is not intended for general use.</para></remarks>
    [Obsolete("Parameterless constructor is not supported.", true)]
    public InvokerTracer() => throw new NotSupportedException();

    /// <summary>
    /// Initializes a new instance of the <see cref="InvokerTracer"/> struct.
    /// </summary>
    /// <param name="invoker">The initiating <see cref="IInvoker"/>.</param>
    /// <param name="caller">The caller (owner) value.</param>
    /// <param name="memberName">The calling member name.</param>
    /// <param name="parent">The optional parent <see cref="InvokerTracer"/>.</param>
    /// <remarks>Creates the tracing <see cref="Activity.OperationName"/> by concatenating the invoking <see name="CallerName"/> (<see cref="Type.FullName"/>) and <paramref name="memberName"/> separated by '<c> -> </c>'. This is <i>not</i>
    /// meant to represent the fully-qualified member/method name.</remarks>
    internal InvokerTracer(IInvoker invoker, object? caller, string? memberName, InvokerTracer? parent)
    {
        Invoker = invoker.ThrowIfNull();
        Caller = caller;
        CallerName = Caller is null ? NullName : InvokerNameAttribute.GetName(Caller.GetType());
        MemberName = memberName ?? NullName;
        OperationName = $"{CallerName}->{MemberName}";

        try
        {
            var enabled = Internal.MemoryCache.GetOrCreate<(bool IsTracingEnabled, bool IsLoggingEnabled)>(Invoker.Type, e =>
            {
                // These are cached to avoid the overhead of repeated configuration lookups.
                var type = (Type)e.Key;
                e.SlidingExpiration = SlidingExpirationTimeSpan;
                return (!invoker.IsTracingDisabled && IsTracingEnabled(type, invoker.Configuration), !invoker.IsLoggingDisabled && IsLoggingEnabled(type, invoker.Configuration));
            });

            if (enabled.IsTracingEnabled)
            {
                var activitySource = _activitySources.GetOrAdd(Invoker.Name, name => new ActivitySource(name));
                Activity = activitySource.CreateActivity(OperationName, Invoker.ActivityKind);
                if (Activity is not null)
                {
                    if (parent.HasValue && parent.Value.Activity is not null)
                        Activity.SetParentId(parent.Value.Activity!.TraceId, parent.Value.Activity.SpanId, parent.Value.Activity.ActivityTraceFlags);

                    if (ExecutionContext.TryGetCurrent(out var ec) && ec.TenantId is not null)
                        Activity.SetTag(InvokerTenantName, ec.TenantId);

                    Invoker.OnActivityStart(this);
                    Activity.Start();
                }
            }

            if (enabled.IsLoggingEnabled)
            {
                Logger = invoker.Logger ?? ExecutionContext.GetService<ILogger<Invoker>>();
                if (Logger is null || !Logger.IsEnabled(LogLevel.Debug))
                    Logger = null;
                else
                {
                    InvokerLogger.InvokeStart(Logger, LogLevel.Debug, Invoker.Name, OperationName, StartStateText);
                    Stopwatch = Stopwatch.StartNew();
                }
            }
        }
        catch
        {
            // Continue; do not allow tracing/logging to impact the execution!
            Activity?.Dispose();
            Activity = null;
            Logger = null;
        }
    }

    /// <summary>
    /// Gets the initiating <see cref="IInvoker"/>.
    /// </summary>
    public readonly IInvoker Invoker { get; }

    /// <summary>
    /// Gets the caller instance.
    /// </summary>
    public readonly object? Caller { get; }

    /// <summary>
    /// Gets the <see cref="Caller"/> name.
    /// </summary>
    public readonly string CallerName { get; }

    /// <summary>
    /// Gets the calling member name.
    /// </summary>
    public readonly string MemberName { get; }

    /// <summary>
    /// Gets the operation name.
    /// </summary>
    public readonly string OperationName { get; }

    /// <summary>
    /// Gets the <see cref="System.Diagnostics.Activity"/> leveraged for standardized (open-telemetry) tracing.
    /// </summary>
    /// <remarks>Will be <see langword="null"/> where tracing is <i>not</i> enabled.</remarks>
    public Activity? Activity { get; }

    /// <summary>
    /// Gets the <see cref="ILogger"/> leveraged for standardized invoker logging.
    /// </summary>
    public ILogger? Logger { get; }

    /// <summary>
    /// Gets the <see cref="Stopwatch"/> leveraged for standardized invoker timing.
    /// </summary>
    public Stopwatch? Stopwatch { get; }

    /// <inheritdoc/>
    public override readonly string ToString() => OperationName;

    /// <summary>
    /// Adds the result outcome to the <see cref="Activity"/> (where successful) then performs an <see cref="Activity.Stop"/>.
    /// </summary>
    internal readonly void TraceComplete(bool isSuccess)
    {
        if (isSuccess)
        {
            if (Activity is not null)
            {
                Activity.SetTag(InvokerResultName, CompleteStateText);
                Invoker.OnActivityComplete(this);
                Activity.SetStatus(ActivityStatusCode.Ok);
                Activity.Stop();
            }

            if (Logger?.IsEnabled(LogLevel.Debug) ?? false)
            {
                Stopwatch!.Stop();
                InvokerLogger.InvokeComplete(Logger, LogLevel.Debug, Invoker.Name, OperationName, CompleteStateText, Stopwatch.Elapsed.TotalMilliseconds);
            }
        }

        Activity?.Stop();
    }

    /// <summary>
    /// Completes the <see cref="Activity"/> tracing (where started) recording the <see cref="InvokerResultName"/> with the <see cref="ExceptionStateText"/> and capturing the corresponding <see cref="Exception.Message"/>.
    /// </summary>
    /// <param name="ex">The <see cref="Exception"/>.</param>
    internal readonly void TraceException(Exception ex)
    {
        Stopwatch?.Stop();

        if (ex is IExtendedException eex && eex.IsError)
        {
            Activity?.SetTag(InvokerResultName, ErrorStateText).SetTag(InvokerErrorName, eex.ErrorType).SetTag(InvokerErrorMessageName, eex.Message).SetTag(InvokerErrorCodeName, eex.ErrorCode);
            Activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            if (Logger?.IsEnabled(LogLevel.Debug) ?? false)
                InvokerLogger.InvokeError(Logger, LogLevel.Debug, Invoker.Name, OperationName, ErrorStateText, eex.ErrorType, Stopwatch!.Elapsed.TotalMilliseconds);
        }
        else
        {
            Activity?.SetTag(InvokerResultName, ExceptionStateText).SetTag(InvokerErrorName, ex.GetType().Name).SetTag(InvokerErrorMessageName, ex.Message);
            Activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            if (Logger?.IsEnabled(LogLevel.Debug) ?? false)
                InvokerLogger.InvokeException(Logger, LogLevel.Debug, Invoker.Name, OperationName, ExceptionStateText, ex.GetType().Name, ex.Message, Stopwatch!.Elapsed.TotalMilliseconds);
        }

        if (Activity is not null)
            Invoker.OnActivityException(this, ex);
    }

    /// <summary>
    /// Logs additional <see cref="LogLevel.Debug"/> <paramref name="context"/> where the <see cref="Logger"/> is enabled.
    /// </summary>
    /// <param name="context">The context message.</param>
    /// <remarks>This is intended to provide additional <paramref name="context"/> in a log message structured similarly to those automatically output.</remarks>
    public readonly void LogContext(string context)
    {
        if (Logger?.IsEnabled(LogLevel.Debug) ?? false)
            InvokerLogger.InvokeContext(Logger, LogLevel.Debug, Invoker.Name, OperationName, ContextStateText, context ?? NullName);
    }

    /// <inheritdoc/>
    public readonly void Dispose() => Activity?.Dispose();

    /// <summary>
    /// Releases (disposes) all <see cref="ActivitySource"/> instances.
    /// </summary>
    public static void ReleaseAll()
    {
        foreach (var item in _activitySources.ToArray())
        {
            if (_activitySources.TryRemove(item.Key, out var activitySource))
                activitySource?.Dispose();
        }
    }
}