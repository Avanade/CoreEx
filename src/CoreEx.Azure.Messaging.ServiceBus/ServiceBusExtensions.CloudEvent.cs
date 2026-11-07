namespace CoreEx.Azure.Messaging.ServiceBus;

public static partial class ServiceBusExtensions
{
    /// <summary>
    /// Gets the <see cref="CloudEvent"/> attribute prefix.
    /// </summary>
    public const string CloudEventPrefix = "ce_";

    /// <summary>
    /// Gets the <see cref="CloudEvent"/> specification version (<see cref="CloudEvent.SpecVersion"/>) property name: '<c>ce_specversion</c>'.
    /// </summary>
    public const string CloudEventSpecVersionPropertyName = CloudEventPrefix + "specversion";

    /// <summary>
    /// Gets the <see cref="CloudEvent"/> source (<see cref="CloudEvent.Source"/>) property name: '<c>ce_source</c>'.
    /// </summary>
    public const string CloudEventSourcePropertyName = CloudEventPrefix + "source";

    /// <summary>
    /// Gets the <see cref="CloudEvent"/> trace parent property name: '<c>ce_traceparent</c>'.
    /// </summary>
    public const string CloudEventTraceParentPropertyName = CloudEventPrefix + "traceparent";

    /// <summary>
    /// Gets the <see cref="CloudEvent"/> trace state property name: '<c>ce_tracestate</c>'.
    /// </summary>
    public const string CloudEventTraceStatePropertyName = CloudEventPrefix + "tracestate";

    /// <summary>
    /// Gets the <see cref="CloudEvent"/> trace baggage property name: '<c>ce_baggage</c>'.
    /// </summary>
    public const string CloudEventTraceBaggagePropertyName = CloudEventPrefix + "baggage";

    /// <summary>
    /// Gets the <see cref="ServiceBusMessage"/> trace parent property name: '<c>traceparent</c>'.
    /// </summary>
    public const string MessageTraceParentPropertyName = "traceparent";

    /// <summary>
    /// Gets the <see cref="ServiceBusMessage"/> trace state property name: '<c>tracestate</c>'.
    /// </summary>
    public const string MessageTraceStatePropertyName = "tracestate";
    
    /// <summary>
    /// Gets the <see cref="ServiceBusMessage"/> trace baggage property name: '<c>baggage</c>'.
    /// </summary>
    public const string MessageTraceBaggagePropertyName = "baggage";

    /// <summary>
    /// Converts a <see cref="CloudEvent"/> to a <see cref="ServiceBusMessage"/>.
    /// </summary>
    /// <param name="cloudEvent">The <see cref="CloudEvent"/>.</param>
    /// <param name="contentMode">The <see cref="ContentMode"/> to use; defaults to <see cref="ContentMode.Structured"/>.</param>
    /// <param name="includeAttributes">Indicates whether to include all <see cref="CloudEvent.GetPopulatedAttributes"/> as <see cref="ServiceBusMessage.ApplicationProperties"/>; defaults to <see langword="true"/>.</param>
    /// <returns>The <see cref="ServiceBusMessage"/>.</returns>
    /// <remarks>The <see cref="ServiceBusMessage.Subject"/> is set to the <see cref="CloudEvent.Type"/>.</remarks>
    public static ServiceBusMessage ToServiceBusMessage(this CloudEvent cloudEvent, ContentMode contentMode = ContentMode.Structured, bool includeAttributes = true)
    {
        cloudEvent.ThrowIfNull().Validate();
        cloudEvent.TryGetExtensionAttribute("correlationid", out string? correlationId);
        if (NativeMessageEnvelopeCodec.GetRepresentation(cloudEvent) == MessageRepresentation.Native)
        {
            var body = cloudEvent.Data switch
            {
                null => BinaryData.Empty,
                BinaryData data => data,
                _ => throw new NotSupportedException("Native payloads must be BinaryData.")
            };
            var native = new ServiceBusMessage(body)
            {
                ContentType = cloudEvent.DataContentType,
                Subject = NativeMessageEnvelopeCodec.ReadSubject(cloudEvent),
                MessageId = cloudEvent.Id,
                PartitionKey = cloudEvent.GetPartitionKey(),
                CorrelationId = correlationId,
                ReplyTo = NativeMessageEnvelopeCodec.ReadReplyTo(cloudEvent)
            };
            foreach (var property in NativeMessageEnvelopeCodec.ReadAttributes(cloudEvent))
                native.ApplicationProperties.Add(property.Key, property.Value);

            CopyTracing(cloudEvent, native);
            return native;
        }

        var bd = cloudEvent.ThrowIfNull().EncodeToBinaryData(contentMode);

        var msg = new ServiceBusMessage(bd)
        {
            ContentType = bd.MediaType,
            Subject = cloudEvent.Type,
            MessageId = cloudEvent.Id,
            CorrelationId = correlationId,
            PartitionKey = cloudEvent.GetPartitionKey()
        };

        // The spec version must always be included (regardless of includeAttributes) so a Binary-mode message remains identifiable/reconstructable as a CloudEvent on receive (see IsCloudEvent/ToCloudEvent).
        msg.ApplicationProperties.TryAdd(CloudEventSpecVersionPropertyName, cloudEvent.SpecVersion.VersionId);
        if (cloudEvent.Source is not null)
            msg.ApplicationProperties.TryAdd(CloudEventSourcePropertyName, cloudEvent.Source.OriginalString);

        if (cloudEvent.Time is not null)
            msg.ApplicationProperties.TryAdd(CloudEventPrefix + "time", cloudEvent.Time.Value.ToString("O"));

        if (correlationId is not null)
            msg.ApplicationProperties.TryAdd(CloudEventPrefix + "correlationid", correlationId);

        // Single pass: the bulk ce_-prefixed attributes are gated by includeAttributes, but trace context is always propagated regardless.
        foreach (var attr in cloudEvent.GetPopulatedAttributes())
        {
            if (includeAttributes)
                msg.ApplicationProperties.TryAdd($"{CloudEventPrefix}{attr.Key.Name}", attr.Key.Type.Format(attr.Value));

            if (attr.Key.Name == MessageTraceParentPropertyName)
                msg.ApplicationProperties.TryAdd(MessageTraceParentPropertyName, attr.Value);

            if (attr.Key.Name == MessageTraceStatePropertyName)
                msg.ApplicationProperties.TryAdd(MessageTraceStatePropertyName, attr.Value);

            if (attr.Key.Name == MessageTraceBaggagePropertyName)
                msg.ApplicationProperties.TryAdd(MessageTraceBaggagePropertyName, attr.Value);
        }

        return msg;
    }

