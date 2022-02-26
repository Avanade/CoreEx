// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CloudNative.CloudEvents;
using CloudNative.CloudEvents.SystemTextJson;
using CoreEx.Events;
using System;
using System.Net.Mime;
using System.Threading.Tasks;
using Stj = System.Text.Json;

namespace CoreEx.Text.Json
{
    /// <summary>
    /// Provides the <see cref="Stj.JsonSerializer"/>-based <see cref="IEventSerializer"/>.
    /// </summary>
    public class CloudEventSerializer : CloudEventSerializerBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CloudEventSerializer"/> class.
        /// </summary>
        /// <param name="eventDataserializerOptions">The <see cref="EventDataSerializerOptions"/>; where <c>null</c> these will default.</param>
        /// <param name="options">The <see cref="Stj.JsonSerializerOptions"/>; where <c>null</c> these will default.</param>
        public CloudEventSerializer(EventDataSerializerOptions? eventDataserializerOptions = null, Stj.JsonSerializerOptions? options = null) : base(eventDataserializerOptions)
        {
            EventDataSerializerOptions.JsonSerializer ??= new JsonSerializer(options);
            Options = options ?? (Stj.JsonSerializerOptions)EventDataSerializerOptions.JsonSerializer.Options;
        }

        /// <summary>
        /// Gets the <see cref="Stj.JsonSerializerOptions"/>.
        /// </summary>
        public Stj.JsonSerializerOptions Options { get; }

        /// <inheritdoc/>
        protected override Task<CloudEvent> DecodeAsync(BinaryData eventData)
            => Task.FromResult(new JsonEventFormatter(Options, new Stj.JsonDocumentOptions()).DecodeStructuredModeMessage(eventData, new ContentType(MediaTypeNames.Application.Json), null));

        /// <inheritdoc/>
        protected override Task<CloudEvent> DecodeAsync<T>(BinaryData eventData)
            => Task.FromResult(new JsonEventFormatter<T>(Options, new Stj.JsonDocumentOptions()).DecodeStructuredModeMessage(eventData, new ContentType(MediaTypeNames.Application.Json), null));

        /// <inheritdoc/>
        protected override Task<BinaryData> EncodeAsync(CloudEvent cloudEvent)
            => Task.FromResult(new BinaryData(new JsonEventFormatter(Options, new Stj.JsonDocumentOptions()).EncodeStructuredModeMessage(cloudEvent, out var _)));
    }
}