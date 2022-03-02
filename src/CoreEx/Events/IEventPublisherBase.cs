// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System.Threading.Tasks;

namespace CoreEx.Events
{
    /// <summary>
    /// Defines the standardised <b>Event</b> publishing and sending. 
    /// </summary>
    public interface IEventPublisherBase
    {
        /// <summary>
        /// Sends one or more <see cref="EventData"/> objects.
        /// </summary>
        /// <param name="events">One or more <see cref="EventData"/> objects to be sent.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        Task SendAsync(params EventData[] events);

        /// <summary>
        /// Sends one or more <see cref="EventData"/> objects to a named destination.
        /// </summary>
        /// <param name="name">The destination name.</param>
        /// <param name="events">One or more <see cref="EventData"/> objects to be sent.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        /// <remarks>The name could represent a queue name or equivalent where appropriate.</remarks>
        Task SendAsync(string name, params EventData[] events);
    }
}