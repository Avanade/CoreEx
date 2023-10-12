 # Change log

Represents the **NuGet** versions.

## v3.4.0
- *Enhancement:* Added `IEventSubscriberInstrumentation` (and related `EventSubscriberInstrumentationBase`) to enable `EventSubscriberBase.Instrumentation` monitoring of the subscriber as applicable.
- *Enhancement:* Previous `EventSubscriberInvoker` exception/error handling moved into individual subscribers for greater control; a new `ErrorHandler` added to encapsulate the consistent handling of the underlying exceptions/errors. This was internal and should have no impact.
- *Enhancement:* `ErrorHandling.ThrowSubscriberException` renamed to `Handle` and `ErrorHandling.TransientRetry` renamed to `Retry`. Old names have been obsoleted and as such will generate a compile-time error where not corrected.
- *Enhancement:* Added `DataConsistencyException` to support the throwing of possible data consistency issues; internally integrated throughout _CoreEx_.
- *Enhancement:* Added `IDatabase.SqlFromResource` support to enable simple access to SQL statements embedded as a resource within a specified assembly.
- *Enhancement:* `Result.When*` methods updated to support _optional_ `otherwise` function to enable `if/then/else` scenarios (only invoked where `Result.IsSuccess`).

## v3.3.1
- *Fixed:* `ServiceBusSubscriber` was not correctly bubbling (not handling) exceptions where `UnhandledHandling` was set to `ErrorHandling.None`. Was incorrectly treating same as `ErrorHandling.ThrowSubscriberException` and automatically dead-lettering and continuing.

