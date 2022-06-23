// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using CoreEx.Configuration;
using CoreEx.Events;
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
        public Task PurgeDeadLetterAsync(string queueName, Action<PurgedMessageData>? messageAction = null)
        {
            return PurgeAsync(queueName, SubQueue.DeadLetter, messageAction);
        }

        /// <inheritdoc/>
        public Task PurgeQueueAsync(string queueName, Action<PurgedMessageData>? messageAction = null)
        {
            return PurgeAsync(queueName, SubQueue.None, messageAction);
        }

        private async Task PurgeAsync(string queueName, SubQueue subQueue, Action<PurgedMessageData>? messageAction = null)
        {
            if (string.IsNullOrEmpty(queueName))
                throw new ArgumentNullException(nameof(queueName));


            // Get queue name by checking config override.
            var qn = Settings.GetValue<string>($"Publisher_ServiceBusQueueName_{queueName}", defaultValue: queueName);

            // Receive from Dead letter
            await using var receiver = _client.CreateReceiver(qn, new ServiceBusReceiverOptions { SubQueue = subQueue, PrefetchCount = 500, ReceiveMode = ServiceBusReceiveMode.ReceiveAndDelete });

            // Purge messages.
            try
            {
                IReadOnlyList<ServiceBusReceivedMessage>? messages = await receiver.ReceiveMessagesAsync(500, maxWaitTime: TimeSpan.FromSeconds(3));

                while (messages != null && messages.Count > 0)
                {
                    if (messageAction != null)
                    {
                        foreach (var message in messages)
                        {
                            var msgData = new PurgedMessageData(message.MessageId, message.Subject, message.CorrelationId, message.DeadLetterReason, message.DeadLetterErrorDescription, message.DeadLetterSource, message.Body.ToString());
                            messageAction(msgData);
                        }
                    }

                    messages = await receiver.ReceiveMessagesAsync(500, maxWaitTime: TimeSpan.FromSeconds(3));
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, $"Service Bus message(s) couldn't be purged from {qn} sub-queue: {subQueue}.");
                throw;
            }
        }
    }
}