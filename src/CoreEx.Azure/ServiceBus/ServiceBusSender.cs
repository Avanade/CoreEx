// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using CoreEx.Configuration;
using CoreEx.Events;
using CoreEx.Mapping.Converters;
using Microsoft.Extensions.Logging;

namespace CoreEx.Azure.ServiceBus
{
    /// <summary>
    /// Represents an Azure <see cref="ServiceBusClient"/> <see cref="IServiceBusSender"/> (see also <seealso cref="IEventSender"/>).
    /// </summary>
    /// <remarks>See <see cref="EventSendDataToServiceBusConverter"/> for details of automatic <see cref="ServiceBusMessage.SessionId"/> and <see cref="ServiceBusMessage.TimeToLive"/> allocation.
    /// <para>Note, that any <see cref="EventDataBase.Attributes"/> where the <see cref="KeyValuePair{TKey, TValue}.Key"/> starts with an underscore character ('<c>_</c>') will <i>not</i> be included in the <see cref="ServiceBusMessage.ApplicationProperties"/>.</para></remarks>
    public class ServiceBusSender : IServiceBusSender
    {
        private const string _unspecifiedQueueOrTopicName = "$default";
        private static ServiceBusSenderInvoker? _invoker;
        private readonly ServiceBusClient _client;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceBusSender"/> class.
        /// </summary>
        /// <param name="client">The underlying <see cref="ServiceBusClient"/>.</param>
        /// <param name="executionContext">The <see cref="ExecutionContext"/>.</param>
        /// <param name="settings">The <see cref="SettingsBase"/>.</param>
        /// <param name="logger">The <see cref="ILogger"/>.</param>
        /// <param name="invoker">The optional <see cref="ServiceBusSenderInvoker"/>.</param>
        /// <param name="converter">The optional <see cref="IValueConverter{TSource, TDestination}"/> to convert an <see cref="EventSendData"/> to a corresponding <see cref="ServiceBusMessage"/>.</param>
        public ServiceBusSender(ServiceBusClient client, ExecutionContext executionContext, SettingsBase settings, ILogger<ServiceBusSender> logger, ServiceBusSenderInvoker? invoker = null, IValueConverter<EventSendData, ServiceBusMessage>? converter = null)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client), "Verify dependency injection configuration and if service bus connection string for publisher was correctly defined.");
            ExecutionContext = executionContext ?? throw new ArgumentNullException(nameof(executionContext));
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            Invoker = invoker ?? (_invoker ??= new ServiceBusSenderInvoker());
            Converter = converter ?? new EventSendDataToServiceBusConverter();
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
        /// Gets the <see cref="ServiceBusSenderInvoker"/>.
        /// </summary>
        protected ServiceBusSenderInvoker Invoker { get; }

        /// <summary>
        /// Gets the <see cref="CoreEx.ExecutionContext"/>.
        /// </summary>
        protected ExecutionContext ExecutionContext { get; }

        /// <summary>
        /// Gets the <see cref="IValueConverter{TSource, TDestination}"/> to convert an <see cref="EventSendData"/> to a corresponding <see cref="ServiceBusMessage"/>.
        /// </summary>
        protected IValueConverter<EventSendData, ServiceBusMessage> Converter { get; }

        /// <summary>
        /// Gets or sets the default queue or topic name used by <see cref="SendAsync(IEnumerable{EventSendData}, CancellationToken)"/> where <see cref="EventSendData.Destination"/> is <c>null</c>.
        /// </summary>
        public string? DefaultQueueOrTopicName { get; set; }

        /// <inheritdoc/>
        public Task SendAsync(IEnumerable<EventSendData> events, CancellationToken cancellationToken = default)
        {
            if (events == null || !events.Any())
                return Task.CompletedTask;

            return Invoker.InvokeAsync(this, events, async (events, cancellationToken) =>
            {
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
                    var message = Converter.Convert(@event) ?? throw new EventSendException($"The {nameof(Converter)} must return a {nameof(ServiceBusMessage)} instance.");
                    var name = @event.Destination ?? DefaultQueueOrTopicName ?? _unspecifiedQueueOrTopicName;

                    if (queueDict.TryGetValue(name, out var queue))
                        queue.Enqueue((message, index++));
                    else
                    {
                        queue = new Queue<(ServiceBusMessage, int)>();
                        queue.Enqueue((message, index++));
                        queueDict.Add(name, queue);
                    }
                }

                Logger.LogDebug("There are {QueueTopicCount} queues/topics specified; as such there will be that many batches sent as a minimum.", queueDict.Keys.Count);

                // Get queue name by checking configuration override.
                foreach (var qitem in queueDict)
                {
                    var n = qitem.Key == _unspecifiedQueueOrTopicName ? null : qitem.Key;
                    var key = $"{GetType().Name}_QueueOrTopicName{(n is null ? "" : $"_{n}")}";
                    var qn = Settings.GetValue($"{GetType().Name}:QueueOrTopicName{(n is null ? "" : $"_{n}")}", defaultValue: n) ?? throw new EventSendException(PrependStats($"'{key}' configuration setting must have a non-null value.", totalCount, unsentEvents.Count), unsentEvents);
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
                        unsentEvents.RemoveAll(esd => sentIds.Contains(esd.Id ?? string.Empty));
                    }
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Prepend the sent stats to the message.
        /// </summary>
        private static string PrependStats(string message, int totalCount, int unsentCount) => $"{unsentCount} of the total {totalCount} events were not successfully sent. {message}";
    }
}