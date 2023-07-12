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
[`IEventPublisher`](./IEventPublisher.cs) | Enables the publishing via the `Publish` method to internally queue the messages, and when ready perform a `SendAsync` to send the one or more published events in an atomic operation. The `IEventPublisher` is responsible for __orchestrating__ the `EventDataFormatter`, `IEventSerializer` and `IEventSender`. The [`EventPublisher`](./EventPublisher.cs) provides the default implementation.<br/><br/>To enable the likes of unit testing, the [`InMemoryPublisher`](./InMemoryPublisher.cs) provides an in-memory implementation that enables the logically sent events to be inspected to verify content, etc. <br/><br/>The [`NullEventPublisher`](./NullEventPublisher.cs) represents an event publisher whereby the events are simply swallowed/discarded on send.<br/><br/>The [`LoggerEventPublisher`](./LoggerEventPublisher.cs) represents an event publisher whereby the events are logged (`ILogger.LogInformation`) on send.

<br/>

### ServiceBusSender

The [`ServiceBusSender`](../../CoreEx.Azure/ServiceBus/ServiceBusSender.cs) is an [Azure Service Bus](https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-messaging-overview) `IEventSender` implementation; this is designed to _batch_ publish one or more messages to one or more queues/topics. See the corresponding [documentation](../../CoreEx.Azure/ServiceBus/README.md) for more information.

<br/>

### EventOutboxEnqueueBase

