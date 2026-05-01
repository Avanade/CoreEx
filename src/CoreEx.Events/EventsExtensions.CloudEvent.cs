namespace CoreEx.Events;

public static partial class EventsExtensions
{
    /// <summary>
    /// Infers the <see cref="ContentMode"/> from the <see cref="BinaryData"/>'s <see cref="BinaryData.MediaType"/>.
    /// </summary>
    /// <param name="binaryData">The <see cref="BinaryData"/>.</param>
    /// <returns>The inferred <see cref="ContentMode"/>.</returns>
    public static ContentMode InferContentMode(this BinaryData binaryData)
    {
        if (binaryData is null || string.IsNullOrEmpty(binaryData.MediaType))
            return ContentMode.Binary;

        var contentType = new ContentType(binaryData.MediaType);
        return contentType.MediaType.StartsWith("application/cloudevents", StringComparison.OrdinalIgnoreCase) ? ContentMode.Structured : ContentMode.Binary;
    }

    /// <summary>
    /// Encodes the <see cref="CloudEvent"/> to a <see cref="BinaryData"/> using the specified <see cref="ContentMode"/>.
    /// </summary>
    /// <param name="cloudEvent">The <see cref="CloudEvent"/>.</param>
    /// <param name="contentMode">The <see cref="ContentMode"/>; defaults to <see cref="ContentMode.Structured"/>.</param>
    /// <param name="jsonSerializerOptions">The optional <see cref="JsonSerializerOptions"/>.</param>
    /// <returns>The <paramref name="cloudEvent"/> encoded to <see cref="BinaryData"/>.</returns>
    /// <remarks>This uses a customized <see cref="JsonEventFormatter"/> internally to enable.</remarks>
    public static BinaryData EncodeToBinaryData(this CloudEvent cloudEvent, ContentMode contentMode = ContentMode.Structured, JsonSerializerOptions? jsonSerializerOptions = null)
    {
        cloudEvent.ThrowIfNull();
        var formatter = new InternalFormatter(jsonSerializerOptions ?? JsonDefaults.SerializerOptions, default);
        return contentMode switch
        {
            ContentMode.Structured => new BinaryData(formatter.EncodeStructuredModeMessage(cloudEvent.ThrowIfNull(), out var contentType), contentType.ToString()),
            ContentMode.Binary => new BinaryData(formatter.EncodeBinaryModeEventData(cloudEvent.ThrowIfNull()), cloudEvent.DataContentType),
            _ => throw new ArgumentException("Invalid content mode specified.", nameof(contentMode))
        };
    }

    /// <summary>
    /// Decodes the <see cref="BinaryData"/> to a <see cref="CloudEvent"/> using the specified <see cref="ContentMode"/>.
    /// </summary>
    /// <param name="binaryData">The <see cref="BinaryData"/>.</param>
    /// <param name="contentMode">The <see cref="ContentMode"/>; defaults to <see cref="ContentMode.Structured"/>.</param>
    /// <param name="jsonSerializerOptions">The optional <see cref="JsonSerializerOptions"/>.</param>
    /// <returns>The <paramref name="binaryData"/> decoded to a <see cref="CloudEvent"/>.</returns>
    public static CloudEvent DecodeToCloudEvent(this BinaryData binaryData, ContentMode contentMode = ContentMode.Structured, JsonSerializerOptions? jsonSerializerOptions = null)
    {
        var formatter = new InternalFormatter(jsonSerializerOptions ?? JsonDefaults.SerializerOptions, default);
        var contentType = string.IsNullOrEmpty(binaryData?.MediaType) ? null : new ContentType(binaryData.MediaType);

        if (contentMode == ContentMode.Structured)
            return formatter.DecodeStructuredModeMessage(binaryData, contentType, null);

        var cex = new CloudEvent() { DataContentType = contentType?.MediaType };
        if (formatter.IsJsonContentType(contentType))
            cex.Data = binaryData;
        else
            formatter.DecodeBinaryModeEventData(binaryData, cex);

        return cex;
    }

    /// <summary>
    /// Encodes the <see cref="CloudEvent"/> to a <see cref="JsonElement"/> as <see cref="ContentMode.Structured"/>.
    /// </summary>
    /// <param name="cloudEvent">The <see cref="CloudEvent"/>.</param>
    /// <param name="jsonSerializerOptions">The optional <see cref="JsonDefaults"/>.</param>
    /// <returns>The <paramref name="cloudEvent"/> encoded to a <see cref="JsonElement"/>.</returns>
    public static JsonElement EncodeToJsonElement(this CloudEvent cloudEvent, JsonSerializerOptions? jsonSerializerOptions = null)
    {
        var formatter = new InternalFormatter(jsonSerializerOptions ?? JsonDefaults.SerializerOptions, default);
        var jr = new Utf8JsonReader(formatter.EncodeStructuredModeMessage(cloudEvent.ThrowIfNull(), out var _).Span);
        return JsonElement.ParseValue(ref jr);
    }

