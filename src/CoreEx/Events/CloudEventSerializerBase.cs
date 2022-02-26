// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CloudNative.CloudEvents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;

namespace CoreEx.Events
{
    /// <summary>
    /// Provides the base <see cref="CloudEvent"/> <see cref="IEventSerializer"/> capabilities.
    /// </summary>
    public abstract class CloudEventSerializerBase : IEventSerializer
    {
        /// <summary>
        /// Gets the list of reserved attribute names.
        /// </summary>
        public static string[] ReservedNames { get; } = new string[] { "id", "time", "type", "source", "subject", "action", "correlationid", "tenantid", "etag", "partitionkey" };

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudEventSerializerBase"/> class.
        /// </summary>
        /// <param name="eventDataFormatter">The <see cref="Events.EventDataFormatter"/>.</param>
        protected CloudEventSerializerBase(EventDataFormatter? eventDataFormatter) => EventDataFormatter = eventDataFormatter ?? new EventDataFormatter();

        /// <summary>
        /// Gets the <see cref="Events.EventDataFormatter"/>.
        /// </summary>
        public EventDataFormatter EventDataFormatter { get; }

        /// <inheritdoc/>
        public async Task<EventData> DeserializeAsync(BinaryData eventData)
        {
            var ce = await DecodeAsync(eventData).ConfigureAwait(false);
            var @event = new EventData { Value = ce.Data };
            DeserializeFromCloudEvent(ce, @event);
            return @event;
        }

        /// <inheritdoc/>
        public async Task<EventData<T>> DeserializeAsync<T>(BinaryData eventData)
        {
            var ce = await DecodeAsync<T>(eventData).ConfigureAwait(false);
            var @event = new EventData<T> { Value = (T)ce.Data! };
            DeserializeFromCloudEvent(ce, @event);
            return @event;
        }

        /// <summary>
        /// Deserializes from the <paramref name="cloudEvent"/> into the <paramref name="event"/>.
        /// </summary>
        private void DeserializeFromCloudEvent(CloudEvent cloudEvent, EventDataBase @event)
        {
            @event.Id = cloudEvent.Id;
            @event.Timestamp = cloudEvent.Time;
            @event.Type = cloudEvent.Type;
            @event.Source = cloudEvent.Source;

            if (TryGetExtensionAttribute(cloudEvent, "subject", out string val))
                @event.Subject = val;

            if (TryGetExtensionAttribute(cloudEvent, "action", out val))
                @event.Action = val;

            if (TryGetExtensionAttribute(cloudEvent, "correlationid", out val))
                @event.CorrelationId = val;
            else
                @event.CorrelationId = null;

            if (TryGetExtensionAttribute(cloudEvent, "partitionkey", out val))
                @event.PartitionKey = val;
            
            if (TryGetExtensionAttribute(cloudEvent, "tenantid", out val))
                @event.TenantId = val;

            if (TryGetExtensionAttribute(cloudEvent, "etag", out val))
                @event.ETag = val;

            foreach (var att in cloudEvent.ExtensionAttributes)
            {
                if (!ReservedNames.Contains(att.Name))
                {
                    if (@event.Attributes == null)
                        @event.Attributes = new Dictionary<string, string>();

                    TryGetExtensionAttribute(cloudEvent, att.Name, out val);
                    @event.Attributes.Add(att.Name, val);
                }
            }

            OnDeserialize(cloudEvent, @event);
        }

        /// <summary>
        /// Invoked after the standard <see cref="EventData"/> properties have been updated from the <see cref="CloudEvent"/> to enable further customization where required.
        /// </summary>
        /// <param name="event">The source <see cref="EventData"/>.</param>
        /// <param name="cloudEvent">The corresponding <see cref="CloudEvent"/>.</param>
        protected virtual void OnDeserialize(CloudEvent cloudEvent, EventDataBase @event) { }

        /// <summary>
        /// Decodes (deserializes) the JSON <paramref name="eventData"/> into a <see cref="CloudEvent"/>.
        /// </summary>
        /// <returns>The <see cref="CloudEvent"/>.</returns>
        protected abstract Task<CloudEvent> DecodeAsync(BinaryData eventData);

