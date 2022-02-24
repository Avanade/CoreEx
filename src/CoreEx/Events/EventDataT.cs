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
        /// Gets or sets the event data.
        /// </summary>
        public new T Data { get => (T)base.Data!; set => base.Data = value; }
    }
}