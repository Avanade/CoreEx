# Change log

Represents the **NuGet** versions.

## v1.0.2
- *Enhancement:* *Breaking change* The event publishing (`IEventPublisher`) is now designed to occur in three distinct phases: 1) formatting (`EventDataFormatter.Format`), 2) serialization (`IEventSerializer.SerializeAsync`), and 3) sending (`IEventSender.SendAsync`). The `EventPublisher` has been added to orchestrate this flow.
- *Enhancement:* Updated the `IJsonSerializer` implementation defaults to align with the expected default serialization behavior.

## v1.0.1
- *New:* Initial publish to GitHub/NuGet.