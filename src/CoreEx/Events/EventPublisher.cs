// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Events
{
    /// <summary>
    /// Provides the event publishing and sending; being the <see cref="EventData">event</see> <see cref="EventDataFormatter.Format(EventData)">formatting</see>, 
    /// <see cref="IEventSerializer.SerializeAsync{T}(EventData{T}, CancellationToken)">serialization</see> and <see cref="IEventSender.SendAsync(IEnumerable{EventSendData}, CancellationToken)">send</see>.
    /// </summary>
    public class EventPublisher : IEventPublisher, IDisposable
    {
        private readonly ConcurrentQueue<(string? Destination, EventData Event)> _queue = new();
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventPublisher"/> class.
        /// </summary>
        /// <param name="eventDataFormatter">The <see cref="EventDataFormatter"/>.</param>
        /// <param name="eventSerializer">The <see cref="IEventSerializer"/>.</param>
        /// <param name="eventSender">The <see cref="IEventSender"/>.</param>
        public EventPublisher(EventDataFormatter? eventDataFormatter, IEventSerializer eventSerializer, IEventSender eventSender)
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

        /// <inheritdoc/>
        public bool IsEmpty => _queue.IsEmpty;

        /// <summary>
        /// Sends one or more <see cref="EventData"/> objects.
        /// </summary>
        /// <param name="events">One or more <see cref="EventData"/> objects to be published.</param>
        /// <returns>The <see cref="IEventPublisher"/> to support fluent-style method-chaining.</returns>
        public IEventPublisher Publish(params EventData[] events) => PublishInternal(null, events);

        /// <summary>
        /// Sends one or more <see cref="EventData"/> objects to a named destination (e.g. queue or topic).
        /// </summary>
        /// <param name="name">The destination name.</param>
        /// <param name="events">One or more <see cref="EventData"/> objects to be published.</param>
        /// <returns>The <see cref="IEventPublisher"/> to support fluent-style method-chaining.</returns>
        /// <remarks>The <paramref name="name"/> could represent a queue name or equivalent where appropriate.</remarks>
        public IEventPublisher Publish(string name, params EventData[] events)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            return PublishInternal(name, events);
        }

        /// <summary>
        /// Performs the formatting and queues internally.
        /// </summary>
        private EventPublisher PublishInternal(string? name, params EventData[] events)
        {
            foreach (var @event in events)
            {
                var e = @event.Copy();
                EventDataFormatter.Format(e);
                _queue.Enqueue((name, e));
            }

            return this;
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="cancellationToken"><inheritdoc/></param>
        /// <remarks>Initially performs the <see cref="EventSerializer">serialization</see> for each queued event, then performs a single <see cref="EventSender">send</see> for all.</remarks>
        public async Task SendAsync(CancellationToken cancellationToken = default)
        {
            var list = new List<EventSendData>();
            while (_queue.TryDequeue(out var item))
            { 
                var bd = await EventSerializer.SerializeAsync(item.Event, cancellationToken).ConfigureAwait(false);
                var esd = new EventSendData(item.Event) { Destination = item.Destination, Data = bd };
                await OnEventSendAsync(item.Destination, item.Event, esd, cancellationToken).ConfigureAwait(false);
                list.Add(esd);
            }

            await EventSender.SendAsync(list.ToArray(), cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Invoked on the send of the <see cref="EventSendData"/>.
        /// </summary>
        /// <param name="name">The destination name.</param>
        /// <param name="eventData">The <see cref="EventData"/> (after the <see cref="EventDataFormatter.Format(EventData)"/> is applied).</param>
        /// <param name="eventSendData">The corresponding <see cref="EventSendData"/> after <see cref="EventSerializer"/> invocation.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        protected virtual Task OnEventSendAsync(string? name, EventData eventData, EventSendData eventSendData, CancellationToken cancellationToken) => Task.CompletedTask;

        /// <inheritdoc/>
        public virtual void Reset() => _queue.Clear();

        /// <summary>
        /// Dispose of resources.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when there are unsent events.</exception>
        public void Dispose()
        {
            if (!_disposed)
            {
                if (!_queue.IsEmpty)
                    throw new InvalidOperationException($"Attempting to dispose of an {GetType().Name} when there are '{_queue.Count}' unsent event(s); must be either explicity sent ({nameof(SendAsync)}) or cleared ({nameof(Reset)}).");

                _disposed = true;
            }

            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="EventPublisher"/> and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing) { }
    }
}