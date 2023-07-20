// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Invokers;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Events
{
    /// <summary>
    /// Provides the invocation wrapping for the <see cref="EventPublisher"/> instances.
    /// </summary>
    public class EventPublisherInvoker : InvokerBase<EventPublisher>
    {
        private const string InvokerEventSender = "invoker.eventsender";
        private static EventPublisherInvoker? _default;

        /// <summary>
        /// Gets the current configured instance (see <see cref="ExecutionContext.ServiceProvider"/>).
        /// </summary>
        public static EventPublisherInvoker Current => CoreEx.ExecutionContext.GetService<EventPublisherInvoker>() ?? (_default ??= new EventPublisherInvoker());

        /// <inheritdoc/>
        protected override TResult OnInvoke<TResult>(InvokeArgs invokeArgs, EventPublisher invoker, Func<TResult> func) => throw new NotSupportedException();

        /// <inheritdoc/>
        protected override Task<TResult> OnInvokeAsync<TResult>(InvokeArgs invokeArgs, EventPublisher invoker, Func<CancellationToken, Task<TResult>> func, CancellationToken cancellationToken)
        {
            invokeArgs.Activity?.AddTag(InvokerEventSender, invoker.EventSender.GetType().FullName);
            return base.OnInvokeAsync(invokeArgs, invoker, func, cancellationToken);
        }
    }
}