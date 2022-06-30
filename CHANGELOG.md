# Change log

Represents the **NuGet** versions.

## v1.0.6
- *Enhancement:* Added `WithTimeout(TimeSpan timeout)` support to `TypedHttpClientBase` to enable per request timeouts.
- *Enhancement:* Added `AddFluentValidators<TAssembly>` to automatically add the requisite dependency injection (DI) configuration for all validators defined within an `Assembly`.
- *Enhancement:* **Breaking change** Refactored the extended entities to simplify implementation and improve experience via new `EntityBase.GetPropertyValues` and corresponding `PropertyValue`. 
- *Enhancement:* Ported validation framework from _Beef_ repo.
- *Enhancement:* Added support for `IReferenceData` serialization where only the `Code` is serialized/deserialized. This also required new `IReferenceDataContentJsonSerializer`, `ReferenceDataContentJsonSerializer` and `ReferenceDataContentWebApi` for when full `IReferenceData` content serialization is required.
- *Enhancement:* Serializers updated to support `ICollectionResult` which by default only (de)serializes the underlying `Collection`. The `Paging` is expected to be handled separately.
- *Enhancement:* Ported _core_ database framework components from _DbEx_ rep.
- *Enhancement:* Ported extended database and entity framework components from _Beef_ repo.
- *Enhancement:* Added implementation agnostic `IMapper` for typed value mappings. Added _AutoMapper_ implementation with wrapper to enable.

## v1.0.5
- *Enhancement:* Overloads added to `WebApi` and `WebApiPublisher` to allow the body value to be passed versus reading from the `HttpRequest`. This is useful where allowing the likes of the ASP.NET infrastructure to deserialize value directly.
- *Enhancement:* Automatic `ETag` generation is performed prior to field filtering as this is considered a post response action and should not affect `ETag` value.
- *Enhancement:* Added `EventSendException` to provide a standard means to capture the events not sent to enable additional processing of those where required.

## v1.0.4
- *Enhancement:* Status code checking added to `TypedHttpClientBase<TSelf>`.
- *Enhancement:* Added `IValidator<T>` to enable any implementation (agnostic); created wrappers to enable `FluentValidation` (including dependency injection helper).
- *Enhancement:* Added `AcceptsBodyAttribute` to enable Swagger (via `AcceptsBodyOperationFilter`) to output body type characteristics where not explicitly defined.
- *Enhancement:* Added opt-in simulated concurrency (ETag) checking/generation to `WebApi.PutAsync` and `WebApi.PatchAsync` where underlying data source does not support.
- *Enhancement:* Added `CancellationToken` to all `Async` methods.

## v1.0.3
- *Enhancement:* `IIdentifier.GetIdentifier` method replaced with `IIdentifier.Id`. The `IIdentifier<T>` overrides the `Id` property hiding the base `IIdentifier.Id`.
- *Enhancement:* `ValueContentResult` properties are now all get and set enabled. The `Value` property has been removed as it is JSON serialized into `Content`.
- *Fixed:* `ValueContentResult.ETag` generation enhanced to handle different query string parameters when performing an HTTP GET for `IEnumerable` (collection) types.
- *Enhancement:* Added `HttpClientEx` as a light-weight means to instantiate a one-off instance from an `HttpClient`.
- *Enhancement:* Added `JsonMergePatch` (`application/merge-patch+json`) whereby the contents of a JSON document are merged into an existing object value as per [RFC7396](https://tools.ietf.org/html/rfc7396).
- *Enhancement:* Added/updated reference data capabilities.
- Plus, many more minor fixes and enhancements.

## v1.0.2
- *Enhancement:* **Breaking change** The event publishing (`IEventPublisher`) is now designed to occur in three distinct phases: 1) formatting (`EventDataFormatter.Format`), 2) serialization (`IEventSerializer.SerializeAsync`), and 3) sending (`IEventSender.SendAsync`). The `EventPublisher` has been added to orchestrate this flow.
- *Enhancement:* Updated the `IJsonSerializer` implementation defaults to align with the expected default serialization behavior.
- *Fixed:* The `TypedHttpClientBase` fixed to handle where the `requestUri` parameter is only a query string and not a path.

## v1.0.1
- *New:* Initial publish to GitHub/NuGet.