    /// <summary>
    /// Copies only standard unprefixed trace context for native delivery.
    /// </summary>
    private static void CopyTracing(CloudEvent cloudEvent, ServiceBusMessage message)
    {
        foreach (var name in new[] { MessageTraceParentPropertyName, MessageTraceStatePropertyName, MessageTraceBaggagePropertyName })
        {
            if (cloudEvent[name] is string value)
                message.ApplicationProperties.Add(name, value);
        }
    }

    /// <summary>
    /// Indicates whether the <see cref="ServiceBusReceivedMessage"/> is a <see cref="CloudEvent"/>.
    /// </summary>
    /// <param name="message">The <see cref="ServiceBusReceivedMessage"/>.</param>
    /// <returns><see langword="true"/> where the <see cref="ServiceBusReceivedMessage"/> is a <see cref="CloudEvent"/>; otherwise, <see langword="false"/>.</returns>
    /// <remarks>Recognizes structured JSON media types or the binary binding's specversion property. Detection does not validate the envelope.</remarks>
    public static bool IsCloudEvent(this ServiceBusReceivedMessage message)
    {
        message.ThrowIfNull();
        return message.ApplicationProperties.ContainsKey(CloudEventSpecVersionPropertyName) || message.Body.WithMediaType(message.ContentType).InferContentMode() == ContentMode.Structured;
    }

    /// <summary>
    /// Converts a <see cref="ServiceBusReceivedMessage"/> to a <see cref="CloudEvent"/>.
    /// </summary>
    /// <param name="message">The <see cref="ServiceBusReceivedMessage"/>.</param>
    /// <param name="contentMode">The <see cref="ContentMode"/> to use; defaults to <see langword="null"/>.</param>
    /// <returns>The <see cref="CloudEvent"/>.</returns>
    /// <remarks>Where <paramref name="contentMode"/> is <see langword="null"/>, the <see cref="ContentMode"/> is inferred from the <see cref="ServiceBusReceivedMessage.ContentType"/> (see <see cref="EventsExtensions.InferContentMode"/>).</remarks>
    public static CloudEvent ToCloudEvent(this ServiceBusReceivedMessage message, ContentMode? contentMode = null)
    {
        var bd = message.ThrowIfNull().Body.WithMediaType(message.ContentType);
        contentMode ??= bd.InferContentMode();
        if (contentMode == ContentMode.Binary && !message.ApplicationProperties.ContainsKey(CloudEventSpecVersionPropertyName))
            throw new InvalidOperationException("The binary message is not a CloudEvent; use a native subscriber.");

        var ce = bd.DecodeToCloudEvent(contentMode.Value);

        if (contentMode == ContentMode.Binary && message.IsCloudEvent())
        {
            // Manually populate CloudEvent properties from ApplicationProperties for Binary mode.
            var version = message.ApplicationProperties[CloudEventSpecVersionPropertyName] as string
                ?? throw new InvalidOperationException("CloudEvent specversion must be a string.");
            var spec = CloudEventsSpecVersion.FromVersionId(version) ?? throw new NotSupportedException($"CloudEvent version '{version}' is not supported.");
            ce = new CloudEvent(spec) { Data = ce.Data, DataContentType = ce.DataContentType };

            foreach (var ap in message.ApplicationProperties)
            {
                if (ap.Key.StartsWith(CloudEventPrefix) && ap.Key != CloudEventSpecVersionPropertyName)
                {
                    var name = ap.Key[CloudEventPrefix.Length..];
                    if (name == spec.IdAttribute.Name || name == spec.TypeAttribute.Name || name == spec.DataContentTypeAttribute.Name)
                        continue;

                    if (ap.Value is string text)
                        ce.SetAttributeFromString(name, text);
                    else
                        ce[name] = ap.Value;
                }

                if (ap.Key == MessageTraceParentPropertyName)
                    ce[MessageTraceParentPropertyName] = ap.Value;

                if (ap.Key == MessageTraceStatePropertyName)
                    ce[MessageTraceStatePropertyName] = ap.Value;

                if (ap.Key == MessageTraceBaggagePropertyName)
                    ce[MessageTraceBaggagePropertyName] = ap.Value;
            }

            ce.Id = message.MessageId;
            ce.Type = message.Subject;
            if (!ce.TryGetExtensionAttribute<string>("correlationid", out _))
                ce.SetExtensionAttribute("correlationid", message.CorrelationId);

            if (string.IsNullOrEmpty(ce.Id) || string.IsNullOrEmpty(ce.Type) || ce.Source is null)
                throw new InvalidOperationException("A binary CloudEvent requires id, type, and source.");
        }

        return ce;
    }
}
