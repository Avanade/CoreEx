// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CoreEx.Configuration;
using CoreEx.Events;
using CoreEx.Mapping.Converters;
using Microsoft.Extensions.Logging;
using SolaceSystems.Solclient.Messaging;

namespace CoreEx.Solace.PubSub
{
    /// <summary>
    /// Represents a PubSub <see cref="ISession"/> <see cref="IPubSubSender"/> (see also <seealso cref="IEventSender"/>).
    /// </summary>
    /// <remarks>See <see cref="EventSendDataToPubSubConverter"/> for details</remarks>
    public class PubSubSender : IPubSubSender
    {
        private const string _unspecifiedQueueOrTopicName = "$default";
        private const int PUBSUB_MAX_BATCH_SIZE = 50;
        private static PubSubSenderInvoker? _invoker;

        /// <summary>
        /// Initializes a new instance of the <see cref="PubSubSender"/> class.
        /// </summary>
        /// <param name="solaceContext">The Solace <see cref="IContext"/>.</param>
        /// <param name="sessionProperties">The Solace <see cref="SessionProperties"/>.</param>
        /// <param name="settings">The <see cref="SettingsBase"/>.</param>
        /// <param name="logger">The <see cref="ILogger"/>.</param>
        /// <param name="invoker">The optional <see cref="PubSubSenderInvoker"/>.</param>
        /// <param name="converter">The optional <see cref="IValueConverter{TSource, TDestination}"/> to convert an <see cref="EventSendData"/> to a corresponding <see cref="IMessage"/>.</param>
        public PubSubSender(IContext solaceContext, SessionProperties sessionProperties, SettingsBase settings, ILogger<PubSubSender> logger, PubSubSenderInvoker? invoker = null, IValueConverter<EventSendData, IMessage>? converter = null)
        {
            SolaceContext = solaceContext ?? throw new ArgumentNullException(nameof(solaceContext), "Verify PubSub connection properties have been correctly defined.");
            SessionProperties = sessionProperties ?? throw new ArgumentNullException(nameof(sessionProperties), "Verify PubSub connection properties have been correctly defined.");
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            Invoker = invoker ?? (_invoker ??= new PubSubSenderInvoker());
            Converter = converter ?? new EventSendDataToPubSubConverter();
            DefaultQueueOrTopicName = Settings.GetValue($"{GetType().Name}:QueueOrTopicName", defaultValue: _unspecifiedQueueOrTopicName);
        }

        /// <summary>
        /// Gets the <see cref="IContext"/>.
        /// </summary>
        protected IContext SolaceContext { get; set; }

        /// <summary>
        /// Gets the <see cref="SessionProperties"/>.
        /// </summary>
        protected SessionProperties SessionProperties { get; }

        /// <summary>
        /// Gets the <see cref="SettingsBase"/>.
        /// </summary>
        protected SettingsBase Settings { get; }

        /// <summary>
        /// Gets the <see cref="ILogger"/>.
        /// </summary>
        protected ILogger Logger { get; }

        /// <summary>
        /// Gets the <see cref="PubSubSenderInvoker"/>.
        /// </summary>
        protected PubSubSenderInvoker Invoker { get; }

        /// <summary>
        /// Gets the <see cref="IValueConverter{TSource, TDestination}"/> to convert an <see cref="EventSendData"/> to a corresponding <see cref="IMessage"/>.
        /// </summary>
        protected IValueConverter<EventSendData, IMessage> Converter { get; }

        /// <summary>
        /// Gets or sets the default queue or topic name used by <see cref="SendAsync(IEnumerable{EventSendData}, CancellationToken)"/> where <see cref="EventSendData.Destination"/> is <c>null</c>.
        /// </summary>
        public string? DefaultQueueOrTopicName { get; set; }

