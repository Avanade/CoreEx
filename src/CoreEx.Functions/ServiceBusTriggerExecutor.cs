// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using Azure.Messaging.ServiceBus;
using CoreEx.Abstractions;
using CoreEx.Configuration;
using CoreEx.Events;
using CoreEx.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CoreEx.Functions
{
    /// <summary>
    /// Provides the standard <see cref="ServiceBusTriggerAttribute"/> execution encapsulation to run the underlying function logic in a consistent manner.
    /// </summary>
    /// <remarks>Each <c>Run</c> is wrapped with the same logic. The correlation identifier is set using the <see cref="ServiceBusReceivedMessage.CorrelationId"/>; where <c>null</c> a
    /// <see cref="Guid.NewGuid"/> is used as the default. A <see cref="ILogger.BeginScope{TState}(TState)"/> with the <see cref="ExecutionContext.CorrelationId"/> and <see cref="ServiceBusReceivedMessage.MessageId"/> is performed to wrap the logic
    /// with the correlation and message identifiers. Where the unhandled <see cref="Exception"/> is <see cref="IExtendedException.IsTransient"/> this will bubble out for the Azure Function runtime/fabric to retry and automatically 
    /// deadletter; otherwise, it will be immediately deadletted with a resaon of <see cref="IExtendedException.ErrorType"/> or <see cref="ErrorType.UnhandledError"/> depending on the exception <see cref="Type"/>.</remarks>
    public class ServiceBusTriggerExecutor : Executor, IServiceBusTriggerExecutor
    {
        private const string _errorText = "Invalid message: body was not provided, contained invalid JSON, or was incorrectly formatted:";

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceBusTriggerExecutor"/> class.
        /// </summary>
        /// <param name="eventSerializer">The <see cref="IEventSerializer"/>.</param>
        /// <param name="executionContext">The <see cref="ExecutionContext"/>.</param>
        /// <param name="settings">The <see cref="SettingsBase"/>.</param>
        /// <param name="logger">The <see cref="ILogger"/>.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public ServiceBusTriggerExecutor(IEventSerializer eventSerializer, ExecutionContext executionContext, SettingsBase settings, ILogger<ServiceBusTriggerExecutor> logger) : base(executionContext, settings, logger)
            => EventSerializer = eventSerializer ?? throw new ArgumentNullException(nameof(eventSerializer));

        /// <summary>
        /// Gets the <see cref="IEventSerializer"/>.
        /// </summary>
        public IEventSerializer EventSerializer { get; }

        /// <inheritdoc/>
        public async Task RunAsync<T>(ServiceBusReceivedMessage message, ServiceBusMessageActions messageActions, Func<EventData<T>, Task> function, bool valueIsRequired = true, Func<EventData<T>, Task>? afterReceive = null)
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
                EventData<T> @event = default!;
                ValidationException? vex = null;
                try
                {
                    @event = await EventSerializer.DeserializeAsync<T>(message.Body).ConfigureAwait(false)!;
                    if (valueIsRequired && @event.Value == null)
                        vex = new ValidationException($"{_errorText} Value is mandatory.");
                }
                catch (Exception ex)
                {
                    vex = new ValidationException($"{_errorText} {ex.Message}", ex);
                }

                if (vex != null)
                {
                    await DeadLetterExceptionAsync(message, messageActions, vex.ErrorType, vex).ConfigureAwait(false);
                    return;
                }

                if (afterReceive != null)
                    await afterReceive(@event).ConfigureAwait(false);

                // Invoke the actual function logic.
                await function(@event).ConfigureAwait(false);

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