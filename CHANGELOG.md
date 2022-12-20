 # Change log

Represents the **NuGet** versions.

## v2.3.0
- *Enhancement:* `PagingArgs.MaxTake` default set by `SettingsBase.PagingMaxTake`.
- *Enhancement:* Reference data `ICacheEntry` policy configuration can now be defined in settings.

## v2.2.0
- *Fixed:* Entity Framework `EfDb.UpdateAsync` resolved error where the instance of entity type cannot be tracked because another instance with the same key value is already being tracked.
- *Fixed:* The `CollectionMapper` was incorrectly appending items to an existing collection, versus performing a replacement operation.
- *Enhancement:* Improved Entity Framework support where entities contain relationships, both query and update; new `EfDbArgs.QueryNoTracking` and `EfDbArgs.ClearChangeTrackerAfterGet` added to configure/override default behaviour. 
- *Enhancement:* Added `TypedHttpClientOptions OnBeforeRequest` and within `TypedHttpClientBase<TSelf>` to enable updating of the `HttpRequestMessage` before it is sent.

## v2.1.0
- *Enhancement:* Added additional `ReferenceDataBaseEx.GetRefDataText` method overload with a parameter of `id`; as an alternative to the pre-existing `code`. 
- *Enhancement:* `ReferenceDataOrchestrator` caching supports virtual `OnGetCacheKey` to enable overridding classes to further define the key characteristics.

