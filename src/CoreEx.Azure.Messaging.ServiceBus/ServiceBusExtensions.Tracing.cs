namespace CoreEx.Azure.Messaging.ServiceBus;

public static partial class ServiceBusExtensions
{
    /// <summary>
    /// Enriches the current host activity with transport metadata and valid remote trace context without replacing its parent.
    /// </summary>
    /// <param name="message">The received message.</param>
    /// <param name="propagateBaggage">Whether to import W3C baggage; defaults to false.</param>
    /// <remarks>A valid traceparent takes precedence over Diagnostic-Id. Malformed trace metadata is ignored, not classified as invalid message data.
    /// Baggage is limited to an 8192-byte header and 64 imported entries, and existing keys are preserved. No activity or authenticated execution context is created.</remarks>
    public static void EnrichReceiveActivity(this ServiceBusReceivedMessage message, bool propagateBaggage = false)
    {
        message.ThrowIfNull();
        var activity = Activity.Current;
        if (activity is null)
            return;

        activity.SetTag("messaging.system", "servicebus");
        activity.SetTag("messaging.subject", message.Subject);
        activity.SetTag("messaging.message.id", message.MessageId);
        activity.SetTag("messaging.message.conversation_id", message.CorrelationId);
        activity.SetTag("messaging.servicebus.message.delivery_count", message.DeliveryCount);
        activity.SetTag("messaging.servicebus.message.enqueued_time", message.EnqueuedTime.ToString("O"));
        activity.SetTag("messaging.servicebus.message.session_id", message.SessionId);

        var traceState = GetStringProperty(message, MessageTraceStatePropertyName);
        if (ActivityContext.TryParse(GetStringProperty(message, MessageTraceParentPropertyName), traceState, isRemote: true, out var context)
            || ActivityContext.TryParse(GetStringProperty(message, "Diagnostic-Id"), traceState, isRemote: true, out context))
            activity.LinkTraceContext(context);

        if (!propagateBaggage)
            return;

        EventBaggage.Import(activity, EventBaggage.Parse(GetStringProperty(message, MessageTraceBaggagePropertyName)));
    }

    /// <summary>
    /// Reads a string-valued application property without coercing arbitrary transport values.
    /// </summary>
    private static string? GetStringProperty(ServiceBusReceivedMessage message, string name)
        => message.ApplicationProperties.TryGetValue(name, out var value) ? value as string : null;
}
