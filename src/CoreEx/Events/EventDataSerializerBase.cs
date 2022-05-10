// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Json;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Events
{
    /// <summary>
    /// Provides the base <see cref="EventData"/> <see cref="IEventSerializer"/> capabilities.
    /// </summary>
    /// <remarks>The <see cref="SerializeValueOnly"/> indicates whether the <see cref="EventData.Value"/> is serialized only; or alternatively, the complete <see cref="EventData"/> (default).</remarks>
    public abstract class EventDataSerializerBase : IEventSerializer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventDataSerializerBase"/> class.
        /// </summary>
        /// <param name="jsonSerializer">The <see cref="IJsonSerializer"/>.</param>
        /// <param name="eventDataFormatter">The <see cref="Events.EventDataFormatter"/>.</param>
        protected EventDataSerializerBase(IJsonSerializer jsonSerializer, EventDataFormatter? eventDataFormatter)
        {
            JsonSerializer = jsonSerializer ?? throw new ArgumentNullException(nameof(jsonSerializer));
            EventDataFormatter = eventDataFormatter ?? new EventDataFormatter();
        }

        /// <summary>
        /// Gets the <see cref="IJsonSerializer"/>.
        /// </summary>
        public IJsonSerializer JsonSerializer { get; }

        /// <summary>
        /// Gets the <see cref="Events.EventDataFormatter"/>.
        /// </summary>
        public EventDataFormatter EventDataFormatter { get; }

        /// <summary>
        /// Indicates whether the <see cref="EventData.Value"/> is serialized only (<c>true</c>); or alternatively, the complete <see cref="EventData"/> (<c>false</c>).
        /// </summary>
        /// <remarks>Defaults to <c>true</c>.</remarks>
        public bool SerializeValueOnly { get; set; } = true;

        /// <inheritdoc/>
        public Task<EventData> DeserializeAsync(BinaryData eventData, CancellationToken cancellationToken = default)
        {
            if (SerializeValueOnly)
                return Task.FromResult(new EventData { Value = JsonSerializer.Deserialize(eventData) });
            else
                return Task.FromResult(JsonSerializer.Deserialize<EventData>(eventData))!;
        }

        /// <inheritdoc/>
        public Task<EventData<T>> DeserializeAsync<T>(BinaryData eventData, CancellationToken cancellationToken = default)
        {
            if (SerializeValueOnly)
                return Task.FromResult(new EventData<T> { Id = null, Timestamp = null, CorrelationId = null, Value = JsonSerializer.Deserialize<T>(eventData)! });
            else
                return Task.FromResult(JsonSerializer.Deserialize<EventData<T>>(eventData))!;
        }

        /// <inheritdoc/>
        public Task<BinaryData> SerializeAsync(EventData @event, CancellationToken cancellationToken = default)
        {
            if (SerializeValueOnly)
                return Task.FromResult(JsonSerializer.SerializeToBinaryData(@event.Value));

            @event = @event.Copy();
            EventDataFormatter.Format(@event);
            return Task.FromResult(JsonSerializer.SerializeToBinaryData(@event));
        }

        /// <inheritdoc/>
        public Task<BinaryData> SerializeAsync<T>(EventData<T> @event, CancellationToken cancellationToken = default)
        {
            if (SerializeValueOnly)
                return Task.FromResult(JsonSerializer.SerializeToBinaryData(@event.Value));

            @event = @event.Copy();
            EventDataFormatter.Format(@event);
            return Task.FromResult(JsonSerializer.SerializeToBinaryData(@event));
        }
    }
}