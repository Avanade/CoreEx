// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Results;
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
        private const string InvokerType = "invoker.type";
        private const string InvokerOwner = "invoker.owner";
        private const string InvokerMember = "invoker.member";
        private const string InvokerState = "invoker.state";
        private const string InvokerFailure = "invoker.failure";
        private const string SuccessState = "Success";
        private const string FailureState = "Failure";
        private const string ExceptionState = "Exception";

        private IResult? _result;

        /// <summary>
        /// Initializes a new instance of the <see cref="InvokeArgs"/> struct.
        /// </summary>
        /// <param name="invokerType">The invoker <see cref="Type"/> used to manage the activity sources.</param>
        /// <param name="ownerType">The invoking (owner) <see cref="Type"/> to include as part of the <see cref="Activity.OperationName"/>.</param>
        /// <param name="memberName">The calling member name.</param>
        /// <remarks>Creates the tracing <see cref="Activity.OperationName"/> by concatenating the invoking <paramref name="ownerType"/> (<see cref="Type.FullName"/>) and <paramref name="memberName"/> separated by '<c> -> </c>'. This is <i>not</i>
        /// meant to represent the fully-qualified member/method name.</remarks>
        internal InvokeArgs(Type invokerType, Type? ownerType, string? memberName)
        {
            try
            {
                var activitySource = _activitySources.GetOrAdd(invokerType, type => new ActivitySource(type.FullName ?? NullName));
                Activity = activitySource.StartActivity(ownerType is null ? memberName ?? NullName : $"{ownerType.FullName} -> {memberName ?? NullName}");
                if (Activity is not null)
                {
                    Activity.SetTag(InvokerType, activitySource!.Name);
                    Activity.SetTag(InvokerOwner, ownerType?.FullName);
                    Activity.SetTag(InvokerMember, memberName);
                }
            }
            catch
            {
                // Continue; do not allow tracing to impact the execution.
                Activity = null;
            }
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
        internal TResult TraceResult<TResult>(TResult result)
        {
            if (Activity is not null)
            {
                _result = result as IResult ?? Result.Success;
                Activity.SetTag(InvokerState, _result.IsSuccess ? SuccessState : FailureState);
                if (_result.IsFailure)
                    Activity.SetTag(InvokerFailure, $"{_result.Error.Message} [{_result.Error.GetType().Name}]");
            }

            return result;
        }

        /// <summary>
        /// Completes the <see cref="Activity"/> (if started).
        /// </summary>
        internal void Complete()
        {
            if (Activity is not null)
            {
                // Where no result then it can only be as a result of an exception.
                if (_result is null)
                    Activity.SetTag(InvokerState, ExceptionState);

                Activity.Stop();
            }
        }

        /// <summary>
        /// Releases (disposes) all <see cref="ActivitySource"/> instances.
        /// </summary>
        public static void ReleaseAll()
        {
            foreach (var item in _activitySources.ToArray())
            {
                if (_activitySources.TryRemove(item.Key, out var itemValue))
                    itemValue.Dispose();
            }
        }
    }
}