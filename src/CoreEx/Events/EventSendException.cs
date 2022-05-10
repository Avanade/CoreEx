// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Collections.Generic;

namespace CoreEx.Events
{
    /// <summary>
    /// Represents an <see cref="IEventSender.SendAsync(IEnumerable{EventSendData}, System.Threading.CancellationToken)"/> <see cref="Exception"/> with a collection of <see cref="NotSentEvents"/>.
    /// </summary>
    public class EventSendException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventSendException"/> class with a <paramref name="message"/> and <paramref name="notSentEvents"/>.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="notSentEvents">The events that were not sent.</param>
        public EventSendException(string message, IEnumerable<EventSendData>? notSentEvents = null) : base(message) => NotSentEvents = notSentEvents;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventSendException"/> class with a <paramref name="message"/>, <paramref name="innerException"/> and <paramref name="notSentEvents"/>.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="innerException">The inner <see cref="Exception"/>.</param>
        /// <param name="notSentEvents">The events that were not sent.</param>
        public EventSendException(string message, Exception innerException, IEnumerable<EventSendData>? notSentEvents = null) : base(message, innerException) => NotSentEvents = notSentEvents;

        /// <summary>
        /// Gets the events that were not sent to enable further exception processing of these where required.
        /// </summary>
        public IEnumerable<EventSendData>? NotSentEvents { get; }
    }
}