// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Threading.Tasks;

namespace CoreEx.Events
{
    /// <summary>
    /// Defines an <see cref="EventData"/> serializer.
    /// </summary>
    public interface IEventSerializer
    {
        /// <summary>
        /// Serializes the <see cref="EventData"/> to a <see cref="BinaryData"/>.
        /// </summary>
        /// <param name="event">The <see cref="EventData"/>.</param>
        /// <returns>The event <see cref="BinaryData"/>.</returns>
        Task<BinaryData> SerializeAsync(EventData @event);

        /// <summary>
        /// Deserializes the <see cref="BinaryData"/> to an <see cref="EventData"/>.
        /// </summary>
        /// <param name="eventData">The event <see cref="BinaryData"/>.</param>
        /// <returns>The <see cref="EventData"/>.</returns>
        Task<EventData> DeserializeAsync(BinaryData eventData);

        /// <summary>
        /// Deserializes the <see cref="BinaryData"/> to an <see cref="EventData{T}"/>.
        /// </summary>
        /// <param name="eventData">The event <see cref="BinaryData"/>.</param>
        /// <returns>The <see cref="EventData{T}"/>.</returns>
        Task<EventData<T>> DeserializeAsync<T>(BinaryData eventData);
    }
}