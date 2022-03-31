# Change log

Represents the **NuGet** versions.

## v1.0.3
- *Enhancement:* `IIdentifier.GetIdentifier` method replaced with `IIdentifier.Id`. The `IIdentifier<T>` overrides the `Id` property hiding the base `IIdentifier.Id`.
- *Enhancement:* `ValueContentResult` properties are now all get and set enabled.
- *Fixed:* `ValueContentResult.ETag` generation enhanced to handle different query string parameters when performing an HTTP GET for `IEnumerable` (collection) types.

## v1.0.2
- *Enhancement:* **Breaking change** The event publishing (`IEventPublisher`) is now designed to occur in three distinct phases: 1) formatting (`EventDataFormatter.Format`), 2) serialization (`IEventSerializer.SerializeAsync`), and 3) sending (`IEventSender.SendAsync`). The `EventPublisher` has been added to orchestrate this flow.
- *Enhancement:* Updated the `IJsonSerializer` implementation defaults to align with the expected default serialization behavior.
- *Fixed:* The `TypedHttpClientBase` fixed to handle where the `requestUri` parameter is only a query string and not a path.

## v1.0.1
- *New:* Initial publish to GitHub/NuGet.