namespace CoreEx.Azure.Functions.TestApp;

/// <summary>
/// Demonstrates a native trigger delegating to CoreEx subscriber processing and explicit settlement.
/// </summary>
/// <param name="subscriber">The scoped subscriber dispatcher.</param>
public sealed class ServiceBusFunctions(ServiceBusSubscribedSubscriber subscriber)
{
    private readonly ServiceBusSubscribedSubscriber _subscriber = subscriber.ThrowIfNull();

    /// <summary>
    /// Receives commands addressed to the test application's Functions domain.
    /// </summary>
    [Function(nameof(ProcessMessage))]
    public Task ProcessMessage([ServiceBusTrigger("coreex-functions", Connection = "ServiceBus", AutoCompleteMessages = false)] ServiceBusReceivedMessage message, ServiceBusMessageActions actions, CancellationToken cancellationToken)
        => _subscriber.FunctionReceiveAsync(message, actions, "coreex-functions", cancellationToken: cancellationToken);
}
