// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CloudNative.CloudEvents;
using CloudNative.CloudEvents.SystemTextJson;
using CoreEx.Events;
using System;
using System.Collections.Generic;
using System.Net.Mime;
using System.Threading.Tasks;
using Stj = System.Text.Json;

namespace CoreEx.Text.Json
{
    /// <summary>
    /// Provides the <see cref="Stj.JsonSerializer"/>-based <see cref="IEventSerializer"/>.
    /// </summary>
    public class CloudEventSerializer : IEventSerializer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CloudEventSerializer"/> class.
        /// </summary>
        /// <param name="options">The <see cref="Stj.JsonSerializerOptions"/>; where <c>null</c> these will default.</param>
        public CloudEventSerializer(Stj.JsonSerializerOptions? options = null) => Options = options ?? new JsonSerializer().Options;

        /// <summary>
        /// Gets the <see cref="Stj.JsonSerializerOptions"/>.
        /// </summary>
        public Stj.JsonSerializerOptions Options { get; }

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

            SetExtensionAttribute(ce, "subject", @event.Subject?.ToLowerInvariant());
            SetExtensionAttribute(ce, "action", @event.Action?.ToLowerInvariant());
            SetExtensionAttribute(ce, "correlationid", @event.CorrelationId);
            SetExtensionAttribute(ce, "partitionkey", @event.PartitionKey);

            OnSerialize(@event, ce);

            ce.DataContentType = MediaTypeNames.Application.Json;
            ce.Data = @event.Data;

            return Task.FromResult(new BinaryData(new JsonEventFormatter(Options, new Stj.JsonDocumentOptions()).EncodeStructuredModeMessage(ce, out var _)));
        }

        /// <summary>
        /// Invoked after the standard <see cref="EventData"/> properties have been updated to the <see cref="CloudEvent"/> to enable further customization where required.
        /// </summary>
        /// <param name="event">The source <see cref="EventData"/>.</param>
        /// <param name="cloudEvent">The corresponding <see cref="CloudEvent"/>.</param>
        protected virtual void OnSerialize(EventData @event, CloudEvent cloudEvent) { }

        /// <summary>
        /// Sets the <see cref="CloudEvent"/> extension attribute where not <c>null</c>.
        /// </summary>
        /// <param name="ce">The <see cref="CloudEvent"/>.</param>
        /// <param name="name">The attribute name.</param>
        /// <param name="value">The attribute value.</param>
        public static void SetExtensionAttribute<T>(CloudEvent ce, string name, T value)
        {
            if (Comparer<T>.Default.Compare(value, default!) == 0)
                return;

            ce[name] = value;
        }
    }
}