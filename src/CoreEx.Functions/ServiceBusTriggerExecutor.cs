// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using Azure.Messaging.ServiceBus;
using CoreEx.Configuration;
using CoreEx.Events;
using CoreEx.Json;
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
    /// <remarks>Each <c>Run</c> is wrapped with the same logic. The correlation identifier is set (<see cref="Executor.SetCorrelationId(string?)"/>) using the <see cref="ServiceBusReceivedMessage.CorrelationId"/>; where <c>null</c> a
    /// <see cref="Guid.NewGuid"/> is used as the default. A <see cref="ILogger.BeginScope{TState}(TState)"/> with the <see cref="Executor.GetCorrelationId"/> and <see cref="ServiceBusReceivedMessage.MessageId"/> is performed to wrap the logic
    /// with the correlation and message identifiers. The following exceptions are caught and handled as follows: <see cref="ValidationException"/> results in 
    /// <see cref="ServiceBusMessageActions.DeadLetterMessageAsync(ServiceBusReceivedMessage, string, string, System.Threading.CancellationToken)"/> with the <see cref="DeadLetterValidationReason"/>,
    /// <see cref="TransientException"/> results in the message being re-processed by the Azure Function runtime/fabric; and finally, any unhandled exception results in 
    /// <see cref="ServiceBusMessageActions.DeadLetterMessageAsync(ServiceBusReceivedMessage, string, string, System.Threading.CancellationToken)"/> with the <see cref="DeadLetterUnhandledReason"/>.</remarks>
    public class ServiceBusTriggerExecutor : Executor, IServiceBusTriggerExecutor
    {
        private const string _errorText = "Invalid message: body was not provided, contained invalid JSON, or was incorrectly formatted:";

        /// <summary>
        /// Gets or sets the dead letter reason for a <see cref="ValidationException"/>.
        /// </summary>
        public static string DeadLetterValidationReason { get; set; } = "Validation exception";

        /// <summary>
        /// Gets or sets the dead letter reason for a unhandled <see cref="Exception"/>.
        /// </summary>
        public static string DeadLetterUnhandledReason { get; set; } = "Unhandled exception";

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceBusTriggerExecutor"/> class.
        /// </summary>
        /// <param name="eventSerializer">The <see cref="IEventSerializer"/>.</param>
        /// <param name="settings">The <see cref="SettingsBase"/>.</param>
        /// <param name="logger">The <see cref="ILogger"/>.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public ServiceBusTriggerExecutor(IEventSerializer eventSerializer, SettingsBase settings, ILogger<ServiceBusTriggerExecutor> logger) : base(settings, logger)
            => EventSerializer = eventSerializer ?? throw new ArgumentNullException(nameof(eventSerializer));

        /// <summary>
        /// Gets the <see cref="IEventSerializer"/>.
        /// </summary>
        public IEventSerializer EventSerializer { get; }

        /// <inheritdoc/>
        public async Task RunAsync<T>(ServiceBusReceivedMessage message, Func<EventData<T>, Task> function, ServiceBusMessageActions messageActions, bool valueIsRequired = true, Func<EventData<T>, Task>? afterReceive = null)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            if (function == null)
                throw new ArgumentNullException(nameof(function));

            SetCorrelationId(message.CorrelationId);

            var scope = Logger.BeginScope(new Dictionary<string, object>()
            {
                { CorrelationIdName, GetCorrelationId() },
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
                    if (valueIsRequired && @event.Data == null)
                        vex = new ValidationException($"{_errorText} Value is mandatory.");
                }
                catch (Exception ex)
                {
                    vex = new ValidationException($"{_errorText} {ex.Message}", ex);
                }

                if (vex != null)
                {
                    await DeadLetterValidationExceptionAsync(message, messageActions, vex).ConfigureAwait(false);
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
            catch (TransientException tex)
            {
                // Do not abandon the message, as there may be a Function Retry Policy configured; otherwise, it will eventaully be dead-lettered by the Azure Function runtime/fabric.
                Logger.LogWarning(tex, "Transient error while processing message '{Message}'. Processing attempt {Count}", message.MessageId, message.DeliveryCount);
                throw;
            }
            catch (ValidationException vex)
            {
                await DeadLetterValidationExceptionAsync(message, messageActions, vex).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Unhandled exception while processing message '{Message}': {Error}", message.MessageId, ex.Message);
                await messageActions.DeadLetterMessageAsync(message, DeadLetterUnhandledReason, ToDeadLetterReason(ex.ToString())).ConfigureAwait(false);
            }
            finally
            {
                scope.Dispose();
                SetCorrelationId(null);
            }
        }

        /// <summary>
        /// Performs the <see cref="ValidationException"/> dead-lettering.
        /// </summary>
        private async Task DeadLetterValidationExceptionAsync(ServiceBusReceivedMessage message, ServiceBusMessageActions messageActions, ValidationException vex)
        {
            Logger.LogError(vex, "Validation error for message '{Message}': {Error}", message.MessageId, vex.Message);
            await messageActions.DeadLetterMessageAsync(message, DeadLetterValidationReason, ToDeadLetterReason(vex.ToString())).ConfigureAwait(false);
        }

        /// <summary>
        /// Shortens the reason text to 4096 characters, which is the maximum allowed length for a dead letter reason.
        /// </summary>
        private static string? ToDeadLetterReason(string? reason) => reason?[..Math.Min(reason.Length, 4096)];
    }
}