        /// <inheritdoc/>
        public Task SendAsync(IEnumerable<EventSendData> events, CancellationToken cancellationToken = default)
        {
            if (events == null || !events.Any())
                return Task.CompletedTask;

            Invoker.Invoke(this, events, (events) =>
            {
                var totalCount = events.Count();
                Logger.LogDebug("{TotalCount} events in total are to be sent.", totalCount);

                if (totalCount == 0)
                    return;

                if (totalCount != events.Select(x => x.Id).Distinct().Count())
                    throw new EventSendException(PrependStats($"All events must have a unique identifier ({nameof(EventSendData)}.{nameof(EventSendData.Id)}).", totalCount, totalCount), events);

                // Sets up the list of unsent events.
                var unsentEvents = new List<EventSendData>(events);

                var queueDict = new Dictionary<string, Queue<(IMessage Message, int Index)>>();
                var index = 0;
                foreach (var @event in events)
                {
                    //Convert message
                    var message = Converter.Convert(@event) ?? throw new EventSendException($"The {nameof(Converter)} must return a {nameof(IMessage)} instance.");

                    var name = @event.Destination ?? DefaultQueueOrTopicName ?? throw new InvalidOperationException($"The {nameof(DefaultQueueOrTopicName)} must be specified where the {nameof(EventSendData)}.{nameof(EventSendData.Destination)} is null.");
                    message.Destination = ContextFactory.Instance.CreateTopic(name);

                    //Enqueue event message
                    if (queueDict.TryGetValue(name, out var queue))
                        queue.Enqueue((message, index++));
                    else
                    {
                        queue = new Queue<(IMessage, int)>();
                        queue.Enqueue((message, index++));
                        queueDict.Add(name, queue);
                    }
                }

                Logger.LogDebug("There are {QueueTopicCount} queues/topics specified; as such there will be that many batches sent as a minimum.", queueDict.Keys.Count);

                // Establish session and dispose when done.
                using var session = EstablishSessionToPubSubBroker();

                // Get queue name by checking configuration override.
                foreach (var qitem in queueDict)
                {
                    var n = qitem.Key == _unspecifiedQueueOrTopicName ? null : qitem.Key;
                    var key = $"{GetType().Name}_QueueOrTopicName{(n is null ? "" : $"_{n}")}";
                    var queue = qitem.Value;
                    var sentIds = new List<string>();

                    // Send in batches.
                    while (queue.Count > 0)
                    {
                        sentIds.Clear();
                        var messageBatch = new List<IMessage>();

                        // Add the first message to the batch.
                        var firstMsg = queue.Peek();
                        messageBatch.Add(firstMsg.Message);
                        sentIds.Add(firstMsg.Message.ApplicationMessageId);
                        queue.Dequeue();

                        // Keep adding until done or max size reached for batch.
                        while (queue.Count > 0 && messageBatch.Count < PUBSUB_MAX_BATCH_SIZE)
                        {
                            messageBatch.Add(queue.Peek().Message);
                            sentIds.Add(queue.Peek().Message.ApplicationMessageId);
                            queue.Dequeue();
                        }

                        try
                        {
                            Logger.LogInformation("Sending {Count} message(s) to PubSub Broker.", messageBatch.Count);
                            var returnCode = session.Send(messageBatch.ToArray(), 0, messageBatch.Count, out int sentCount);

                            if (returnCode != ReturnCode.SOLCLIENT_OK)
                            {
                                Logger.LogDebug("{UnsentCount} of the total {TotalCount} events were not successfully sent.", unsentEvents.Count, totalCount);
                                throw new EventSendException(PrependStats($"PubSubMessage send failed with return code {Enum.GetName(typeof(ReturnCode), returnCode)}.", totalCount, unsentEvents.Count), unsentEvents);
                            }

                            if (messageBatch.Count != sentCount)
                            {
                                Logger.LogDebug("{UnsentCount} of the total {TotalCount} events were not successfully sent.", unsentEvents.Count, totalCount);
                                throw new InvalidOperationException("Not all messages in batch were sent; only {sentCount} of {messageBatch.Count} were sent.");
                            }

                            Logger.LogInformation("Successful send of {Count} message(s).", messageBatch.Count);
                        }
                        catch (Exception ex)
                        {
                            Logger.LogDebug("{UnsentCount} of the total {TotalCount} events were not successfully sent.", unsentEvents.Count, totalCount);
                            throw new EventSendException(PrependStats($"PubSubMessage cannot be sent: {ex.Message}", totalCount, unsentEvents.Count), ex, unsentEvents);
                        }

                        // Begin next batch after confirming sent events; continue ^ where any left.
                        unsentEvents.RemoveAll(esd => sentIds.Contains(esd.Id ?? string.Empty));
                    }
                }
            });

            return Task.CompletedTask;
        }

        /// <summary>
        /// Establishes the session to the PubSub broker.
        /// </summary>
        private ISession EstablishSessionToPubSubBroker()
        {
            Logger.LogDebug("Establishing Solace Session as {UserName}@{VPNName} on {Host} with SSL Trust Store directory {SSLTrustStoreDir}.", SessionProperties.UserName, SessionProperties.VPNName, SessionProperties.Host, SessionProperties.SSLTrustStoreDir);

            var session = SolaceContext.CreateSession(SessionProperties, null, null);
            var returnCode = session.Connect();
            if (returnCode == ReturnCode.SOLCLIENT_OK)
                return session;

            session.Dispose();
            throw new InvalidOperationException($"Cannot establish Solace PubSub broker session. Return code is {Enum.GetName(typeof(ReturnCode), returnCode)}.");
        }

        /// <summary>
        /// Prepend the sent stats to the message.
        /// </summary>
        private static string PrependStats(string message, int totalCount, int unsentCount) => $"{unsentCount} of the total {totalCount} events were not successfully sent. {message}";
    }
}