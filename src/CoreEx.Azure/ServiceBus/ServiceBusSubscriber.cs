// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using Azure.Messaging.ServiceBus;
using CoreEx.Abstractions;
using CoreEx.Configuration;
using CoreEx.Events;
using CoreEx.Http;
using CoreEx.Validation;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CoreEx.Azure.ServiceBus
{
    /// <summary>
    /// Provides the standard <see cref="ServiceBusReceivedMessage"/> subscribe (receive) execution encapsulation to run the underlying function logic in a consistent manner.
    /// </summary>
    /// <remarks>The <c>ReceiveAsync</c> enables the standardized logic. The correlation identifier is set using the <see cref="ServiceBusReceivedMessage.CorrelationId"/>; where <c>null</c> a <see cref="Guid.NewGuid"/> will be used as the 
    /// default. A <see cref="ILogger.BeginScope{TState}(TState)"/> with the <see cref="ExecutionContext.CorrelationId"/> and <see cref="ServiceBusReceivedMessage.MessageId"/> is performed to wrap the logic logging with the correlation and
    /// message identifiers. Where the unhandled <see cref="Exception"/> is <see cref="IExtendedException.IsTransient"/> this will bubble out for the Azure Function runtime/fabric to retry and automatically deadletter; otherwise, it will be
    /// immediately deadletted with a reason of <see cref="IExtendedException.ErrorType"/> or <see cref="ErrorType.UnhandledError"/> depending on the exception <see cref="Type"/>.</remarks>
    public class ServiceBusSubscriber : EventSubscriberBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceBusSubscriber"/> class.
        /// </summary>
        /// <param name="eventSerializer">The <see cref="IEventSerializer"/>.</param>
        /// <param name="executionContext">The <see cref="ExecutionContext"/>.</param>
        /// <param name="settings">The <see cref="SettingsBase"/>.</param>
        /// <param name="logger">The <see cref="ILogger"/>.</param>
        public ServiceBusSubscriber(IEventSerializer eventSerializer, ExecutionContext executionContext, SettingsBase settings, ILogger<ServiceBusSubscriber> logger)
            : base(eventSerializer, executionContext, settings, logger) { }

        /// <summary>
        /// Encapsulates the execution of an <see cref="ServiceBusReceivedMessage"/> <paramref name="function"/> converting the <paramref name="message"/> into a corresponding <see cref="EventData{T}"/> for processing.
        /// </summary>
        /// <typeparam name="TValue">The event value <see cref="Type"/>.</typeparam>
        /// <param name="message">The <see cref="ServiceBusReceivedMessage"/>.</param>
        /// <param name="messageActions">The <see cref="ServiceBusMessageActions"/>.</param>
        /// <param name="function">The function logic to invoke.</param>
        /// <param name="valueIsRequired">Indicates whether the value is required; will consider invalid where null.</param>
        /// <param name="validator">The <see cref="IValidator{T}"/> to validate the deserialized value.</param>
        /// <param name="afterReceive">A function that enables the <paramref name="message"/> <see cref="EventData"/> to be processed directly after the message is received and deserialized.</param>
        public async Task ReceiveAsync<TValue>(ServiceBusReceivedMessage message, ServiceBusMessageActions messageActions, Func<EventData<TValue>, Task> function, bool valueIsRequired = true, IValidator<TValue>? validator = null,
            Func<EventData<TValue>, Task>? afterReceive = null)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            if (messageActions == null)
                throw new ArgumentNullException(nameof(messageActions));

            if (function == null)
                throw new ArgumentNullException(nameof(function));

            if (!string.IsNullOrEmpty(message.CorrelationId))
                ExecutionContext.CorrelationId = message.CorrelationId;

            var scope = Logger.BeginScope(new Dictionary<string, object>()
            {
                { HttpConsts.CorrelationIdHeaderName, ExecutionContext.CorrelationId },
                { "MessageId", message.MessageId }
            });

            try
            {
                Logger.LogDebug("Received Service Bus message '{Message}'.", message.MessageId);

                // Deserialize the JSON into the selected type.
                (var @event, var vex) = await DeserializeEventAsync<TValue>(message.Body, valueIsRequired, validator).ConfigureAwait(false);
                if (vex != null)
                {
                    await DeadLetterExceptionAsync(message, messageActions, vex.ErrorType, vex).ConfigureAwait(false);
                    return;
                }

                if (afterReceive != null)
                    await afterReceive(@event!).ConfigureAwait(false);

                // Invoke the actual function logic.
                await function(@event!).ConfigureAwait(false);

                // Everything is good, so complete the message.
                await messageActions.CompleteMessageAsync(message).ConfigureAwait(false);
                Logger.LogDebug("Completed Service Bus message '{Message}'.", message.MessageId);
            }
            catch (Exception ex)
            {
                if (ex is IExtendedException eex)
                {
                    if (eex.IsTransient)
                    {
                        // Do not abandon the message when transient, as there may be a Function Retry Policy configured; otherwise, it will eventaully be dead-lettered by the Azure Function runtime/fabric.
                        Logger.LogWarning("{Reason} while processing message '{Message}'. Processing attempt {Count}", eex.ErrorType, message.MessageId, message.DeliveryCount);
                        throw;
                    }

                    await DeadLetterExceptionAsync(message, messageActions, eex.ErrorType, ex).ConfigureAwait(false);
                }
                else
                    await DeadLetterExceptionAsync(message, messageActions, ErrorType.UnhandledError.ToString(), ex).ConfigureAwait(false);
            }
            finally
            {
                scope.Dispose();
            }
        }

        /// <summary>
        /// Performs the dead-lettering.
        /// </summary>
        private async Task DeadLetterExceptionAsync(ServiceBusReceivedMessage message, ServiceBusMessageActions messageActions, string errorReason, Exception ex)
        {
            Logger.LogError(ex, "{Reason} for Service Bus message '{Message}': {Error}", errorReason, message.MessageId, ex.Message);
            await messageActions.DeadLetterMessageAsync(message, errorReason, ToDeadLetterReason(ex.ToString())).ConfigureAwait(false);
        }

        /// <summary>
        /// Shortens the reason text to 4096 characters, which is the maximum allowed length for a dead letter reason.
        /// </summary>
        private static string? ToDeadLetterReason(string? reason) => reason?[..Math.Min(reason.Length, 4096)];
    }
}