<br/>

![Logo](./images/Logo256x256.png "CoreEx")

<br/>

## Introduction

_CoreEx_ provides enriched capabilities for building business services by _extending_ the core capabilities of .NET.

The _CoreEx_ solution is divided into a number of projects, with `CoreEx` providing the core/shared capabilities, with additional projects enabling other related capabilities that can optionally be included within the final consuming solution.

_CoreEx_ at its core is a non-opinionated framework, meaning that it is not intended to be all-or-nothing, or drive a particular architectural style, but provide building block capabilities that can be leveraged as required to simplify development, and add extended/richer/consistent functionality with minimal effort.

Please review the **[Wiki](https://github.com/Avanade/CoreEx/wiki)** for further details; this will continue to be maintained over time.

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
[My.Hr](./samples/My.Hr) | A sample to demonstrate the usage of _CoreEx_ within the context of a fictitious Human Resources solution. The main intent is to show how _CoreEx_ can be leveraged to build Web APIs and Azure Functions. Additionally, the unit testing provided within demonstrates the thoroughness of testing that can be achieved with some of the other repos mentioned below.  

<br/>

## Other repos

These other _Avanade_ repositories leverage _CoreEx_:

- [DbEx](https://github.com/Avanade/dbex) - Provides database extensions for both, DbUp-based database migrations, and ADO.NET database access.
- [UnitTestEx](https://github.com/Avanade/unittestex) - Provides .NET testing extensions to the most popular testing frameworks (MSTest, NUnit and Xunit).
- [NTangle](https://github.com/Avanade/ntangle) - Change Data Capture (CDC) code generation tool and runtime.

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