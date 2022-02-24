// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace CoreEx.Events
{
    /// <summary>
    /// Provides an in-memory publisher which can be used for the likes of testing.
    /// </summary>
    public class InMemoryPublisher : IEventPublisher
    {
        private readonly ConcurrentDictionary<string?, ConcurrentQueue<EventData>> _dict = new();

        /// <inheritdoc/>
        public Task SendAsync(params EventData[] events) => SendInternalAsync(null, events);

        /// <inheritdoc/>
        public Task SendAsync(string name, params EventData[] events)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            return SendInternalAsync(name, events);
        }

        /// <summary>
        /// Internally queue the events.
        /// </summary>
        private Task SendInternalAsync(string? name, EventData[] events)
        {
            var queue = _dict.GetOrAdd(name, _ => new ConcurrentQueue<EventData>());
            foreach (var e in events)
            {
                queue.Enqueue(e);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Gets the list of names used for sending. 
        /// </summary>
        /// <returns>An array of names.</returns>
        /// <remarks>Where <see cref="SendAsync(EventData[])"/> (no name) is used the underlying name will be <c>null</c>.</remarks>
        public string?[] GetNames() => _dict.Keys.ToArray();

        /// <summary>
        /// Gets the events sent (in order) to the named destination.
        /// </summary>
        /// <param name="name">The destination name.</param>
        /// <returns>The corresponding events.</returns>
        public EventData[] GetEvents(string? name = null) => _dict.TryGetValue(name, out var queue) ? queue.ToArray() : Array.Empty<EventData>();
    }
}