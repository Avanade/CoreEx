# CoreEx.Events

The `CoreEx.Events` namespace provides extended capabilities to facilitate the publishing and subscribing of events (messages) in a consistent, but flexible, manner.

<br/>

## Motivation

To provide an event/message publishing capability that is flexible enough to support varying message formatting, from basic JSON payloads, all the way through to more advanced formatting such as [CloudEvents](https://cloudevents.io/). Additionally, to decouple the message formatting from the actual sending in a _pluggable_ manner, to be able to leverage any messaging platform (e.g. [Azure Service Bus](https://azure.microsoft.com/en-us/services/service-bus), [Solace](https://solace.com/), [Kafka](https://kafka.apache.org/), etc.) consistently. 

<br/>

## Publishing

Event publishing is enabled by the following key capabilities:

Capability | Description
-|-
[`EventData`](./EventDataT.cs) | Provides the core properties (data) for an event/message. The standard properties are enabled by [`EventDataBase`](./EventDataBase.cs). Additional `Attributes` can be added where required; however, the key properties for most scenarios should be accounted for.
[`EventDataFormatter`](./EventDataFormatter.cs) | Formats an `EventData` instance, setting additional properties, etc. to meet the requirements of the application. There are a number of options within this class to support a rich level of formatting, and this class can be inherited to support more advanced scenarios as necessary.
[`IEventSerializer`](./IEventSerializer.cs) | Provides the capbilities to serialize the `EventData` into a corresponding [`BinaryData`](https://docs.microsoft.com/en-us/dotnet/api/system.binarydata) (i.e. `byte[]`) format ready for sending. An [`EventDataSerializerBase`](./EventDataSerializerBase.cs) and [`CloudEventSerializerBase`](./CloudEventSerializerBase.cs) provide the base implementation to perform basic JSON serialization or CloudEvents JSON serialization respectively, using either `System.Text.Json` or `Newtonsoft.Json` as required.
[`IEventSender`](./IEventSender.cs) | Performs the event sending via the actual messaging platform/protocol. For example, an Azure [`ServiceBusSender`](../../CoreEx.Azure/ServiceBus/ServiceBusSender.cs) implementation is provided.
[`IEventPublisher`](./IEventPublisher.cs) | Enables the publishing via the `Publish` method to internally queue the messages, and when ready perform a `SendAsync` to send the one or more published events in an atomic operation. The `IEventPublisher` is responsible for __orchestrating__ the `EventDataFormatter`, `IEventSerializer` and `IEventSender`. The [`EventPublisher`](./EventPublisher.cs) provides the default implementation.<br/><br/>To enable the likes of unit testing, the [`InMemoryPublisher`](./InMemoryPublisher.cs) provides an in-memory implementation that enables the logically sent events to be inspected to verify content, etc.

<br/>

## Subscribing

Event subscribing is more tightly coupled, in that the implementation is going to be more aligned to the capabilities of the messaging platform.

The previously discussed [`IEventSerializer`](./IEventSerializer.cs) provides the corresponding deserialization back into the originating (formatted) [`EventData`](./EventDataT.cs) where required.

The [`EventSubscriberBase`](./EventSubscriberBase.cs) can be used to provide the underlying `DeserializeEventAsync` capability.

<br/>

## Azure ServiceBus Subscriber

An Azure [`ServiceBusSubscriber`](../../CoreEx.Azure/ServiceBus/ServiceBusSubscriber.cs) implementation is provided.

The `ReceiveAsync` which requires the [`ServiceBusReceivedMessage`](https://docs.microsoft.com/en-us/dotnet/api/azure.messaging.servicebus.servicebusreceivedmessage) and [`ServiceBusMessageActions`](https://docs.microsoft.com/en-us/dotnet/api/microsoft.azure.webjobs.servicebus.servicebusmessageactions) performs the following steps:
- Begins a logging scope to include the correlation identifier from the message.
- Deserializes the `ServiceBusReceivedMessage` into the corresponding [`EventData`](../tree/main/src/CoreEx/Events/EventDataT.cs).
- Invokes the processing logic for the event and where successful calls `ServiceBusMessageActions.CompleteMessageAsync`.
- Handle all exceptions:
  - Where the exception implements [`IExtendedException`](../tree/main/src/CoreEx/Abstractions/IExtendedException.cs) and `IsTransient` then log a warning and bubble the exception for the host process to manage a retry.
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
    [ExponentialBackoffRetry(3, "00:02:00", "00:30:00")]
    public Task RunAsync([ServiceBusTrigger("%" + nameof(HrSettings.VerificationQueueName) + "%", Connection = nameof(HrSettings.ServiceBusConnection))] ServiceBusReceivedMessage message, ServiceBusMessageActions messageActions)
        => _subscriber.ReceiveAsync<EmployeeVerificationRequest>(message, messageActions, ed => _service.VerifyAndPublish(ed.Validate<EmployeeVerificationRequest, EmployeeVerificationValidator>()));
}
```