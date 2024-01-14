// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CloudNative.CloudEvents;
using CoreEx.Events.Attachments;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Events
{
    /// <summary>
    /// Provides the base <see cref="CloudEvent"/> <see cref="IEventSerializer"/> capabilities.
    /// </summary>
    public abstract class CloudEventSerializerBase : IEventSerializer
    {
        private const string SubjectName = "subject";
        private const string ActionName = "action";
        private const string CorrelationIdName = "correlationid";
        private const string PartitionKeyName = "partitionkey";
        private const string TenantIdName = "tenantid";
        private const string ETagName = "etag";
        private const string KeyName = "key";

        /// <summary>
        /// Gets the list of reserved attribute names.
        /// </summary>
        /// <remarks>The reserved names are as follows: '<c>id</c>', '<c>time</c>', '<c>type</c>', '<c>source</c>', '<c>subject</c>', '<c>action</c>', '<c>correlationid</c>', '<c>tenantid</c>', '<c>etag</c>', '<c>partitionkey</c>', '<c>key</c>'. Also,
        /// an attribute name must consist of lowercase letters and digits only; any that contain other characters will be ignored.</remarks>
        public static string[] ReservedNames { get; } = new string[] { "id", "time", "type", "source", SubjectName, ActionName, CorrelationIdName, TenantIdName, ETagName, PartitionKeyName, KeyName };

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudEventSerializerBase"/> class.
        /// </summary>
        /// <param name="eventDataFormatter">The <see cref="Events.EventDataFormatter"/>.</param>
        protected CloudEventSerializerBase(EventDataFormatter? eventDataFormatter) => EventDataFormatter = eventDataFormatter ?? new EventDataFormatter();

        /// <inheritdoc/>
        public EventDataFormatter EventDataFormatter { get; }

        /// <inheritdoc/>
        public IAttachmentStorage? AttachmentStorage { get; set; }

        /// <inheritdoc/>
        public async Task<EventData> DeserializeAsync(BinaryData eventData, CancellationToken cancellationToken = default)
        {
            CloudEvent ce;
            var @event = new EventData();
            if (AttachmentStorage is null)
            {
                ce = await DecodeAsync(eventData, cancellationToken).ConfigureAwait(false);
                @event.Value = ce.Data;
            }
            else
            {
                ce = await DecodeAsync<EventAttachment>(eventData, cancellationToken).ConfigureAwait(false);
                if (ce.Data is not null && ce.Data is EventAttachment attachment && !attachment.IsEmpty)
                {
                    var val = await AttachmentStorage.ReadAync(attachment, cancellationToken).ConfigureAwait(false)!;
                    @event.Value = EventDataFormatter.JsonSerializer!.Deserialize(val)!;
                }
                else
                {
                    ce = await DecodeAsync(eventData, cancellationToken).ConfigureAwait(false);
                    @event.Value = ce.Data;
                }
            }

            DeserializeFromCloudEvent(ce, @event);
            return @event;
        }

        /// <inheritdoc/>
        public async Task<EventData<T>> DeserializeAsync<T>(BinaryData eventData, CancellationToken cancellationToken = default)
        {
            CloudEvent ce;
            var @event = new EventData<T>();
            if (AttachmentStorage is null)
            {
                ce = await DecodeAsync<T>(eventData, cancellationToken).ConfigureAwait(false);
                @event.Value = (T)ce.Data!;
            }
            else
            {
                ce = await DecodeAsync<EventAttachment>(eventData, cancellationToken).ConfigureAwait(false);
                if (ce.Data is not null && ce.Data is EventAttachment attachment && !attachment.IsEmpty)
                {
                    var val = await AttachmentStorage.ReadAync(attachment, cancellationToken).ConfigureAwait(false)!;
                    @event.Value = EventDataFormatter.JsonSerializer!.Deserialize<T>(val)!;
                }
                else
                {
                    ce = await DecodeAsync<T>(eventData, cancellationToken).ConfigureAwait(false);
                    @event.Value = (T)ce.Data!;
                }
            }

            DeserializeFromCloudEvent(ce, @event);
            return @event;
        }

        /// <inheritdoc/>
        public async Task<EventData> DeserializeAsync(BinaryData eventData, Type valueType, CancellationToken cancellationToken = default)
        {
            if (valueType == null)
                throw new ArgumentNullException(nameof(valueType));

            var mi = GetType().GetMethod(nameof(DeserializeAsync), 1, new Type[] { typeof(BinaryData), typeof(CancellationToken) })!;
            dynamic task = mi.MakeGenericMethod(valueType).Invoke(this, new object[] { eventData, cancellationToken })!;
            return await task.ConfigureAwait(false);
        }

        /// <summary>
        /// Deserializes from the <paramref name="cloudEvent"/> into the <paramref name="event"/>.
        /// </summary>
        private void DeserializeFromCloudEvent(CloudEvent cloudEvent, EventData @event)
        {
            @event.Id = cloudEvent.Id;
            @event.Timestamp = cloudEvent.Time;
            @event.Type = cloudEvent.Type;
            @event.Source = cloudEvent.Source;

            if (TryGetExtensionAttribute(cloudEvent, SubjectName, out string? val))
                @event.Subject = val;

            if (TryGetExtensionAttribute(cloudEvent, ActionName, out val))
                @event.Action = val;

            if (TryGetExtensionAttribute(cloudEvent, CorrelationIdName, out val))
                @event.CorrelationId = val;
            else
                @event.CorrelationId = null;

            if (TryGetExtensionAttribute(cloudEvent, PartitionKeyName, out val))
                @event.PartitionKey = val;
            
            if (TryGetExtensionAttribute(cloudEvent, TenantIdName, out val))
                @event.TenantId = val;

            if (TryGetExtensionAttribute(cloudEvent, ETagName, out val))
                @event.ETag = val;

            if (TryGetExtensionAttribute(cloudEvent, KeyName, out val))
                @event.Key = val;

            foreach (var att in cloudEvent.ExtensionAttributes)
            {
                if (!ReservedNames.Contains(att.Name) && TryGetExtensionAttribute(cloudEvent, att.Name, out val))
                    @event.AddAttribute(att.Name, val);
            }

            OnDeserialize(cloudEvent, @event);
        }

        /// <summary>
        /// Invoked after the standard <see cref="EventData"/> properties have been updated from the <see cref="CloudEvent"/> to enable further customization where required.
        /// </summary>
        /// <param name="event">The source <see cref="EventData"/>.</param>
        /// <param name="cloudEvent">The corresponding <see cref="CloudEvent"/>.</param>
        protected virtual void OnDeserialize(CloudEvent cloudEvent, EventData @event) { }

        /// <summary>
        /// Decodes (deserializes) the JSON <paramref name="eventData"/> into a <see cref="CloudEvent"/>.
        /// </summary>
        /// <returns>The <see cref="CloudEvent"/>.</returns>
        protected abstract Task<CloudEvent> DecodeAsync(BinaryData eventData, CancellationToken cancellation);

        /// <summary>
        /// Decodes (deserializes) the typed <paramref name="eventData"/> into a <see cref="CloudEvent"/>.
        /// </summary>
        /// <returns>The <see cref="CloudEvent"/>.</returns>
        protected abstract Task<CloudEvent> DecodeAsync<T>(BinaryData eventData, CancellationToken cancellation);

        /// <inheritdoc/>
        public Task<BinaryData> SerializeAsync(EventData @event, CancellationToken cancellationToken = default)
        {
            if (@event == null)
                throw new ArgumentNullException(nameof(@event));

            @event = @event.Copy();
            return SerializeToCloudEventAsync(@event, cancellationToken);
        }

        /// <inheritdoc/>
        public Task<BinaryData> SerializeAsync<T>(EventData<T> @event, CancellationToken cancellationToken = default)
        {
            if (@event == null)
                throw new ArgumentNullException(nameof(@event));

            @event = @event.Copy();
            return SerializeToCloudEventAsync(@event, cancellationToken);
        }

        /// <summary>
        /// Serializes the <paramref name="event"/>.
        /// </summary>
        private async Task<BinaryData> SerializeToCloudEventAsync(EventData @event, CancellationToken cancellationToken)
        {
            EventDataFormatter.Format(@event);

            var ce = new CloudEvent
            {
                Id = @event.Id,
                Time = @event.Timestamp,
                Type = @event.Type ?? throw new InvalidOperationException("CloudEvents must have a Type; the EventDataFormatter should be updated to set."),
                Source = @event.Source ?? throw new InvalidOperationException("CloudEvents must have a Source; the EventDataFormatter should be updated to set.")
            };

            SetExtensionAttribute(ce, SubjectName, @event.Subject);
            SetExtensionAttribute(ce, ActionName, @event.Action);
            SetExtensionAttribute(ce, CorrelationIdName, @event.CorrelationId);
            SetExtensionAttribute(ce, PartitionKeyName, @event.PartitionKey);
            SetExtensionAttribute(ce, TenantIdName, @event.TenantId);
            SetExtensionAttribute(ce, ETagName, @event.ETag);
            SetExtensionAttribute(ce, KeyName, @event.Key);

            if (@event.Attributes != null)
            {
                foreach (var att in @event.Attributes.Where(x => !string.IsNullOrEmpty(x.Key) && x.Key.All(c => char.IsLetterOrDigit(c))))
                {
                    SetExtensionAttribute(ce, EventDataFormatter.TextInfo.ToLower(att.Key), att.Value);
                }
            }

            OnSerialize(@event, ce);

            if (@event.Value is not null)
            {
                ce.DataContentType = MediaTypeNames.Application.Json;
                ce.Data = @event.Value;

                // Where attachments are supported, check the size of the data and write to the attachment storage if required.
                if (AttachmentStorage != null)
                {
                    var data = EventDataFormatter.JsonSerializer!.SerializeToBinaryData(@event.Value);
                    if (data.ToMemory().Length >= AttachmentStorage!.MaxDataSize)
                        ce.Data = await AttachmentStorage.WriteAsync(@event, data, cancellationToken).ConfigureAwait(false);
                }
            }

            return await EncodeAsync(ce, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Encodes (serializes) the <paramref name="cloudEvent"/> into a <see cref="BinaryData"/>.
        /// </summary>
        /// <param name="cloudEvent">The <see cref="CloudEvent"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The resulting <see cref="BinaryData"/>.</returns>
        protected abstract Task<BinaryData> EncodeAsync(CloudEvent cloudEvent, CancellationToken cancellationToken); 

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
        private static bool TryGetExtensionAttribute<T>(CloudEvent ce, string name, [NotNullWhen(true)] out T? value)
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