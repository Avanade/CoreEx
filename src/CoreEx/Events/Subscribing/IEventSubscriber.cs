// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Threading.Tasks;
using System.Threading;
using CoreEx.Results;

namespace CoreEx.Events.Subscribing
{
    /// <summary>
    /// Defines the core event subscriber capabilities.
    /// </summary>
    public interface IEventSubscriber : IErrorHandling
    {
        /// <summary>
        /// Gets the <see cref="EventData"/> or <see cref="EventData{T}"/> <see cref="Type"/>.
        /// </summary>
        Type EventDataType { get; }

        /// <summary>
        /// Gets the <see cref="EventData.Value"/> <see cref="Type"/> if any.
        /// </summary>
        Type? ValueType { get; }

        /// <summary>
        /// Received and process the subscribed <paramref name="event"/>.
        /// </summary>
        /// <param name="event">The <see cref="EventData"/>.</param>
        /// <param name="args">The <see cref="EventSubscriberArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        Task<Result> ReceiveAsync(EventData @event, EventSubscriberArgs args, CancellationToken cancellationToken);
    }
}