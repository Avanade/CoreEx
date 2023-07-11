// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

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
    public abstract class EventDataSerializerBase : IEventSerializer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventDataSerializerBase"/> class.
        /// </summary>
        /// <param name="jsonSerializer">The <see cref="IJsonSerializer"/>.</param>
        /// <param name="eventDataFormatter">The <see cref="Events.EventDataFormatter"/>.</param>
        protected EventDataSerializerBase(IJsonSerializer jsonSerializer, EventDataFormatter? eventDataFormatter)
        {
            JsonSerializer = jsonSerializer ?? throw new ArgumentNullException(nameof(jsonSerializer));
            EventDataFormatter = eventDataFormatter ?? new EventDataFormatter();
        }

        /// <summary>
        /// Gets the <see cref="IJsonSerializer"/>.
        /// </summary>
        public IJsonSerializer JsonSerializer { get; }

        /// <summary>
        /// Gets the <see cref="Events.EventDataFormatter"/>.
        /// </summary>
        public EventDataFormatter EventDataFormatter { get; }

        /// <summary>
        /// Gets or sets the optional <see cref="IAttachmentStorage"/> to use for <see cref="EventAttachment">attachments</see>.
        /// </summary>
        public IAttachmentStorage? AttachmentStorage { get; set; }

        /// <summary>
        /// Indicates whether the <see cref="EventData.Value"/> is serialized only (<c>true</c>); or alternatively, the complete <see cref="EventData"/> (<c>false</c>).
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
            if (valueType == null)
                throw new ArgumentNullException(nameof(valueType));

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

            // Only serializes the value
            if (SerializeValueOnly)
            {
                data = JsonSerializer.SerializeToBinaryData(@event.Value);
                if (AttachmentStorage is null || data.ToMemory().Length <= AttachmentStorage!.MaxDataSize)
                    return JsonSerializer.SerializeToBinaryData(@event.Value);

                // Create the attachment and serialize the event with the attachment reference.
                attachment = await AttachmentStorage.WriteAsync(@event, data, cancellationToken).ConfigureAwait(false);
                return JsonSerializer.SerializeToBinaryData(attachment);
            }

            // Serializes the complete event including metadata.
            var e = @event.Copy();
            EventDataFormatter.Format(e);
            if (AttachmentStorage is null)
                return JsonSerializer.SerializeToBinaryData(e);

            // Serialize the value and check if needs to be an attachment.
            data = JsonSerializer.SerializeToBinaryData(e.Value);
            if (data.ToMemory().Length < AttachmentStorage!.MaxDataSize)
                return JsonSerializer.SerializeToBinaryData(e);

            // Create the attachment and re-serialize the event with the attachment reference.
            attachment = await AttachmentStorage.WriteAsync(@event, data, cancellationToken).ConfigureAwait(false);
            return JsonSerializer.SerializeToBinaryData(new EventData(e) { Value = attachment });
        }
    }
}