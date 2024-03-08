// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Json;
using System;
using System.Collections.Generic;

namespace CoreEx.Events
{
    /// <summary>
    /// Enables the adding of <see cref="CustomEventSerializer"/> for specific <see cref="EventData.Value"/> types to customize the serialization.
    /// </summary>
    /// <remarks>This allows the JSON to be manipulated before it is sent; for example, to remove properties and/or mask content where applicable.</remarks>
    public class CustomEventSerializers
    {
        private readonly Dictionary<Type, CustomEventSerializer> _serializers = [];

        /// <summary>
        /// Adds a serializer for the specified <see cref="EventData.Value"/> type.
        /// </summary>
        /// <typeparam name="T">The <see cref="EventData.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="serializer">The <see cref="CustomEventSerializer"/> to be used to perform the <see cref="BinaryData"/> serialization.</param>
        /// <returns>The <see cref="CustomEventSerializers"/> to enable fluent-style method-chaining.</returns>
        public CustomEventSerializers Add<T>(CustomEventSerializer serializer)
        {
            _serializers.Add(typeof(T), serializer);
            return this;
        }

        /// <summary>
        /// Serialize the <paramref name="event"/> (<see cref="EventData.Value"/>) to JSON <see cref="BinaryData"/>.
        /// </summary>
        /// <param name="event">The <see cref="EventData"/> for serialization.</param>
        /// <param name="jsonSerializer">The <see cref="IJsonSerializer"/> to be used.</param>
        /// <param name="serializeValueOnly">Indicates whether the <see cref="EventData.Value"/> is serialized only (<c>true</c>); or alternatively, the complete <see cref="EventData"/> including all metadata (<c>false</c>).</param>
        /// <returns>The JSON <see cref="BinaryData"/>.</returns>
        public virtual BinaryData SerializeToBinaryData(EventData @event, IJsonSerializer jsonSerializer, bool serializeValueOnly)
        {
            @event.ThrowIfNull(nameof(@event));
            jsonSerializer.ThrowIfNull(nameof(jsonSerializer));

            if (@event.Value is not null && _serializers.TryGetValue(@event.Value.GetType(), out var serializer))
                return serializer(@event, jsonSerializer, serializeValueOnly);
            else
                return DefaultEventSerializer(@event, jsonSerializer, serializeValueOnly);
        }

        /// <summary>
        /// Provides the default event serialization.
        /// </summary>
        public static CustomEventSerializer DefaultEventSerializer { get; } = (@event, jsonSerializer, serializeValueOnly) =>
        {
            @event.ThrowIfNull(nameof(@event));
            jsonSerializer.ThrowIfNull(nameof(jsonSerializer));

            return serializeValueOnly ? jsonSerializer.SerializeToBinaryData(@event.Value) : jsonSerializer.SerializeToBinaryData(@event);
        };
    }

    /// <summary>
    /// Represents the method that provides the custom event serialization.
    /// </summary>
    /// <param name="event">The <see cref="EventData"/> for serialization.</param>
    /// <param name="jsonSerializer">The <see cref="IJsonSerializer"/> to be used.</param>
    /// <param name="serializeValueOnly">Indicates whether the <see cref="EventData.Value"/> is serialized only (<c>true</c>); or alternatively, the complete <see cref="EventData"/> including all metadata (<c>false</c>).</param>
    /// <returns></returns>
    public delegate BinaryData CustomEventSerializer(EventData @event, IJsonSerializer jsonSerializer, bool serializeValueOnly);
}