// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using Azure.Messaging.ServiceBus;
using CoreEx.Abstractions;
using CoreEx.Events;
using CoreEx.Http;
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
        /// <inheritdoc/>
        protected async override Task<TResult> OnInvokeAsync<TResult>(EventSubscriberBase invoker, Func<CancellationToken, Task<TResult>> func, (ServiceBusReceivedMessage Message, ServiceBusMessageActions MessageActions) args, CancellationToken cancellationToken)
        {
            if (args.Message == null)
                throw new ArgumentNullException(nameof(args), $"The {nameof(ServiceBusReceivedMessage)} value is required.");

            if (args.MessageActions == null)
                throw new ArgumentNullException(nameof(args), $"The {nameof(ServiceBusMessageActions)} value is required.");

            if (!string.IsNullOrEmpty(args.Message.CorrelationId))
                invoker.ExecutionContext.CorrelationId = args.Message.CorrelationId;

            var scope = invoker.Logger.BeginScope(new Dictionary<string, object>()
            {
                { HttpConsts.CorrelationIdHeaderName, invoker.ExecutionContext.CorrelationId },
                { "MessageId", args.Message.MessageId }
            });

            try
            {
                invoker.Logger.LogDebug("Received Service Bus message '{Message}'.", args.Message.MessageId);

                // Leverage the EventSubscriberInvoker to manage execution and standardized exception handling.
                var result = await invoker.EventSubscriberInvoker.InvokeAsync(invoker, async (ct) =>
                {
                    // Execute the logic.
                    return await base.OnInvokeAsync(invoker, func, args, cancellationToken).ConfigureAwait(false);
                }, invoker.Logger, cancellationToken).ConfigureAwait(false);

                // Everything is good, so complete the message.
                await args.MessageActions.CompleteMessageAsync(args.Message, cancellationToken).ConfigureAwait(false);
                invoker.Logger.LogDebug("Completed Service Bus message '{Message}'.", args.Message.MessageId);

                return result;
            }
            catch (Exception ex)
            {
                if (ex is IExtendedException eex)
                {
                    if (eex.IsTransient)
                    {
                        // Do not abandon the message when transient, as there may be a Retry Policy configured; otherwise, it will eventaully be dead-lettered by the host/runtime/fabric.
                        invoker.Logger.LogWarning("{Reason} while processing message '{Message}'. Processing attempt {Count}", eex.ErrorType, args.Message.MessageId, args.Message.DeliveryCount);
                        throw;
                    }

                    await DeadLetterExceptionAsync(invoker, args.Message, args.MessageActions, eex.ErrorType, ex, cancellationToken).ConfigureAwait(false);
                }
                else
                    await DeadLetterExceptionAsync(invoker, args.Message, args.MessageActions, ErrorType.UnhandledError.ToString(), ex, cancellationToken).ConfigureAwait(false);

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
            invoker.Logger.LogError(exception, "{Reason} for Service Bus message '{Message}': {Error}", errorReason, message.MessageId, exception.Message);
            await messageActions.DeadLetterMessageAsync(message, errorReason, ToDeadLetterReason(exception.ToString()), cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Shortens the reason text to 4096 characters, which is the maximum allowed length for a dead letter reason.
        /// </summary>
        private static string? ToDeadLetterReason(string? reason) => reason?[..Math.Min(reason.Length, 4096)];
    }
}