## v3.3.0
- *Enhancement:* [Distributed tracing](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/distributed-tracing-instrumentation-walkthroughs#best-practices) has been added via the `InvokerBase` set of classes throughout `CoreEx` to ensure coverage and consistency of implementation. A new `InvokeArgs` has been added to house the [`ActivitySource`](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.activitysource) instance; this also provides for further extension opportunities limiting future potential breaking changes.

## v3.2.0
- *Enhancement:* Added `ServiceBusReceiverActions` as a means to encapsulate the `ServiceBusReceivedMessage` and `ServiceBusReceiver` as a `ServiceBusMessageActions` equivalent to enable both the `ServiceBusSubscriber` and `ServiceBusOrchestratedSubscriber` to be leveraged outside of native Azure Functions.
- *Enhancement:* Added support for [claim-check pattern](https://learn.microsoft.com/en-us/azure/architecture/patterns/claim-check) for large messages. `EventData.Value` serialization to be stored as an attachment in the likes of blob storage and then referenced (claim-check) from the actual message. A new `IAttachmentStorage` encapsulates the attachment behavior with the `IEventSerializer` implementations referencing as applicable; whereby separating this behavior from the `IEventSender` enabling greater consistency and reuse. Added `BlobAttachmentStorage` and `BlobSasAttachmentStorage` to support Azure blob storage.

## v3.1.1
- *Fixed:* The `DatabaseParameterCollection.AddParameter` now explicitly sets the `DbParameter.Value` to `DbNull.Value` where the value passed in is `null`.

## v3.1.0
- *Enhancement:* Added `Hosting.ServiceBase` class for a self-orchestrated service to execute for a specified `MaxIterations`; provides an alternative to using a `HostedService`. Useful for the likes of timer trigger Azure Functions for eample.
- *Enhancement:* Added `EventOutboxService` as an alternative to `EventOutboxHostedService`; related to (and leverages) above to achieve same outcome.
- *Fixed:* `Database.OnDbException` was incorrectly converting the unhandled exception to a `Result`; will now throw as expected.

## v3.0.0
- *Enhancement:* Added new `CoreEx.Results` namespace with primary `Result` and `Result<T>` classes to enable [monadic](https://en.wikipedia.org/wiki/Monad_(functional_programming)) error-handling, often referred to [Railway-oriented programming](https://swlaschin.gitbooks.io/fsharpforfunandprofit/content/posts/recipe-part2.html); see [`CoreEx.Results`](./src/CoreEx/Results/README.md) for more implementation details. Thanks [Adi](https://github.com/AdiThakker) for inspiring and guiding on this change. Related changes as follows:
  - *Enhancement:* `EventSubscriberBase`, `SubscriberBase` and `SubscriberBase<T>` modified to include `EventSubscriberArgs` (`Dictionary<string, object?>`) to allow other parameters to be passed in. The `ReceiveAsync` methods now support the args as a parameter, and must return a `Result` to better support errors; breaking change. 
  - *Enhancement:* Where overriding `Validator.OnValidateAsync` this method must return a `Result`, as does the `CustomRule` (for consistency); breaking change. The `Result` enables other errors to be returned avoiding the need/cost to throw an exception.
  - *Enhancement:* `ExecutionContext` user authorization methods have been renamed (`UserIsAuthorized` and `UserIsInRole`) and explicitly leverage `Result`; breaking change.
  - *Enhancement:* `IReferenceDataProvider.GetAsync` method now supports a return type of `Result<T>`; breaking change.
- *Enhancement:* The `WebApi` namespace has been moved to a new `CoreEx.AspNetCore` project/package to decouple these explicit [ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/introduction-to-aspnet-core) capabilities from the core; breaking change.
  - The `IExceptionResult` interface has been deprecated as a result; all exceptions have been updated accordingly.
- *Fixed:* Validation extension method `EnsureValue` has been renamed to `Required` to be more explicit as to purpose; breaking change.
- *Fixed:* `InvokerBase` and `InvokerBase<TResult>` now split sync and async code to avoid sync over async; requires both the sync and async virtual methods to be overridden to implement correctly.
- *Enhancement:* Ad-hoc performance optimizations; some minor breaking changes primarily impacting internal usage.
- *Enhancement:* Added `net6.0` and `net7.0` support in addition to [.NET Standard](https://learn.microsoft.com/en-us/dotnet/standard/net-standard#when-to-target-net50-or-net60-vs-netstandard) to all packages. This will allow access to additional features per version where required, and overall performance improvements.
- *Enhancement:* Added `CoreEx.Solace` to enable the publishing of messages to [Solace](https://solace.com/) message broker; thanks [Israel](https://github.com/israels).
- *Enhancement:* Updated `CoreEx.Cosmos` to support direct model queries using `ModelQuery` methods where applicable.
- *Enhancement:* Added `PagingOperationFilterFields` to allow specific selection of fields for the `PagingOperationFilter`. This was influenced by pull request [67](https://github.com/Avanade/CoreEx/pull/67).

## v2.10.1
- *Fixed:* `EventOutboxHostedService` updated so when a new `IServiceScope` is created that `ExecutionContext.Reset` is invoked to ensure existing `ServiceProvider` is not reused.
- *Fixed:* `EventDataFormatter` defaults `PartitionKey` and `TenantId` properties, where not already set, from the value where implements `IPartitionKey` and `ITenantId` respectively.

## v2.10.0
- *Enhancement:* Added `IServiceBusSubscriber` with following properties: `AbandonOnTransient` (perform an Abandon versus bubbling exception), `MaxDeliveryCount` (max delivery count check within subscriber), `RetryDelay` (basic transient retry specification) and `MaxRetryDelay` (defines upper bounds of retry delay). These are defaulted from corresponding `IConfiguration` settings. Both the `ServiveBusSubscriber` and `ServiveBusOrchestratedSubscriber` implement; related logic within `ServiceBusSubscriberInvoker`.
- *Enhancement:* Added `RetryAfterSeconds` to `TransientException` to allow overriding; defaults to `120` seconds.
- *Fixed:* Log output from subscribers will no longer write exception stack trace where known `IExtendedException` (noise reduction).
- *Fixed:* `ValidationException` message reformatted such that newlines are no longer included (message simplification).

## v2.9.1
- *Fixed:* The dead-lettering within `ServiceBusSubscriberInvoker` will write the exception stack trace, etc. to a new message property named `SubscriberException` to ensure this content is consistently persisted, with related error description being the exception message only.

## v2.9.0
- *Enhancement:* Added `PagingAttribute` and `PagingOperationFilter` to enable swagger output of `PagingArgs` parameters for an operation.

## v2.8.0
- *Enhancement:* Added `CoreEx.EntityFrameworkCore` support for .NET framework `net7.0`.
- *Enhancement:* Updated `ServiceBusSubscriberInvoker` to improve logging, including opportunities to inherit and add further before and after processing logging and/or monitoring.
- *Enhancement:* Updated `ServiceBusOrchestratedSubscriber` to perform a `LogInformation` on success.
- *Enhancement:* The `TypedHttpClientBase<TSelf>` will probe settings by `GetType().Name` to enable settings per implementation type as an overridding configurable option.
- *Fixed:* `HttpResult.CreateExtendedException` passes inner `HttpRequestException` for context. 
- *Fixed:* `EventSubscriberOrchestrator.AmbiquousSubscriberHandling` is correctly set to `ErrorHandling.CriticalFailFast` by default.

## v2.7.0
- *Enhancement:* Simplified usage for `TypedHttpClientCore` and `TypedHttpClientBase` such that all parameters with the exception of `HttpClient` default where not specified.
- *Enhancement:* `IServiceCollection` extension methods for `CoreEx.Validation` and `CoreEx.FluentValidation` support option to include/exclude underlying interfaces where performing register using `AddValidator` and `AddValidators`.
- *Enhancement:* Enable interoperability between `CoreEx.Validation` and a non-`CoreEx.Validation` mapped `IValidator`; see `Interop` validation extension method.
- *Enhancement:* `ServiceBusOrchestratedSubscriber` added to support orchestrated (`EventSubscriberOrchestrator`) event subscribers (`IEventSubscriber`, `SubscriberBase` and `SubscriberBase<T>`) based on matching metadata (`EventSubscriberAttribute`) to determine which registered subscriber to execute for the current `ServiceBusReceivedMessage`.
- *Enhancement:* `BlobLockSynchronizer` added to perform `IServiceSynchronizer` using Azure Blob storage.
- *Fixed:* Resolved transaction already diposed exception for the `EventOutboxHostedService` by creating a new `IServiceProvider` scope and instantiating a `EventOutboxDequeueBase` per execution to ensure all dependencies are reinstantiated where applicable.
- *Enhancement:* Updated all package dependencies to latest.

## v2.6.0
- *Enhancement:* `ReferenceDataOrchestrator` supports `IConfigureCacheEntry` to enable flexibility of configuration; no changes to current behaviour.
- *Fixed:* `ReferenceDataBase` was not correctly managing the `Id` and `IdType` throughout the inheritence hierarchy.
- *Enhancement:* Database, Entity Framework, and Cosmos capabilities can be configured within their respective `*Args` to perform a `Cleaner.Clean` automatically on the response. Defaults to `false` to maintain current functionality.
- *Fixed:* `Mapper<T>` was not correctly initializing nullable destination properties during a `Flatten`; for example, a destination `DateTime?` was being set with a `DateTime.MinValue` where source property was not nullable. A new `InitializeDestination` can be optionally specified (or overridden) to perform; otherwise, current initialization behavior will continue.

## v2.5.3
- *Fixed:* Database `RowVersion` conversion fixed to correctly enable per database provider.

## v2.5.2
- *Fixed:* `ReferenceDataOrchestrator` further updated to attempt to use `ExecutionContext` where possible when `Current` has not  previously been set; this is similar to previous behaviour (< `2.5.1`).
- *Fixed:* `ReferenceDataOrchestrator` updated to leverage `AsyncLocal` for `Current` to remove `static` value leakage; lifetime within the context of the request.

## v2.5.1
- *Fixed:* `System.ObjectDisposedException: Cannot access a disposed object` for the `IServiceProvider` has been resolved where reference data loading (`ReferenceDataOrchestrator`), that in turn loaded child reference data. A new start up `UseReferenceDataOrchestrator` method simplifies set up.

## v2.5.0
- *Enhancement:* Added string casing support to `Cleaner` and `EntityCore` using new `StringCase`; being `None` (as-is default), `Upper`, `Lower` and `Title`. Leverages standard .NET  `TextInfo` to implement underlying case conversion. 
- *Fixed:* Applied all changes identified by Code Analysis.
- *Fixed:* `NullReferenceException` in `EntityBaseDictionary` where item value is `null` corrected.
- *Enhancement:* Added `KeyModifier` function to `ObservableDictionary` to allow key manipulation to ensure consistency, i.e. all uppercase.
- *Fixed:* Potential SQL Injection opportunity within `DatabaseExtendedExtensions.ReferenceData` corrected when schema and table names were being specified explicitly; now quoted using `DbCommandBuilder.QuoteIdentifier`.

## v2.4.0
- *Enhancement:* Added `CompareValuesRule`, `EnumRule` and `EnumValueRule` as additional validation rules.

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