// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

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
        /// <returns>The <see cref="Task"/>.</returns>
        Task SendAsync(params EventSendData[] events);
    }
}