// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Events;
using CoreEx.Json;
using System;

namespace CoreEx.Newtonsoft.Json
{
    /// <summary>
    /// Provides the <see cref="JsonSerializer"/>-based <see cref="IEventSerializer"/>.
    /// </summary>
    public class EventDataSerializer : EventDataSerializerBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventDataSerializer"/> class.
        /// </summary>
        /// <param name="jsonSerializer">The <see cref="IJsonSerializer"/>.</param>
        /// <param name="eventDataFormatter">The <see cref="Events.EventDataFormatter"/>.</param>
        public EventDataSerializer(IJsonSerializer? jsonSerializer = null, EventDataFormatter? eventDataFormatter = null) : base(jsonSerializer ?? new JsonSerializer(), eventDataFormatter)
        {
            if (JsonSerializer is not CoreEx.Newtonsoft.Json.JsonSerializer)
                throw new ArgumentException($"The {nameof(IJsonSerializer)} instance must be of Type '{typeof(CoreEx.Newtonsoft.Json.JsonSerializer).FullName}'.", nameof(jsonSerializer));
        }
    }
}