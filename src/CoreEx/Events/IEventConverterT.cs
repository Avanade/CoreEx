// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Events
{
    /// <summary>
    /// Enables conversion between <see cref="EventData"/> and <typeparamref name="TMessage"/> type.
    /// </summary>
    /// <typeparam name="TMessage">The messaging sub-system <see cref="Type"/>.</typeparam>
    public interface IEventDataConverter<TMessage> : IEventDataConverter where TMessage : class
    {
        /// <inheritdoc/>
        async Task<object> IEventDataConverter.ConvertToAsync(EventData @event, CancellationToken cancellationToken) => await ConvertToAsync(@event, cancellationToken).ConfigureAwait(false);

        /// <inheritdoc/>
        Task<EventData> IEventDataConverter.ConvertFromAsync(object message, CancellationToken cancellationToken) => ConvertFromAsync(message, null, cancellationToken);

        /// <inheritdoc/>
        Task<EventData> IEventDataConverter.ConvertFromAsync(object message, Type? valueType, CancellationToken cancellationToken) => ConvertFromAsync((TMessage)message, valueType, cancellationToken);

        /// <inheritdoc/>
        Task<EventData<T>> IEventDataConverter.ConvertFromAsync<T>(object message, CancellationToken cancellationToken) => ConvertFromAsync<T>((TMessage)message, cancellationToken);

        /// <summary>
        /// Converts the <paramref name="event"/> to a <typeparamref name="TMessage"/>.
        /// </summary>
        /// <param name="event">The <see cref="EventData"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <typeparamref name="TMessage"/> value.</returns>
        new Task<TMessage> ConvertToAsync(EventData @event, CancellationToken cancellationToken);

        /// <summary>
        /// Converts from a <typeparamref name="TMessage"/> to an <see cref="EventData"/> value. 
        /// </summary>
        /// <param name="message">The <typeparamref name="TMessage"/> value.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="EventData"/> value.</returns>
        public Task<EventData> ConvertFromAsync(TMessage message, CancellationToken cancellationToken) => ConvertFromAsync(message, null, cancellationToken);

        /// <summary>
        /// Converts from a <typeparamref name="TMessage"/> to an <see cref="EventData"/> or <see cref="EventData{T}"/> value depending on <paramref name="valueType"/>. 
        /// </summary>
        /// <param name="message">The <typeparamref name="TMessage"/> value.</param>
        /// <param name="valueType">The <see cref="EventData{T}.Value"/> <see cref="Type"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="EventData"/> or <see cref="EventData{T}"/> value.</returns>
        Task<EventData> ConvertFromAsync(TMessage message, Type? valueType, CancellationToken cancellationToken);

        /// <summary>
        /// Converts from a <typeparamref name="TMessage"/> to an <see cref="EventData{T}"/> value.
        /// </summary>
        /// <typeparam name="T">The <see cref="EventData{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="message">The <typeparamref name="TMessage"/> value.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="EventData{T}"/> value.</returns>
        Task<EventData<T>> ConvertFromAsync<T>(TMessage message, CancellationToken cancellationToken);
    }
}