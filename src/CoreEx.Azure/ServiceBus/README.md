﻿# CoreEx.Azure.ServiceBus

Provides the key [Azure Service Bus](https://docs.microsoft.com/en-us/azure/service-bus-messaging/service-bus-messaging-overview) capabilities, leveraging and extending the [`Azure.Messaging.ServiceBus`](https://docs.microsoft.com/en-us/dotnet/api/overview/azure/messaging.servicebus-readme) library.

<br/>

## Publishing

A _CoreEx_ [`ServiceBusSender`](./ServiceBusSender.cs) provides the [`IEventSender.SendAsync`](../../CoreEx/Events/IEventSender.cs) capabilities to batch send one or more events/mesages to Azure Service Bus.

<br/>

## Subscribing

A _CoreEx_ [`ServiceBusSubscriber`](../../CoreEx.Azure/ServiceBus/ServiceBusSubscriber.cs) implementation is provided to encapsulate the standard capabilities to ensure consistency with respect to the processing and underlying management of the message.

The `ReceiveAsync` method requires the [`ServiceBusReceivedMessage`](https://docs.microsoft.com/en-us/dotnet/api/azure.messaging.servicebus.servicebusreceivedmessage) and [`ServiceBusMessageActions`](https://docs.microsoft.com/en-us/dotnet/api/microsoft.azure.webjobs.servicebus.servicebusmessageactions) and performs the following steps:
- Begins a logging scope to include the correlation identifier from the message.
- Deserializes the `ServiceBusReceivedMessage` into the corresponding [`EventData`](../../CoreEx/Events/EventDataT.cs).
- Invokes the processing logic for the event and where successful calls `ServiceBusMessageActions.CompleteMessageAsync`.
- Handle all exceptions:
  - Where the exception implements [`IExtendedException`](../../CoreEx/Abstractions/IExtendedException.cs) and `IsTransient` then log a warning and bubble the exception for the host process to manage a retry.
  - Finally, log the error and invoke `ServiceBusMessageActions.DeadLetterMessageAsync`.

<br/>

### Azure ServiceBus-triggered Function example

The following demonstrates usage when using the [`ServiceBusTrigger`](https://docs.microsoft.com/en-us/dotnet/api/microsoft.azure.webjobs.servicebustriggerattribute) within an Azure Function:

``` csharp
public class ServiceBusExecuteVerificationFunction
{
    private readonly ServiceBusSubscriber _subscriber;
    private readonly VerificationService _service;

    public ServiceBusExecuteVerificationFunction(ServiceBusSubscriber subscriber, VerificationService service)
    {
        _subscriber = subscriber;
        _service = service;
    }

    [FunctionName(nameof(ServiceBusExecuteVerificationFunction))]
    public Task RunAsync([ServiceBusTrigger("%" + nameof(HrSettings.VerificationQueueName) + "%", Connection = nameof(HrSettings.ServiceBusConnection))] ServiceBusReceivedMessage message, ServiceBusMessageActions messageActions)
        => _subscriber.ReceiveAsync<EmployeeVerificationRequest>(message, messageActions, ed => _service.VerifyAndPublish(ed.Value), validator: new EmployeeVerificationValidator().Wrap());
}
```

<br/>

### Instrumentation

To get further insights into the processing of the messages an [`IEventSubscriberInstrumentation`](../../CoreEx/Events/IEventSubscriberInstrumentation.cs) can be implemented. The corresponding `EventSubscriberBase.Instrumentation` property should be set during construction; typically performed during dependency injection. Determine whether the instrumentation instance should also be registered as a _singleton_.

An example implementation for Azure Application Insights would be similar to as follows:

``` csharp
public class AppInsightInstrumentation : EventSubscriberInstrumentationBase
{
    private readonly TelemetryClient _telemetryClient;

    public AppInsightInstrumentation(TelemetryClient telemetryClient) 
        => _telemetryClient = telemetryClient ?? throw new ArgumentNullException(nameof(telemetryClient));

    public override void Instrument(ErrorHandling? errorHandling = null, Exception? exception = null)
        => _telemetryClient.TrackEvent(GetInstrumentName("Subscriber", errorHandling, exception));
}
```
