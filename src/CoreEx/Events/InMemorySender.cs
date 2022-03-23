// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace CoreEx.Events
{
    /// <summary>
    /// Provides an in-memory sender which can be used for the likes of testing.
    /// </summary>
    public class InMemorySender : IEventSender
    {
        private readonly ConcurrentQueue<EventSendData> _queue = new();

        /// <inheritdoc/>
        public Task SendAsync(params EventSendData[] events)
        {
            events.ForEach(e => _queue.Enqueue(e));
            return Task.CompletedTask;
        }

        /// <summary>
        /// Gets the events sent (in order).
        /// </summary>
        public EventSendData[] GetEvents() => _queue.ToArray();

        /// <summary>
        /// Resets (clears) the in-memory state.
        /// </summary>
        public void Reset() => _queue.Clear();
    }
}