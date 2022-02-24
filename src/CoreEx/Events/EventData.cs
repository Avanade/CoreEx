// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;

namespace CoreEx.Events
{
    /// <summary>
    /// Represents the core event data.
    /// </summary>
    public class EventData : IIdentifier<string?>, ITenantId, IPartitionKey
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventData"/> class.
        /// </summary>
        public EventData() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventData"/> class copying from another <paramref name="event"/> <i>excluding</i> the underlying <see cref="Data"/> (which must be set explicitly).
        /// </summary>
        public EventData(EventData @event)
        {
            Id = (@event ?? throw new ArgumentNullException(nameof(@event))).Id;
            Subject = @event.Subject;
            Action = @event.Action;
            Type = @event.Type;
            Source = @event.Source;
            Timestamp = @event.Timestamp;
            CorrelationId = @event.CorrelationId;
            TenantId = @event.TenantId;
            PartitionKey = @event.PartitionKey;
        }

        /// <summary>
        /// Gets or sets the unique event identifier.
        /// </summary>
        /// <remarks>Defaults to the <see cref="string"/> representation of a <see cref="Guid.NewGuid"/>.</remarks>
        public string? Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Gets or sets the event subject.
        /// </summary>
        /// <remarks>This is the core subject. Often this will be the name (noun) of the entity being published.</remarks>
        public string? Subject { get; set; }

        /// <summary>
        /// Gets or sets the event action.
        /// </summary>
        /// <remarks>This is the action or command (verb) related to the <see cref="Subject"/>.</remarks>
        public string? Action { get; set; }

        /// <summary>
        /// Gets or sets the event type.
        /// </summary>
        /// <remarks>This describes the type of occurrence which has happened. Often this attribute is used for routing, observability, policy enforcement, etc.</remarks>
        public string? Type { get; set; }

        /// <summary>
        /// Gets or sets the event source.
        /// </summary>
        /// <remarks>This describes the event producer. Often this will include information such as the type of the event source, the organization publishing the event, the process that produced the event, and some unique identifiers.</remarks>
        public Uri? Source { get; set; }

        /// <summary>
        /// Gets or sets the event timestamp.
        /// </summary>
        /// <remarks>Defaults to <see cref="DateTimeOffset.UtcNow"/>.</remarks>
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Gets or sets the event correlation identifier.
        /// </summary>
        public string? CorrelationId { get; set; }

        /// <summary>
        /// Gets or sets the tenant identifier.
        /// </summary>
        public string? TenantId { get; set; }

        /// <summary>
        /// Gets or sets the partition key.
        /// </summary>
        public string? PartitionKey { get; set; }

        /// <summary>
        /// Gets or sets the underlying data.
        /// </summary>
        public object? Data { get; set; }
    }
}