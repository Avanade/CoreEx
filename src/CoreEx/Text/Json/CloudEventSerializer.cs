// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CloudNative.CloudEvents;
using CloudNative.CloudEvents.SystemTextJson;
using CoreEx.Events;
using System;
using System.Net.Mime;
using System.Threading;
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
        /// <param name="eventDataFormatter">The <see cref="EventDataFormatter"/>; where <c>null</c> this will default.</param>
        /// <param name="options">The <see cref="Stj.JsonSerializerOptions"/>; where <c>null</c> this will default.</param>
        public CloudEventSerializer(EventDataFormatter? eventDataFormatter = null, Stj.JsonSerializerOptions? options = null) : base(eventDataFormatter)
        {
            EventDataFormatter.JsonSerializer ??= new JsonSerializer(options);
            Options = options ?? (Stj.JsonSerializerOptions)EventDataFormatter.JsonSerializer.Options;
        }

        /// <summary>
        /// Gets the <see cref="Stj.JsonSerializerOptions"/>.
        /// </summary>
        public Stj.JsonSerializerOptions Options { get; }

        /// <inheritdoc/>
        protected override Task<CloudEvent> DecodeAsync(BinaryData eventData, CancellationToken cancellation = default)
            => Task.FromResult(new JsonEventFormatter(Options, new Stj.JsonDocumentOptions()).DecodeStructuredModeMessage(eventData, new ContentType(MediaTypeNames.Application.Json), null));

        /// <inheritdoc/>
        protected override Task<CloudEvent> DecodeAsync<T>(BinaryData eventData, CancellationToken cancellation = default)
            => Task.FromResult(new JsonEventFormatter<T>(Options, new Stj.JsonDocumentOptions()).DecodeStructuredModeMessage(eventData, new ContentType(MediaTypeNames.Application.Json), null));

        /// <inheritdoc/>
        protected override Task<BinaryData> EncodeAsync(CloudEvent cloudEvent, CancellationToken cancellation = default)
            => Task.FromResult(new BinaryData(new JsonEventFormatter(Options, new Stj.JsonDocumentOptions()).EncodeStructuredModeMessage(cloudEvent, out var _)));
    }
}