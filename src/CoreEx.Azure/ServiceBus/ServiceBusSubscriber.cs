// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using Azure.Messaging.ServiceBus;
using CoreEx.Abstractions;
using CoreEx.Configuration;
using CoreEx.Events;
using CoreEx.Events.Subscribing;
using CoreEx.Validation;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Azure.ServiceBus
{
    /// <summary>
    /// Provides the standard <see cref="ServiceBusReceivedMessage"/> subscribe (receive) execution encapsulation to run the underlying function logic in a consistent manner.
    /// </summary>
    /// <remarks>The <c>ReceiveAsync</c> enables the standardized logic. The correlation identifier is set using the <see cref="ServiceBusReceivedMessage.CorrelationId"/>; where <c>null</c> a <see cref="Guid.NewGuid"/> will be used as the 
    /// default. A <see cref="ILogger.BeginScope{TState}(TState)"/> with the <see cref="ExecutionContext.CorrelationId"/> and <see cref="ServiceBusReceivedMessage.MessageId"/> is performed to wrap the logic logging with the correlation and
    /// message identifiers. Where the unhandled <see cref="Exception"/> is <see cref="IExtendedException.IsTransient"/> this will bubble out for the Azure Function runtime/fabric to retry and automatically deadletter; otherwise, it will be
    /// immediately deadletted with a reason of <see cref="IExtendedException.ErrorType"/> or <see cref="ErrorType.UnhandledError"/> depending on the exception <see cref="Type"/>.
    /// <para>The <see cref="UpdateEventDataWithServiceBusMessage(EventData, ServiceBusReceivedMessage, ServiceBusMessageActions)"/> is invoked after each <see cref="EventData"/> deserialization.</para></remarks>
    public class ServiceBusSubscriber : EventSubscriberBase, IServiceBusSubscriber
    {
        /// <summary>
        /// Gets the <see cref="EventDataBase.Internal"/> name to access the <see cref="ServiceBusMessage"/>.
        /// </summary>
        public const string ServiceBusReceivedMessageName = "_ServiceBusReceivedMessage";

        /// <summary>
        /// Gets the <see cref="EventDataBase.Internal"/> name to access the <see cref="ServiceBusMessageActions"/>.
        /// </summary>
        public const string ServiceBusMessageActionsName = "ServiceBusMessageActions";

        internal static ServiceBusSubscriberInvoker? _invoker;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceBusSubscriber"/> class.
        /// </summary>
        /// <param name="executionContext">The <see cref="ExecutionContext"/>.</param>
        /// <param name="settings">The <see cref="SettingsBase"/>.</param>
        /// <param name="logger">The <see cref="ILogger"/>.</param>
        /// <param name="eventSubscriberInvoker">The optional <see cref="EventSubscriberInvoker"/>.</param>
        /// <param name="serviceBusSubscriberInvoker">The optional <see cref="ServiceBus.ServiceBusSubscriberInvoker"/>.</param>
        /// <param name="eventDataConverter">The optional <see cref="IEventDataConverter{ServiceBusReceivedMessage}"/>.</param>
        /// <param name="eventSerializer">The optional <see cref="IEventSerializer"/>.</param>
        public ServiceBusSubscriber(ExecutionContext executionContext, SettingsBase settings, ILogger<ServiceBusSubscriber> logger, EventSubscriberInvoker? eventSubscriberInvoker = null, ServiceBusSubscriberInvoker? serviceBusSubscriberInvoker = null, IEventDataConverter<ServiceBusReceivedMessage>? eventDataConverter = null, IEventSerializer? eventSerializer = null)
            : base(eventDataConverter ?? new ServiceBusReceivedMessageEventDataConverter(eventSerializer ?? new CoreEx.Text.Json.EventDataSerializer()), executionContext, settings, logger, eventSubscriberInvoker)
        {
            ServiceBusSubscriberInvoker = serviceBusSubscriberInvoker ?? (_invoker ??= new ServiceBusSubscriberInvoker());
            AbandonOnTransient = settings.GetValue($"{GetType().Name}__{nameof(AbandonOnTransient)}", false);
            MaxDeliveryCount = settings.GetValue<int?>($"{GetType().Name}__{nameof(MaxDeliveryCount)}");
            RetryDelay = settings.GetValue<TimeSpan?>($"{GetType().Name}__{nameof(RetryDelay)}");
            MaxRetryDelay = settings.GetValue<TimeSpan?>($"{GetType().Name}__{nameof(MaxRetryDelay)}");
        }

        /// <summary>
        /// Gets the <see cref="ServiceBus.ServiceBusSubscriberInvoker"/>.
        /// </summary>
        protected ServiceBusSubscriberInvoker ServiceBusSubscriberInvoker { get; }

        /// <summary>
        /// Gets the <see cref="IEventDataConverter{ServiceBusReceivedMessage}"/>.
        /// </summary>
        protected new IEventDataConverter<ServiceBusReceivedMessage> EventDataConverter => (IEventDataConverter<ServiceBusReceivedMessage>)base.EventDataConverter;

        /// <inheritdoc/>
        public bool AbandonOnTransient { get; set; }

        /// <inheritdoc/>
        public int? MaxDeliveryCount { get; set; }

        /// <inheritdoc/>
        public TimeSpan? RetryDelay { get; set; }

        /// <inheritdoc/>
        public TimeSpan? MaxRetryDelay { get; set; }

        /// <summary>
        /// Encapsulates the execution of an <see cref="ServiceBusReceivedMessage"/> <paramref name="function"/> converting the <paramref name="message"/> into a corresponding <see cref="EventData"/> (with no value) for processing.
        /// </summary>
        /// <param name="message">The <see cref="ServiceBusReceivedMessage"/>.</param>
        /// <param name="messageActions">The <see cref="ServiceBusMessageActions"/>.</param>
        /// <param name="function">The function logic to invoke.</param>
        /// <param name="afterReceive">A function that enables the <paramref name="message"/> <see cref="EventData"/> to be processed directly after the message is received and deserialized.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public Task ReceiveAsync(ServiceBusReceivedMessage message, ServiceBusMessageActions messageActions, Func<EventData, Task> function, Func<EventData, Task>? afterReceive = null, CancellationToken cancellationToken = default)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            if (messageActions == null)
                throw new ArgumentNullException(nameof(messageActions));

            if (function == null)
                throw new ArgumentNullException(nameof(function));

            return ServiceBusSubscriberInvoker.InvokeAsync(this, async ct =>
            {
                // Deserialize the JSON into the selected type.
                var @event = await DeserializeEventAsync(message, cancellationToken).ConfigureAwait(false);
                if (@event is null)
                    return;

                UpdateEventDataWithServiceBusMessage(@event, message, messageActions);
                if (afterReceive != null)
                    await afterReceive(@event!).ConfigureAwait(false);

                // Invoke the actual function logic.
                await function(@event!).ConfigureAwait(false);

                Logger.LogDebug("{Type} executed successfully - Service Bus message '{Message}'.", GetType().Name, message.MessageId);
            }, (message, messageActions), cancellationToken);
        }

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
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public Task ReceiveAsync<TValue>(ServiceBusReceivedMessage message, ServiceBusMessageActions messageActions, Func<EventData<TValue>, Task> function, bool valueIsRequired = true, IValidator<TValue>? validator = null,
            Func<EventData<TValue>, Task>? afterReceive = null, CancellationToken cancellationToken = default)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            if (messageActions == null)
                throw new ArgumentNullException(nameof(messageActions));

            if (function == null)
                throw new ArgumentNullException(nameof(function));

            return ServiceBusSubscriberInvoker.InvokeAsync(this, async ct =>
            {
                // Deserialize the JSON into the selected type.
                var @event = await DeserializeEventAsync(message, valueIsRequired, validator, cancellationToken).ConfigureAwait(false);
                if (@event is null)
                    return;

                UpdateEventDataWithServiceBusMessage(@event, message, messageActions);
                if (afterReceive != null)
                    await afterReceive(@event!).ConfigureAwait(false);

                // Invoke the actual function logic.
                await function(@event!).ConfigureAwait(false);

                Logger.LogInformation("{Type} executed successfully - Service Bus message '{Message}'.", GetType().Name, message.MessageId);
            }, (message, messageActions), cancellationToken);
        }

        /// <summary>
        /// Updates the <paramref name="event"/> <see cref="EventDataBase.Internal"/> dictionary with the corresponding <paramref name="message"/> and <paramref name="messageActions"/>.
        /// </summary>
        /// <param name="event">The <see cref="EventData"/>.</param>
        /// <param name="message">The <see cref="ServiceBusReceivedMessage"/>.</param>
        /// <param name="messageActions">The <see cref="ServiceBusMessageActions"/>.</param>
        /// <remarks>This will allow access to these valyes from within the event processing logic and is intended for advanced scenarios only; care should be taken to not perform an action that would result in the underlying host to fail
        /// unexpectantly.</remarks>
        public static void UpdateEventDataWithServiceBusMessage(EventData @event, ServiceBusReceivedMessage message, ServiceBusMessageActions messageActions)
        {
            (@event ?? throw new ArgumentNullException(nameof(@event))).Internal.Add(ServiceBusReceivedMessageName, message ?? throw new ArgumentNullException(nameof(message)));
            @event.Internal.Add(ServiceBusMessageActionsName, messageActions ?? throw new ArgumentNullException(nameof(messageActions)));
        }
    }
}