// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Events;

namespace CoreEx.Text.Json
{
    /// <summary>
    /// Provides the <see cref="JsonSerializer"/>-based <see cref="IEventSerializer"/>.
    /// </summary>
    public class EventDataSerializer : EventDataSerializerBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventDataSerializer"/> class.
        /// </summary>
        /// <param name="jsonSerializer">The <see cref="JsonSerializer"/>.</param>
        /// <param name="eventDataFormatter">The <see cref="Events.EventDataFormatter"/>.</param>
        public EventDataSerializer(JsonSerializer jsonSerializer, EventDataFormatter? eventDataFormatter = null) : base(jsonSerializer, eventDataFormatter) { }
    }
}