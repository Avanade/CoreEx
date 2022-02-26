// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CoreEx.Events
{
    /// <summary>
    /// Provides the base event data.
    /// </summary>
    public abstract class EventDataBase : IIdentifier<string?>, ITenantId, IPartitionKey, IETag
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventData"/> class.
        /// </summary>
        protected EventDataBase() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventDataBase"/> class copying from another <paramref name="event"/>.
        /// </summary>
        /// <param name="event">The <paramref name="event"/> to copy from.</param>
        protected EventDataBase(EventDataBase @event)
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
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        /// <summary>
        /// Gets or sets the event subject.
        /// </summary>
        /// <remarks>This is the core subject. Often this will be the name (noun) of the entity being published.</remarks>
        [JsonPropertyName("subject")]
        public string? Subject { get; set; }

        /// <summary>
        /// Gets or sets the event action.
        /// </summary>
        /// <remarks>This is the action or command (verb) related to the <see cref="Subject"/>.</remarks>
        [JsonPropertyName("action")]
        public string? Action { get; set; }

        /// <summary>
        /// Gets or sets the event type.
        /// </summary>
        /// <remarks>This describes the type of occurrence which has happened. Often this attribute is used for routing, observability, policy enforcement, etc.</remarks>
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        /// <summary>
        /// Gets or sets the event source.
        /// </summary>
        /// <remarks>This describes the event producer. Often this will include information such as the type of the event source, the organization publishing the event, the process that produced the event, and some unique identifiers.</remarks>
        [JsonPropertyName("source")]
        public Uri? Source { get; set; }

        /// <summary>
        /// Gets or sets the event timestamp.
        /// </summary>
        [JsonPropertyName("timestamp")]
        public DateTimeOffset? Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the event correlation identifier.
        /// </summary>
        [JsonPropertyName("correlationId")]
        public string? CorrelationId { get; set; }

        /// <summary>
        /// Gets or sets the tenant identifier.
        /// </summary>
        [JsonPropertyName("tenantId")]
        public string? TenantId { get; set; }

        /// <summary>
        /// Gets or sets the partition key.
        /// </summary>
        [JsonPropertyName("partitionKey")]
        public string? PartitionKey { get; set; }

        /// <summary>
        /// Gets or sets the entity tag.
        /// </summary>
        [JsonPropertyName("etag")]
        public string? ETag { get; set; }

        /// <summary>
        /// Gets or sets the list of extended attributes.
        /// </summary>
        [JsonPropertyName("attributes")]
        public IDictionary<string, string>? Attributes { get; set; }

        /// <summary>
        /// Gets the underlying value.
        /// </summary>
        /// <returns>The underlying value.</returns>
        public abstract object? GetValue();
    }
}