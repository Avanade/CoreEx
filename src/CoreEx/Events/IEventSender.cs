// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Events
{
    /// <summary>
    /// Defines the standardized <b>Event</b> sending via the actual messaging platform/protocol.
    /// </summary>
    /// <remarks>The <see cref="EventSendData.Data"/> is expected to be already serialized <see cref="IEventSerializer"/>.</remarks>
    public interface IEventSender
    {
        /// <summary>
        /// Sends one or more <see cref="EventSendData"/> objects.
        /// </summary>
        /// <param name="events">One or more <see cref="EventData"/> objects to be sent.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        Task SendAsync(IEnumerable<EventSendData> events, CancellationToken cancellationToken = default);

        /// <summary>
        /// Occurs after a successful <see cref="SendAsync"/>.
        /// </summary>
        event EventHandler? AfterSend;
    }
}