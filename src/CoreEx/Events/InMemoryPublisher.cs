// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Json;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreEx.Events
{
    /// <summary>
    /// Provides an in-memory publisher base which can be used for the likes of testing.
    /// </summary>
    /// <remarks>Where a <see cref="Logger"/> is provided then each <see cref="EventData"/> will also be logged during <i>Send</i>.</remarks>
    public class InMemoryPublisher : IEventPublisher
    {
        private readonly ConcurrentDictionary<string, ConcurrentQueue<EventData>> _dict = new();
        private const string NullName = "!@#$%";

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryPublisher"/> class.
        /// </summary>
        /// <param name="logger">The optional <see cref="ILogger"/> for logging the events (each <see cref="EventData"/>).</param>
        /// <param name="jsonSerializer">The optional <see cref="IJsonSerializer"/> used specifically for logging. Defaults to <see cref="CoreEx.Json.JsonSerializer.Default"/>.</param>
        /// <param name="eventDataFormatter">The <see cref="EventDataFormatter"/>; defaults where not specified.</param>
        public InMemoryPublisher(ILogger? logger = null, IJsonSerializer? jsonSerializer = null, EventDataFormatter? eventDataFormatter = null)
        {
            Logger = logger;
            JsonSerializer = jsonSerializer ?? CoreEx.Json.JsonSerializer.Default;
            EventDataFormatter = eventDataFormatter ?? new EventDataFormatter();
        }

        /// <summary>
        /// Gets the <see cref="Logger"/>.
        /// </summary>
        protected ILogger? Logger { get; }

        /// <summary>
        /// Gets the <see cref="IJsonSerializer"/>.
        /// </summary>
        protected IJsonSerializer JsonSerializer { get; }

        /// <summary>
        /// Gets the <see cref="EventDataFormatter"/>.
        /// </summary>
        protected EventDataFormatter EventDataFormatter { get; }

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
            var sb = new StringBuilder($"The following events were sent{(name == null ? ":" : $" to '{name}':")}");

            var queue = _dict.GetOrAdd(name ?? NullName, _ => new ConcurrentQueue<EventData>());
            foreach (var @event in events.Where(e => e != null))
            {
                var e = @event.Copy();
                EventDataFormatter.Format(e);
                queue.Enqueue(e);

                sb.AppendLine();
                sb.Append(JsonSerializer.Serialize(e, JsonWriteFormat.Indented));
            }

            Logger?.LogInformation("{Events}", sb.ToString());
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
        public EventData[] GetEvents(string? name = null) => _dict.TryGetValue(name ?? NullName, out var queue) ? queue.ToArray() : Array.Empty<EventData>();
    }
}