    /// <summary>
    /// Decodes the <see cref="JsonElement"/> to a <see cref="CloudEvent"/> assuming <see cref="ContentMode.Structured"/>.
    /// </summary>
    /// <param name="jsonElement">The <see cref="JsonElement"/>.</param>
    /// <param name="jsonSerializerOptions">The optional <see cref="JsonDefaults"/>.</param>
    /// <returns>The <paramref name="jsonElement"/> decoded to a <see cref="CloudEvent"/>.</returns>
    public static CloudEvent DecodeToCloudEvent(this JsonElement jsonElement, JsonSerializerOptions? jsonSerializerOptions = null)
    {
        var formatter = new InternalFormatter(jsonSerializerOptions ?? JsonDefaults.SerializerOptions, default);

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            jsonElement.WriteTo(writer);
        }

        stream.Position = 0;
        return formatter.DecodeStructuredModeMessage(stream, new ContentType(MediaTypeNames.Application.Json), null);
    }

    /// <summary>
    /// Customized internal <see cref="JsonEventFormatter"/>.
    /// </summary>
    private sealed class InternalFormatter(JsonSerializerOptions options, JsonDocumentOptions jsonDocumentOptions) : JsonEventFormatter(options, jsonDocumentOptions)
    {
        /// <summary>
        /// Indicates whether the <see cref="ContentType"/> is considered JSON.
        /// </summary>
        public bool IsJsonContentType(ContentType? contentType) => contentType is not null && IsJsonMediaType(contentType.MediaType);

        /// <inheritdoc/>
        protected override void EncodeStructuredModeData(CloudEvent cloudEvent, Utf8JsonWriter writer)
        {
            if (cloudEvent.Data is BinaryData bd && !string.IsNullOrEmpty(cloudEvent.DataContentType) && IsJsonMediaType(cloudEvent.DataContentType))
            {
                writer.WritePropertyName(DataPropertyName);
                writer.WriteRawValue(bd, true);
            }
            else
                base.EncodeStructuredModeData(cloudEvent, writer);
        }

        /// <inheritdoc/>
        public override ReadOnlyMemory<byte> EncodeBinaryModeEventData(CloudEvent cloudEvent)
        {
            if (cloudEvent is not null && cloudEvent.Data is BinaryData bd && !string.IsNullOrEmpty(cloudEvent.DataContentType) && IsJsonMediaType(cloudEvent.DataContentType))
                return bd.ToArray();
            else
                return base.EncodeBinaryModeEventData(cloudEvent!);
        }

        protected override void DecodeStructuredModeDataProperty(JsonElement dataElement, CloudEvent cloudEvent)
        {
            cloudEvent.Data = new BinaryData(dataElement.GetRawText());
        }

        /// <inheritdoc/>
        public override void DecodeBinaryModeEventData(ReadOnlyMemory<byte> body, CloudEvent cloudEvent)
        {
            base.DecodeBinaryModeEventData(body, cloudEvent);

            if (cloudEvent.Data is not null && cloudEvent.Data is not BinaryData)
                cloudEvent.Data = new BinaryData(cloudEvent.Data);
        }
    }

    /// <summary>
    /// Sets the <see cref="CloudEvent"/> extension attribute where not default value.
    /// </summary>
    /// <param name="ce">The <see cref="CloudEvent"/>.</param>
    /// <param name="name">The attribute name.</param>
    /// <param name="value">The attribute value.</param>
    public static void SetExtensionAttribute<T>(this CloudEvent ce, string name, T value)
    {
        if (Comparer<T>.Default.Compare(value, default!) == 0)
            return;

        ce[name] = value;
    }

    /// <summary>
    /// Tries to get the named <see cref="CloudEvent"/> extension attribute value.
    /// </summary>
    /// <param name="ce">The <see cref="CloudEvent"/>.</param>
    /// <param name="name">The attribute name.</param>
    /// <param name="value">The attribute value.</param>
    /// <returns><see langword="true"/> indicates that the extension attribute exists; otherwise, <see langword="false"/>.</returns>
    public static bool TryGetExtensionAttribute<T>(this CloudEvent ce, string name, [NotNullWhen(true)] out T value)
    {
        var val = ce[name];
        if (val is null)
        {
            value = default!;
            return false;
        }

        value = (T)val;
        return true;
    }
}