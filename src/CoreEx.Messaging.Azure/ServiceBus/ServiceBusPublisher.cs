// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using Azure.Messaging.ServiceBus;
using CoreEx.Configuration;
using CoreEx.Events;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Mime;
using System.Threading.Tasks;

namespace CoreEx.Messaging.Azure.ServiceBus
{
    /// <summary>
    /// Represents an Azure <see cref="ServiceBusClient"/> <see cref="IEventPublisher"/>.
    /// </summary>
    /// <remarks>See <see cref="OnServiceBusMessage(EventData, ServiceBusMessage)"/> for details of automatic <see cref="ServiceBusMessage.SessionId"/> allocation.</remarks>
    public class ServiceBusPublisher : IEventPublisher
    {
        private readonly ServiceBusClient _client;
        private readonly EventDataFormatter _eventDataFormatter;
        private readonly IEventSerializer _eventSerializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoggerEventPublisher"/> class.
        /// </summary>
        /// <param name="client">The underlying <see cref="ServiceBusClient"/>.</param>
        /// <param name="eventDataFormatter">The <see cref="EventDataFormatter"/>.</param>
        /// <param name="eventSerializer">The <see cref="IEventSerializer"/>.</param>
        /// <param name="executionContext">The <see cref="ExecutionContext"/>.</param>
        /// <param name="settings">The <see cref="SettingsBase"/>.</param>
        /// <param name="logger">The <see cref="ILogger"/>.</param>
        public ServiceBusPublisher(ServiceBusClient client, EventDataFormatter? eventDataFormatter, IEventSerializer eventSerializer, ExecutionContext executionContext, SettingsBase settings, ILogger<ServiceBusPublisher> logger)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client), "Verify dependency injection configuration and if service bus connection string for publisher was correctly defined.");
            _eventDataFormatter = eventDataFormatter ?? new EventDataFormatter();
            _eventSerializer = eventSerializer ?? throw new ArgumentNullException(nameof(eventSerializer));
            ExecutionContext = executionContext ?? throw new ArgumentNullException(nameof(executionContext));
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets the <see cref="SettingsBase"/>.
        /// </summary>
        protected SettingsBase Settings { get; }

        /// <summary>
        /// Gets the <see cref="CoreEx.ExecutionContext"/>.
        /// </summary>
        protected ExecutionContext ExecutionContext { get; }

        /// <summary>
        /// Gets the <see cref="ILogger"/>.
        /// </summary>
        protected ILogger Logger { get; }

        /// <summary>
        /// Gets or sets the default queue or topic name used by <see cref="SendAsync(EventData[])"/>.
        /// </summary>
        public string? DefaultQueueOrTopicName { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="EventDataBase.Attributes"/> name for the <see cref="ServiceBusMessage.SessionId"/>.
        /// </summary>
        /// <remarks>Defaults to '<c>SessionId</c>'.</remarks>
        public string SessionIdAttributeName { get; set; } = nameof(ServiceBusMessage.SessionId);

        /// <summary>
        /// Indicates whether to use the <see cref="EventDataBase.PartitionKey"/> as the <see cref="ServiceBusMessage.SessionId"/>.
        /// </summary>
        public bool UsePartitionKeyAsSessionId { get; set; } = true;

        /// <inheritdoc/>
        public async Task SendAsync(params EventData[] events)
        {
            if (string.IsNullOrEmpty(DefaultQueueOrTopicName))
                throw new InvalidOperationException($"A queue or topic name is required for Azure Service Bus, as such the {nameof(DefaultQueueOrTopicName)} is required where not explicitly provided.");

            await SendInternalAsync(DefaultQueueOrTopicName, events).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task SendAsync(string name, params EventData[] events)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            await SendInternalAsync(name, events).ConfigureAwait(false);
        }

        /// <summary>
        /// Sends the messages to Azure Service Bus in a batch.
        /// </summary>
        private async Task SendInternalAsync(string queueOrTopicName, EventData[] events)
        {
            if (events == null || events.Length == 0)
                return;

            // Why this logic: https://github.com/Azure/azure-sdk-for-net/tree/Azure.Messaging.ServiceBus_7.1.0/sdk/servicebus/Azure.Messaging.ServiceBus/#send-and-receive-a-batch-of-messages
            var queue = new Queue<(ServiceBusMessage Message, int Index)>();
            var index = 0;
            foreach (var @event in events)
            {
                var e = @event.Copy();
                _eventDataFormatter.Format(e);
                var bd = await _eventSerializer.SerializeAsync(e).ConfigureAwait(false);

                var msg = new ServiceBusMessage(bd)
                {
                    MessageId = e.Id,
                    ContentType = MediaTypeNames.Application.Json,
                    CorrelationId = e.CorrelationId ?? ExecutionContext.CorrelationId,
                    Subject = e.Subject
                };

                OnServiceBusMessage(e, msg);

                queue.Enqueue((msg, index++));
            }

            // Get queue name by checking config override.
            var qn = Settings.GetValue<string>($"Publisher_ServiceBusName_{queueOrTopicName}", defaultValue: queueOrTopicName);

            // Send in batches.
            await using var sender = _client.CreateSender(qn);
            while (queue.Count > 0)
            {
                using var batch = await sender.CreateMessageBatchAsync().ConfigureAwait(false);

                // Add the first message to the batch.
                var firstMsg = queue.Peek();
                if (batch.TryAddMessage(firstMsg.Message))
                    queue.Dequeue();
                else
                {
                    Logger.LogError("Service Bus message cannot be published: Message is too large.");
                    throw new EventPublisherException(new EventPublisherDataError[] { new EventPublisherDataError { Index = firstMsg.Index, Message = "Message is too large and cannot be published." } });
                }

                // Keep adding until done or max size reached for batch.
                while (queue.Count > 0 && batch.TryAddMessage(queue.Peek().Message))
                {
                    queue.Dequeue();
                }

                try
                {
                    await sender.SendMessagesAsync(batch).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    var list = new List<EventPublisherDataError>();
                    for (int i = firstMsg.Index; i < events.Length; i++)
                    {
                        list.Add(new EventPublisherDataError { Index = firstMsg.Index, Message = $"Message cannot be published: {ex.Message}" });
                    }

                    Logger.LogError(ex, $"Service Bus message(s) cannot be published: {ex.Message}");
                    throw new EventPublisherException(list);
                }

                // Begin next batch; continue ^ where any left.
            }
        }

        /// <summary>
        /// Invoked to modify the <see cref="ServiceBusMessage"/> configuration prior to send.
        /// </summary>
        /// <param name="event">The <see cref="EventData"/>.</param>
        /// <param name="message">The <see cref="ServiceBusMessage"/>.</param>
        /// <remarks>By default the <see cref="SessionIdAttributeName"/> will be used to update the <see cref="ServiceBusMessage.SessionId"/> from the <see cref="EventDataBase.Attributes"/>, followd by the
        /// <see cref="UsePartitionKeyAsSessionId"/> option, until not <c>null</c>; otherwise, will be left as <c>null</c>./</remarks>
        protected virtual void OnServiceBusMessage(EventData @event, ServiceBusMessage message)
        {
            if (message.SessionId != null)
                return;

            if (@event.Attributes != null && @event.Attributes.Count > 0)
            {
                if (@event.Attributes.TryGetValue(SessionIdAttributeName, out var sessionId))
                {
                    message.SessionId = sessionId;
                    return;
                }
            }

            message.SessionId = UsePartitionKeyAsSessionId ? @event.PartitionKey : null;
        }
    }
}