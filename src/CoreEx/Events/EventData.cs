// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Collections.Generic;

namespace CoreEx.Events
{
    /// <summary>
    /// Represents the core event data with a generic <see cref="object"/> <see cref="Value"/>.
    /// </summary>
    public class EventData : IIdentifier<string?>, ITenantId, IPartitionKey, IETag
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventData"/> class.
        /// </summary>
        public EventData() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventData"/> class copying from another <paramref name="event"/> (excludes <see cref="Value"/>).
        /// </summary>
        /// <param name="event">The <paramref name="event"/> to copy from.</param>
        /// <remarks>Does not copy the underlying <see cref="Value"/>; this must be set explicitly.</remarks>
        public EventData(EventData @event)
        {
            Id = (@event ?? throw new ArgumentNullException(nameof(@event))).Id;
            Timestamp = @event.Timestamp;
            Subject = @event.Subject;
            Action = @event.Action;
            Type = @event.Type;
            Source = @event.Source;
            CorrelationId = @event.CorrelationId;
            TenantId = @event.TenantId;
            PartitionKey = @event.PartitionKey;
            ETag = @event.ETag;

            if (@event.Attributes != null)
            {
                Attributes = new Dictionary<string, string>();
                foreach (var att in @event.Attributes)
                {
                    Attributes.Add(att.Key, att.Value);
                }
            }
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
        public DateTimeOffset? Timestamp { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Gets or sets the event correlation identifier.
        /// </summary>
        /// <remarks>Default to <see cref="Executor.GetCorrelationId"/>.</remarks>
        public string? CorrelationId { get; set; } = Executor.GetCorrelationId();

        /// <summary>
        /// Gets or sets the tenant identifier.
        /// </summary>
        public string? TenantId { get; set; }

        /// <summary>
        /// Gets or sets the partition key.
        /// </summary>
        public string? PartitionKey { get; set; }

        /// <summary>
        /// Gets or sets the entity tag.
        /// </summary>
        public string? ETag { get; set; }

        /// <summary>
        /// Gets or sets the list of extended attributes.
        /// </summary>
        public IDictionary<string, string>? Attributes { get; set; }

        /// <summary>
        /// Gets or sets the underlying data.
        /// </summary>
        public object? Value { get; set; }

        /// <summary>
        /// Copies the <see cref="EventData"/> (including the <see cref="Value"/>) creating a new instance.
        /// </summary>
        /// <returns>A new <see cref="EventData"/> instance.</returns>
        public EventData Copy() => new(this) { Value = Value };
    }
}