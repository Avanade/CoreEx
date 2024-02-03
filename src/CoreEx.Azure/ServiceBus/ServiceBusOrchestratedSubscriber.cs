// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using Azure.Messaging.ServiceBus;
using CoreEx.Abstractions;
using CoreEx.Azure.ServiceBus.Abstractions;
using CoreEx.Configuration;
using CoreEx.Events;
using CoreEx.Events.Subscribing;
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
    /// immediately deadletted with a reason of <see cref="IExtendedException.ErrorType"/> or <see cref="ErrorType.UnhandledError"/> depending on the exception <see cref="Type"/>.
    /// <para>The <see cref="ServiceBusSubscriber.UpdateEventSubscriberArgsWithServiceBusMessage(EventSubscriberArgs, ServiceBusReceivedMessage, ServiceBusMessageActions)"/> is invoked after each <see cref="EventData"/> deserialization.</para></remarks>
    public class ServiceBusOrchestratedSubscriber : EventSubscriberBase, IServiceBusSubscriber
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
        /// <param name="eventDataConverter">The optional <see cref="IEventDataConverter{TMessage}"/>.</param>
        /// <param name="eventSerializer">The optional <see cref="IEventSerializer"/>.</param>
        public ServiceBusOrchestratedSubscriber(EventSubscriberOrchestrator orchestrator, ExecutionContext executionContext, SettingsBase settings, ILogger<ServiceBusSubscriber> logger, EventSubscriberInvoker? eventSubscriberInvoker = null, ServiceBusSubscriberInvoker? serviceBusSubscriberInvoker = null, IEventDataConverter<ServiceBusReceivedMessage>? eventDataConverter = null, IEventSerializer? eventSerializer = null)
            : base(eventDataConverter ?? new ServiceBusReceivedMessageEventDataConverter(eventSerializer ?? new CoreEx.Text.Json.EventDataSerializer()), executionContext, settings, logger, eventSubscriberInvoker)
        {
            Orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
            ServiceBusSubscriberInvoker = serviceBusSubscriberInvoker ?? (ServiceBusSubscriber._invoker ??= new ServiceBusSubscriberInvoker());
            AbandonOnTransient = settings.GetValue($"{GetType().Name}__{nameof(AbandonOnTransient)}", false);
            MaxDeliveryCount = settings.GetValue<int?>($"{GetType().Name}__{nameof(MaxDeliveryCount)}");
            RetryDelay = settings.GetValue<TimeSpan?>($"{GetType().Name}__{nameof(RetryDelay)}");
            MaxRetryDelay = settings.GetValue<TimeSpan?>($"{GetType().Name}__{nameof(MaxRetryDelay)}");
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

        /// <inheritdoc/>
        public bool AbandonOnTransient { get; set; }

        /// <inheritdoc/>
        public int? MaxDeliveryCount { get; set; }

        /// <inheritdoc/>
        public TimeSpan? RetryDelay { get; set; }

        /// <inheritdoc/>
        public TimeSpan? MaxRetryDelay { get; set; }

        /// <summary>
        /// Encapsulates the execution of an <see cref="ServiceBusReceivedMessage"/> leveraging the underlying <see cref="Orchestrator"/> to receive and process the message.
        /// </summary>
        /// <param name="message">The <see cref="ServiceBusReceivedMessage"/>.</param>
        /// <param name="messageActions">The <see cref="ServiceBusMessageActions"/>.</param>
        /// <param name="args">The <see cref="EventSubscriberArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public Task ReceiveAsync(ServiceBusReceivedMessage message, ServiceBusMessageActions messageActions, EventSubscriberArgs? args = null, CancellationToken cancellationToken = default)
        {
            message.ThrowIfNull(nameof(message));
            messageActions.ThrowIfNull(nameof(messageActions));

            return ServiceBusSubscriberInvoker.InvokeAsync(this, async (_, ct) =>
            {
                // Perform any pre-processing.
                var canProceed = await OnBeforeProcessingAsync(message.MessageId, message, cancellationToken).ConfigureAwait(false);
                if (!canProceed)
                    return;

                // Get the event (without value as type unknown).
                var @event = await DeserializeEventAsync(message.MessageId, message, cancellationToken);
                if (@event is null)
                    return;

                ServiceBusSubscriber.UpdateEventSubscriberArgsWithServiceBusMessage(args ??= [], message, messageActions);

                // Match subscriber to metadata.
                var match = Orchestrator.TryMatchSubscriber(this, @event, args);
                if (!match.Matched)
                {
                    var txt = $"Subject: {(string.IsNullOrEmpty(@event.Subject) ? "<none>" : @event.Subject)}, Action: {(string.IsNullOrEmpty(@event.Action) ? "<none>" : @event.Action)}, Type: {(string.IsNullOrEmpty(@event.Type) ? "<none>" : @event.Type)}";
                    var esex = new EventSubscriberException(match.Subscriber == null ? $"No corresponding Subscriber could be matched; {txt}" : $"More than one Subscriber was matched (ambiguous); {txt}")
                        { ExceptionSource = match.Subscriber == null ? EventSubscriberExceptionSource.OrchestratorNotSubscribed : EventSubscriberExceptionSource.OrchestratorAmbiquousSubscriber };

                    await ErrorHandler.HandleErrorAsync(new ErrorHandlerArgs(@event.Id, esex, match.Subscriber == null ? Orchestrator.NotSubscribedHandling : Orchestrator.AmbiquousSubscriberHandling, Logger) { Instrumentation = Instrumentation, WorkOrchestrator = WorkStateOrchestrator }, cancellationToken);
                    return;
                }

                // Deserialize the event (again) where there is a value as value not deserialized previously.
                if (match.ValueType is not null)
                {
                    @event = await DeserializeEventAsync(message.MessageId, message, match.ValueType, cancellationToken).ConfigureAwait(false);
                    if (@event is null)
                        return;
                }

                // Execute subscriber receive with the event.
                await Orchestrator.ReceiveAsync(this, match.Subscriber!, @event, args, cancellationToken).ConfigureAwait(false);
            }, (message, messageActions), cancellationToken);
        }
    }
}