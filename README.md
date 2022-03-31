<br/>

![Logo](./images/Logo256x256.png "CoreEx")

<br/>

## Introduction

_CoreEx_ provides the base capabilities for building business services by _extending_ the core capabilities of .NET.

The _CoreEx_ solution is dividied into a number of projects, with `CoreEx` providing the core/shared capabilities, with additional projects enabling other related capabilities that can optionally be included within the final consuming solution.

The motivation for _CoreEx_ is to primarily identify key back-end business services patterns and provide additional capabilities to standardize and simplify the development of these. The intent is that _CoreEx_ is less opinionated about usage, and enables opt-in usage where benefits can be derived. As well as being able to co-exist within a solution that leverages other frameworks, etc.

<br/>

## Capabilities

The **key** capabilities that _CoreEx_ is looking to address is:

Capability | Description
-|-
Configuration | Extend `IConfiguration` within [`SettingsBase`](./src/CoreEx/Configuration/SettingsBase.cs) to enable a more flexible means to get and override configuration values, especially within a microservices environment, where some settings may be shared.
Entities | Provides optional interfaces and classes to aid with the implementation of entities, collections, identifiers, primary and composite keys, ETags, paging, partitioning, tenancy, etc.
Events | Provides a standardized means to define an [event](./src/CoreEx/Events/EventData.cs) with properties to support multiple messaging systems and protocols. Plus capabilities to manage the [publishing](./src/CoreEx/Events/IEventPublisher.cs) orhcestration, including [formatting](./src/CoreEx/Events/EventDataFormatter.cs), [serialization](./src/CoreEx/Events/IEventSerializer.cs) (including to [CloudEvents](./src/CoreEx/Events/CloudEventSerializerBase.cs)), and [sending](./src/CoreEx/Events/IEventSender.cs) to the destination messaging system (e.g. [Azure Service Bus](./src/CoreEx.Messaging.Azure/ServiceBus/ServiceBusSender.cs)).
HTTP | Provides capabilities to enable extended [typed](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/http-requests?view=aspnetcore-6.0#typed-clients) [`HttpClient`](./src/CoreEx/Http/TypedHttpClientBaseT.cs) functionality providing a fluent-style method-chaining to enable the likes of `WithRetry`, `EnsureSuccess` and `ThrowTransientException`, etc. to improve the per invocation experience. Additionally, [`HttpRequestOptions`](./src/CoreEx/Http/HttpRequestOptions.cs) enable additional standardized options to be specified per request.
JSON | Whilst .NET recently added [`System.Text.Json`](https://docs.microsoft.com/en-us/dotnet/api/system.text.json) there is still extensive usage of [`Newtonsoft.Json`](https://www.newtonsoft.com/json), and there can be [challenges migrating](https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-migrate-from-newtonsoft-how-to) from the latter to the former. To aid the transition, and to support each, _CoreEX_ introduces [`IJsonSerializer`](./src/CoreEx/Json/IJsonSerializer.cs) which is used almost exclusively within _CoreEx_ to encapsulate usage -  [`CoreEx.Text.Json.JsonSerializer`](./src/CoreEx/Text/Json/JsonSerializer.cs) and [`CoreEx.Newtonsoft.Json.JsonSerializer`](./src/CoreEx.Newtonsoft/Json/JsonSerializer.cs) implementations are provided.
Localization | To enable a simple and consistent [localization](https://docs.microsoft.com/en-us/dotnet/core/extensions/globalization-and-localization) experience, the [`LText`](./src/CoreEx/Localization/LText.cs) struct provides a light-weight wrapper over a [`ITextProvider`](./src/CoreEx/Localization/ITextProvider.cs) that implements the string localization replacement. 
Reference data | Provides the base capabilities to implement a stardardized and consistent approach to the implementation of reference data via the likes of [`IReferenceData`](./src/CoreEx/RefData/IReferenceData.cs) and [`IReferenceDataExtended`](./src/CoreEx/RefData/IReferenceDataExtended.cs).
Web API | Provides extended capabilities to build Web APIs, for the likes of [ASP.NET](https://dotnet.microsoft.com/en-us/apps/aspnet/apis) or [HTTP-triggered Azure functions](https://docs.microsoft.com/en-us/azure/azure-functions/functions-bindings-http-webhook-trigger). The [WebApi](./src/CoreEx/WebApis/WebApi.cs) and [WebApiPublisher](./src/CoreEx/WebApis/WebApiPublisher.cs) capabilities within encapsulate the consistent handling of the HTTP request and corresponding response, whilst also providing additional capabilities that are not available out-of-the-box within the .NET runtime.

Please review the [wiki](https://github.com/Avanade/CoreEx/wiki) for further details; these will continue to be maintained over time.

<br/>

## Status

The build status is [![CI](https://github.com/Avanade/CoreEx/workflows/CI/badge.svg)](https://github.com/Avanade/CoreEx/actions?query=workflow%3ACI) with the NuGet package status as follows:

Package | Status
-|-
`CoreEx` | [![NuGet version](https://badge.fury.io/nu/CoreEx.svg)](https://badge.fury.io/nu/CoreEx)
`CoreEx.FluentValidation` | [![NuGet version](https://badge.fury.io/nu/CoreEx.FluentValidation.svg)](https://badge.fury.io/nu/CoreEx.FluentValidation)
`CoreEx.HealthChecks` | [![NuGet version](https://badge.fury.io/nu/CoreEx.HealthChecks.svg)](https://badge.fury.io/nu/CoreEx.HealthChecks)
`CoreEx.Messaging.Azure` | [![NuGet version](https://badge.fury.io/nu/CoreEx.Messaging.Azure.svg)](https://badge.fury.io/nu/CoreEx.Messaging.Azure)
`CoreEx.Newtonsoft` |[![NuGet version](https://badge.fury.io/nu/CoreEx.Newtonsoft.svg)](https://badge.fury.io/nu/CoreEx.Newtonsoft)

The included [change log](CHANGELOG.md) details all key changes per published version.

<br/>

## Samples

The following samples are provided to guide usage:

Sample | Description
-|-
My.Hr | A sample to demonstrate the usage of _CoreEx_ within the context of a fictitious Human Resources solution. The main intent is to show how _CoreEx_ can be leveraged to build Web APIs and Azure Functions.  

<br/>

## Other repos

These other _Avanade_ repositories leverage _CoreEx_:

- [NTangle](https://github.com/Avanade/ntangle) - Change Data Capture (CDC) code generation tool and runtime.
- [DbEx](https://github.com/Avanade/dbex) - Provides database extensions for both, DbUp-based database migrations, and ADO.NET database access.
- [UnitTestEx](https://github.com/Avanade/unittestex) - Provides .NET testing extensions to the most popular testing frameworks (MSTest, NUnit and Xunit).

<br/>

## License

_CoreEx_ is open source under the [MIT license](./LICENCE) and is free for commercial use.

<br/>

## Contributing

One of the easiest ways to contribute is to participate in discussions on GitHub issues. You can also contribute by submitting pull requests (PR) with code changes. Contributions are welcome. See information on [contributing](./CONTRIBUTING.md), as well as our [code of conduct](https://avanade.github.io/code-of-conduct/).

<br/>

## Security

See our [security disclosure](./SECURITY.md) policy.

<br/>

## Who is Avanade?

[Avanade](https://www.avanade.com) is the leading provider of innovative digital and cloud services, business solutions and design-led experiences on the Microsoft ecosystem, and the power behind the Accenture Microsoft Business Group.