        /// <summary>
        /// Decodes (deserializes) the typed <paramref name="eventData"/> into a <see cref="CloudEvent"/>.
        /// </summary>
        /// <returns>The <see cref="CloudEvent"/>.</returns>
        protected abstract Task<CloudEvent> DecodeAsync<T>(BinaryData eventData);

        /// <inheritdoc/>
        public Task<BinaryData> SerializeAsync(EventData @event)
        {
            if (@event == null)
                throw new ArgumentNullException(nameof(@event));

            @event = @event.Copy();
            return SerializeToCloudEventAsync(@event);
        }

        /// <inheritdoc/>
        public Task<BinaryData> SerializeAsync<T>(EventData<T> @event)
        {
            if (@event == null)
                throw new ArgumentNullException(nameof(@event));

            @event = @event.Copy();
            return SerializeToCloudEventAsync(@event);
        }

        /// <summary>
        /// Serializes the <paramref name="event"/>.
        /// </summary>
        private async Task<BinaryData> SerializeToCloudEventAsync(EventDataBase @event)
        {
            EventDataFormatter.Format(@event);

            var ce = new CloudEvent
            {
                Id = @event.Id,
                Time = @event.Timestamp,
                Type = @event.Type,
                Source = @event.Source
            };

            SetExtensionAttribute(ce, "subject", @event.Subject);
            SetExtensionAttribute(ce, "action", @event.Action);
            SetExtensionAttribute(ce, "correlationid", @event.CorrelationId);
            SetExtensionAttribute(ce, "partitionkey", @event.PartitionKey);
            SetExtensionAttribute(ce, "tenantid", @event.TenantId);
            SetExtensionAttribute(ce, "etag", @event.ETag);

            if (@event.Attributes != null)
            {
                foreach (var att in @event.Attributes)
                {
                    SetExtensionAttribute(ce, att.Key, att.Value);
                }
            }

            OnSerialize(@event, ce);

            ce.DataContentType = MediaTypeNames.Application.Json;
            ce.Data = @event.GetValue();

            return await EncodeAsync(ce).ConfigureAwait(false);
        }

        /// <summary>
        /// Encodes (serializes) the <paramref name="cloudEvent"/> into <see cref="BinaryData"/>.
        /// </summary>
        /// <param name="cloudEvent"></param>
        /// <returns></returns>
        protected abstract Task<BinaryData> EncodeAsync(CloudEvent cloudEvent); 

        /// <summary>
        /// Invoked after the standard <see cref="EventData"/> properties have been updated to the <see cref="CloudEvent"/> to enable further customization where required.
        /// </summary>
        /// <param name="event">The source <see cref="EventDataBase"/>.</param>
        /// <param name="cloudEvent">The corresponding <see cref="CloudEvent"/>.</param>
        protected virtual void OnSerialize(EventDataBase @event, CloudEvent cloudEvent) { }

        /// <summary>
        /// Sets the <see cref="CloudEvent"/> extension attribute where not default value.
        /// </summary>
        /// <param name="ce">The <see cref="CloudEvent"/>.</param>
        /// <param name="name">The attribute name.</param>
        /// <param name="value">The attribute value.</param>
        protected static void SetExtensionAttribute<T>(CloudEvent ce, string name, T value)
        {
            if (Comparer<T>.Default.Compare(value, default!) == 0)
                return;

            ce[name] = value;
        }

        /// <summary>
        /// Gets the <see cref="CloudEvent"/> extension attribute value.
        /// </summary>
        /// <param name="ce">The <see cref="CloudEvent"/>.</param>
        /// <param name="name">The attribute name.</param>
        /// <param name="value">The attribute value.</param>
        /// <returns><c>true</c> indicates that the extension attribute exists; otherwise, <c>false</c>.</returns>
        private static bool TryGetExtensionAttribute<T>(CloudEvent ce, string name, out T value)
        {
            value = default!;
            var val = ce[name];
            if (val == null)
                return false;

            value = (T)val;
            return true;
        }
    }
}