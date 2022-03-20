// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using Azure.Messaging.ServiceBus;
using CoreEx.Configuration;
using CoreEx.Events;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;

namespace CoreEx.Messaging.Azure.ServiceBus
{
    /// <summary>
    /// Represents an Azure <see cref="ServiceBusClient"/> <see cref="IEventSender"/>.
    /// </summary>
    /// <remarks>See <see cref="OnServiceBusMessage(EventSendData, ServiceBusMessage)"/> for details of automatic <see cref="ServiceBusMessage.SessionId"/> allocation.</remarks>
    public class ServiceBusSender : IEventSender
    {
        private readonly ServiceBusClient _client;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoggerEventPublisher"/> class.
        /// </summary>
        /// <param name="client">The underlying <see cref="ServiceBusClient"/>.</param>
        /// <param name="executionContext">The <see cref="ExecutionContext"/>.</param>
        /// <param name="settings">The <see cref="SettingsBase"/>.</param>
        /// <param name="logger">The <see cref="ILogger"/>.</param>
        public ServiceBusSender(ServiceBusClient client, ExecutionContext executionContext, SettingsBase settings, ILogger<ServiceBusSender> logger)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client), "Verify dependency injection configuration and if service bus connection string for publisher was correctly defined.");
            ExecutionContext = executionContext ?? throw new ArgumentNullException(nameof(executionContext));
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets the <see cref="SettingsBase"/>.
        /// </summary>
        protected SettingsBase Settings { get; }

        /// <summary>
        /// Gets the <see cref="ILogger"/>.
        /// </summary>
        protected ILogger Logger { get; }

        /// <summary>
        /// Gets the <see cref="CoreEx.ExecutionContext"/>.
        /// </summary>
        protected ExecutionContext ExecutionContext { get; }

        /// <summary>
        /// Gets or sets the default queue or topic name used by <see cref="SendAsync(EventSendData[])"/> where <see cref="EventSendData.DestinationName"/> is <c>null</c>.
        /// </summary>
        public string? DefaultQueueOrTopicName { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="EventSendData"/> property selection; where a property is selected it will be sent as one of the <see cref="ServiceBusMessage.ApplicationProperties"/>.
        /// </summary>
        /// <remarks>Defaults to <see cref="EventDataProperty.All"/>.</remarks>
        public EventDataProperty PropertySelection { get; set; } = EventDataProperty.All;

        /// <summary>
        /// Gets or sets the <see cref="EventDataBase.Attributes"/> name for the <see cref="ServiceBusMessage.SessionId"/>.
        /// </summary>
        /// <remarks>Defaults to '<c>_SessionId</c>'.</remarks>
        public string SessionIdAttributeName { get; set; } = $"_{nameof(ServiceBusMessage.SessionId)}";

        /// <summary>
        /// Indicates whether to use the <see cref="EventDataBase.PartitionKey"/> as the <see cref="ServiceBusMessage.SessionId"/>.
        /// </summary>
        public bool UsePartitionKeyAsSessionId { get; set; } = true;

        /// <inheritdoc/>
        public async Task SendAsync(params EventSendData[] events)
        {
            if (events == null || events.Length == 0)
                return;

            // Why this logic: https://github.com/Azure/azure-sdk-for-net/tree/Azure.Messaging.ServiceBus_7.1.0/sdk/servicebus/Azure.Messaging.ServiceBus/#send-and-receive-a-batch-of-messages
            var queueDict = new Dictionary<string, Queue<(ServiceBusMessage Message, int Index)>>();
            var index = 0;
            foreach (var @event in events)
            {
                var msg = new ServiceBusMessage(@event.Data)
                {
                    MessageId = @event.Id,
                    ContentType = MediaTypeNames.Application.Json,
                    CorrelationId = @event.CorrelationId ?? ExecutionContext.CorrelationId,
                    Subject = @event.Subject
                };

                if (PropertySelection.HasFlag(EventDataProperty.Action))
                    msg.ApplicationProperties.Add(nameof(EventData.Action), @event.Action);

                if (PropertySelection.HasFlag(EventDataProperty.Source))
                    msg.ApplicationProperties.Add(nameof(EventData.Source), @event.Source);

                if (PropertySelection.HasFlag(EventDataProperty.Type))
                    msg.ApplicationProperties.Add(nameof(EventData.Source), @event.Type);

                if (PropertySelection.HasFlag(EventDataProperty.TenantId))
                    msg.ApplicationProperties.Add(nameof(EventData.TenantId), @event.TenantId);

                if (PropertySelection.HasFlag(EventDataProperty.PartitionKey))
                    msg.ApplicationProperties.Add(nameof(EventData.PartitionKey), @event.PartitionKey);

                if (PropertySelection.HasFlag(EventDataProperty.ETag))
                    msg.ApplicationProperties.Add(nameof(EventData.ETag), @event.ETag);

                if (PropertySelection.HasFlag(EventDataProperty.Attributes))
                {
                    foreach (var attribute in @event.Attributes.Where(x => !string.IsNullOrEmpty(x.Key) && x.Key != SessionIdAttributeName))
                    {
                        msg.ApplicationProperties.Add(attribute.Key, attribute.Value);
                    }
                }

                OnServiceBusMessage(@event, msg);

                var name = @event.DestinationName ?? DefaultQueueOrTopicName ?? throw new InvalidOperationException($"{nameof(DefaultQueueOrTopicName)} must have a non null value.");
                if (queueDict.TryGetValue(name, out var queue))
                    queue.Enqueue((msg, index++));
                else
                {
                    queue = new Queue<(ServiceBusMessage, int)>();
                    queue.Enqueue((msg, index++));
                    queueDict.Add(name, queue);
                }
            }

            // Get queue name by checking configuration override.
            foreach (var qitem in queueDict)
            {
                var qn = Settings.GetValue($"Publisher_ServiceBusName_{qitem.Key}", defaultValue: qitem.Key);
                var queue = qitem.Value;

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
                        throw new InvalidOperationException("ServiceBusMessage is too large and cannot be sent.");

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
                        throw new InvalidOperationException($"ServiceBusMessage cannot be sent: {ex.Message}", ex);
                    }

                    // Begin next batch; continue ^ where any left.
                }
            }
        }

        /// <summary>
        /// Invoked to modify the <see cref="ServiceBusMessage"/> configuration prior to send.
        /// </summary>
        /// <param name="event">The <see cref="EventSendData"/>.</param>
        /// <param name="message">The <see cref="ServiceBusMessage"/>.</param>
        /// <remarks>By default the <see cref="SessionIdAttributeName"/> will be used to update the <see cref="ServiceBusMessage.SessionId"/> from the <see cref="EventDataBase.Attributes"/>, followd by the
        /// <see cref="UsePartitionKeyAsSessionId"/> option, until not <c>null</c>; otherwise, will be left as <c>null</c>./</remarks>
        protected virtual void OnServiceBusMessage(EventSendData @event, ServiceBusMessage message)
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