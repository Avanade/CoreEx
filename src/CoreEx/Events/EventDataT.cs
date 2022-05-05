// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;

namespace CoreEx.Events
{
    /// <summary>
    /// Represents the <see cref="EventData"/> with a typed <see cref="Value"/>.
    /// </summary>
    /// <typeparam name="T">The <see cref="Value"/> <see cref="Type"/>.</typeparam>
    public class EventData<T> : EventData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventData"/> class.
        /// </summary>
        public EventData() : base() => Value = default!;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventData"/> class copying from another <paramref name="event"/> (excludes <see cref="Value"/>).
        /// </summary>
        /// <param name="event">The <paramref name="event"/> to copy from.</param>
        /// <remarks>Does not copy the underlying <see cref="Value"/>; this must be set explicitly.</remarks>
        public EventData(EventDataBase @event) : base(@event) { }

        /// <summary>
        /// Gets or sets the event data.
        /// </summary>
        public new T Value { get => (T)base.Value!; set => base.Value = value; }

        /// <summary>
        /// Copies the <see cref="EventData"/> (including the <see cref="Value"/>) creating a new instance.
        /// </summary>
        /// <returns>A new <see cref="EventData"/> instance.</returns>
        public new EventData<T> Copy() => new(this) { Value = Value };
    }
}