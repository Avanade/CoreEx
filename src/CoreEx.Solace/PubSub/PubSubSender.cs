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
        /// <param name="sessionProperties">The pubsub session properties used to creat a session</param>
        /// <param name="settings">The <see cref="SettingsBase"/>.</param>
        /// <param name="logger">The <see cref="ILogger"/>.</param>
        /// <param name="invoker">The optional <see cref="PubSubSenderInvoker"/>.</param>
        /// <param name="converter">The optional <see cref="IValueConverter{TSource, TDestination}"/> to convert an <see cref="EventSendData"/> to a corresponding <see cref="IMessage"/>.</param>
        public PubSubSender(SessionProperties sessionProperties, SettingsBase settings, ILogger<PubSubSender> logger, PubSubSenderInvoker? invoker = null, IValueConverter<EventSendData, IMessage>? converter = null)
        {
            SessionProperties = sessionProperties ?? throw new ArgumentNullException(nameof(sessionProperties), "Verify PubSub connection properties have been correctly defined.");
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            Invoker = invoker ?? (_invoker ??= new PubSubSenderInvoker());
            Converter = converter ?? new EventSendDataToPubSubConverter();
            DefaultQueueOrTopicName = Settings.GetValue($"{GetType().Name}:QueueOrTopicName", defaultValue: _unspecifiedQueueOrTopicName);

            EstablishConnectionToPubSubBroker();
        }

        /// <summary>
        /// Gets the <see cref="ISession"/>.
        /// </summary>
        protected ISession Session { get; set; }

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
        /// Gets the <see cref="CoreEx.ExecutionContext"/>.
        /// </summary>
        protected ExecutionContext ExecutionContext { get; }

        /// <summary>
        /// Gets the <see cref="IValueConverter{TSource, TDestination}"/> to convert an <see cref="EventSendData"/> to a corresponding <see cref="ServiceBusMessage"/>.
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

            return Invoker.InvokeAsync(this, events, async (events, cancellationToken) =>
            {
                var totalCount = events.Count();
                Logger.LogDebug("{TotalCount} events in total are to be sent.", totalCount);

                if (events.Count() != events.Select(x => x.Id).Distinct().Count())
                    throw new EventSendException(PrependStats($"All events must have a unique identifier ({nameof(EventSendData)}.{nameof(EventSendData.Id)}).", totalCount, totalCount), events);

                // Sets up the list of unsent events.
                var unsentEvents = new List<EventSendData>(events);

                var queueDict = new Dictionary<string, Queue<(IMessage Message, int Index)>>();
                var index = 0;
                foreach (var @event in events)
                {
                    //Convert message
                    var message = Converter.Convert(@event) ?? throw new EventSendException($"The {nameof(Converter)} must return a {nameof(IMessage)} instance.");

                    var name = @event.Destination ?? DefaultQueueOrTopicName;
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
                            Logger.LogInformation($"Sending {messageBatch.Count} message(s) to PubSub Broker.");
                            var returnCode = Session.Send(messageBatch.ToArray(), 0, messageBatch.Count, out int sentCount);

                            if (messageBatch.Count != sentCount)
                                throw new Exception("Not all messages in batch were sent.");
                            if (returnCode == ReturnCode.SOLCLIENT_OK)
                            {
                                Logger.LogInformation($"Successful send of {messageBatch.Count} message(s)");
                            }
                            else
                            {
                                Logger.LogError("Publishing to PubSub failed, return code: {0}", returnCode);
                            }
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
            }, cancellationToken);
        }

        private void EstablishConnectionToPubSubBroker()
        {
            // Initialize Solace Systems Messaging API with logging to console at Warning level
            ContextFactoryProperties cfp = new ContextFactoryProperties()
            {
                SolClientLogLevel = SolLogLevel.Warning
            };
            cfp.LogToConsoleError();
            ContextFactory.Instance.Init(cfp);

            Logger.LogInformation($"Connecting to Solace as {SessionProperties.UserName}@{SessionProperties.VPNName} on {SessionProperties.Host}" +
                $"with SSL Trust Store directory {SessionProperties.SSLTrustStoreDir}");

            var context = ContextFactory.Instance.CreateContext(new ContextProperties(), null);
            var session = context.CreateSession(SessionProperties, null, null);
            var returnCode = session.Connect();
            if (returnCode == ReturnCode.SOLCLIENT_OK)
            {
                SolaceContext = context;
                Session = session;
            }
            else
            {
                Logger.LogInformation($"Cannot connect to PubSub broker");
                Logger.LogDebug($"Error Connecting to Solace.  Return code is {Enum.GetName(typeof(ReturnCode), returnCode)}");
            }
        }


        /// <summary>
        /// Prepend the sent stats to the message.
        /// </summary>
        private static string PrependStats(string message, int totalCount, int unsentCount) => $"{unsentCount} of the total {totalCount} events were not successfully sent. {message}";
    }
}
