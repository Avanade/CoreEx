// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System.Threading.Tasks;

namespace CoreEx.Events
{
    /// <summary>
    /// Defines the standardized <b>Event</b> publishing (generally encapsulates the <see cref="IEventSerializer"/> and <see cref="IEventSender"/> orchestration). 
    /// </summary>
    public interface IEventPublisher
    {
        /// <summary>
        /// Publishes one or more <see cref="EventData"/> objects.
        /// </summary>
        /// <param name="events">One or more <see cref="EventData"/> objects to be sent.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        Task PublishAsync(params EventData[] events);

        /// <summary>
        /// Publishes one or more <see cref="EventData"/> objects to a named destination (e.g. queue or topic).
        /// </summary>
        /// <param name="name">The destination name.</param>
        /// <param name="events">One or more <see cref="EventData"/> objects to be sent.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        /// <remarks>The name could represent a queue name or equivalent where appropriate.</remarks>
        Task PublishAsync(string name, params EventData[] events);
    }
}