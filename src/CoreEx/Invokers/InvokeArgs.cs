// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Configuration;
using CoreEx.Results;
using Microsoft.Extensions.Configuration;
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
        private static readonly ConcurrentDictionary<Type, (ActivitySource ActivitySource, bool IsTracingEnabled)> _invokerOptions = new();

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
            try
            {
                var options = _invokerOptions.GetOrAdd(invokerType, type => (new ActivitySource(type.FullName ?? NullName), IsTracingEnabled(invokerType)));
                if (!options.IsTracingEnabled)
                    return;

                Activity = options.ActivitySource.CreateActivity(ownerType is null ? memberName ?? NullName : $"{ownerType.FullName} -> {memberName ?? NullName}", ActivityKind.Internal);
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
            catch
            {
                // Continue; do not allow tracing to impact the execution.
                Activity = null;
            }
        }

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
        /// Gets the <see cref="System.Diagnostics.Activity"/> leveraged for standardized (open-telemetry) tracing.
        /// </summary>
        /// <remarks>Will be <c>null</c> where tracing is <i>not</i> enabled.</remarks>
        public Activity? Activity { get; }

        /// <summary>
        /// Adds the result outcome to the <see cref="Activity"/> (where started).
        /// </summary>
        /// <typeparam name="TResult">The result <see cref="Type"/>.</typeparam>
        /// <param name="result">The result value.</param>
        /// <returns>The <paramref name="result"/>.</returns>
        public TResult TraceResult<TResult>(TResult result)
        {
            if (Activity is not null)
            {
                var ir = result as IResult;
                Activity.SetTag(InvokerResult, ir is null ? CompleteState : (ir.IsSuccess ? SuccessState : FailureState));
                if (ir is not null && ir.IsFailure)
                    Activity.SetTag(InvokerFailure, $"{ir.Error.Message} [{ir.Error.GetType().Name}]");

                _isComplete = true;
            }

            return result;
        }

        /// <summary>
        /// Completes the <see cref="Activity"/> (where started).
        /// </summary>
        public readonly void Complete()
        {
            if (Activity is not null)
            {
                // Where no result then it can only be as a result of an exception.
                if (!_isComplete)
                    Activity.SetTag(InvokerResult, ExceptionState);

                Activity.Stop();
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