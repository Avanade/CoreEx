# CoreEx.Solace.PubSub

Provides the key [Solace PubSub+ Event Broker](https://solace.com/products/event-broker/) capabilities, leveraging the [`SolaceSystems.Solclient.Messaging`](https://docs.solace.com/API/Messaging-APIs/dotNet-API/net-api-home.htm) library.

<br/>

## Publishing

A _CoreEx_ [`PubSubSender`](./PubSubSender.cs) provides the [`IEventSender.SendAsync`](../../CoreEx/Events/IEventSender.cs) capabilities to batch send one or more events/mesages to PubSub+.

<br/>