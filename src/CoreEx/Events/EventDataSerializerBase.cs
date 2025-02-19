﻿// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Events.Attachments;
using CoreEx.Json;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Events
{
    /// <summary>
    /// Provides the base <see cref="EventData"/> <see cref="IEventSerializer"/> capabilities.
    /// </summary>
    /// <remarks>The <see cref="SerializeValueOnly"/> indicates whether the <see cref="EventData.Value"/> is serialized only (default); or alternatively, the complete <see cref="EventData"/>.</remarks>
    /// <param name="jsonSerializer">The <see cref="IJsonSerializer"/>.</param>
    /// <param name="eventDataFormatter">The <see cref="Events.EventDataFormatter"/>.</param>
    public abstract class EventDataSerializerBase(IJsonSerializer jsonSerializer, EventDataFormatter? eventDataFormatter) : IEventSerializer
    {
        /// <summary>
        /// Gets the <see cref="IJsonSerializer"/>.
        /// </summary>
        public IJsonSerializer JsonSerializer { get; } = jsonSerializer.ThrowIfNull(nameof(jsonSerializer));

        /// <inheritdoc/>
        public EventDataFormatter EventDataFormatter { get; } = eventDataFormatter ?? new EventDataFormatter();

        /// <inheritdoc/>
        public IAttachmentStorage? AttachmentStorage { get; set; }

        /// <inheritdoc/>
        public CustomEventSerializers CustomSerializers { get; } = new();

        /// <summary>
        /// Indicates whether the <see cref="EventData.Value"/> is serialized only (<c>true</c>); or alternatively, the complete <see cref="EventData"/> including all metadata (<c>false</c>).
        /// </summary>
        /// <remarks>Defaults to <c>true</c>.</remarks>
        public bool SerializeValueOnly { get; set; } = true;

        /// <inheritdoc/>
        public async Task<EventData> DeserializeAsync(BinaryData eventData, CancellationToken cancellationToken = default)
        {
            var (Attachment, Data) = await DeserializeAttachmentAsync(eventData, cancellationToken).ConfigureAwait(false);

            if (SerializeValueOnly)
                return new EventData { Value = JsonSerializer.Deserialize(Data) };

            if (AttachmentStorage is null || Attachment is null)
                return JsonSerializer.Deserialize<EventData>(eventData)!;

            return new EventData(DeserializeAsBase(eventData)) { Value = JsonSerializer.Deserialize(Data)! };
        }

        /// <inheritdoc/>
        public async Task<EventData<T>> DeserializeAsync<T>(BinaryData eventData, CancellationToken cancellationToken = default)
        {
            var (Attachment, Data) = await DeserializeAttachmentAsync(eventData, cancellationToken).ConfigureAwait(false);

            if (SerializeValueOnly)
                return new EventData<T> { Value = JsonSerializer.Deserialize<T>(Data)! };

            if (AttachmentStorage is null || Attachment is null)
                return JsonSerializer.Deserialize<EventData<T>>(Data)!;

            return new EventData<T>(DeserializeAsBase(eventData)) { Value = JsonSerializer.Deserialize<T>(Data)! };
        }

        /// <inheritdoc/>
        public async Task<EventData> DeserializeAsync(BinaryData eventData, Type valueType, CancellationToken cancellationToken = default)
        {
            valueType.ThrowIfNull(nameof(valueType));
            var (Attachment, Data) = await DeserializeAttachmentAsync(eventData, cancellationToken).ConfigureAwait(false);
            var edvt = typeof(EventData<>).MakeGenericType(valueType);
            EventData ed;

            if (SerializeValueOnly)
            {
                ed = (EventData)Activator.CreateInstance(edvt)!;
                ed.Value = JsonSerializer.Deserialize(Data, valueType);
                return ed;
            }

            if (AttachmentStorage is null || Attachment is null)
                return (EventData)JsonSerializer.Deserialize(Data, edvt)!;

            ed = (EventData)Activator.CreateInstance(edvt)!;
            ed.CopyMetadata(DeserializeAsBase(eventData));
            ed.Value = JsonSerializer.Deserialize(Data, valueType);
            return ed;
        }

        /// <summary>
        /// Determine whether the event data is an attachment reference; and if so, replace current event data with the attachment contents.
        /// </summary>
        private async Task<(EventAttachment? Attachment, BinaryData Data)> DeserializeAttachmentAsync(BinaryData eventData, CancellationToken cancellationToken)
        {
            EventAttachment? attachment = null;
            var data = eventData;
            if (AttachmentStorage is not null)
            {
                attachment = SerializeValueOnly ? JsonSerializer.Deserialize<EventAttachment>(eventData) : JsonSerializer.Deserialize<EventData<EventAttachment>>(eventData)?.Value;
                if (attachment is not null && !attachment.IsEmpty)
                    data = await AttachmentStorage.ReadAync(attachment, cancellationToken).ConfigureAwait(false);
            }

            return (attachment is not null && attachment.IsEmpty ? null : attachment, data);
        }

        /// <summary>
        /// Deserializes as the base <see cref="EventData"/>.
        /// </summary>
        private EventData DeserializeAsBase(BinaryData eventData)
            => SerializeValueOnly ? new EventData { Value = JsonSerializer.Deserialize(eventData) } : JsonSerializer.Deserialize<EventData>(eventData)!;

        /// <inheritdoc/>
        public Task<BinaryData> SerializeAsync(EventData @event, CancellationToken cancellationToken = default)
            => SerializeInternalAsync(@event, cancellationToken);

        /// <inheritdoc/>
        public Task<BinaryData> SerializeAsync<T>(EventData<T> @event, CancellationToken cancellationToken = default)
            => SerializeInternalAsync(@event, cancellationToken);

        /// <summary>
        /// Serializes the <see cref="EventData"/> to a <see cref="BinaryData"/> and optionally writes any attachment.
        /// </summary>
        private async Task<BinaryData> SerializeInternalAsync(EventData @event, CancellationToken cancellationToken = default)
        {
            BinaryData data;
            EventAttachment attachment;

            // Only serializes the value.
            if (SerializeValueOnly)
            {
                data = CustomSerializers.SerializeToBinaryData(@event, JsonSerializer, SerializeValueOnly);
                if (AttachmentStorage is null || data.ToMemory().Length <= AttachmentStorage!.MaxDataSize)
                    return data;

                // Create the attachment and serialize the event with the attachment reference.
                attachment = await AttachmentStorage.WriteAsync(@event, data, cancellationToken).ConfigureAwait(false);
                return JsonSerializer.SerializeToBinaryData(attachment);
            }

            // Serializes the complete event including metadata.
            if (AttachmentStorage is null)
                return CustomSerializers.SerializeToBinaryData(@event, JsonSerializer, SerializeValueOnly);

            // Serialize the value and check if needs to be an attachment.
            data = CustomSerializers.SerializeToBinaryData(@event, JsonSerializer, true);
            if (data.ToMemory().Length < AttachmentStorage!.MaxDataSize)
                return CustomSerializers.SerializeToBinaryData(@event, JsonSerializer, false);

            // Create the attachment and re-serialize the event with the attachment reference.
            attachment = await AttachmentStorage.WriteAsync(@event, data, cancellationToken).ConfigureAwait(false);
            return JsonSerializer.SerializeToBinaryData(new EventData(@event) { Value = attachment });
        }
    }
}