## v2.0.0
- *Enhancement:* Added support for [`MySQL`](https://dev.mysql.com/) through introduction of `MySqlDatabase` that supports similar pattern to `SqlServerDatabase`.
- *Enhancement:* Added new `EncodedStringToDateTimeConverter` to simulate row versioning from a `DateTime` (timestamp) as an alternative.
- *Enhancement:* **Breaking change**: `CoreEx.EntityFrameworkCore` updated to only have database provider independent reference of `Microsoft.EntityFrameworkCore`. Developer will need to add database specific within own solution to use.
- *Enhancement:* **Breaking change**: Moved classes that inherit from the likes of `EntityBase` into corresponding `Extended` namespace as secondary, and moved the corresponding `Models` implementation into root as primary and removed namespace accordingly. This is to ensure consistency, such that _extended_ usage is explicit (non-default). `MessageItem` updated to no longer inherit from `EntityBase` as the extended capabilities are not required.
- *Enhancement:* **Breaking change**: The `AddValidators` extension method has been updated to register the implementing validators directly, versus the underlying `IValidatorEx`. This enables multiple validators to be registered for an entity. Any references to the interface will need to be updated to reference the concrete to continue functioning through dependency injection. Generally, the validators are not mocked, and the concrete classes can be if need using `MOQ` where required; impact of change is considered low risk for higher reward.
- *Enhancement:* Added the security related capabilities to `ExecutionContext` as was previously available in _[Beef](https://github.com/Avanade/Beef)_.

## v1.0.12
- *Enhancement:* Added new `Mapping.Mapper` as a simple (explicit) `IMapper` capability as an alternative to AutoMapper. Enable the key `Map`, `Flatten` and `Expand` mapping capabilities. This is no reflection/compiling magic, just specified mapping code which executes very fast.
- *Enhancement:* **Breaking change**: Validation `Additional` method renamed to `AdditionalAsync` to be more explicit.
- *Enhancement:* **Breaking change**: The `SqlServer` specific capabilities within `CoreEx.Database` project/assembly have been moved to a new `CoreEx.Database.SqlServer` project/assembly.

## v1.0.11
- *Enhancement:* Updated the `EventOutboxEnqueueBase` and `EventOutboxDequeueBase` to include the `EventDataBase.Key` value/column.
- *Enhancement:* Added the `EventOutboxHostedService` (migrated from `NTangle`) to enable hosted outbox publishing service execution.
- *Enhancement:* `LoggerEventSender` updated to also log event metadata.

## v1.0.10
- *Enhancement:* Loosened `EntityCollectionResult` generic `TEntity` constraint to `EntityBase` to enable inherited extended entitites.
- *Enhancement:* Extended `TypedHttpClientBase<TSelf>` to support `DefaultOptions` and `SendOptions` to enable default configuration of new `TypedHttpClientOptions`; i.e. the likes of `WithRetry` can now default versus having to be set per invocation of `SendAsync`.
- *Enhancement:* Added `TypedMappedHttpClientBase`, `TypedMappedHttpClientCore` and `TypedMappedHttpClient` with new `IMapper` property used to add extended support for request and response type mappings as part of the request. New methods are `GetMappedAsync`, `PostMappedAsync`, `PutMappedAsync` and `PatchMappedAsync` where applicable.
- *Enhancement:* `IConverter<T>` usability improvements; including others that leverage.
- *Enhancement:* AutoMapper converters added for common `IConverter<T>` implementations to enable.
- *Enhancement:* `ReferenceDataOrchestrator.ConvertFromId(object? id)` overload added to enable usage when `Type` of `Id` is unknown.
- *Enhancement:* Added `RefDataLoader` overload that supports stored procedure command usage.
- *Enhancement:* Extended `TableValuedParameter` to support standard list types; including corresponding configurable `DatabaseColumn` names.
- *Enhancement:* Add `DatabaseCommand.SelectMultiSetAsync` overloads to support paging. 
- *Enhancement:* Added `IEntityKey` to enable key-based support in a consistent and standardized manner; refactored `IIdentifier` and `IPrimaryKey` to leverage; existing references within updated to leverage `IEntityKey` where applicable.
- *Enhancement:* Improved validation handling of nullable vs non-nullable types when adding rules.
- *Enhancement:* `EntityBase` usage simplified especially where inheriting indirectly, i.e. from a base class that inherits `EntityBase`. As a result `EntityBase<>` will be deprecated next version. `ICloneable` support removed, now supported via `ExtendedExtensions.Clone<T>()`.
- *Enhancement:* Improved the `HttpArg` query string output support.
- *Enhancement:* Added `Models.ChangeLog` (does not inherit from `EntityBase`) as alternative to `Entities.ChangeLog` (which does). Also, added corresponding `AutoMapper` mapping between the two.
- *Enhancement:* `CosmosDbContainerBase` updated to further centralize functionality, inheriting classes updated accordingly.
- *Enhancement:* **Breaking change**: `ICollectionResult.Collection` renamed to `ICollectionResult.Items`.
- *Enhancement:* **Breaking change**: `IReferenceDataCollection` properties `AllList` and `ActiveList` renamed to `AllItems` and `ActiveItems` respectively.
- *Enhancement:* **Breaking change**: `HttpClientEx` removed as was a duplicate of `TypedHttpClient`; the latter was/is the intended implementation.
- *Enhancement:* `JsonDataReader` when loading `IReferenceData` will attempt to read using both JSON and .NET Property names before overridding to allow additional flexibility within the specified JSON/YAML.
- *Enhancement:* `ReferenceDataOrchestrator` concurrency support improved to ensure loading of reference data items for a	`Type` is managed with a `SemaphoreSlim` to ensure only a single thread loads (only once).
- *Fixed:* `SettingsBase` was not looking for keys containing `__` or `:` consistently.
- *Fixed:* `JsonFilterer` implementations now filters contents of a JSON object array correctly.

## v1.0.9
- *Enhancement:* Ported and refactored CosmosDb components from _Beef_ repo.
- *Enhancement:* **Breaking change**: Replaced `DatabaseArgs.Paging` with `DatabaseQuery.Paging` and `DatabaseQuery.WithPaging`.
- *Enhancement:* **Breaking change**: Replaced `EfDbArgs.Paging` with `EfDbQuery.Paging` and `EfDbQuery.WithPaging`.
- *Enhancement:* Added `JsonDataReader` to enable dynamic loading of either YAML or JSON formatted data for data migration/uploading.
- *Enhancement:* Added `WebApiExceptionHandlerMiddleware` to manage any unhandled exceptions.
- *Enhancement:* Added `TypedHttpClient` to enable basic support for instantiating a `TypedHttpClientCore` without having to explicitly inherit.
- *Enhancement:* **Breaking change**: HealthChecks project deprecated with functionality moved to individual projects where applicable.
- *Enhancement:* Added `EfDbEntity` to provide a typed entity wrapper over the `IEfDb` operations.
- *Enhancement:* `AddAzureServiceBusClient` has had support to configure `ServiceBusClientOptions` added.
- *Fixed:* The `ServiceBusMessage` cannot be sent due to local transactions not being supported with other resource managers/DTC resolved.
- *Fixed:* The `AuthenticationException` and `AuthorizationException` HTTP status codes were incorrect; updated to `401` and `403` respectively.

## v1.0.8
- *Enhancement:* `InvokerBase<TInvoker, TArgs>` has been updated to that the `TArgs` value is optional.
- *Enhancement:* `ReferenceDataFilter` added to simplify HTTP Agent filtering as a single encapsulated object.
- *Enhancement:* `WebApi.ConvertNotfoundToDefaultStatusCodeOnDelete` property added to convert `NotFoundException` to the default `StatusCode` (`NoContent`) as considered an idempotent operation; defaults to `true`.
- *Enhancement:* `PropertyExpression.SentenceCaseConverter` added to enable overridding of default `ToSentenceCase` logic.
- *Fixed:* `HttpArg` where `HttpArgType.FromUriUseProperties` was incorrectly formatting string values.
- *Fixed:* `WebApi.Patch` operation was not returning the updated value correctly.
- *Fixed:* `WebApiExecutionContextMiddleware` not setting `Username` and `Timestamp` correctly.
- *Fixed:* `WebApiInvoker` was not setting the `x-error-type` and `x-error-code` headers for `IExtendedException` exceptions.
- *Fixed:* `WebApiParam` updated to use `ETag` header (primary) then `IETag.ETag` property (secondary) as request `ETag` value.
- *Fixed:* `ValidationException` updated to return message as `MediaTypeNames.Text.Plain` where message only (i.e. no property errors).
- *Fixed:* `IChangeLog` values set correctly for `IDatabase` and `IEfDb` create and update.
- *Fixed:* `IDatabase` connection open override methods now called correctly.
- *Fixed:* `CollectionRuleItem<TItem>` updated to support duplicate checking by `IIdentifier<T>` as well as the existing `IPrimaryKey`.

## v1.0.7
- *Fixed:* Invokers updated to leverage `async/await` correctly.

## v1.0.6
- *Enhancement:* Added `WithTimeout(TimeSpan timeout)` support to `TypedHttpClientBase` to enable per request timeouts.
- *Enhancement:* Added `AddFluentValidators<TAssembly>` to automatically add the requisite dependency injection (DI) configuration for all validators defined within an `Assembly`.
- *Enhancement:* **Breaking change**: Refactored the extended entities to simplify implementation and improve experience via new `EntityBase.GetPropertyValues` and corresponding `PropertyValue`. 
- *Enhancement:* Ported and refactored validation framework from _Beef_ repo.
- *Enhancement:* Added support for `IReferenceData` serialization where only the `Code` is serialized/deserialized. This also required new `IReferenceDataContentJsonSerializer`, `ReferenceDataContentJsonSerializer` and `ReferenceDataContentWebApi` for when full `IReferenceData` content serialization is required.
- *Enhancement:* Serializers updated to support `ICollectionResult` which by default only (de)serializes the underlying `Collection`. The `Paging` is expected to be handled separately.
- *Enhancement:* Ported and refactored _core_ database framework components from _DbEx_ rep.
- *Enhancement:* Ported and refactored extended database and entity framework components from _Beef_ repo.
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
- *Enhancement:* **Breaking change**: The event publishing (`IEventPublisher`) is now designed to occur in three distinct phases: 1) formatting (`EventDataFormatter.Format`), 2) serialization (`IEventSerializer.SerializeAsync`), and 3) sending (`IEventSender.SendAsync`). The `EventPublisher` has been added to orchestrate this flow.
- *Enhancement:* Updated the `IJsonSerializer` implementation defaults to align with the expected default serialization behavior.
- *Fixed:* The `TypedHttpClientBase` fixed to handle where the `requestUri` parameter is only a query string and not a path.

## v1.0.1
- *New:* Initial publish to GitHub/NuGet.