// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Events
{
    /// <summary>
    /// Defines the standardized <b>Event</b> publishing and sending (generally encapsulates the <see cref="IEventSerializer"/> and <see cref="IEventSender"/> orchestration). 
    /// </summary>
    /// <remarks>Note to implementers: The <i>Publish*</i> methods should only cache/store the events queue (order must be maintained) to be sent; they should only be sent where <see cref="SendAsync"/> is explicitly requested.
    /// The key reason for queuing the published events it to promote a single atomic send operation; i.e. all events should be sent together, and either succeed or fail together.
    /// <para>The <see cref="EventExtensions.CreateEvent(IEventPublisher, Uri, string?, string?)"/> convenience methods will create an <see cref="EventData"/> or <see cref="EventData{T}"/> formatting the <see cref="EventDataBase.Key"/> (where applicable) using the
    /// <see cref="EventDataFormatter.KeySeparatorCharacter"/>; whilst also, adding the corresponding <see cref="CompositeKey.Args"/> to <see cref="EventDataBase.Internal"/> with a key of '<c>Key</c>'. These can be further 
    /// referenced during formal formatting/publish to add additional context/functionality as required.</para></remarks>
    public interface IEventPublisher
    {
        /// <summary>
        /// Gets the corresponding <see cref="Events.EventDataFormatter"/>.
        /// </summary>
        EventDataFormatter EventDataFormatter { get; }

        /// <summary>
        /// Indicates whether the internal queue is empty.
        /// </summary>
        /// <returns><c>true</c> where empty; otherwise, <c>false</c>.</returns>
        /// <remarks>The queue will not be empty where events have been published but not <see cref="SendAsync">sent</see> or <see cref="Reset">cleared</see>.</remarks>
        bool IsEmpty { get; }

        /// <summary>
        /// Publishes (queues in-process) one or more <see cref="EventData"/> objects ready for <see cref="SendAsync"/>.
        /// </summary>
        /// <param name="events">One or more <see cref="EventData"/> objects to be published.</param>
        /// <returns>The <see cref="IEventPublisher"/> to support fluent-style method-chaining.</returns>
        IEventPublisher Publish(params EventData[] events);

        /// <summary>
        /// Publishes (queues in-process) one or more <see cref="EventData"/> objects for a <paramref name="name">named</paramref> destination (e.g. queue or topic) ready for <see cref="SendAsync"/>.
        /// </summary>
        /// <param name="name">The destination name.</param>
        /// <param name="events">One or more <see cref="EventData"/> objects to be published.</param>
        /// <remarks>The name could represent a queue name or equivalent where appropriate.</remarks>
        /// <returns>The <see cref="IEventPublisher"/> to support fluent-style method-chaining.</returns>
        IEventPublisher PublishNamed(string name, params EventData[] events);

        /// <summary>
        /// Sends all previously published (queued) events.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        Task SendAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Resets by clearing the internal cache/store.
        /// </summary>
        void Reset();
    }
}