// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Events.Attachments;
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
        /// Gets the <see cref="Events.EventDataFormatter"/>.
        /// </summary>
        EventDataFormatter EventDataFormatter { get; }

        /// <summary>
        /// Gets or sets the optional <see cref="IAttachmentStorage"/> to use for an <see cref="EventAttachment"/>.
        /// </summary>
        public IAttachmentStorage? AttachmentStorage { get; set; }

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

        /// <summary>
        /// Deserializes the <see cref="BinaryData"/> to a specified <see cref="EventData"/> <see cref="Type"/>.
        /// </summary>
        /// <param name="eventData">The event <see cref="BinaryData"/>.</param>
        /// <param name="valueType">The <see cref="EventData.Value"/> <see cref="Type"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="EventData"/>.</returns>
        Task<EventData> DeserializeAsync(BinaryData eventData, Type valueType, CancellationToken cancellationToken = default);
    }
}