The [`EventOutboxEnqueueBase`](../../CoreEx.Database.SqlServer/Outbox/EventOutboxEnqueueBase.cs) provides a Microsoft SQL Server `IEventSender` to support the [transactional outbox pattern](https://microservices.io/patterns/data/transactional-outbox.html); i.e. persists the events within the database within a [transactional](https://learn.microsoft.com/en-us/dotnet/api/system.transactions.transactionscope) context.

[_DbEx_](https://github.com/Avanade/DbEx/blob/main/docs/SqlServerEventOutbox.md) provides the capabilities to generate the required Microsoft SQL Server and C# artefacts to support. The [`MyEf.Hr`](https://github.com/Avanade/Beef/tree/master/samples/MyEf.Hr) sample within [_Beef_](https://github.com/Avanade/Beef) demonstrates end-to-end usage.

<br/>

## Subscribing

Event subscribing is more tightly coupled, in that the implementation is going to be more aligned to the capabilities of the underlying messaging platform. However, the intent is to still decouple the messaging platform from the underlying processing by deserializing the message back into the originating (formatted) [`EventData`](./EventData.cs) or [`EventData<T>`](./EventDataT.cs) where required using the previously discussed [`IEventSerializer`](./IEventSerializer.cs). This has the added advantage that the underlying messaging platform can evolve over time within minimal change, whilst the underlying processing logic can remain largely constant.

The [`EventSubscriberBase`](./EventSubscriberBase.cs) provides the messaging platform host agnostic base functionality that should be inherited. This provides the base [`IErrorHandling`](./Subscribing/IErrorHandling.cs) configuration, being the corresponding [`ErrorHandling`](./Subscribing/ErrorHandling.cs) action per error type.

The `EventSubscriberBase.DeserializeEventAsync` methods manage the deserialization of the originating message using an [`IEventDataConverter`](./IEventDataConverter.cs) that encapsulates the `IEventSerializer` functionality (including handling exceptions) to perform the message conversion into the corresponding [`EventData`](./EventDataT.cs) or [`EventData<T>`](./EventDataT.cs). 

The [`EventSubscriberInvoker`](./Subscribing/EventSubscriberInvoker.cs) via the `EventSubscriberBase.EventSubscriberInvoker` property **must** be used to invoke the underlying processing as this includes the [`IErrorHandling`](./Subscribing/IErrorHandling.cs) logic; converting any errors into an [`EventSubscriberException`](./EventSubscriberException.cs). The `EventSubscriberException.IsTransient` property allows for the inheriting host to _retry_ where applicable and/or supported (versus possible [dead letter](https://en.wikipedia.org/wiki/Dead_letter_queue) where supported).

<br/>

### ServiceBusSubscriber

The [`ServiceBusSubscriber`](../../CoreEx.Azure/ServiceBus/ServiceBusSubscriber.cs) is an [Azure Service Bus](https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-messaging-overview) implementation; this is designed to process messages from a queue/topic that has a singlular [`EventData`](./EventData.cs) or [`EventData<T>`](./EventDataT.cs) type. See the corresponding [documentation](../../CoreEx.Azure/ServiceBus/README.md) for more information.

<br/>

## Orchestrated subscribing

Within an [event-driven architecture](https://learn.microsoft.com/en-us/azure/architecture/guide/architecture-styles/event-driven) multiple events (message types) may be published/produced to the same underlying messaging platform. Therefore, there may be the need to subscribe/consume to one or more events (message types) in the published order (sequence). 

To enable the [`EventSubscriberOrchestrator`](./subscribing/EventSubscriberOrchestrator.cs) enables none or more subscribers ([`IEventSubscriber`](./subscribing/IEventSubscriber.cs)) to be added (`EventSubscriberOrchestrator.AddSubscribers`). To simplify the implementation of an `IEventSubscriber` the [`SubscriberBase`](./subscribing/SubscriberBase.cs) and [`SubscriberBase<T>`](./subscribing/SubscriberBaseT.cs) enable. These also include [`IErrorHandling`](./Subscribing/IErrorHandling.cs) configuration, being the corresponding [`ErrorHandling`](./Subscribing/ErrorHandling.cs) action per error type to enable _subscriber_-specific handling where applicable.

The `IEventSubscriber` implementation provides the corresponding `ReceiveAsync` method that must be overridden to implement the specific processing functionality. Additionally, the [`SubscriberBase<T>`](./subscribing/SubscriberBaseT.cs) supports the specification of an [`IValidator<T>`](../Validation/IValidatorT.cs) to pre-validate the `EventData<T>.Value` before invoking the `ReceiveAsync`.

One or more [`EventSubscriberAttribute`](./subscribing/EventSubscriberAttribute.cs) must be specified for the `IEventSubscriber` to configure the subscription matching criteria (includes wildcard support). The `EventSubscriberOrchestrator` for each event invocation will iterate through the subscribers (`IEventSubscriber`) and use the `EventSubscriberAttribute` to match; where there is a single match that matched `IEventSubscriber` will be invoked.

The underlying `IEventSubscriber` must also be registered as services (see `IServiceCollection.AddEventSubscribers`) so that they can be instantiated using the underlying `IServivceProvider` from the host (enables dependency injection).

The following demonstrates an `IEventSubscriber` implementation.

``` csharp
[EventSubscriber("my.hr.employee", "created", "updated")]
public class EmployeeeSubscriber : SubscriberBase<Employee>
{
    public override Task<Result> ReceiveAsync(EventData<Employee> @event, EventSubscriberArgs args, CancellationToken cancellationToken)
    {
        // Perform requisite business logic.
        return Task.FromResult(Result.Success);
    }
```

<br/>

### ServiceBusOrchestratedSubscriber

The [`ServiceBusOrchestratedSubscriber`](../../CoreEx.Azure/ServiceBus/ServiceBusOrchestratedSubscriber.cs) is an [Azure Service Bus](https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-messaging-overview) implementation that inherits from ``EventSubscriberBase`` and supports `EventSubscriberOrchestrator` functionality. See the corresponding [documentation](../../CoreEx.Azure/ServiceBus/README.md) for more information.

The [`MyEf.Hr`](https://github.com/Avanade/Beef/tree/master/samples/MyEf.Hr) sample within [_Beef_](https://github.com/Avanade/Beef) demonstrates end-to-end usage.

<br/>

## Advanced

The following provides further advanced capabilities.

<br/>

### Claim-check pattern

The [claim-check pattern](https://docs.microsoft.com/en-us/azure/architecture/patterns/claim-check) is a messaging pattern that enables the payload to be stored externally to the message. This is useful where the payload is large and/or the message is to be published to multiple subscribers.

To support the [`IAttachmentStorage`](./Attachments/IAttachmentStorage.cs) enables a pluggable approach to support the storage of the payload attachment (represented by an [`EventAttachment`](./Attachments/EventAttachment.cs)); enabling persistence within Azure, AWS, on-premises, etc.

To further simplify the implementation, and to separate this capability from the underlying messaging sub-system, the [`IEventSerializer.AttachmentStorage`](./IEventSerializer.cs) property enables the optional specification. Where this value is non-null and the serialized `EventData.Value` length is greater than or equal to the `IAttachmentStorage.MaxDatSize`, then this will be stored as an attachment and the `EventData.Value` will be set to the corresponding `EventAttachment` value.

The following demonstrates an example `EventAttachment` serialization where the attachment reference is set to the underlying unique `EventData.Id` value:

``` json
{
  "contentType": "application/json",
  "attachment": "550e8400-e29b-41d4-a716-446655440000.json"
}
```