// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Configuration;
using CoreEx.Results;
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
        private static readonly ConcurrentDictionary<Type, (ActivitySource ActivitySource, bool IsTracingEnabled, bool IsLoggingEnabled)> _invokerOptions = new();

        private const string NullName = "null";
        private const string InvokerType = "invoker.type";
        private const string InvokerOwner = "invoker.owner";
        private const string InvokerMember = "invoker.member";
        private const string InvokerResult = "invoker.result";
        private const string InvokerFailure = "invoker.failure";
        private const string CompleteState = "Complete";
        private const string SuccessState = "Success";
        private const string FailureState = "Failure";
        private const string ExceptionState = "Exception";

        private bool _isComplete;

        /// <summary>
        /// Determines whether tracing is enabled for the <paramref name="invokerType"/>.
        /// </summary>
        private static bool IsTracingEnabled(Type invokerType)
        {
            var settings = ExecutionContext.GetService<SettingsBase>() ?? new DefaultSettings(ExecutionContext.GetService<IConfiguration>());
            if (settings.Configuration is null)
                return true;

            return settings.GetValue<bool?>($"Invokers:{invokerType.FullName}:TracingEnabled") ?? settings.GetValue<bool?>("Invokers:Default:TracingEnabled") ?? true;
        }

        /// <summary>
        /// Determines whether logging is enabled for the <paramref name="invokerType"/>.
        /// </summary>
        private static bool IsLoggingEnabled(Type invokerType)
        {
            var settings = ExecutionContext.GetService<SettingsBase>() ?? new DefaultSettings(ExecutionContext.GetService<IConfiguration>());
            if (settings.Configuration is null)
                return true;

            return settings.GetValue<bool?>($"Invokers:{invokerType.FullName}:LoggingEnabled") ?? settings.GetValue<bool?>("Invokers:Default:LoggingEnabled") ?? true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InvokeArgs"/> struct.
        /// </summary>
        public InvokeArgs() => Type = typeof(Invoker);

        /// <summary>
        /// Initializes a new instance of the <see cref="InvokeArgs"/> struct.
        /// </summary>
        /// <param name="invokerType">The invoker <see cref="Type"/> used to manage the activity sources.</param>
        /// <param name="ownerType">The invoking (owner) <see cref="Type"/> to include as part of the <see cref="Activity.OperationName"/>.</param>
        /// <param name="memberName">The calling member name.</param>
        /// <param name="invokeArgs">The optional parent <see cref="InvokeArgs"/>.</param>
        /// <remarks>Creates the tracing <see cref="Activity.OperationName"/> by concatenating the invoking <paramref name="ownerType"/> (<see cref="Type.FullName"/>) and <paramref name="memberName"/> separated by '<c> -> </c>'. This is <i>not</i>
        /// meant to represent the fully-qualified member/method name.</remarks>
        public InvokeArgs(Type invokerType, Type? ownerType, string? memberName, InvokeArgs? invokeArgs)
        {
            Type = invokerType;
            OwnerType = ownerType;
            MemberName = memberName;

            try
            {
                var options = _invokerOptions.GetOrAdd(invokerType, type => (new ActivitySource(type.FullName ?? NullName), IsTracingEnabled(invokerType), IsLoggingEnabled(invokerType)));
                if (options.IsTracingEnabled)
                {
                    Activity = options.ActivitySource.CreateActivity(ownerType is null ? memberName ?? NullName : $"{ownerType}->{memberName ?? NullName}", ActivityKind.Internal);
                    if (Activity is not null)
                    {
                        if (invokeArgs.HasValue && invokeArgs.Value.Activity is not null)
                            Activity.SetParentId(invokeArgs.Value.Activity!.TraceId, invokeArgs.Value.Activity.SpanId, invokeArgs.Value.Activity.ActivityTraceFlags);

                        Activity.SetTag(InvokerType, options.ActivitySource!.Name);
                        Activity.SetTag(InvokerOwner, ownerType?.FullName);
                        Activity.SetTag(InvokerMember, memberName);
                        Activity.Start();
                    }
                }

                if (options.IsLoggingEnabled)
                {
                    Logger = ExecutionContext.GetService<ILogger<Invoker>>();
                    if (Logger is null || !Logger.IsEnabled(LogLevel.Debug))
                        Logger = null;
                    else
                    {
                        Logger.LogDebug("{InvokerType}: Start {InvokerCaller}.", invokerType.ToString(), FormatCaller());
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
        /// Gets the invoker <see cref="Type"/>.
        /// </summary>
        public Type Type { get; }

        /// <summary>
        /// Gets the invoking (owner) <see cref="Type"/>
        /// </summary>
        public Type? OwnerType { get; }

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
        /// Formats the caller.
        /// </summary>
        private readonly string FormatCaller() => OwnerType is null ? MemberName ?? NullName : $"{OwnerType}->{MemberName ?? NullName}";

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
                Activity.SetTag(InvokerResult, ir is null ? CompleteState : (ir.IsSuccess ? SuccessState : FailureState));
                if (ir is not null && ir.IsFailure)
                    Activity.SetTag(InvokerFailure, $"{ir.Error.Message} ({ir.Error.GetType()})");

                _isComplete = true;
            }

            if (Logger is not null)
            {
                Stopwatch!.Stop();
                var ir = result as IResult;
                Logger.LogDebug("{InvokerType}: {InvokerResult} {InvokerCaller}{InvokerFailure} [{Elapsed}ms].", 
                    Type.ToString(), ir is null ? CompleteState : (ir.IsSuccess ? SuccessState : FailureState), FormatCaller(), (ir is not null && ir.IsFailure) ? $" {ir.Error.Message} ({ir.Error.GetType()})" : string.Empty, Stopwatch.Elapsed.TotalMilliseconds);
               
                _isComplete = true;
            }

            return result;
        }

        /// <summary>
        /// Completes the <see cref="Activity"/> tracing (where started) recording the <see cref="InvokerResult"/> with the <see cref="ExceptionState"/> and capturing the corresponding <see cref="Exception.Message"/>.
        /// </summary>
        /// <param name="ex">The <see cref="System.Exception"/>.</param>
        public void TraceException(Exception ex)
        {
            if (Activity is not null && ex is not null)
            {
                Activity.SetTag(InvokerResult, ExceptionState);
                Activity.SetTag(InvokerFailure, $"{ex.Message} ({ex.GetType()})");
                _isComplete = true;
            }

            if (Logger is not null && ex is not null)
            {
                Stopwatch!.Stop();
                Logger.LogDebug("{InvokerType}: {InvokerResult} {InvokerCaller}{InvokerFailure} [{Elapsed}ms].", Type.ToString(), ExceptionState, FormatCaller(), $" {ex.Message} ({ex.GetType()})", Stopwatch.Elapsed.TotalMilliseconds);
                _isComplete = true;
            }
        }

        /// <summary>
        /// Completes (stops) the <see cref="Activity"/> tracing (where started).
        /// </summary>
        /// <remarks>Where not previously recorded as complete will set the <see cref="InvokerResult"/> to <see cref="ExceptionState"/>.</remarks>
        public readonly void TraceComplete()
        {
            if (Activity is not null)
            {
                // Where no result then it can only be as a result of an exception.
                if (!_isComplete)
                    Activity.SetTag(InvokerResult, ExceptionState);

                Activity.Stop();
            }

            if (Logger is not null && !_isComplete)
            {
                Stopwatch!.Stop();
                Logger.LogDebug("{InvokerType}: {InvokerResult} {InvokerCaller} [{Elapsed}ms].", Type.ToString(), ExceptionState, FormatCaller(), Stopwatch.Elapsed.TotalMilliseconds);
            }
        }

        /// <summary>
        /// Creates (and started) a new <see cref="InvokeArgs"/> instance for a related invocation.
        /// </summary>
        /// <param name="invokerType">The invoker <see cref="Type"/> used to manage the activity sources.</param>
        /// <param name="ownerType">The invoking (owner) <see cref="Type"/> to include as part of the <see cref="Activity.OperationName"/>.</param>
        /// <param name="memberName">The calling member name.</param>
        /// <returns>The <see cref="InvokeArgs"/>.</returns>
        public readonly InvokeArgs StartNewRelated(Type invokerType, Type? ownerType, string? memberName) => new(invokerType, ownerType, memberName, this);

        /// <summary>
        /// Releases (disposes) all <see cref="ActivitySource"/> instances.
        /// </summary>
        public static void ReleaseAll()
        {
            foreach (var item in _invokerOptions.ToArray())
            {
                if (_invokerOptions.TryRemove(item.Key, out var options))
                    options.ActivitySource?.Dispose();
            }
        }
    }
}