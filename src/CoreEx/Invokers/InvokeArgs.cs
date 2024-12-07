// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Abstractions;
using CoreEx.Configuration;
using CoreEx.Results;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace CoreEx.Invokers
{
    /// <summary>
    /// Represents the runtime arguments for an <see cref="InvokerBase{TInvoker}"/> or <see cref="InvokerBase{TInvoker, TArgs}"/> invocation.
    /// </summary>
    public struct InvokeArgs
    {
        private static readonly ConcurrentDictionary<Type, ActivitySource> _activitySources = new();

        private const string NullName = "null";
        private const string InvokerTypeName = "invoker.type";
        private const string InvokerOwnerName = "invoker.owner";
        private const string InvokerMemberName = "invoker.member";
        private const string InvokerResultName = "invoker.result";
        private const string InvokerFailureName = "invoker.failure";
        private const string CompleteStateText = "Complete";
        private const string SuccessStateText = "Success";
        private const string FailureStateText = "Failure";
        private const string ExceptionStateText = "Exception";

        private Type? _ownerType;
        private bool _isComplete;

        /// <summary>
        /// Determines whether tracing is enabled for the <paramref name="invokerType"/>.
        /// </summary>
        private static bool IsTracingEnabled(Type invokerType)
        {
            var settings = ExecutionContext.GetService<SettingsBase>() ?? new DefaultSettings(ExecutionContext.GetService<IConfiguration>());
            if (settings.Configuration is null)
                return true;

            return settings.GetCoreExValue<bool?>($"Invokers:{invokerType.FullName}:TracingEnabled") ?? settings.GetCoreExValue<bool?>("Invokers:Default:TracingEnabled") ?? true;
        }

        /// <summary>
        /// Determines whether logging is enabled for the <paramref name="invokerType"/>.
        /// </summary>
        private static bool IsLoggingEnabled(Type invokerType)
        {
            var settings = ExecutionContext.GetService<SettingsBase>() ?? new DefaultSettings(ExecutionContext.GetService<IConfiguration>());
            if (settings.Configuration is null)
                return true;

            return settings.GetCoreExValue<bool?>($"Invokers:{invokerType.FullName}:LoggingEnabled") ?? settings.GetCoreExValue<bool?>("Invokers:Default:LoggingEnabled") ?? true;
        }

        /// <summary>
        /// Provides the default <see cref="IInvoker.CallerLoggerFormatter"/> implementation.
        /// </summary>
        /// <param name="args">The <see cref="InvokeArgs"/>.</param>
        /// <returns>The caller information to be included in the log output.</returns>
        public static string DefaultCallerLogFormatter(InvokeArgs args) => args.OwnerType is null ? args.MemberName ?? NullName : $"{args.OwnerType}->{args.MemberName ?? NullName}";

        /// <summary>
        /// Gets or sets the <see cref="ICacheEntry.AbsoluteExpirationRelativeToNow"/> <see cref="TimeSpan"/> for <i>tracing</i> and <i>logging</i> enablement determination.
        /// </summary>
        /// <remarks>These are cached to avoid the overhead of repeated configuration lookups and allow for dynamic configuration changes.</remarks>
        public static TimeSpan AbsoluteExpirationTimeSpan { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Initializes a new instance of the <see cref="InvokeArgs"/> struct.
        /// </summary>
        /// <remarks>This will throw a <see cref="NotSupportedException"/>.</remarks>
        public InvokeArgs() => throw new NotSupportedException($"The {nameof(InvokeArgs)} default constructor is not supported; please use other(s).");

        /// <summary>
        /// Initializes a new instance of the <see cref="InvokeArgs"/> struct.
        /// </summary>
        /// <param name="invoker">The initiating <see cref="IInvoker"/>.</param>
        /// <param name="owner">The invoking (owner) value.</param>
        /// <param name="memberName">The calling member name.</param>
        /// <param name="invokeArgs">The optional parent <see cref="InvokeArgs"/>.</param>
        /// <remarks>Creates the tracing <see cref="Activity.OperationName"/> by concatenating the invoking <see name="OwnerType"/> (<see cref="Type.FullName"/>) and <paramref name="memberName"/> separated by '<c> -> </c>'. This is <i>not</i>
        /// meant to represent the fully-qualified member/method name.</remarks>
        public InvokeArgs(IInvoker invoker, object? owner, string? memberName, InvokeArgs? invokeArgs)
        {
            Invoker = invoker ?? throw new ArgumentNullException(nameof(invoker));
            InvokerType = invoker.GetType();
            Owner = owner;
            MemberName = memberName;

            try
            {
                var enabled = Internal.MemoryCache.GetOrCreate<(bool IsTracingEnabled, bool IsLoggingEnabled)>(InvokerType, e =>
                {
                    // These are cached to avoid the overhead of repeated configuration lookups and allow for dynamic configuration changes.
                    var type = (Type)e.Key;
                    e.SetAbsoluteExpiration(TimeSpan.FromMinutes(5));
                    return (IsTracingEnabled(type), IsLoggingEnabled(type));
                });
                
                if (enabled.IsTracingEnabled)
                {
                    var activitySource = _activitySources.GetOrAdd(InvokerType, type => new ActivitySource(type.FullName ?? NullName));
                    Activity = activitySource.CreateActivity(OwnerType is null ? memberName ?? NullName : $"{OwnerType}->{memberName ?? NullName}", ActivityKind.Internal);
                    if (Activity is not null)
                    {
                        if (invokeArgs.HasValue && invokeArgs.Value.Activity is not null)
                            Activity.SetParentId(invokeArgs.Value.Activity!.TraceId, invokeArgs.Value.Activity.SpanId, invokeArgs.Value.Activity.ActivityTraceFlags);

                        Activity.SetTag(InvokerTypeName, activitySource.Name);
                        Activity.SetTag(InvokerOwnerName, OwnerType?.FullName);
                        Activity.SetTag(InvokerMemberName, memberName);
                        Invoker.OnActivityStart?.Invoke(this);
                        Activity.Start();
                    }
                }

                if (enabled.IsLoggingEnabled)
                {
                    Logger = ExecutionContext.GetService<ILogger<Invoker>>();
                    if (Logger is null || !Logger.IsEnabled(LogLevel.Debug))
                        Logger = null;
                    else
                    {
                        Logger.LogDebug("{InvokerType}: Start {InvokerCaller}.", InvokerType.ToString(), Invoker.CallerLoggerFormatter(this));
                        Stopwatch = Stopwatch.StartNew();
                    }
                }
            }
            catch
            {
                // Continue; do not allow tracing/logging to impact the execution.
                Activity = null;
                Logger = null;
            }
        }

        /// <summary>
        /// Gets the initiating <see cref="IInvoker"/>.
        /// </summary>
        public IInvoker Invoker { get; }

        /// <summary>
        /// Gets the <see cref="IInvoker"/> <see cref="Type"/>.
        /// </summary>
        public Type InvokerType { get; }

        /// <summary>
        /// Gets the owning invocation value.
        /// </summary>
        public object? Owner { get; }

        /// <summary>
        /// Gets the owning invocation <see cref="Type"/>
        /// </summary>
        public Type? OwnerType => _ownerType ??= Owner?.GetType();

        /// <summary>
        /// Gets the calling member name.
        /// </summary>
        public string? MemberName { get; }

        /// <summary>
        /// Gets the <see cref="System.Diagnostics.Activity"/> leveraged for standardized (open-telemetry) tracing.
        /// </summary>
        /// <remarks>Will be <c>null</c> where tracing is <i>not</i> enabled.</remarks>
        public Activity? Activity { get; }

        /// <summary>
        /// Gets the <see cref="ILogger"/> leveraged for standardized invoker logging.
        /// </summary>
        public ILogger? Logger { get; }

        /// <summary>
        /// Gets the <see cref="Stopwatch"/> leveraged for standardized invoker timing.
        /// </summary>
        public Stopwatch? Stopwatch { get; }

        /// <summary>
        /// Adds the result outcome to the <see cref="Activity"/> (where started).
        /// </summary>
        /// <typeparam name="TResult">The result <see cref="Type"/>.</typeparam>
        /// <param name="result">The result value.</param>
        /// <returns>The <paramref name="result"/>.</returns>
        /// <remarks>Where the <typeparamref name="TResult"/> is a <see cref="Result"/> then the underlying <see cref="Result.Success"/> or <see cref="Result.Error"/> will be recorded accordingly.</remarks>
        public TResult TraceResult<TResult>(TResult result)
        {
            if (Activity is not null)
            {
                var ir = result as IResult;
                Activity.SetTag(InvokerResultName, ir is null ? CompleteStateText : (ir.IsSuccess ? SuccessStateText : FailureStateText));
                if (ir is not null && ir.IsFailure)
                    Activity.SetTag(InvokerFailureName, $"{ir.Error.Message} ({ir.Error.GetType()})");

                Invoker.OnActivityComplete?.Invoke(this);
                _isComplete = true;
            }

            if (Logger is not null)
            {
                Stopwatch!.Stop();
                var ir = result as IResult;
                Logger.LogDebug("{InvokerType}: {InvokerResult} {InvokerCaller}{InvokerFailure} [{Elapsed}ms].", 
                    InvokerType.ToString(), ir is null ? CompleteStateText : (ir.IsSuccess ? SuccessStateText : FailureStateText), Invoker.CallerLoggerFormatter(this), (ir is not null && ir.IsFailure) ? $" {ir.Error.Message} ({ir.Error.GetType()})" : string.Empty, Stopwatch.Elapsed.TotalMilliseconds);
               
                _isComplete = true;
            }

            return result;
        }

        /// <summary>
        /// Completes the <see cref="Activity"/> tracing (where started) recording the <see cref="InvokerResultName"/> with the <see cref="ExceptionStateText"/> and capturing the corresponding <see cref="Exception.Message"/>.
        /// </summary>
        /// <param name="ex">The <see cref="System.Exception"/>.</param>
        public void TraceException(Exception ex)
        {
            if (Activity is not null && ex is not null)
            {
                Activity.SetTag(InvokerResultName, ExceptionStateText);
                Activity.SetTag(InvokerFailureName, $"{ex.Message} ({ex.GetType()})");
                Invoker.OnActivityException?.Invoke(this, ex);
                _isComplete = true;
            }

            if (Logger is not null && ex is not null)
            {
                Stopwatch!.Stop();
                Logger.LogDebug("{InvokerType}: {InvokerResult} {InvokerCaller}{InvokerFailure} [{Elapsed}ms].", InvokerType.ToString(), ExceptionStateText, Invoker.CallerLoggerFormatter(this), $" {ex.Message} ({ex.GetType()})", Stopwatch.Elapsed.TotalMilliseconds);
                _isComplete = true;
            }
        }

        /// <summary>
        /// Completes (stops) the <see cref="Activity"/> tracing (where started).
        /// </summary>
        /// <remarks>Where not previously recorded as complete will set the <see cref="InvokerResultName"/> to <see cref="ExceptionStateText"/>.</remarks>
        public readonly void TraceComplete()
        {
            if (Activity is not null)
            {
                // Where no result then it can only be as a result of an exception.
                if (!_isComplete)
                {
                    Activity.SetTag(InvokerResultName, ExceptionStateText);
                    Invoker.OnActivityException?.Invoke(this, new InvalidOperationException("The invocation was not completed successfully."));
                }

                Activity.Stop();
            }

            if (Logger is not null && !_isComplete)
            {
                Stopwatch!.Stop();
                Logger.LogDebug("{InvokerType}: {InvokerResult} {InvokerCaller} [{Elapsed}ms].", InvokerType.ToString(), ExceptionStateText, Invoker.CallerLoggerFormatter(this), Stopwatch.Elapsed.TotalMilliseconds);
            }
        }

        /// <summary>
        /// Creates (and started) a new <see cref="InvokeArgs"/> instance for a related invocation.
        /// </summary>
        /// <param name="invoker">The invoker used to manage the activity sources.</param>
        /// <param name="owner">The invoking (owner) value.</param>
        /// <param name="memberName">The calling member name.</param>
        /// <returns>The <see cref="InvokeArgs"/>.</returns>
        public readonly InvokeArgs StartNewRelated(IInvoker invoker, object? owner, string? memberName) => new(invoker, owner, memberName, this);

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
}