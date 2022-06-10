// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Threading.Tasks;

namespace CoreEx.Events
{
    /// <summary>
    /// Defines the standardized <b>Event</b> purging via the actual messaging platform/protocol.
    /// </summary>
    public interface IEventPurger
    {
        /// <summary>
        /// Purges the dead letter queue from all the messages. <paramref name="messageAction"/> is called for each message.
        /// </summary>
        Task PurgeDeadLetterAsync(string queueName, Action<PurgedMessageData>? messageAction = null);
        /// <summary>
        /// Purges the queue from all the messages.  <paramref name="messageAction"/> is called for each message.
        /// </summary>
        Task PurgeQueueAsync(string queueName, Action<PurgedMessageData>? messageAction = null);
    }
}