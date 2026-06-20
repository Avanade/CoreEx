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
        var bd = cloudEvent.ThrowIfNull().EncodeToBinaryData(contentMode);

        var msg = new ServiceBusMessage(bd)
        {
            ContentType = bd.MediaType,
            Subject = cloudEvent.Type,
            MessageId = cloudEvent.Id,
            PartitionKey = cloudEvent.GetPartitionKey()
        };

        if (includeAttributes)
        {
            msg.ApplicationProperties.TryAdd(CloudEventSpecVersionPropertyName, cloudEvent.SpecVersion.VersionId);

            foreach (var attr in cloudEvent.GetPopulatedAttributes())
            {
                msg.ApplicationProperties.TryAdd($"{CloudEventPrefix}{attr.Key.Name}", attr.Value);
            }
        }

        foreach (var attr in cloudEvent.GetPopulatedAttributes())
        {
            if (attr.Key.Name == CloudEventTraceParentPropertyName)
                msg.ApplicationProperties.TryAdd(MessageTraceParentPropertyName, attr.Value);

            if (attr.Key.Name == CloudEventTraceStatePropertyName)
                msg.ApplicationProperties.TryAdd(MessageTraceStatePropertyName, attr.Value);

            if (attr.Key.Name == CloudEventTraceBaggagePropertyName)
                msg.ApplicationProperties.TryAdd(MessageTraceBaggagePropertyName, attr.Value);
        }

        return msg;
    }

    /// <summary>
    /// Indicates whether the <see cref="ServiceBusReceivedMessage"/> is a <see cref="CloudEvent"/>.
    /// </summary>
    /// <param name="message">The <see cref="ServiceBusReceivedMessage"/>.</param>
    /// <returns><see langword="true"/> where the <see cref="ServiceBusReceivedMessage"/> is a <see cref="CloudEvent"/>; otherwise, <see langword="false"/>.</returns>
    /// <remarks>Determines if the <see cref="ServiceBusReceivedMessage"/> contains the <see cref="CloudEvent.SpecVersion"/> property.</remarks>
    public static bool IsCloudEvent(this ServiceBusReceivedMessage message)
    {
        message.ThrowIfNull();
        return message.ApplicationProperties.ContainsKey(CloudEventSpecVersionPropertyName);
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
        var ce = bd.DecodeToCloudEvent(contentMode.Value);

        if (contentMode == ContentMode.Binary && message.IsCloudEvent())
        {
            // Manually populate CloudEvent properties from ApplicationProperties for Binary mode.
            ce.Id = message.MessageId;
            ce.Type = message.Subject;
            ce.Time = message.EnqueuedTime;

            foreach (var ap in message.ApplicationProperties)
            {
                if (ap.Key.StartsWith(CloudEventPrefix) && ap.Key != CloudEventSpecVersionPropertyName)
                {
                    ce[ap.Key[CloudEventPrefix.Length..]] = ap.Value;
                }

                if (ap.Key == MessageTraceParentPropertyName)
                    ce[CloudEventTraceParentPropertyName] = ap.Value;

                if (ap.Key == MessageTraceStatePropertyName)
                    ce[CloudEventTraceStatePropertyName] = ap.Value;

                if (ap.Key == MessageTraceBaggagePropertyName)
                    ce[CloudEventTraceBaggagePropertyName] = ap.Value;
            }
        }

        return ce;
    }
}