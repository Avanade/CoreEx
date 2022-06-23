// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using Azure.Messaging.ServiceBus;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Azure.ServiceBus
{
    /// <summary>
    /// Defines the standardized <b>Event</b> purging via the actual messaging platform/protocol.
    /// </summary>
    public interface IEventPurger
    {
        /// <summary>
        /// Purges the dead letter <paramref name="queueName"/> of all the messages.
        /// </summary>
        /// <param name="queueName">The queue name.</param>
        /// <param name="messageAction">The optional action for each purged message.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        Task PurgeDeadLetterAsync(string queueName, Action<ServiceBusReceivedMessage>? messageAction = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Purges the <paramref name="queueName"/>  of all the messages.
        /// </summary>
        /// <param name="queueName">The queue name.</param>
        /// <param name="messageAction">The optional action for each purged message.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        Task PurgeAsync(string queueName, Action<ServiceBusReceivedMessage>? messageAction = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Purges the dead letter <paramref name="topicName"/> and <<paramref name="subscriptionName"/> of all the messages.
        /// </summary>
        /// <param name="topicName">The topic name.</param>
        /// <param name="subscriptionName">The subscription name.</param>
        /// <param name="messageAction">The optional action for each purged message.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        Task PurgeDeadLetterAsync(string topicName, string subscriptionName, Action<ServiceBusReceivedMessage>? messageAction = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Purges the <paramref name="topicName"/> and <<paramref name="subscriptionName"/> of all the messages.
        /// </summary>
        /// <param name="topicName">The topic name.</param>
        /// <param name="subscriptionName">The subscription name.</param>
        /// <param name="messageAction">The optional action for each purged message.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        Task PurgeAsync(string topicName, string subscriptionName, Action<ServiceBusReceivedMessage>? messageAction = null, CancellationToken cancellationToken = default);
    }
}