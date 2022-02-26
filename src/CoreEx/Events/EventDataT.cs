// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Text.Json.Serialization;

namespace CoreEx.Events
{
    /// <summary>
    /// Represents the <see cref="EventData"/> with a typed <see cref="Value"/>.
    /// </summary>
    /// <typeparam name="T">The <see cref="Value"/> <see cref="Type"/>.</typeparam>
    public class EventData<T> : EventDataBase
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
        /// Gets or sets the event data.
        /// </summary>
        [JsonPropertyName("value")]
        public T Value { get; set; } = default!;

        /// <inheritdoc/>
        public override object? GetValue() => Value;

        /// <summary>
        /// Copies the <see cref="EventData"/> (including the <see cref="Value"/>) creating a new instance.
        /// </summary>
        /// <returns>A new <see cref="EventData"/> instance.</returns>
        public EventData<T> Copy() => new(this) { Value = Value };
    }
}