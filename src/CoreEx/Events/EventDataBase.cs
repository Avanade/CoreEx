// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace CoreEx.Events
{
    /// <summary>
    /// Provides the base event data.
    /// </summary>
    public abstract class EventDataBase : IIdentifier<string>, ITenantId, IPartitionKey, IETag
    {
        private IDictionary<string, object?>? _internal;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventData"/> class.
        /// </summary>
        protected EventDataBase() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventDataBase"/> class copying the metadata from another <paramref name="event"/>.
        /// </summary>
        /// <param name="event">The <paramref name="event"/> to copy the metadata from.</param>
        protected EventDataBase(EventDataBase @event) => CopyMetadata(@event);

        /// <summary>
        /// Gets or sets the unique event identifier.
        /// </summary>
        public string? Id { get; set; }

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
        public DateTimeOffset? Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the event correlation identifier.
        /// </summary>
        public string? CorrelationId { get; set; }

        /// <summary>
        /// Gets or sets the event key. 
        /// </summary>
        public string? Key { get; set; }

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
        /// Gets or sets the list of extended/additional attributes to be published/sent.
        /// </summary>
        /// <remarks>The key and value are both of <see cref="System.Type"/> <see cref="string"/> to ensure that the dictionary can be serialized/deserialized consistently where required.
        /// <para>It is recommeded to use the <see cref="Internal"/> for data not intended for publishing/sending purposes.</para></remarks>
        public IDictionary<string, string>? Attributes { get; set; }

        /// <summary>
        /// Indicates whether there are any items within the <see cref="Attributes"/> dictionary.
        /// </summary>
        [JsonIgnore()]
        public bool HasAttributes => Attributes != null && Attributes.Count > 0;

        /// <summary>
        /// Adds a new attribute to the <see cref="Attributes"/>.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public void AddAttribute(string key, string value) => (Attributes ??= new Dictionary<string, string>()).Add(key, value ?? throw new ArgumentNullException(nameof(value)));

        /// <summary>
        /// Determines whether the <see cref="Attributes"/> contain an attribute with the specified <paramref name="key"/>. 
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns><c>true</c> indicates that the attribute exists; otherwise, <c>false</c>.</returns>
        public bool HasAttribute(string key) => Attributes is not null && Attributes.ContainsKey(key);

        /// <summary>
        /// Gets the <see cref="Attributes"/> <paramref name="value"/> associated with the specified <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value where exists; otherwise, <c>null</c>.</param>
        /// <returns><c>true</c> indicates that the attribute exists; otherwise, <c>false</c>.</returns>
        public bool TryGetAttribute(string key, [NotNullWhen(true)] out string? value)
        {
            if (Attributes is null)
            {
                value = null;
                return false;
            }

            return Attributes.TryGetValue(key, out value);
        }

        /// <summary>
        /// Gets the internal properties; note that these are for internal storage pre-publishing and sending; and therefore are not automatically published.
        /// </summary>
        /// <remarks>It is recommened to use the <see cref="Attributes"/> for the purposes of publishing and sending of additional data.</remarks>
        [JsonIgnore()]
        public IDictionary<string, object?> Internal => _internal ??= new Dictionary<string, object?>();

        /// <summary>
        /// Indicates whether there are any items within the <see cref="Internal"/> dictionary.
        /// </summary>
        [JsonIgnore()]
        public bool HasInternal => _internal != null && _internal.Count > 0;

        /// <summary>
        /// Copies the metadata from the specified <paramref name="event"/> replacing existing.
        /// </summary>
        /// <param name="event">The <see cref="EventDataBase"/> to copy from.</param>
        public void CopyMetadata(EventDataBase @event)
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
            Key = @event.Key;

            if (@event.HasAttributes)
            {
                Attributes ??= new Dictionary<string, string>();
                Attributes.Clear();
                foreach (var attribute in @event.Attributes!)
                    Attributes.Add(attribute.Key, attribute.Value);
            }
            else
                Attributes?.Clear();

            if (@event.HasInternal)
            {
                _internal ??= new Dictionary<string, object?>(@event.Internal);
                _internal.Clear();
                foreach (var attribute in @event.Internal)
                    _internal.Add(attribute.Key, attribute.Value);
            }
            else
                _internal?.Clear();
        }
    }
}