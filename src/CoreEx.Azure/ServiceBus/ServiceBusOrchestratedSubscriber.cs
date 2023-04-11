// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using Azure.Messaging.ServiceBus;
using CoreEx.Abstractions;
using CoreEx.Configuration;
using CoreEx.Events;
using CoreEx.Events.Subscribing;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Azure.ServiceBus
{
    /// <summary>
    /// Provides the <see cref="EventSubscriberOrchestrator"/>-managed <see cref="ServiceBusReceivedMessage"/> subscribe (receive) execution encapsulation to run the underlying function logic in a consistent manner.
    /// </summary>
    /// <remarks>The <c>ReceiveAsync</c> enables the standardized logic. The correlation identifier is set using the <see cref="ServiceBusReceivedMessage.CorrelationId"/>; where <c>null</c> a <see cref="Guid.NewGuid"/> will be used as the 
    /// default. A <see cref="ILogger.BeginScope{TState}(TState)"/> with the <see cref="ExecutionContext.CorrelationId"/> and <see cref="ServiceBusReceivedMessage.MessageId"/> is performed to wrap the logic logging with the correlation and
    /// message identifiers. Where the unhandled <see cref="Exception"/> is <see cref="IExtendedException.IsTransient"/> this will bubble out for the Azure Function runtime/fabric to retry and automatically deadletter; otherwise, it will be
    /// immediately deadletted with a reason of <see cref="IExtendedException.ErrorType"/> or <see cref="ErrorType.UnhandledError"/> depending on the exception <see cref="Type"/>.</remarks>
    public class ServiceBusOrchestratedSubscriber : EventSubscriberBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceBusOrchestratedSubscriber"/> class.
        /// </summary>
        /// <param name="orchestrator">The <see cref="EventSubscriberOrchestrator"/>.</param>
        /// <param name="executionContext">The <see cref="ExecutionContext"/>.</param>
        /// <param name="settings">The <see cref="SettingsBase"/>.</param>
        /// <param name="logger">The <see cref="ILogger"/>.</param>
        /// <param name="eventSubscriberInvoker">The optional <see cref="EventSubscriberInvoker"/>.</param>
        /// <param name="serviceBusSubscriberInvoker">The optional <see cref="ServiceBus.ServiceBusSubscriberInvoker"/>.</param>
        /// <param name="eventDataConverter">The optional <see cref="IEventDataConverter{ServiceBusReceivedMessage}"/>.</param>
        /// <param name="eventSerializer">The optional <see cref="IEventSerializer"/>.</param>
        public ServiceBusOrchestratedSubscriber(EventSubscriberOrchestrator orchestrator, ExecutionContext executionContext, SettingsBase settings, ILogger<ServiceBusSubscriber> logger, EventSubscriberInvoker? eventSubscriberInvoker = null, ServiceBusSubscriberInvoker? serviceBusSubscriberInvoker = null, IEventDataConverter<ServiceBusReceivedMessage>? eventDataConverter = null, IEventSerializer? eventSerializer = null)
            : base(eventDataConverter ?? new ServiceBusReceivedMessageEventDataConverter(eventSerializer ?? new CoreEx.Text.Json.EventDataSerializer()), executionContext, settings, logger, eventSubscriberInvoker)
        {
            Orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
            ServiceBusSubscriberInvoker = serviceBusSubscriberInvoker ?? (ServiceBusSubscriber._invoker ??= new ServiceBusSubscriberInvoker());
        }

        /// <summary>
        /// Gets the <see cref="EventSubscriberOrchestrator"/>.
        /// </summary>
        protected EventSubscriberOrchestrator Orchestrator { get; }

        /// <summary>
        /// Gets the <see cref="ServiceBus.ServiceBusSubscriberInvoker"/>.
        /// </summary>
        protected ServiceBusSubscriberInvoker ServiceBusSubscriberInvoker { get; }

        /// <summary>
        /// Gets the <see cref="IEventDataConverter{ServiceBusReceivedMessage}"/>.
        /// </summary>
        protected new IEventDataConverter<ServiceBusReceivedMessage> EventDataConverter => (IEventDataConverter<ServiceBusReceivedMessage>)base.EventDataConverter;

        /// <summary>
        /// Encapsulates the execution of an <see cref="ServiceBusReceivedMessage"/> leveraging the underlying <see cref="Orchestrator"/> to receive and process the message.
        /// </summary>
        /// <param name="message">The <see cref="ServiceBusReceivedMessage"/>.</param>
        /// <param name="messageActions">The <see cref="ServiceBusMessageActions"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public Task ReceiveAsync(ServiceBusReceivedMessage message, ServiceBusMessageActions messageActions, CancellationToken cancellationToken = default)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            if (messageActions == null)
                throw new ArgumentNullException(nameof(messageActions));

            return ServiceBusSubscriberInvoker.InvokeAsync(this, async ct =>
            {
                // Get the event (without value as type unknown).
                var @event = await DeserializeEventAsync(message, cancellationToken);
                if (@event is null)
                    return;

                // Match subscriber to metadata.
                if (!Orchestrator.TryMatchSubscriber(this, @event, out var subscriber, out var valueType))
                    return;

                // Deserialize the event (again) where there is a value as value not deserialized previously.
                if (valueType is not null)
                {
                    @event = await DeserializeEventAsync(message, valueType, cancellationToken).ConfigureAwait(false);
                    if (@event is null)
                        return;
                }

                // Execute subscriber receive with the event.
                await Orchestrator.ReceiveAsync(this, subscriber!, @event, cancellationToken).ConfigureAwait(false);
            }, (message, messageActions), cancellationToken);
        }
    }
}