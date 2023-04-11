// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Events
{
    /// <summary>
    /// Enables conversion between <see cref="EventData"/> and messaging sub-system type.
    /// </summary>
    public interface IEventDataConverter
    {
        /// <summary>
        /// Converts the <paramref name="event"/> to a messaging sub-system value.
        /// </summary>
        /// <param name="event">The <see cref="EventData"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The messaging sub-system value.</returns>
        Task<object> ConvertToAsync(EventData @event, CancellationToken cancellationToken);

        /// <summary>
        /// Converts from a  messaging sub-system value to an <see cref="EventData"/>. 
        /// </summary>
        /// <param name="message">The messaging sub-system value.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="EventData"/>.</returns>
        public Task<EventData> ConvertFromAsync(object message, CancellationToken cancellationToken) => ConvertFromAsync(message, null, cancellationToken);

        /// <summary>
        /// Converts from a  messaging sub-system value to an <see cref="EventData"/> or <see cref="EventData{T}"/> depending on <paramref name="valueType"/>. 
        /// </summary>
        /// <param name="message">The messaging sub-system value.</param>
        /// <param name="valueType">The <see cref="EventData{T}.Value"/> <see cref="Type"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="EventData"/> or <see cref="EventData{T}"/>.</returns>
        Task<EventData> ConvertFromAsync(object message, Type? valueType, CancellationToken cancellationToken);

        /// <summary>
        /// Converts from a  messaging sub-system value to an <see cref="EventData{T}"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="EventData{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="message">The messaging sub-system value.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="EventData{T}"/>.</returns>
        Task<EventData<T>> ConvertFromAsync<T>(object message, CancellationToken cancellationToken);
    }
}