 # Change log

Represents the **NuGet** versions.

## v4.1.0
- *Enhancement:* Added `NamedDestinationProvider` (`AddNamedDestinationProvider()`); events are sent to the shared destination (topic), whereas commands are sent to a per-domain queue named `{Destination}-{domain}` so that only the addressed domain receives them. A `null` or empty command domain is self-addressed to the `IHostSettings.DomainName`.
- *Enhancement:* Added keyed `WithReceiver`/`WithSessionReceiver` and `WithKeyedSubscribedSubscriber` support to `AzureServiceBusReceiving` to enable multiple receivers (e.g. events topic and commands queue) within a single Subscribe host.
- *Enhancement:* Updated `CoreEx.Template` hosts to use the `NamedDestinationProvider` and a keyed subscriber by default, with the Service Bus topology now code-based in the generated `Test.Common` `ServiceBus` class.
- *Enhancement:* Added/extended end-to-end (`WithAspireTester`) Aspire testing capabilities within `CoreEx.UnitTesting`: `DistributedApplication` helpers to migrate SQL Server/PostgreSQL databases, clear the Redis cache and reset the Azure Service Bus emulator (queues, topics and subscriptions) from code; `AddEndpoints`, `AddHostedServiceSupport` and `DisableHttpCertificateValidation` AppHost extensions; and a WireMock-based `MockHost` to stub third-party HTTP dependencies. The `CoreEx.Template` `coreex-aspire` template now generates the AppHost, `MockHost` and `Test.Aspire` projects.
- *Enhancement:* Added `coreex-command-publish-e2e`, `coreex-command-subscribe-e2e` and `coreex-aspire` AI skills, with associated instructions, prompts and documentation.
- *Enhancement:* Samples migrated to command queues; Aspire end-to-end tests now included in CI (`net10.0` only).
- *Enhancement:* Updated `CoreEx.CodeGen` to support mutability of reference data (side-effect was a rename of the previously generated `ReferenceDataService` (manually remove existing) to `ReferenceDataProvider`).

## v4.0.0
- This is a **major** version release; a re-imagine / re-invention of the existing capabilities to enable a more modern, flexible and maintainable codebase.
  - This release contains **significant breaking changes** - there is **no** upgrade path from the previous `v3.x` versions; however, the core capabilities and patterns remain largely consistent.
  - A number of capabilities have been removed as they were not widely used, considered legacy/obsolete, or there are better alternatives available.
  - Not all existing capabilities have been re-implemented in this release; the intention is to (re-)add further capabilities in future releases as required.
- Special call out to key contributors: [israels](https://github.com/israels), [spruit-avanade](https://github.com/spruit-avanade) and [chullybun](https://github.com/chullybun).

## v3.* and earlier
- These versions are now considered **legacy** and are no longer maintained; there are no further updates planned for these versions, including bug fixes or security patches.
