// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

namespace CoreEx.Events
{
    /// <summary>
    /// Represents the core event data with a generic <see cref="object"/> <see cref="Value"/>.
    /// </summary>
    public class EventData : EventDataBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventData"/> class.
        /// </summary>
        public EventData() : base() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventData"/> class copying from another <paramref name="event"/> (excludes <see cref="Value"/>).
        /// </summary>
        /// <param name="event">The <paramref name="event"/> to copy from.</param>
        /// <remarks>Does not copy the underlying <see cref="Value"/>; this must be set explicitly.</remarks>
        public EventData(EventDataBase @event) : base(@event) { }

        /// <summary>
        /// Gets or sets the underlying data.
        /// </summary>
        public object? Value { get; set; }

        /// <summary>
        /// Copies the <see cref="EventData"/> (including the <see cref="Value"/>) creating a new instance.
        /// </summary>
        /// <returns>A new <see cref="EventData"/> instance.</returns>
        public EventData Copy() => new(this) { Value = Value };
    }
}