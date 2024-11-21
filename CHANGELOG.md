 # Change log

Represents the **NuGet** versions.

## v3.30.0
- *Enhancement:* Integrated `UnitTestEx` version `5.0.0` to enable the latest capabilities and improvements.
  - `CoreEx.UnitTesting.NUnit` given changes is no longer required and has been deprecated, the `UnitTestEx.NUnit` (or other) must be explicitly referenced as per testing framework being used.
  - `CoreEx.UnitTesting` package updated to include only standard .NET core capabilities to follow new `UnitTestEx` pattern; new packages created to house specific as follows:
    - `CoreEx.UnitTesting.Azure.Functions` created to house Azure Functions specific capabilities;
    - `CoreEx.UnitTesting.Azure.ServiceBus` created to house Azure Service Bus specific capabilities.
  - Existing usage will require references to the new packages as required. There should be limited need to update existing tests to use beyond the requirement for the root `UnitTestEx` namespace. The updated default within `UnitTestEx` is to expose the key capabilities from the root namespace. For example, `using UnitTestEx.NUnit`, should be replaced with `using UnitTestEx`.

## v3.29.0
- *Enhancement:* Added `net9.0` support.
- *Enhancement:* Deprecated `net7.0` support; no longer supported by [Microsoft](https://dotnet.microsoft.com/en-us/platform/support/policy).
- *Enhancement:* Updated dependencies to latest; including transitive where applicable.

## v3.28.0
- *Enhancement:* Added extended capabilities to the `InvokeArgs` to allow additional customization.

## v3.27.3
- *Fixed:* The `ExecutionContext.Messages` were not being returned as intended within the `x-messages` HTTP Response header; enabled within the `ExtendedStatusCodeResult` and `ExtendedContentResult` on success only (status code `>= 200` and `<= 299`). Note these messages are JSON serialized as the underlying `MessageItemCollection` type.
- *Fixed:* The `AgentTester` has been updated to return a `HttpResultAssertor` where the operation returns a `HttpResult` to enable further assertions to be made on the `Result` itself.

## v3.27.2
- *Fixed:* The `IServiceCollection.AddCosmosDb` extension method was registering as a singleton; this has been corrected to register as scoped. The dependent `CosmosClient` should remain a singleton as is [best practice](https://learn.microsoft.com/en-us/azure/cosmos-db/nosql/best-practice-dotnet).

## v3.27.1
- *Fixed:* Updated `Microsoft.Extensions.Caching.Memory` package depenedency to latest (including related); resolve [Microsoft Security Advisory CVE-2024-43483](https://github.com/advisories/GHSA-qj66-m88j-hmgj).
- *Fixed:* Fixed the `ExecutionContext.UserIsAuthorized` to have base implementation similar to `UserIsInRole`.
- *Fixed:* Rationalize the `UtcNow` usage to be consistent, where applicable `ExecutionContext.SystemTime.UtcNow` is leveraged.

## v3.27.0
- *Fixed:* The `ValueContentResult.TryCreateValueContentResult` would return `NotModified` where the request `ETag` was `null`; this has been corrected to return `OK` with the resulting `value`.
- *Fixed:* The `ValueContentResult.TryCreateValueContentResult` now returns `ExtendedStatusCodeResult` versus `StatusCodeResult` as this offers additional capabilities where required.
- *Enhancement:* The `ExtendedStatusCodeResult` and `ExtendedContentResult` now implement `IExtendedActionResult` to standardize access to the `BeforeExtension` and `AfterExtension` functions.
- *Enhancement:* Added `WebApiParam.CreateActionResult` helper methods to enable execution of the underlying `ValueContentResult.CreateValueContentResult` (which is no longer public as this was always intended as internal only).
- *Fixed:* `PostgresDatabase.OnDbException` corrected to use `PostgresException.MessageText` versus `Message` as it does not include the `SQLSTATE` code.
- *Enhancement:* Improve debugging insights by adding `ILogger.LogDebug` start/stop/elapsed for the `InvokerArgs`.
- *Fixed*: Updated `System.Text.Json` package depenedency to latest (including related); resolve [Microsoft Security Advisory CVE-2024-43485](https://github.com/advisories/GHSA-8g4q-xg66-9fp4).

## v3.26.0
- *Enhancement:* Enable JSON serialization of database parameter values; added `DatabaseParameterCollection.AddJsonParameter` method and associated `JsonParam`, `JsonParamWhen` and `JsonParamWith` extension methods.
- *Enhancement:* Updated (simplified) `EventOutboxEnqueueBase` to pass events to the underlying stored procedures as JSON versus existing TVP removing database dependency on a UDT (user-defined type).
- **Note:** Accidently published as `v3.25.6`, re-publishing as `v3.26.0` as intended - includes no code changes.

## v3.25.5
- *Fixed:* Fixed the unit testing `CreateServiceBusMessage` extension method so that it no longer invokes a `TesterBase.ResetHost` (this reset should now be invoked explicitly by the developer as required).

## v3.25.4
- *Fixed*: Fixed the `InvalidOperationException` with a 'Sequence contains no elements' when performing validation with the `CompareValuesRule` that has the `OverrideValue` set. 
- *Fixed:* Updated all dependencies to latest versions.

## v3.25.3
- *Fixed:* Added function parameter support for `WithDefault()` to enable runtime execution of the default statement where required for the query filter capabilities.

## v3.25.2
- *Fixed:* `HttpRequestOptions.WithQuery` fixed to ensure any previously set `Include` and `Exclude` fields are not lost (results in a merge); i.e. only the `Filter` and `OrderBy` properties are explicitly overridden.

## v3.25.1
- *Fixed:* Extend `QueryFilterFieldConfigBase` to include `AsNullable()` to specifiy whether the field supports `null`.
- *Fixed:* Extend `QueryFilterFieldConfigBase` to include `WithResultWriter()` to specify a function to override the corresponding LINQ statement result writing.
- *Fixed:* Adjusted the fluent-style method-chaining interface to improve usability (and consistency).

## v3.25.0
- *Enhancement:* Added new `CoreEx.Data` project/package to encapsulate all generic data-related capabilities, specifically the new `QueryFilterParser` and `QueryOrderByParser` classes. These enable a limited, explicitly supported, dynamic capability to `$filter` and `$orderby` an underlying query _similar_ to _OData_. This is **not** intended to be a replacement for the full capabilities of OData, GraphQL, etc. but to offer basic dynamic flexibility where needed.
  - Added `IQueryable<T>.Where()` and `IQueryable<T>.OrderBy` extension method that will use the aforementioned parsers configured within the new `QueryArgsConfig` and `QueryArgs` and apply leveraging `System.Linq.Dynamic.Core`.
  - Updated `HttpRequestOptions` and `WebApiRequestOptions` to support `QueryArgs` (`$filter` and `$orderby` query string arguments) similar to the existing `PagingArgs`.
  - Added `QueryAttribute` to enable _Swagger/Swashbuckle_ generated documentation.
- *Fixed:* Fixed missing `IServiceCollection.AddCosmosDb` including corresponding `CosmosDbHealthCheck`.
- *Fixed:* Added `JsonIgnore` to all interfaces that have a `CompositeKey` property as _not_ intended to be serialized by default.
- *Fixed:* Fixed `ReferenceDataCollectionBase<TId, TRef, TSelf>` constructor which was hiding `sortOrder` and `codeComparer` parameters.

## v3.24.1
- *Fixed*: `CosmosDb.SelectMultiSetWithResultAsync` updated to skip items that are not considered valid; ensures same outcome as if using a `CosmosDbModelQueryBase` with respect to filtering.

## v3.24.0
- *Enhancement:* `CosmosDb.SelectMultiSetWithResultAsync` and `SelectMultiSetAsync` added to enable the selection of multiple sets of data in a single operation; see also `MultiSetSingleArgs` and `MultiSetCollArgs`.
- *Enhancement:* `CosmosDbValue.Type` is now updatable and defaults from `CosmosDbValueModelContainer<TModel>.TypeName` (updateable using `UseTypeName`).

## v3.23.5
- *Fixed:* `CosmosDbValue<TModel>.PrepareBefore` corrected to set the `PartitionKey` where the underlying `Value` implements `IPartitionKey`.
- *Fixed:* `CosmosDbBatch` corrected to default to the `CosmosDbContainerBase<TSelf>.DbArgs` where not specified.
- *Fixed:* `CosmosDbArgs.AutoMapETag` added, indicates whether when mapping the model to the corresponding entity that the `IETag.ETag` is to be automatically mapped (default is `true`, existing behavior).
 
## v3.23.4
- *Fixed:* Added `Result<T>.AdjustsAsync` to support asynchronous adjustments.

## v3.23.3
- *Fixed:* Added `Result<T>.Adjusts` as wrapper for `ObjectExtensions.Adjust` to simplify support and resolve issue where the compiler sees the adjustment otherwise as a implicit cast resulting in an errant outcome.

## v3.23.2
- *Fixed:* `DatabaseExtendedExtensions.DeleteWithResultAsync` corrected to return a `Task<Result>`.`

## v3.23.1
- *Fixed:* Updated all dependencies to latest versions (specifically _UnitTestEx_).

## v3.23.0
- *Enhancement:* Added `ICacheKey` and updated `RequestCache` accordingly to support, in addition to the existing `IEntityKey`, to enable additional caching key specification.
- *Enhancement:* Added `ItemKeySelector` to `EntityBaseDictionary` to enable automatic inference of the key from an item being added.
- *Fixed:* Updated all dependencies to latest versions.

## v3.22.0
- *Enhancement:* Identifier parsing and `CompositeKey` formatting moved to the `CosmosDbArgs` to enable overriding where required.
- *Enhancement:* Cosmos model constraint softened to allow for `IEntityKey` to support more flexible identifier scenarios.
- *Enhancement:* All Cosmos methods updated to support `CompositeKey` versus `object` for identifier specification for greater flexibility.
- *Enhancement:* `CosmosDbModelContainer` and `CosmosDbValueModelContainer` enable model-only access; also, all model capabilities housed under new `Model` namespace.
- *Fixed:* `PagingOperationFilter` correctly specifies a format of `int64` for the `number`-type paging parameters.
- *Fixed:* `CompositeKey` correctly supports `IReferenceData` types leveraging the underlying `IReferenceData.Code`.

## v3.21.1
- *Fixed:* `Mapper.MapSameTypeWithSourceValue` added (defaults to `true`) to map the source value to the destination value where the types are the same; previously this would result in an exception unless added explicitly. The `Mapper.SameTypeMapper` enables.
- *Fixed:* `ReferenceDataOrchestrator.GetAllTypesInNamespace` added to get all the `IReferenceData` types in the specified namespace. Needed for the likes of the `CosmosDbBatch.ImportValueBatchAsync` where a list of types is required.

## v3.21.0
- *Enhancement*: `CoreEx.Cosmos` improvements:
  - Added `CosmosDbArgs` to `CosmosDbContainerBase` to allow per container configuration where required.
  - Partition key specification centralized into `CosmosDbArgs`.
  - `ITenantId` and `ILogicallyDeleted` support integrated into `CosmosDbContainerBase`, etc. to offer consistent behavior with `EfDb`.

## v3.20.0
- *Fixed*: Include all constructor parameters when using `AddReferenceDataOrchestrator`.
- *Enhancement*: Integrated dynamic `ITenantId` filtering into `EfDb` (controlled with `EfDbArgs`). 

## v3.19.0
- *Fixed:* Updated all dependencies to latest versions.
- *Enhancement:* Added `DatabaseCommand.SelectAsync` and `SelectWithResultAsync` that has no integrated typing and mapping.

## v3.18.1
- *Fixed*: The `ITypedMappedHttpClient.MapResponse` was not validating the input HTTP response correctly before mapping; resulted in a `null` success value versus the originating error/exception.
- *Fixed*: The `HttpResult<T>.ThrowOnError` was not correctly throwing the internal exception. 

## v3.18.0
- *Fixed*: Removed `Azure.Identity` dependency as no longer required; related to `https://github.com/advisories/GHSA-wvxc-855f-jvrv`.
- *Fixed*: Removed `AspNetCore.HealthChecks.SqlServer` dependency as no longer required.
- *Fixed:* Updated all dependencies to latest versions.
- *Fixed*: `CoreEx.AutoMapper` updated to leverage latest major version (`13.0.1`); as such `netstandard` no longer supported.
- *Fixed*: The `TimerHostedServiceBase` was incorrectly resetting the `LastException` on sleep versus wake. 
- *Fixed*: The `AddEventSender` dependency injection extension methods now correctly register as _Scoped_.
- *Fixed*: The `Logger.LogInformation` invocations refactored to `Logger.LogDebug` where applicable to reduce noise in the logs.
- *Fixed*: The `IPropertyRule.ValidateAsync` method removed as it was not required and could lead to incorrect usage.
- *Fixed:* The `ValueValidator` now only supports a `Configure` method to enable `IPropertyRule`-based configuration (versus directly).
- *Fixed:* The `CommonValidator.ValidateAsync` is now internal as this was not intended and could lead to incorrect usage.
- *Enhancement*: Added `AfterSend` event to `IEventSender` to enable post-send processing.
- *Enhancement*: Added `EventOutboxHostedService.OneOffTrigger` method to enable a _one-off_ trigger interval to be specified for the registered (DI) instance.

## v3.17.0
- *Enhancement*: Additional `CoreEx.Validation` usability improvements:
  - `Validator.CreateFor<T>` added to enable the creation of a `CommonValidator<T>` instance for a specified type `T` (more purposeful name); synonym for existing `CommonValidator.Create<T>` (unchanged).
  - `Validator.Null<T>` added to enable simplified specification of a `IValidatorEx<T>` of `null` to avoid explicit `null` casting.
  - `Collection` extension method has additional overload to pass in the `IValidatorEx<TItem>` to use for each item in the collection; versus, having to use `CollectionRuleItem.Create`.
  - `Dictionary` extension method has additional overload to pass in the `IValidatorEx<TKey>` and `IValidator<TValue>` to use for each entry in the dictionary; versus, having to use `DictionaryRuleItem.Create`.
  - `MinimumCount` and `MaximumCount` extension methods for `ICollection` added to enable explicit specification of these two basic validations. 
  - `Validation.CreateCollection` renamed to `Validation.CreateForCollection` and creates a `CommonValidator<TColl>`.
	- Existing `CollectionValidator` deprecated as the `CommonValidator<TColl>` offers same; removes duplication of capability.
  - `Validation.CreateDictionary` renamed to `Validation.CreateForDictionary` and creates a `CommonValidator<TDict>`.
	- Existing `DictionaryValidator` deprecated as the `CommonValidator<TDict>` offers same; removes duplication of capability.
- *Enhancement*: Added `ServiceBusReceiverHealthCheck` to perform a peek message on the `ServiceBusReceiver` as a means to determine health. Use `IHealthChecksBuilder.AddServiceBusReceiverHealthCheck` to configure. 
- *Fixed:* The `FileLockSynchronizer`, `BlobLeaseSynchronizer` and `TableWorkStatePersistence` have had any file/blob/table validations/manipulations moved from the constructor to limit critical failures at startup from a DI perspective; now only performed where required/used. This also allows improved health check opportunities as means to verify.

## v3.16.0
- *Enhancement*: Added basic [FluentValidator](https://docs.fluentvalidation.net/en/latest/) compatibility to the `CoreEx.Validation` by supporting _key_ (common) named capabilities:
  - `AbstractValidator<T>` added as a wrapper for `Validator<T>`; with both supporting `RuleFor` method (wrapper for existing `Property`).
  - `NotEmpty`, `NotNull`, `Empty`, `Null`, `InclusiveBetween`, `ExclusiveBetween`, `Equal`, `NotEqual`, `LessThan`, `LessThanOrEqualTo`, `GreaterThan`, `GreaterThanOrEqualTo`, `Matches`, `Length`, `MinimumLength`, `MaximumLength`, `PrecisionScale`, `EmailAddress` and `IsInEnum` extension methods added (invoking existing equivalents).
  - `NullRule` and `NotNullRule` added to support the `Null` and `NotNull` capabilities specifically.
  - `WithMessage` added to explcitly set the error message for a preceeding `IValueRule` (equivalent to specifying when invoking extension method).
  - `ValidatorStrings` have had their fallback texts added to ensure an appropriate text is output where `ITextProvider` is not available.
  - _Note:_ The above changes are to achieve a basic level of compatibility, they are not intended to implement the full capabilities of _FluentValidation_; nor, will it ever. The `CoreEx.FluentValidation` enables _FluentValidation_ to be used directly where required; also, the existing `CoreEx.Validation.InteropRule` enables interoperability between the two.
- *Enhancement*: Added `StringSyntaxAttribute` support to improve intellisense for JSON and URI specification.
- *Enhancement*: Added `EventPublisherHealthCheck` that will send an `EventData` message to verify that the `IEventPublisher` is functioning correctly.
  - _Note:_ only use where the corresponding subscriber(s)/consumer(s) are aware and can ignore/filter to avoid potential downstream challenges.

## v3.15.0
- *Enhancement*: This is a clean-up version to remove all obsolete code and dependencies. This will result in a number of minor breaking changes, but will ensure that the codebase is up-to-date and maintainable.
  - As per [`v3.14.0`](#v3.14.0) the previously obsoleted `TypedHttpClientBase` methods `WithRetry`, `WithTimeout`, `WithCustomRetryPolicy` and `WithMaxRetryDelay` are now removed; including `TypedHttpClientOptions`, `HttpRequestLogger` and related `SettingsBase` capabilities.
  - Health checks:
	- `CoreEx.Azure.HealthChecks` namespace and classes removed.
	- `SqlServerHealthCheck` replaced with simple generic `DatabaseHealthCheck`.
	- `IServiceCollection.AddDatabase` automatically adds `DatabaseHealthCheck`.
	- `IServiceCollection.AddSqlServerEventOutboxHostedService` automatically adds `TimerHostedServiceHealthCheck`.
	- `IServiceCollection.AddReferenceDataOrchestrator` automatically adds `ReferenceDataOrchestratorHealthCheck` (reports cache statistics).
	- `HealthReportStatusWriter` added to support richer JSON reporting.
	- Generally recommend using 3rd-party library to enable further health checks; for example: [`https://github.com/Xabaril/AspNetCore.Diagnostics.HealthChecks`](https://github.com/Xabaril/AspNetCore.Diagnostics.HealthChecks).

## v3.14.1
- *Fixed*: The `Result.ValidatesAsync` extension method signature has had the value nullability corrected to enable fluent-style method-chaining.
- *Fixed*: The fully qualified type and property name is now correctly used as the `LText.KeyAndOrText` when creating within the `PropertyExpression<TEntity, TProperty>` to enable a qualified _key_ that can be used by the `ITextProvider` to substitute the text at runtime; the existing text fallback behavior remains such that an appropriate text is used. The `PropertyExpression.CreatePropertyLTextKey` function can be overridden to change this behavior.

## v3.14.0
- *Enhancement*: Planned feature obsoletion. The `TypedHttpClientBase` methods `WithRetry`, `WithTimeout`, `WithCustomRetryPolicy` and `WithMaxRetryDelay` are now marked as obsolete and will result in a compile-time warning. Related `TypedHttpClientOptions`, `HttpRequestLogger` and `SettingsBase` capabilities have also been obsoleted.
  - Why? Primarily based on Microsoft guidance around [`IHttpClientFactory`](https://learn.microsoft.com/en-us/dotnet/core/extensions/httpclient-factory) usage. Specifically advances in native HTTP [resilency](https://learn.microsoft.com/en-us/dotnet/core/resilience/http-resilience) support, and the [.NET 8 networking improvements](https://devblogs.microsoft.com/dotnet/dotnet-8-networking-improvements/).
  - When? Soon, planned within the next minor release (`v3.15.0`). This will simplify the underlying `TypedHttpClientBase` logic and remove the internal dependency on an older version of the [_Polly_](https://www.nuget.org/packages/Polly/7.2.4) package.
  - How? Review the compile-time warnings, and [update the codebase](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/implement-resilient-applications/implement-http-call-retries-exponential-backoff-polly) to use the native `IHttpClientFactory` resiliency capabilities.
- *Enhancement*: Updated `CoreEx.UnitTesting` to leverage the latest `UnitTestEx` (`v4.2.0`) which has added support for testing `HttpMessageHandler` and `HttpClient` configurations. This will enable improved mocked testing as a result of the above changes where applicable.
- *Enhancement*: Added `CustomSerializers` property to `IEventSerializer` of type `CustomEventSerializers`. This allows for the add (registration) of custom JSON serialization logic for a specified `EventData.Value` type. This is intended to allow an opportunity to serialize a specific type in a different manner to the default JSON serialization; for example, exclude certain properties, or use a different serialization format.
- *Enhancement*: Updated the unit testing `ExpectedEventPublisher` so that it now executes the configured `IEventSerializer` during publishing. A new `UnitTestBase.GetExpectedEventPublisher` extension method added to simplify access to the `ExpectedEventPublisher` instance and corresponding `GetPublishedEvents` property to enable further assert where required.

## v3.13.0
- *Enhancement*: Added `DatabaseMapperEx` enabling extended/explicit mapping where performance is critical versus existing that uses reflection and compiled expressions; can offer up to 40%+ improvement in some scenarios.
- *Enhancement*: The `AddMappers<TAssembly>()` and `AddValidators<TAssembly>()` extension methods now also support two or three assembly specification overloads.
- *Enhancement*: A `WorkState.UserName` has been added to enable the tracking of the user that initiated the work; this is then checked to ensure that only the initiating user can interact with their own work state.
- *Fixed:* The `ReferenceDataOrchestrator.GetByTypeAsync` has had the previous sync-over-async corrected to be fully async.
- *Fixed*: Validation extensions `Exists` and `ExistsAsync` which expect a non-null resultant value have been renamed to `ValueExists` and `ValueExistsAsync` to improve usability; also they are `IResult` aware and will act accordingly.
- *Fixed*: The `ETag` HTTP handling has been updated to correctly output and expect the weak `W/"xxxx"` format. 
- *Fixed*: The `ETagGenerator` implementation has been further optimized to minimize unneccessary string allocations.
- *Fixed*: The `ValueContentResult` will only generate a response header ETag (`ETagGenerator`) for a `GET` or `HEAD` request. The underlying result `IETag.ETag` is used as-is where there is no query string; otherwise, generates as assumes query string will alter result (i.e. filtering, paging, sorting, etc.). The result `IETag.ETag` is unchanged so the consumer can still use as required for a further operation.
- *Fixed*: The `SettingsBase` has been optimized. The internal recursion checking has been removed and as such an endless loop (`StackOverflowException`) may occur where misconfigured; given frequency of `IConfiguration` usage the resulting performance is deemed more important. Additionally, `prefixes` are now optional.
  - The existing support of referencing a settings property by name (`settings.GetValue<T>("NamedProperty")`) and it using reflection to find before querying the `IConfiguration` has been removed. This was not a common, or intended usage, and was somewhat magical, and finally was non-performant.

## v3.12.0
- *Enhancement*: Added new `CoreEx.Database.Postgres` project/package to support [PostgreSQL](https://www.postgresql.org/) database capabilities. Primarily encapsulates the open-source [`Npqsql`](https://www.npgsql.org/) .NET ADO database provider for PostgreSQL.
  - Added `EncodedStringToUInt32Converter` to support PostgreSQL `xmin` column encoding as the row version/etag.
- *Enhancement*: Migrated sentence case logic from inside `PropertyExpression` into `CoreEx.Text.SentenceCase` to improve discoverablity and reuse opportunities.
- *Fixed:* The `IServiceCollection.AddAzureServiceBusClient` extension method as been removed; the `ServiceBusClient` will need to be instantiated prior to usage. Standard approach is for consumers to create client instances independently.
- *Fixed*: The `WorkOrchestrator.GetAsync<T>()` and `WorkOrchestrator.GetAsync(string type, ..)` methods were not automatically cancelling where expired.
- *Fixed*: The `InvokerArgs` activity tracing updated to correctly capture the `Exception.Message` where an `Exception` has been thrown.
- *Internal*: 
  - All `throw new ArgumentNullException` checking migrated to the `xxx.ThrowIfNull` extension method equivalent.
  - All	_Run Code Analysis_ issues resolved.

## v3.11.0
- *Enhancement*: The `ITypedToResult` updated to correctly implement `IToResult` as the simple `ToResult` where required. 
- *Enhancement*: Added `Result.AsTask()` and `Result<T>.AsTask` to simplify the conversion to a completed `Task<Result>` or `Task<Result<T>>` where applicable.
- *Enhancement*: Added `IResult.IsFailureOfType<TException>` to indicate whether the result is in a failure state and the underlying error is of the specified `TException` type.
- *Enhancement*: Added `EventTemplate` property to the `WebApiPublisherArgs` and `WebApiPublisherCollectionArgs` to define an `EventData` template.
- *Enhancement*: Added `SubscriberBase<T>` constructor overload to enable specification of `valueValidator` and `ValueIsRequired` parameters versus setting properties directly simplifying usage.
- *Enhancement:* Enum renames to improve understanding of intent for event subscribing logic: `ErrorHandling.None` is now `ErrorHandling.HandleByHost` and `ErrorHandling.Handle` is now `ErrorHandling.HandleBySubscriber`.
- *Enhancement:* Simplified the `ServiceBusSubscriber.Receive` methods by removing the `afterReceive` parameter which served no real purpose; also, reversed the `validator` and `valueIsRequired` parameters (order as stated) as the `validator` is more likely to be specified than `valueIsRequired` which defaults to `true`.
- *Enhancement*: Added `CoreEx.Hosting.Work` namespace which includes light-weight/simple foundational capabilities to track and orchestrate work; intended for the likes of [_asynchronous request-response_](https://learn.microsoft.com/en-us/azure/architecture/patterns/async-request-reply) scenarios.
  - Added `IWorkStatePersistence` to enable flexible/pluggable persistence of the `WorkState` and resulting data; includes `InMemoryWorkStatePersistence` for testing, `FileWorkStatePersistence` for file-based, and `TableWorkStatePersistence` leveraging Azure table storage.
  - Added `WorkStateOrchestrator` support to `EventSubscriberBase`, including corresponding `ServiceBusSubscriber` and `ServiceBusOrchestratedSubscriber` using the `ServiceBusMessage.MessageId` as the corresponding `WorkState.Id`.
  - Extended `EventSubscriberArgs` to support a new `SetWorkStateDataAsync` operation to enable the setting of the underlying `WorkState` data is a consistent manner where using the event subscriber capabilities.

## v3.10.0
- *Enhancement*: The `WebApiPublisher` publishing methods have been simplified (breaking change), primarily through the use of a new _argument_ that encapsulates the various related options. This will enable the addition of further options in the future without resulting in breaking changes or adding unneccessary complexities. The related [`README`](./src/CoreEx.AspNetCore/WebApis/README.md) has been updated to document.
- *Enhancement*: Added `ValidationUseJsonNames` to `SettingsBase` (defaults to `true`) to allow setting `ValidationArgs.DefaultUseJsonNames` to be configurable.

## v3.9.0
- *Enhancement*: A new `Abstractions.ServiceBusMessageActions` has been created to encapsulate either a `Microsoft.Azure.WebJobs.ServiceBus.ServiceBusMessageActions` (existing [_in-process_](https://learn.microsoft.com/en-us/azure/azure-functions/functions-dotnet-class-library) function support) or `Microsoft.Azure.Functions.Worker.ServiceBusMessageActions` (new [_isolated_](https://learn.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-guide) function support) and used internally. Implicit conversion is enabled to simplify usage; existing projects will need to be recompiled. The latter capability does not support `RenewAsync` and as such this capability is no longer leveraged for consistency; review documented [`PeekLock`](https://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-service-bus-trigger?tabs=python-v2%2Cisolated-process%2Cnodejs-v4%2Cextensionv5&pivots=programming-language-csharp#peeklock-behavior) behavior to get desired outcome.
- *Enhancement*: The `Result`, `Result<T>`, `PagingArgs` and `PagingResult` have had `IEquatable` added to enable equality comparisons.
- *Enhancement*: Upgraded `UnitTestEx` dependency to `4.0.2` to enable _isolated_ function testing.
- *Enhancement*: Enabled `IJsonSerializer` support for `CompositeKey` JSON serialization/deserialization.
- *Enhancement*: Added `IEventDataFormatter` which when implemented by the value set as the `EventData.Value` allows additional formatting to be applied by the `EventDataFormatter`.
- *Enhancement*: Added `IsMapNullIfNull` to `BidirectionalMapper` that indicates whether to map `null` source value to a corresponding `null` destination automatically.
- *Fixed*: Added `ReferenceDataMultiDictionaryConverterFactory` to ensure each `IReferenceDataCollection` is serialized correctly according to its underlying type.
- *Fixed*: `EventDataFormatter` and `CloudEventSerializerBase` updated to correctly set the `Key` property where applicable.
- *Internal:* Upgraded `NUnit` dependency to `4.0.1` for all `CoreEx` unit test; also, all unit tests now leverage the [_NUnit constraint model_](https://docs.nunit.org/articles/nunit/writing-tests/assertions/assertion-models/constraint.html) testing approach.

## v3.8.1
- *Fixed*: The `CoreEx.Text.JsonSerializer` has been updated to cache the _indented_ option correctly.
- *Fixed*: The `ReferenceDataOrchestator` updated to use the correct serializer for `ETag` generation. 

## v3.8.0
- *Enhancement*: The `ValueContentResult.CreateResult` has been updated to return the resulting value as-is where is an instance of `IActionResult`; otherwise, converts `value` to a `ValueContentResult` (previous behavior).
- *Enhancement*: The `PagingArgs` has been extended to support `Token`; being a continuation token to enable paging to be performed where the underlying data source does not support skip/take-style paging.

## v3.7.2
- *Fixed*: The `ReferenceDataMultiCollection` and `ReferenceDataMultiItem` have been replaced with the `ReferenceDataMultiDictionary` as existing resulted in an unintended format with which to return the data. This fix also removed the need for the `ReferenceDataMultiCollectionConverterFactory` as custom serialization for this is no longer required.

## v3.7.1
- *Fixed*: The `WebApi.PutWithResultAsync` methods that support `get` function parameter have had the result nullability corrected.
- *Fixed*: The `BidirectionalMapper<TFrom, TTo>` has been added to further simplify the specification of a bidirectional mapping capability.

## v3.7.0
- *Enhancement:* The `Mapper<TSource, TDestination>` has a new constructor override to enable the specification of the mapping (`OnMap` equivalent) logic.
- *Enhancement:* The `Mapper` has had `When*` helper methods added to aid the specification of the mapping logic depending on the `OperationTypes` (singular) being performed.
- *Enhancement:* A new `NoneRule` validation has been added to ensure that a value is none (i.e. must be its default value). 

## v3.6.3
- *Fixed:* All related package dependencies updated to latest.

## v3.6.2
- *Enhancement:* Added `Converter.Create<TSource, TDestionation>` to enable a simple one-off `IConverter<TSource, TDestionation>` implementation to be created.
- *Fixed:* The `IReferenceData.SetInvalid` method corrected to throw `NotImplementedException` where not explicitly implemented.
- *Fixed:* The `ReferenceDataBase` updated to handle the `IsValid` and `SetInvalid` functionality correctly.

## v3.6.1
- *Enhancement:* Added `IBidirectionalMapper<TFrom, TTo>` to enable a single mapping capability that can support mapping both ways.
- *Enhancement:* Added `IBidirectionalMapper<TFrom, TTo>` registration support to `Mapper.Register` and by extension `IServiceCollection.AddMappings`.
- *Enhancement:* Finalized initial capabilities for `CoreEx.OData`; package now published.

## v3.6.0
- *Enhancement:* `UnitTestEx` as of `v4.0.0` removed all dependencies to `CoreEx`, breaking a long-time circular reference challenge.  Added extension capabilities to enable existing behaviors. These extensions have been added within `CoreEx.UnitTesting` and `CoreEx.UnitTesting.NUnit` respectively; using `UnitTestEx` namespace to minimize breaking changes and clearly separate. The following will need to be corrected where applicable:
  - Add `UnitTestEx` namespace where missing to enable new extension methods.  
  - Replace existing `TestSetUp.Default.ExpectedEventsEnabled = true` with `TestSetUp.Default.EnableExpectedEvents()`; changed to a method as extension properties are not currently supported in C#.
  - Replace existing `TestSetUp.Default.ExpectNoEvents = true` with `TestSetUp.Default.ExpectNoEvents()`; changed to a method as extension properties are not currently supported in C#.
  - The existing `ApiTester.Agent` property has had to be made an extension method as follows:
	- Before: `test.Agent<ContactAgent, Contact>().Expect...`
	- After: `test.Agent().With<ContactAgent, Contact>().Expect...`
  - The `ValidationTester` has _not_ been ported; but has been implemented using extension methods on the `GenericTester` as follows:
	- Before: `ValidationTester.Create().ExpectErrors("").Run<XxxValidator, Xxx>(x);`
	- After: `GenericTester.Create().ExpectErrors("").Validation().With<XxxValidator, Xxx>(x);`
- *Enhancement:* Added `net8.0` support.

## v3.5.0
- *Enhancement:* Update the `JsonFilterer` classes to support qualified (indexed) property names; all paths are standardized with the `$` prefix internally.
- *Enhancement:* Added `JsonNode` extension methods `ApplyInclude` and `ApplyExclude` to simplify corresponding `JsonFilterer` usage.
- *Enhancement:* Added `JsonElementComparer` to compare two `JsonElement` values (and typed values) and return the differences (`JsonElementComparerResult`). Additionally, the `JsonElementComparerResult.ToMergePatch` will create a corresponding `JsonNode` that represents an `application/merge-patch+json` representation of the differences.
- *Enhancement:* Added `DateTimeToStringConverter` to enable the explicit formatting of a `DateTime` to a `string` and vice-versa.
- *Enhancement:* Added `JsonObjectMapper` to enable explicit mapping of a `Type` (class) to a `JsonObject` and vice-versa (versus serialization). This enables property conversion, mapping and operation types to be specified, similar to other _CoreEx_ mapping capabilities.
- *Enhancement:* Renamed `WebApiPublisher.PublishAsync<TColl, TItem>` to `WebApiPublisher.PublishCollectionAsync<TColl, TItem>` to be more explicit with respect to purpose and usage.
- *Enhancement:* The `WebApiPublisher.PublishAsync` has had the `eventModifier` delegate parameter simplified to no longer include a value as this is already available via the `Value` property of the existing `EventData` parameter.
- *Enhancement:* Added additional overloads to `WebApiPublisher.PublishAsync` and `WebApiPublisher.PublishCollectionAsync` to support the event publishing of a different (mapped) type where applicable; see `eventModifier` delegate parameter. Additionally, supports `WebApiPublisher.Mapper` to convert/map by default where applicable.
- *Fixed:* The `Result.OnFailure*` methods corrected to pass in the `Error` versus the `Value` (previously throwing incorrect exception as a result). 
- *Enhancement:* Added new `CoreEx.OData` project/package to support [OData](https://learn.microsoft.com/en-us/odata/overview) capabilities. Primarily encapsulates the open-source [`Simple.OData.Client`](https://github.com/simple-odata-client/Simple.OData.Client).
	- _Note:_ this package has not been published as this is currently considered experimental; is subject to future change and/or removal.
- *Enhancement:* Added new `CoreEx.Dataverse` project/package to support [Microsoft Dataverse](https://docs.microsoft.com/en-us/powerapps/developer/data-platform/choose-data-platform) (formerly known as Common Data Service or CDS) capabilities. Primarily encapsulates the [`ServiceClient`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.powerplatform.dataverse.client.serviceclient) and mappings to/from the _Dataverse_ entities.
	- _Note:_ this package has not been published as this is currently considered experimental; is subject to future change and/or removal.

## v3.4.1
- *Fixed:* The `IEfDb.With` fixed (as extension methods) to also support the `with` value being passed into the corresponding `Action<T>` to simplify usage (only a subset of common intrinsic types supported, both nullable and non-nullable overloads).
- *Fixed:* Missing `Result.CacheSet` and `Result.CacheRemove` extension methods added to `CoreEx.Results` to fully enable `IRequestCaching` in addition to existing `Result.CacheGetOrAddAsync`.

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