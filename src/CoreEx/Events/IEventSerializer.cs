// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Events
{
    /// <summary>
    /// Defines an <see cref="EventData"/> to/from <see cref="BinaryData"/> serializer.
    /// </summary>
    public interface IEventSerializer
    {
        /// <summary>
        /// Serializes the <see cref="EventData"/> to a <see cref="BinaryData"/>.
        /// </summary>
        /// <param name="event">The <see cref="EventData"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The event <see cref="BinaryData"/>.</returns>
        Task<BinaryData> SerializeAsync(EventData @event, CancellationToken cancellationToken = default);

        /// <summary>
        /// Serializes the <see cref="EventData{T}"/> to a <see cref="BinaryData"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="EventData{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="event">The <see cref="EventData{T}"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The event <see cref="BinaryData"/>.</returns>
        Task<BinaryData> SerializeAsync<T>(EventData<T> @event, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deserializes the <see cref="BinaryData"/> to an <see cref="EventData"/>.
        /// </summary>
        /// <param name="eventData">The event <see cref="BinaryData"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="EventData"/>.</returns>
        Task<EventData> DeserializeAsync(BinaryData eventData, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deserializes the <see cref="BinaryData"/> to an <see cref="EventData{T}"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="EventData{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="eventData">The event <see cref="BinaryData"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="EventData{T}"/>.</returns>
        Task<EventData<T>> DeserializeAsync<T>(BinaryData eventData, CancellationToken cancellationToken = default);
    }
}