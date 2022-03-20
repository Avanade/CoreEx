// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CoreEx.Events
{
    /// <summary>
    /// Provides the event publish; being the <see cref="EventData">event</see> <see cref="EventDataFormatter.Format(EventDataBase)">formatting</see>, 
    /// <see cref="IEventSerializer.SerializeAsync{T}(EventData{T})">serlialization</see> and <see cref="IEventSender.SendAsync(EventSendData[])">send</see>.
    /// </summary>
    public class EventPublisher : IEventPublisher
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventPublisher"/> class.
        /// </summary>
        /// <param name="eventDataFormatter">The <see cref="EventDataFormatter"/>.</param>
        /// <param name="eventSerializer">The <see cref="IEventSerializer"/>.</param>
        /// <param name="eventSender">The <see cref="IEventSender"/>.</param>
        public EventPublisher(EventDataFormatter eventDataFormatter, IEventSerializer eventSerializer, IEventSender eventSender)
        {
            EventDataFormatter = eventDataFormatter ?? new EventDataFormatter();
            EventSerializer = eventSerializer ?? throw new ArgumentNullException(nameof(eventSerializer));
            EventSender = eventSender ?? throw new ArgumentNullException(nameof(eventSender));
        }

        /// <summary>
        /// Gets the <see cref="Events.EventDataFormatter"/>.
        /// </summary>
        public EventDataFormatter EventDataFormatter { get; }

        /// <summary>
        /// Gets the <see cref="IEventSerializer"/>.
        /// </summary>
        public IEventSerializer EventSerializer { get; }

        /// <summary>
        /// Gets the <see cref="IEventSender"/>.
        /// </summary>
        public IEventSender EventSender { get; }

        /// <summary>
        /// Sends one or more <see cref="EventData"/> objects.
        /// </summary>
        /// <param name="events">One or more <see cref="EventData"/> objects to be published.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        public Task PublishAsync(params EventData[] events) => PublishInternalAsync(null, events);

        /// <summary>
        /// Sends one or more <see cref="EventData"/> objects to a named destination.
        /// </summary>
        /// <param name="name">The destination name.</param>
        /// <param name="events">One or more <see cref="EventData"/> objects to be published.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        /// <remarks>The name could represent a queue name or equivalent where appropriate.</remarks>
        public Task PublishAsync(string name, params EventData[] events)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            return PublishInternalAsync(name, events);
        }

        /// <summary>
        /// Performs the format, serlialization and send.
        /// </summary>
        private async Task PublishInternalAsync(string? name, params EventData[] events)
        {
            var list = new List<EventSendData>();
            foreach (var @event in events)
            {
                var e = @event.Copy();
                EventDataFormatter.Format(e);
                var bd = await EventSerializer.SerializeAsync(e).ConfigureAwait(false);
                list.Add(new EventSendData(e) { DestinationName = name, Data = bd });
            }

            await EventSender.SendAsync(list.ToArray()).ConfigureAwait(false);
        }
    }
}