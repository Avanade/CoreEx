// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Events
{
    /// <summary>
    /// Represents a <c>null</c> event sender; whereby the events are simply swallowed/discarded on send.
    /// </summary>
    public class NullEventSender : IEventSender
    {
        /// <inheritdoc/>
        public Task SendAsync(IEnumerable<EventSendData> events, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}