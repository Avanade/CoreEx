namespace CoreEx.Azure.Functions.TestApp;

/// <summary>
/// Receives a uniform JSON message shape with no CoreEx subscribed dispatcher.
/// </summary>
/// <param name="subscriber">The typed native message subscriber.</param>
public sealed class NativeServiceBusFunctions(NativeMessageSubscriber subscriber)
{
    private readonly NativeMessageSubscriber _subscriber = subscriber.ThrowIfNull();

    /// <summary>
    /// Processes every incoming message with the same handler and explicit settlement.
    /// </summary>
    [Function(nameof(ProcessNativeMessage))]
    public async Task ProcessNativeMessage([ServiceBusTrigger("coreex-native-functions", Connection = "ServiceBus", AutoCompleteMessages = false)] ServiceBusReceivedMessage message, ServiceBusMessageActions actions, CancellationToken cancellationToken)
    {
        var observe = message.ApplicationProperties.TryGetValue("test-tracing", out var value) && value is true;
        var before = observe ? ServiceBusTraceProbe.Capture() : null;
        await _subscriber.FunctionReceiveAsync(message, actions, "coreex-native-functions", cancellationToken: cancellationToken).ConfigureAwait(false);
        if (before is not null)
            ServiceBusTraceProbe.Record(message.MessageId, before, ServiceBusTraceProbe.Capture());
    }
}
