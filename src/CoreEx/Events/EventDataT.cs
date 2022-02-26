// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;

namespace CoreEx.Events
{
    /// <summary>
    /// Represents the typed event data.
    /// </summary>
    /// <typeparam name="T">The <see cref="Data"/> <see cref="Type"/>.</typeparam>
    public class EventData<T> : EventData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventData"/> class.
        /// </summary>
        public EventData() : base() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventData"/> class copying from another <paramref name="event"/> per the <paramref name="propertySelection"/> (excludes <see cref="Data"/>).
        /// </summary>
        /// <param name="event">The <paramref name="event"/> to copy from.</param>
        /// <param name="propertySelection">The <see cref="EventDataProperty"/> selection.</param>
        /// <remarks>Does not copy the underlying <see cref="Data"/>; this must be set explicitly.</remarks>
        public EventData(EventData @event, EventDataProperty propertySelection = EventDataProperty.All) : base(@event, propertySelection) { }

        /// <summary>
        /// Gets or sets the event data.
        /// </summary>
        public new T Data { get => (T)base.Data!; set => base.Data = value; }

        /// <summary>
        /// Copies the <see cref="EventData"/> per the <paramref name="propertySelection"/> (including the <see cref="Data"/>) creating a new instance.
        /// </summary>
        /// <param name="propertySelection">The <see cref="EventDataProperty"/> selection.</param>
        /// <returns></returns>
        public new EventData<T> Copy(EventDataProperty propertySelection = EventDataProperty.All) => new(this, propertySelection) { Data = Data };
    }
}