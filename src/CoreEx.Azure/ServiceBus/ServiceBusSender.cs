// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using Azure.Messaging.ServiceBus;
using CoreEx.Configuration;
using CoreEx.Events;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Azure.ServiceBus
{
    /// <summary>
    /// Represents an Azure <see cref="ServiceBusClient"/> <see cref="IEventSender"/>.
    /// </summary>
    /// <remarks>See <see cref="OnServiceBusMessage(EventSendData, ServiceBusMessage)"/> for details of automatic <see cref="ServiceBusMessage.SessionId"/> allocation.</remarks>
    public class ServiceBusSender : IEventSender
    {
        private readonly ServiceBusClient _client;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceBusSender"/> class.
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
        /// Gets or sets the default queue or topic name used by <see cref="SendAsync(IEnumerable{EventSendData}, CancellationToken)"/> where <see cref="EventSendData.Destination"/> is <c>null</c>.
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
        public async Task SendAsync(IEnumerable<EventSendData> events, CancellationToken cancellationToken = default)
        {
            if (events == null || !events.Any())
                return;

            var totalCount = events.Count();
            Logger.LogDebug("{TotalCount} events in total are to be sent.", totalCount);

            if (events.Count() != events.Select(x => x.Id).Distinct().Count())
                throw new EventSendException(PrependStats($"All events must have a unique identifier ({nameof(EventSendData)}.{nameof(EventSendData.Id)}).", totalCount, totalCount), events);

            // Sets up the list of unsent events.
            var unsentEvents = new List<EventSendData>(events);

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

                if (@event.Action != null && PropertySelection.HasFlag(EventDataProperty.Action))
                    msg.ApplicationProperties.Add(nameof(EventData.Action), @event.Action);

                if (@event.Source != null && PropertySelection.HasFlag(EventDataProperty.Source))
                    msg.ApplicationProperties.Add(nameof(EventData.Source), @event.Source.ToString());

                if (@event.Type != null && PropertySelection.HasFlag(EventDataProperty.Type))
                    msg.ApplicationProperties.Add(nameof(EventData.Type), @event.Type);

                if (@event.TenantId != null && PropertySelection.HasFlag(EventDataProperty.TenantId))
                    msg.ApplicationProperties.Add(nameof(EventData.TenantId), @event.TenantId);

                if (@event.PartitionKey != null && PropertySelection.HasFlag(EventDataProperty.PartitionKey))
                    msg.ApplicationProperties.Add(nameof(EventData.PartitionKey), @event.PartitionKey);

                if (@event.ETag != null && PropertySelection.HasFlag(EventDataProperty.ETag))
                    msg.ApplicationProperties.Add(nameof(EventData.ETag), @event.ETag);

                if (@event.Attributes != null && @event.Attributes.Count > 0 && PropertySelection.HasFlag(EventDataProperty.Attributes))
                {
                    foreach (var attribute in @event.Attributes.Where(x => !string.IsNullOrEmpty(x.Key) && x.Key != SessionIdAttributeName))
                    {
                        msg.ApplicationProperties.Add(attribute.Key, attribute.Value);
                    }
                }

                OnServiceBusMessage(@event, msg);

                var name = @event.Destination ?? DefaultQueueOrTopicName ?? throw new EventSendException(PrependStats($"{nameof(DefaultQueueOrTopicName)} must have a non null value.", totalCount, unsentEvents.Count), unsentEvents);
                if (queueDict.TryGetValue(name, out var queue))
                    queue.Enqueue((msg, index++));
                else
                {
                    queue = new Queue<(ServiceBusMessage, int)>();
                    queue.Enqueue((msg, index++));
                    queueDict.Add(name, queue);
                }
            }

            Logger.LogDebug("There are {QueueTopicCount} queues/topics specified; as such there will be that many batches sent as a minimum.", queueDict.Keys.Count);

            // Get queue name by checking configuration override.
            foreach (var qitem in queueDict)
            {
                var qn = Settings.GetValue($"Publisher_ServiceBusName_{qitem.Key}", defaultValue: qitem.Key);
                var queue = qitem.Value;
                var sentIds = new List<string>();

                // Send in batches.
                await using var sender = _client.CreateSender(qn);
                while (queue.Count > 0)
                {
                    sentIds.Clear();
                    using var batch = await sender.CreateMessageBatchAsync(cancellationToken).ConfigureAwait(false);

                    // Add the first message to the batch.
                    var firstMsg = queue.Peek();
                    if (batch.TryAddMessage(firstMsg.Message))
                    {
                        sentIds.Add(firstMsg.Message.MessageId);
                        queue.Dequeue();
                    }
                    else
                        throw new EventSendException(PrependStats("ServiceBusMessage is too large and cannot be sent.", totalCount, unsentEvents.Count), unsentEvents);

                    // Keep adding until done or max size reached for batch.
                    while (queue.Count > 0 && batch.TryAddMessage(queue.Peek().Message))
                    {
                        sentIds.Add(queue.Peek().Message.MessageId);
                        queue.Dequeue();
                    }

                    try
                    {
                        Logger.LogInformation($"Sending {batch.Count} messages to {qn}.");
                        await sender.SendMessagesAsync(batch, cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogDebug("{UnsentCount} of the total {TotalCount} events were not successfully sent.", unsentEvents.Count, totalCount);
                        throw new EventSendException(PrependStats($"ServiceBusMessage cannot be sent: {ex.Message}", totalCount, unsentEvents.Count), ex, unsentEvents);
                    }

                    // Begin next batch after confirming sent events; continue ^ where any left.
                    unsentEvents.RemoveAll(esd => sentIds.Contains(esd.Id));
                }
            }
        }

        /// <summary>
        /// Prepend the sent stats to the message.
        /// </summary>
        private static string PrependStats(string message, int totalCount, int unsentCount) => $"{unsentCount} of the total {totalCount} events were not successfully sent. {message}";

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