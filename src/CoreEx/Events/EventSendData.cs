﻿// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;

namespace CoreEx.Events
{
    /// <summary>
    /// Represents the <see cref="EventData"/> where the <see cref="Data"/> is the serialized (see <see cref="IEventSerializer.SerializeAsync(EventData, System.Threading.CancellationToken)"/> or 
    /// <see cref="IEventSerializer.SerializeAsync{T}(EventData{T}, System.Threading.CancellationToken)"/>) representation that is to used for a <see cref="IEventSender.SendAsync(System.Collections.Generic.IEnumerable{EventSendData}, System.Threading.CancellationToken)"/>.
    /// </summary>
    public class EventSendData : EventDataBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventSendData"/> class.
        /// </summary>
        public EventSendData() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventSendData"/> class copying from another <paramref name="event"/> excluding the underlying <see cref="Data"/> and <see cref="EventData{T}.Value"/>.
        /// </summary>
        /// <param name="event">The <see cref="EventDataBase"/>.</param>
        public EventSendData(EventDataBase @event) : base(@event) { }

        /// <summary>
        /// Get or sets the optional <see cref="IEventPublisher.PublishNamed(string, EventData[])"/> destination name (i.e. queue or topic).
        /// </summary>
        public string? Destination { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="BinaryData"/>.
        /// </summary>
        public BinaryData? Data { get; set; }
    }
}