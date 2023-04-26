// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using Azure.Messaging.ServiceBus;
using CoreEx.Abstractions;
using CoreEx.Events;
using CoreEx.Invokers;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Azure.ServiceBus
{
    /// <summary>
    /// Provides the standard <see cref="ServiceBusReceivedMessage"/> <see cref="EventSubscriberBase"/> invoker functionality.
    /// </summary>
    public class ServiceBusSubscriberInvoker : InvokerBase<EventSubscriberBase, (ServiceBusReceivedMessage Message, ServiceBusMessageActions MessageActions)>
    {
        private const string SubscriberExceptionPropertyName = "SubscriberException";

        /// <inheritdoc/>
        protected async override Task<TResult> OnInvokeAsync<TResult>(EventSubscriberBase invoker, Func<CancellationToken, Task<TResult>> func, (ServiceBusReceivedMessage Message, ServiceBusMessageActions MessageActions) args, CancellationToken cancellationToken)
        {
            if (args.Message == null)
                throw new ArgumentNullException(nameof(args), $"The {nameof(ServiceBusReceivedMessage)} value is required.");

            if (args.MessageActions == null)
                throw new ArgumentNullException(nameof(args), $"The {nameof(ServiceBusMessageActions)} value is required.");

            if (!string.IsNullOrEmpty(args.Message.CorrelationId))
                invoker.ExecutionContext.CorrelationId = args.Message.CorrelationId;

            var state = new Dictionary<string, object?>
            {
                { nameof(ServiceBusReceivedMessage.MessageId), args.Message.MessageId },
                { nameof(EventData.CorrelationId), invoker.ExecutionContext.CorrelationId }
            };

            // Convert to metadata only to enable logging of standard metadata.
            var @event = await invoker.EventDataConverter.ConvertFromMetadataOnlyAsync(args.Message, cancellationToken).ConfigureAwait(false);
            UpdateLoggerState(args.Message, @event, state);
            var scope = invoker.Logger.BeginScope(state);

            OnBeforeMessageProcessing(invoker, args.Message);

            try
            {
                invoker.Logger.LogDebug("Received - Service Bus message '{Message}'.", args.Message.MessageId);

                // Leverage the EventSubscriberInvoker to manage execution and standardized exception handling.
                var result = await invoker.EventSubscriberInvoker.InvokeAsync(invoker, async (ct) =>
                {
                    // Execute the logic.
                    return await base.OnInvokeAsync(invoker, func, args, cancellationToken).ConfigureAwait(false);
                }, invoker.Logger, cancellationToken).ConfigureAwait(false);

                // Everything is good, so complete the message.
                invoker.Logger.LogDebug("Completing - Service Bus message '{Message}'.", args.Message.MessageId);
                await args.MessageActions.CompleteMessageAsync(args.Message, cancellationToken).ConfigureAwait(false);
                invoker.Logger.LogDebug("Completed - Service Bus message '{Message}'.", args.Message.MessageId);

                return result;
            }
            catch (Exception ex)
            {
                if (ex is IExtendedException eex)
                {
                    if (eex.IsTransient)
                    {
                        // Do not abandon the message when transient, as there may be a Retry Policy configured; otherwise, it should eventaully be dead-lettered by the host/runtime/fabric.
                        invoker.Logger.LogWarning(ex, "Retry - Service Bus message '{Message}'. [{Reason}] Processing attempt {Count}. {Error}", args.Message.MessageId, eex.ErrorType, args.Message.DeliveryCount, ex.Message);
                        OnAfterMessageProcessing(invoker, args.Message, ex);
                        throw;
                    }

                    await DeadLetterExceptionAsync(invoker, args.Message, args.MessageActions, eex.ErrorType, ex, cancellationToken).ConfigureAwait(false);
                }
                else
                    await DeadLetterExceptionAsync(invoker, args.Message, args.MessageActions, ErrorType.UnhandledError.ToString(), ex, cancellationToken).ConfigureAwait(false);

                // It's been handled, swallow the exception and carry on.
                OnAfterMessageProcessing(invoker, args.Message, ex);
                return default!;
            }
            finally
            {
                scope.Dispose();
            }
        }

        /// <summary>
        /// Performs the dead-lettering.
        /// </summary>
        public static async Task DeadLetterExceptionAsync(EventSubscriberBase invoker, ServiceBusReceivedMessage message, ServiceBusMessageActions messageActions, string errorReason, Exception exception, CancellationToken cancellationToken)
        {
            invoker.Logger.LogDebug("Dead Lettering - Service Bus message '{Message}'. [{Reason}] {Error}", message.MessageId, errorReason, exception.Message);
            await messageActions.DeadLetterMessageAsync(message, new Dictionary<string, object?> { { SubscriberExceptionPropertyName, FormatText(exception.ToString()) } }, errorReason, FormatText(exception.Message), cancellationToken).ConfigureAwait(false);
            invoker.Logger.LogError(exception, "Dead Lettered - Service Bus message '{Message}'. [{Reason}] {Error}", message.MessageId, errorReason, exception.Message);
        }

        /// <summary>
        /// Shortens the text to 2048 characters; should be enough to given context - otherwise, full context should have be written to the log.
        /// </summary>
        private static string? FormatText(string? text) => text?[..Math.Min(text.Length, 2048)];

        /// <summary>
        /// Update the <see cref="ILogger.BeginScope{TState}(TState)"/> <paramref name="state"/> from the <paramref name="message"/>.
        /// </summary>
        /// <param name="message">The <see cref="ServiceBusMessage"/>.</param>
        /// <param name="event">The <see cref="EventData"/> metadata only representation of the <paramref name="message"/>.</param>
        /// <param name="state">The state <see cref="IDictionary{TKey, TValue}"/>.</param>
        /// <remarks>The <see cref="ServiceBusReceivedMessage.MessageId"/> and <see cref="ServiceBusReceivedMessage.CorrelationId"/> are automatically added prior.
        /// <para>The <see cref="ServiceBusReceivedMessage.Subject"/>, <see cref="EventDataBase.Action"/>, <see cref="EventDataBase.Source"/> and <see cref="EventDataBase.Type"/> properties represent the default implementation.</para></remarks>
        protected virtual void UpdateLoggerState(ServiceBusReceivedMessage message, EventData @event, IDictionary<string, object?> state)
        {
            if (!string.IsNullOrEmpty(@event.Subject))
                state.Add(nameof(EventData.Subject), @event.Subject);

            if (!string.IsNullOrEmpty(@event.Action))
                state.Add(nameof(EventData.Action), @event.Action);

            if (@event.Source != null)
                state.Add(nameof(EventData.Source), @event.Source.ToString());

            if (!string.IsNullOrEmpty(@event.Type))
                state.Add(nameof(EventData.Type), @event.Type);
        }

        /// <summary>
        /// Provides an opportunity to perform additional logging/monitoring before the <paramref name="message"/> processing occurs.
        /// </summary>
        /// <param name="subscriber">The invoking <see cref="EventSubscriberBase"/>.</param>
        /// <param name="message">The <see cref="ServiceBusReceivedMessage"/>.</param>
        /// <remarks>An <see cref="Exception"/> should not be thrown within as this may result in an unexpected error.</remarks>
        protected virtual void OnBeforeMessageProcessing(EventSubscriberBase subscriber, ServiceBusReceivedMessage message) { }

        /// <summary>
        /// Provides an opportunity to perform additional logging/monitoring after the <paramref name="message"/> processing occurs (including any corresponding <see cref="ServiceBusReceiveActions"/> invocation).
        /// </summary>
        /// <param name="subscriber">The invoking <see cref="EventSubscriberBase"/>.</param>
        /// <param name="message">The <see cref="ServiceBusReceivedMessage"/>.</param>
        /// <param name="exception">The corresponding <see cref="Exception"/> where an error occured.</param>
        /// <remarks>An <see cref="Exception"/> should not be thrown within as this may result in an unexpected error.</remarks>
        protected virtual void OnAfterMessageProcessing(EventSubscriberBase subscriber, ServiceBusReceivedMessage message, Exception? exception) { }
    }
}