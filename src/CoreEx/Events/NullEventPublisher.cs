// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

namespace CoreEx.Events
{
    /// <summary>
    /// Represents a <c>null</c> event publisher; whereby the events are simply swallowed/discarded on send.
    /// </summary>
    public class NullEventPublisher : EventPublisher
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NullEventPublisher"/> class.
        /// </summary>
        public NullEventPublisher() : base(null, new Text.Json.EventDataSerializer(), new NullEventSender()) { }
    }
}