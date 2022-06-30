// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Json;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Events
{
    /// <summary>
    /// Provides an in-memory publisher which can be used for the likes of testing.
    /// </summary>
    /// <remarks>Where a <see cref="Logger"/> is provided then each <see cref="EventData"/> will also be logged during <i>Send</i>.</remarks>
    public class InMemoryPublisher : EventPublisher
    {
        private readonly ILogger? _logger;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly ConcurrentDictionary<string, ConcurrentQueue<EventData>> _dict = new();
        private const string NullName = "!@#$%";

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryPublisher"/> class.
        /// </summary>
        /// <param name="logger">The optional <see cref="ILogger"/> for logging the events (each <see cref="EventData"/>).</param>
        /// <param name="jsonSerializer">The optional <see cref="IJsonSerializer"/> for the logging. Defaults to <see cref="JsonSerializer.Default"/></param>
        /// <param name="eventDataFormatter">The <see cref="EventDataFormatter"/>; defaults where not specified.</param>
        /// <param name="eventSerializer">The optional <see cref="IEventSerializer"/>. Defaults to <see cref="Text.Json.EventDataSerializer"/>.</param>
        public InMemoryPublisher(ILogger? logger = null, IJsonSerializer? jsonSerializer = null, EventDataFormatter? eventDataFormatter = null, IEventSerializer? eventSerializer = null)
            : base(eventDataFormatter, eventSerializer ?? new Text.Json.EventDataSerializer(), new InMemorySender())
        {
            _logger = logger;
            _jsonSerializer = jsonSerializer ?? Json.JsonSerializer.Default;
        }

        /// <inheritdoc/>
        protected override Task OnEventSendAsync(string? name, EventData eventData, EventSendData eventSendData, CancellationToken cancellationToken)
        {
            var queue = _dict.GetOrAdd(name ?? NullName, _ => new ConcurrentQueue<EventData>());
            queue.Enqueue(eventData);

            if (_logger != null)
            {
                var sb = new StringBuilder("Event send");
                if (!string.IsNullOrEmpty(name))
                    sb.Append($" (destination: '{name}')");

                sb.AppendLine(" ->");

                var json = _jsonSerializer.Serialize(eventData, JsonWriteFormat.Indented);
                sb.Append(json);
                _logger.LogInformation("{Event}", sb.ToString());
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Gets the list of destination names (i.e. queue or topic) used for sending. 
        /// </summary>
        /// <returns>An array of names.</returns>
        /// <remarks>Where <see cref="EventPublisher.Publish(EventData[])"/> (with no name) is used the underlying destination name will be <c>null</c>.</remarks>
        public string?[] GetNames() => _dict.Keys.ToArray();

        /// <summary>
        /// Gets the events sent (in order) to the named destination.
        /// </summary>
        /// <param name="name">The destination name.</param>
        /// <returns>The corresponding events.</returns>
        public EventData[] GetEvents(string? name = null) => _dict.TryGetValue(name ?? NullName, out var queue) ? queue.ToArray() : Array.Empty<EventData>();

        /// <summary>
        /// Resets (clears) the in-memory state.
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            _dict.Clear();
            ((InMemorySender)EventSender).Reset();
        }
    }
}