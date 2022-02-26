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
    public class CloudEventSerializer : CloudEventSerializerBase
    {
        private Nsj.JsonSerializer? _jsonSerializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudEventSerializer"/> class.
        /// </summary>
        /// <param name="eventDataFormatter">The <see cref="EventDataFormatter"/>; where <c>null</c> this will default.</param>
        /// <param name="settings">The <see cref="Nsj.JsonSerializerSettings"/>; where <c>null</c> this will default.</param>
        public CloudEventSerializer(EventDataFormatter? eventDataFormatter = null, JsonSerializerSettings? settings = null) : base(eventDataFormatter)
        {
            EventDataFormatter.JsonSerializer ??= new JsonSerializer(settings);
            Settings = settings ?? (JsonSerializerSettings)EventDataFormatter.JsonSerializer.Options;
        }

        /// <summary>
        /// Gets the <see cref="JsonSerializerSettings"/>.
        /// </summary>
        public JsonSerializerSettings Settings { get; }

        /// <summary>
        /// Gets the underlying <see cref="Nsj.JsonSerializer"/> instance.
        /// </summary>
        protected Nsj.JsonSerializer JsonSerializer => _jsonSerializer ??= Nsj.JsonSerializer.Create(Settings);

        /// <inheritdoc/>
        protected override Task<CloudEvent> DecodeAsync(BinaryData eventData)
            => Task.FromResult(new JsonEventFormatter(JsonSerializer).DecodeStructuredModeMessage(eventData, new ContentType(MediaTypeNames.Application.Json), null));

        /// <inheritdoc/>
        protected override Task<CloudEvent> DecodeAsync<T>(BinaryData eventData)
            => Task.FromResult(new JsonEventFormatter<T>(JsonSerializer).DecodeStructuredModeMessage(eventData, new ContentType(MediaTypeNames.Application.Json), null));

        /// <inheritdoc/>
        protected override Task<BinaryData> EncodeAsync(CloudEvent cloudEvent)
            => Task.FromResult(new BinaryData(new JsonEventFormatter(JsonSerializer).EncodeStructuredModeMessage(cloudEvent, out var _)));
    }
}