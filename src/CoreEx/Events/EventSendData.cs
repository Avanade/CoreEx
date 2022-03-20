// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;

namespace CoreEx.Events
{
    /// <summary>
    /// Represents the <see cref="EventData"/> where the <see cref="Data"/> is the serialized (see <see cref="IEventSerializer.SerializeAsync(EventData)"/> or <see cref="IEventSerializer.SerializeAsync{T}(EventData{T})"/>) representation
    /// that is to used for a <see cref="IEventSender.SendAsync(EventSendData[])"/>.
    /// </summary>
    public class EventSendData : EventDataBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventSendData"/> class.
        /// </summary>
        public EventSendData() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventSendData"/> class copying from another <paramref name="event"/> excluding the underlying <see cref="Data"/> or <see cref="EventData{T}.Value"/>.
        /// </summary>
        /// <param name="event">The <see cref="EventDataBase"/>.</param>
        public EventSendData(EventDataBase @event) : base(@event) { }

        /// <summary>
        /// Get or sets the optional <see cref="IEventPublisher.PublishAsync(string, EventData[])"/> destination name.
        /// </summary>
        public string? DestinationName { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="BinaryData"/>.
        /// </summary>
        public BinaryData? Data { get; set; }

        /// <inheritdoc/>
        public override object? GetValue() => Data;
    }
}