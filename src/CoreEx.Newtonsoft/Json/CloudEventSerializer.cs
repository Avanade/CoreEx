// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CloudNative.CloudEvents;
using CloudNative.CloudEvents.NewtonsoftJson;
using CoreEx.Events;
using Newtonsoft.Json;
using System;
using System.Net.Mime;
using System.Threading.Tasks;
using Nsj = Newtonsoft.Json;

namespace CoreEx.Newtonsoft.Json
{
    /// <summary>
    /// Provides the <see cref="Nsj.JsonSerializer"/>-based <see cref="IEventSerializer"/>.
    /// </summary>
    public class CloudEventSerializer : IEventSerializer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CloudEventSerializer"/> class.
        /// </summary>
        /// <param name="settings">The <see cref="Nsj.JsonSerializerSettings"/>; where <c>null</c> these will default.</param>
        public CloudEventSerializer(JsonSerializerSettings? settings = null) => Settings = settings ?? new JsonSerializer().Settings;

        /// <summary>
        /// Gets the <see cref="JsonSerializerSettings"/>.
        /// </summary>
        public JsonSerializerSettings Settings { get; }

        /// <inheritdoc/>
        public Task<EventData> DeserializeAsync(BinaryData eventData)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<EventData<T>> DeserializeAsync<T>(BinaryData eventData)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<BinaryData> SerializeAsync(EventData @event)
        {
            if (@event == null)
                throw new ArgumentNullException(nameof(@event));

            var ce = new CloudEvent
            {
                Type = @event.Type?.ToLowerInvariant(),
                Source = @event.Source,
                Id = @event.Id,
                Time = @event.Timestamp,
            };

            CoreEx.Text.Json.CloudEventSerializer.SetExtensionAttribute(ce, "subject", @event.Subject?.ToLowerInvariant());
            CoreEx.Text.Json.CloudEventSerializer.SetExtensionAttribute(ce, "action", @event.Action?.ToLowerInvariant());
            CoreEx.Text.Json.CloudEventSerializer.SetExtensionAttribute(ce, "correlationid", @event.CorrelationId);
            CoreEx.Text.Json.CloudEventSerializer.SetExtensionAttribute(ce, "partitionkey", @event.PartitionKey);

            OnSerialize(@event, ce);

            ce.DataContentType = MediaTypeNames.Application.Json;
            ce.Data = @event.Data;

            return Task.FromResult(new BinaryData(new JsonEventFormatter(Nsj.JsonSerializer.Create(Settings)).EncodeStructuredModeMessage(ce, out var _)));
        }

        /// <summary>
        /// Invoked after the standard <see cref="EventData"/> properties have been updated to the <see cref="CloudEvent"/> to enable further customization where required.
        /// </summary>
        /// <param name="event">The source <see cref="EventData"/>.</param>
        /// <param name="cloudEvent">The corresponding <see cref="CloudEvent"/>.</param>
        protected virtual void OnSerialize(EventData @event, CloudEvent cloudEvent) { }
    }
}