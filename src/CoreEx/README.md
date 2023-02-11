# CoreEx

The `CoreEx` namespace provides the key root level capabilities. However, the majority of the capabilities are housed in their own respective namespaces. 

<br/>

## Motivation

The motivation for _CoreEx_ is to primarily identify key back-end business services patterns and provide additional capabilities to standardize and simplify the development of these. The intent is that _CoreEx_ is less opinionated about usage and enables opt-in where benefits can be derived. As well as being able to co-exist within a solution that leverages other frameworks, etc.

<br/>

## Namespaces

The following key namespaces are provided; additional documentation is provided for each via their respective links:

Namespace | Description
-|-
[`Abstractions`](./Abstractions) | Provides key abstractions or other largely internal capabilities.
[`Configuration`](./Configuration) | Extends [`IConfiguration`](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.configuration.iconfiguration) to enable a more flexible means to get and override configuration values.
[`Entities`](./Entities) | Provides standardized and enriched capabilities for entities and data models.
[`Events`](./Events) | Provides standardized and enriched capabilities for event (message) declaration, publishing and subscribing.
`Hosting` | Provides extended [`IHostedService`](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.hosting.ihostedservice) capabilities such as [`TimerHostedServiceBase`](./Hosting/TimerHostedServiceBase.cs) and [`SynchronizedTimerHostedServiceBase`](./Hosting/SynchronizedTimerHostedServiceBase.cs) ([`IServiceSynchronizer`](./Hosting/IServiceSynchronizer.cs)).
[`Globalization`](./Globalization) | Provides extended globalization capabilities.
[`HealthChecks`](./HealthChecks) | Provides extended health checks capabilities.
[`Http`](./Http) | Provides extended [`HttpClient`](https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httpclient) capabilities.
`Json` | Whilst .NET recently added [`System.Text.Json`](https://docs.microsoft.com/en-us/dotnet/api/system.text.json) there is still extensive usage of [`Newtonsoft.Json`](https://www.newtonsoft.com/json), and there can be [challenges migrating](https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-migrate-from-newtonsoft-how-to) from the latter to the former. To aid the transition, and to support each, _CoreEX_ introduces [`IJsonSerializer`](./Json/IJsonSerializer.cs) which is used almost exclusively within _CoreEx_ to encapsulate usage -  [`CoreEx.Text.Json.JsonSerializer`](./Text/Json/JsonSerializer.cs) and [`CoreEx.Newtonsoft.Json.JsonSerializer`](../CoreEx.Newtonsoft/Json/JsonSerializer.cs) implementations are provided.
[`Localization`](./Locacalization) | Provided extended localization capabilities.
[`RefData`](./RefData) | Provides standardized and enriched capabilities for reference data.
`Text.Json` | Provides [`System.Text.Json`](https://docs.microsoft.com/en-us/dotnet/api/system.text.json) implementation of the [`IJsonSerializer`](./Json/IJsonSerializer.cs).
`Validation` | Provides for implementation agnostic [`IValidator<T>`](./Validation/IValidatorT.cs).
[`WebApis`](./WebApis) | Provides extended capabilities to build Web APIs, for the likes of [ASP.NET](https://dotnet.microsoft.com/en-us/apps/aspnet/apis) or [HTTP-triggered Azure functions](https://docs.microsoft.com/en-us/azure/azure-functions/functions-bindings-http-webhook-trigger). The [WebApi](./WebApis/WebApi.cs) and [WebApiPublisher](./WebApis/WebApiPublisher.cs) capabilities within encapsulate the consistent handling of the HTTP request and corresponding response, whilst also providing additional capabilities that are not available out-of-the-box within the .NET runtime.
`Wildcards` | Provides standardized approach to parsing and validating [`Wildcard`](./Wildcards/Wildcard.cs) text. 

<br/>