// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using Azure.Messaging.ServiceBus;
using CoreEx.Abstractions;
using CoreEx.Azure.ServiceBus.Abstractions;
using CoreEx.Events;
using CoreEx.Events.Subscribing;
using CoreEx.Invokers;
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
        private const string SubscriberAbandonReasonPropertyName = "SubscriberAbandonReason";

        /// <inheritdoc/>
        protected override TResult OnInvoke<TResult>(InvokeArgs invokeArgs, EventSubscriberBase invoker, Func<InvokeArgs, TResult> func, (ServiceBusReceivedMessage Message, ServiceBusMessageActions MessageActions) args) => throw new NotSupportedException();

        /// <inheritdoc/>
        protected async override Task<TResult> OnInvokeAsync<TResult>(InvokeArgs invokeArgs, EventSubscriberBase invoker, Func<InvokeArgs, CancellationToken, Task<TResult>> func, (ServiceBusReceivedMessage Message, ServiceBusMessageActions MessageActions) args, CancellationToken cancellationToken)
        {
            if (args.Message == null)
                throw new ArgumentException($"The {nameof(ServiceBusReceivedMessage)} value is required.", nameof(args));

            if (args.MessageActions == null)
                throw new ArgumentException($"The {nameof(ServiceBusMessageActions)} value is required.", nameof(args));

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
                return await OnInvokeInternalAsync(invokeArgs, invoker, func, args, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                var keepThrowing = await HandleExceptionAsync(invoker, args.Message, args.MessageActions, ex, cancellationToken).ConfigureAwait(false);
                OnAfterMessageProcessing(invoker, args.Message, ex);

                if (keepThrowing)
                    throw;

                return default!;
            }
            finally
            {
                scope?.Dispose();
            }
        }

        /// <summary>
        /// Performs the internal service bus message processing and error handling.
        /// </summary>
        private async Task<TResult> OnInvokeInternalAsync<TResult>(InvokeArgs invokeArgs, EventSubscriberBase invoker, Func<InvokeArgs, CancellationToken, Task<TResult>> func, (ServiceBusReceivedMessage Message, ServiceBusMessageActions MessageActions) args, CancellationToken cancellationToken)
        {
            TResult result = default!;

            try
            {
                invoker.Logger.LogDebug("Received - Service Bus message '{Message}'.", args.Message.MessageId);

                // Leverage the EventSubscriberInvoker to manage execution and standardized exception handling.
                result = await invoker.EventSubscriberInvoker.InvokeAsync(invoker, async (_, ct) =>
                {
                    // Execute the logic.
                    return await base.OnInvokeAsync(invokeArgs, invoker, func, args, cancellationToken).ConfigureAwait(false);
                }, cancellationToken).ConfigureAwait(false);
            }
            catch (EventSubscriberException) { throw; }
            catch (Exception ex) when (ex is IExtendedException eex)
            {
                // Handle the exception based on the subscriber configuration.
                var handling = ErrorHandler.DetermineErrorHandling(invoker, eex);
                if (handling == ErrorHandling.HandleByHost)
                {
                    if (invoker.WorkStateOrchestrator is not null)
                        await invoker.WorkStateOrchestrator.IndeterminateAsync(args.Message.MessageId, ex.Message, cancellationToken).ConfigureAwait(false);

                    invoker.Instrumentation?.Instrument(ErrorHandling.HandleByHost, ex);
                    throw;
                }

                await invoker.ErrorHandler.HandleErrorAsync(new ErrorHandlerArgs(args.Message.MessageId, new EventSubscriberException(ex.Message, ex), handling, invoker.Logger) { Instrumentation = invoker.Instrumentation, WorkOrchestrator = invoker.WorkStateOrchestrator }, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (invoker.UnhandledHandling != ErrorHandling.HandleByHost)
            {
                await invoker.ErrorHandler.HandleErrorAsync(new ErrorHandlerArgs(args.Message.MessageId, new EventSubscriberException(ex.Message, ex), invoker.UnhandledHandling, invoker.Logger) { Instrumentation = invoker.Instrumentation, WorkOrchestrator = invoker.WorkStateOrchestrator }, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (invoker.UnhandledHandling == ErrorHandling.HandleByHost)
            {
                if (invoker.WorkStateOrchestrator is not null)
                    await invoker.WorkStateOrchestrator.IndeterminateAsync(args.Message.MessageId!, ex.Message, cancellationToken).ConfigureAwait(false);

                invoker.Instrumentation?.Instrument(ErrorHandling.HandleByHost, ex);
                throw;
            }
            finally
            {
                OnAfterMessageProcessing(invoker, args.Message, null);
            }

            // Everything is good, so complete the message.
            invoker.Logger.LogDebug("Completing - Service Bus message '{Message}'.", args.Message.MessageId);
            await args.MessageActions.CompleteMessageAsync(args.Message, cancellationToken).ConfigureAwait(false);
            invoker.Logger.LogDebug("Completed - Service Bus message '{Message}'.", args.Message.MessageId);

            return result;
        }

        /// <summary>
        /// Handle the exception.
        /// </summary>
        private static async Task<bool> HandleExceptionAsync(EventSubscriberBase invoker, ServiceBusReceivedMessage message, ServiceBusMessageActions messageActions, Exception exception, CancellationToken cancellationToken)
        {
            // Handle a known exception type.
            if (exception is EventSubscriberException eex)
            {
                if (eex.ErrorHandling == ErrorHandling.HandleByHost)
                    return true; // Keep throwing; i.e. bubble exception.

                // Where not considered transient then dead-letter.
                if (!eex.IsTransient)
                {
                    await DeadLetterExceptionAsync(invoker, message, messageActions, eex.ErrorType, exception, cancellationToken).ConfigureAwait(false);
                    if (invoker.WorkStateOrchestrator is not null)
                        await invoker.WorkStateOrchestrator.FailAsync(message.MessageId, exception.Message, cancellationToken).ConfigureAwait(false);

                    return false;
                }

                // Determine the delay, if any.
                var sbs = invoker as IServiceBusSubscriber;
                var delay = sbs is not null && sbs.RetryDelay.HasValue ? (int)(sbs.RetryDelay.Value.TotalMilliseconds * message.DeliveryCount) : -1;
                if (sbs is not null && sbs.MaxRetryDelay.HasValue)
                {
                    if (delay < 0 || delay > sbs.MaxRetryDelay.Value.TotalMilliseconds)
                        delay = (int)sbs.MaxRetryDelay.Value.TotalMilliseconds;
                }

                // Where the exception is known then exception and stack trace need not be logged.
                var ex = eex.HasInnerExtendedException ? null : exception;

                // Log the transient retry as a warning.
                if (delay <= 0)
                    invoker.Logger.LogWarning(ex, "Retry - Service Bus message '{Message}'. [{Reason}] Processing attempt {Count}. {Error}", message.MessageId, eex.ErrorType, message.DeliveryCount, exception.Message);
                else
                    invoker.Logger.LogWarning(ex, "Retry - Service Bus message '{Message}'. [{Reason}] Processing attempt {Count}; retry delay {Delay}ms. {Error}", message.MessageId, eex.ErrorType, message.DeliveryCount, delay, exception.Message);

                if (sbs is not null)
                {
                    if (sbs.MaxDeliveryCount.HasValue && message.DeliveryCount >= sbs.MaxDeliveryCount.Value)
                    {
                        // Dead-letter when maximum delivery count achieved.
                        var msg = $"Message could not be consumed after {sbs.MaxDeliveryCount.Value} attempts (as defined by {invoker.GetType().Name}).";
                        await DeadLetterExceptionAsync(invoker, message, messageActions, "MaxDeliveryCountExceeded", new EventSubscriberException(msg, exception), cancellationToken).ConfigureAwait(false);
                        if (invoker.WorkStateOrchestrator is not null)
                            await invoker.WorkStateOrchestrator.FailAsync(message.MessageId, msg, cancellationToken).ConfigureAwait(false);

                        return false;
                    }

                    if (delay > 0)
                    {
                        // Renew the lock to maximize time and then delay.
                        invoker.Logger.LogDebug("Retry delaying - Service Bus message '{Message}'. Retry delay {Delay}ms.", message.MessageId, delay);
                        await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                        invoker.Logger.LogDebug("Retry delayed - Service Bus message '{Message}'.", message.MessageId, delay);
                    }

                    if (sbs.AbandonOnTransient)
                    {
                        // Abandon message versus bubbling.
                        invoker.Logger.LogDebug("Abandoning - Service Bus message '{Message}'.", message.MessageId);
                        await messageActions.AbandonMessageAsync(message, new Dictionary<string, object> { { SubscriberAbandonReasonPropertyName, FormatText(exception.Message) } }, cancellationToken).ConfigureAwait(false);
                        invoker.Logger.LogDebug("Abandoned - Service Bus message '{Message}'.", message.MessageId);
                        return false;
                    }
                }

                return true; // Keep throwing; i.e. bubble exception.
            }

            // For the known exceptions it can be assumed that it only got this far because error handling for it was None so keep bubbling.
            if (exception is IExtendedException)
                return true;

            // Where the unhandled handling is set to None then keep bubbling; do not dead-letter.
            if (invoker.UnhandledHandling == ErrorHandling.HandleByHost)
                return true; 

            // Dead-letter the unhandled exception.
            await DeadLetterExceptionAsync(invoker, message, messageActions, ErrorType.UnhandledError.ToString(), exception, cancellationToken).ConfigureAwait(false);
            if (invoker.WorkStateOrchestrator is not null)
                await invoker.WorkStateOrchestrator.FailAsync(message.MessageId, exception.Message, cancellationToken).ConfigureAwait(false);

            return false;
        }

        /// <summary>
        /// Performs the dead-lettering.
        /// </summary>
        public static async Task DeadLetterExceptionAsync(EventSubscriberBase invoker, ServiceBusReceivedMessage message, ServiceBusMessageActions messageActions, string errorReason, Exception exception, CancellationToken cancellationToken)
        {
            var ex = exception is EventSubscriberException esex && esex.HasInnerExtendedException ? null : exception;

            invoker.Logger.LogDebug("Dead Lettering - Service Bus message '{Message}'. [{Reason}] {Error}", message.MessageId, errorReason, exception.Message);
            await messageActions.DeadLetterMessageAsync(message, new Dictionary<string, object> { { SubscriberExceptionPropertyName, FormatText(exception.ToString()) } }, errorReason, FormatText(exception.Message), cancellationToken).ConfigureAwait(false);
            invoker.Logger.LogError(ex, "Dead Lettered - Service Bus message '{Message}'. [{Reason}] {Error}", message.MessageId, errorReason, exception.Message);
        }

        /// <summary>
        /// Shortens the text to 2048 characters; should be enough to given context - otherwise, full context should have be written to the log.
        /// </summary>
        private static string FormatText(string? text) => text?[..Math.Min(text.Length, 2048)] ?? string.Empty;

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
        /// Provides an opportunity to perform additional logging/monitoring after the <paramref name="message"/> processing occurs (including any corresponding <see cref="ServiceBusReceivedMessage"/> invocation).
        /// </summary>
        /// <param name="subscriber">The invoking <see cref="EventSubscriberBase"/>.</param>
        /// <param name="message">The <see cref="ServiceBusReceivedMessage"/>.</param>
        /// <param name="exception">The corresponding <see cref="Exception"/> where an error occured.</param>
        /// <remarks>An <see cref="Exception"/> should not be thrown within as this may result in an unexpected error.</remarks>
        protected virtual void OnAfterMessageProcessing(EventSubscriberBase subscriber, ServiceBusReceivedMessage message, Exception? exception) { }
    }
}