<br/>

![Logo](./images/Logo256x256.png "CoreEx")

<br/>

## Introduction

_CoreEx_ provides enriched capabilities for building business services by _extending_ the core capabilities of .NET.

The _CoreEx_ solution is divided into a number of projects, with `CoreEx` providing the core/shared capabilities, with additional projects enabling other related capabilities that can optionally be included within the final consuming solution.

_CoreEx_ at its core is a non-opinionated framework, meaning that it is not intended to be all-or-nothing, or drive a particular architectural style, but provide building block capabilities that can be leveraged as required to simplify development, and add extended/richer/consistent functionality with minimal effort.

<br/>

## Status

The build status is [![CI](https://github.com/Avanade/CoreEx/workflows/CI/badge.svg)](https://github.com/Avanade/CoreEx/actions?query=workflow%3ACI) with the NuGet package status as follows, including links to the underlying source code and documentation:

Package | Status | Source & documentation
-|-|-
`CoreEx` | [![NuGet version](https://badge.fury.io/nu/CoreEx.svg)](https://badge.fury.io/nu/CoreEx) | [Link](./src/CoreEx)
`CoreEx.AspNetCore` | [![NuGet version](https://badge.fury.io/nu/CoreEx.AspNetCore.svg)](https://badge.fury.io/nu/CoreEx.AspNetCore) | [Link](./src/CoreEx/CoreEx.AspNetCore)
`CoreEx.AspNetCore.NSwag` | [![NuGet version](https://badge.fury.io/nu/CoreEx.AspNetCore.NSwag.svg)](https://badge.fury.io/nu/CoreEx.AspNetCore.NSwag) | [Link](./src/CoreEx/CoreEx.AspNetCore.NSwag)
`CoreEx.Azure.Messaging.ServiceBus.NSwag` | [![NuGet version](https://badge.fury.io/nu/CoreEx.Azure.Messaging.ServiceBus.svg)](https://badge.fury.io/nu/CoreEx.Azure.Messaging.ServiceBus) | [Link](./src/CoreEx/CoreEx.Azure.Messaging.ServiceBus)
`CoreEx.Caching.FusionCache` | [![NuGet version](https://badge.fury.io/nu/CoreEx.Caching.FusionCache.svg)](https://badge.fury.io/nu/CoreEx.Caching.FusionCache) | [Link](./src/CoreEx/CoreEx.Caching.FusionCache)
`CoreEx.Data` | [![NuGet version](https://badge.fury.io/nu/CoreEx.Data.svg)](https://badge.fury.io/nu/CoreEx.Data) | [Link](./src/CoreEx/CoreEx.Data)
`CoreEx.Database` | [![NuGet version](https://badge.fury.io/nu/CoreEx.Database.svg)](https://badge.fury.io/nu/CoreEx.Database) | [Link](./src/CoreEx/CoreEx.Database)
`CoreEx.Database.Postgres` | [![NuGet version](https://badge.fury.io/nu/CoreEx.Database.Postgres.svg)](https://badge.fury.io/nu/CoreEx.Database.Postgres) | [Link](./src/CoreEx/CoreEx.Database.Postgres)
`CoreEx.Database.SqlServer` | [![NuGet version](https://badge.fury.io/nu/CoreEx.Database.SqlServer.svg)](https://badge.fury.io/nu/CoreEx.Database.SqlServer) | [Link](./src/CoreEx/CoreEx.Database.SqlServer)
`CoreEx.DomainDriven` | [![NuGet version](https://badge.fury.io/nu/CoreEx.DomainDriven.svg)](https://badge.fury.io/nu/CoreEx.DomainDriven) | [Link](./src/CoreEx/CoreEx.DomainDriven)
`CoreEx.EntityFrameworkCore` | [![NuGet version](https://badge.fury.io/nu/CoreEx.EntityFrameworkCore.svg)](https://badge.fury.io/nu/CoreEx.EntityFrameworkCore) | [Link](./src/CoreEx/CoreEx.EntityFrameworkCore)
`CoreEx.Events` | [![NuGet version](https://badge.fury.io/nu/CoreEx.Events.svg)](https://badge.fury.io/nu/CoreEx.Events) | [Link](./src/CoreEx/CoreEx.Events)
`CoreEx.RefData` | [![NuGet version](https://badge.fury.io/nu/CoreEx.RefData.svg)](https://badge.fury.io/nu/CoreEx.RefData) | [Link](./src/CoreEx/CoreEx.RefData)
`CoreEx.Validation` | [![NuGet version](https://badge.fury.io/nu/CoreEx.Validation.svg)](https://badge.fury.io/nu/CoreEx.Validation) | [Link](./src/CoreEx/CoreEx.Validation)
-- | -- | --
`CoreEx.UnitTesting` | [![NuGet version](https://badge.fury.io/nu/CoreEx.UnitTesting.svg)](https://badge.fury.io/nu/CoreEx.UnitTesting) | [Link](./src/CoreEx.UnitTesting)


The included [change log](CHANGELOG.md) details all key changes per published version.

<br/>

## Samples

The following samples are provided to guide usage:

Sample | Description
-|-
[My.Hr](./samples/My.Hr) | A sample to demonstrate the usage of _CoreEx_ within the context of a fictitious Human Resources solution. The main intent is to show how _CoreEx_ can be leveraged to build Web APIs and Azure Functions. Additionally, the unit testing provided within demonstrates the thoroughness of testing that can be achieved with some of the other repos mentioned below.  

<br/>

## Other repos

These other _Avanade_ repositories leverage _CoreEx_:

Repo | Description
-|-
[*Beef*](https://github.com/Avanade/beef) | Code-generation capabilities to support the industrialization of API development leveraging `CoreEx` as the primary runtime framework (_Beef_ version `v5+`).
[*DbEx*](https://github.com/Avanade/dbex) | Provides database extensions for DbUp-inspired database migrations.
[*NTangle*](https://github.com/Avanade/ntangle) | Change Data Capture (CDC) code generation tool and runtime.

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