// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using CoreEx.Configuration;
using Microsoft.Extensions.Logging;

namespace CoreEx.Azure.ServiceBus
{
    /// <summary>
    /// Provides the Azure ServiceBus purging capability.
    /// </summary>
    public class ServiceBusPurger : IEventPurger
    {
        private readonly ServiceBusClient _client;
        private readonly SettingsBase Settings;
        private readonly ILogger Logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceBusPurger"/> class.
        /// </summary>
        /// <param name="client">The underlying <see cref="ServiceBusClient"/>.</param>
        /// <param name="settings">The <see cref="SettingsBase"/>.</param>
        /// <param name="logger">The <see cref="ILogger"/>.</param>
        public ServiceBusPurger(ServiceBusClient client, SettingsBase settings, ILogger<ServiceBusPurger> logger)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client), "Verify dependency injection configuration and if service bus connection string for publisher was correctly defined.");
            _client.ConfigureAwait(false);

            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public Task PurgeDeadLetterAsync(string queueName, Action<ServiceBusReceivedMessage>? messageAction = null, CancellationToken cancellationToken = default) 
            => PurgeAsync(queueName, null, SubQueue.DeadLetter, messageAction, cancellationToken);

        /// <inheritdoc/>
        public Task PurgeAsync(string queueName, Action<ServiceBusReceivedMessage>? messageAction = null, CancellationToken cancellationToken = default)
            => PurgeAsync(queueName, null, SubQueue.None, messageAction, cancellationToken);

        /// <inheritdoc/>
        public Task PurgeDeadLetterAsync(string topicName, string subscriptionName, Action<ServiceBusReceivedMessage>? messageAction = null, CancellationToken cancellationToken = default)
            => PurgeAsync(topicName, subscriptionName, SubQueue.DeadLetter, messageAction, cancellationToken);

        /// <inheritdoc/>
        public Task PurgeAsync(string topicName, string subscriptionName, Action<ServiceBusReceivedMessage>? messageAction = null, CancellationToken cancellationToken = default)
            => PurgeAsync(topicName, subscriptionName, SubQueue.None, messageAction, cancellationToken);

        /// <summary>
        /// Purges the queue or topic subscription.
        /// </summary>
        private async Task PurgeAsync(string queueOrTopicName, string? subscriptionName, SubQueue subQueue, Action<ServiceBusReceivedMessage>? messageAction, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(queueOrTopicName))
                throw new ArgumentNullException(nameof(queueOrTopicName));

            // Get queue name and subscription name by checking settings override.
            var qn = Settings.GetValue($"Publisher_ServiceBusQueueName_{queueOrTopicName}", defaultValue: queueOrTopicName);
            var sn = string.IsNullOrEmpty(subscriptionName) ? null : Settings.GetValue($"Publisher_ServiceBusSubscriptionName_{subscriptionName}", defaultValue: subscriptionName);

            // Receive from Dead letter
            var o = new ServiceBusReceiverOptions { SubQueue = subQueue, PrefetchCount = 500, ReceiveMode = ServiceBusReceiveMode.ReceiveAndDelete };
            await using var receiver = sn == null ? _client.CreateReceiver(qn, o) : _client.CreateReceiver(qn, sn, o);

            // Purge messages.
            try
            {
                var messages = await receiver.ReceiveMessagesAsync(500, maxWaitTime: TimeSpan.FromSeconds(3), cancellationToken).ConfigureAwait(false);

                while (messages != null && messages.Count > 0)
                {
                    if (messageAction != null)
                    {
                        foreach (var message in messages)
                        {
                            messageAction(message);
                        }
                    }

                    messages = await receiver.ReceiveMessagesAsync(500, maxWaitTime: TimeSpan.FromSeconds(3), cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, $"Service Bus message(s) couldn't be purged from {qn} {(subscriptionName == null ? "" : $"{subscriptionName} ")}sub-queue: {subQueue}.");
                throw;
            }
        }
    }
}