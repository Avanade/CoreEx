# CoreEx.Configuration

The `CoreEx.Configuration` namespace primarily extends the .NET [configuration](https://learn.microsoft.com/en-us/dotnet/core/extensions/configuration), specifically the [`IConfiguration`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.configuration.iconfiguration) capabilities.

<br/>

## Motivation

To provide a more flexible, strongly typed, `IConfiguration` encapsulated provider; as well as providing a capability for _CoreEx_ to house and manage configuration. 

<br/>

## Settings base

The [`SettingsBase`](./SettingsBase.cs) class provides the foundational abstract capabilities for retrieving configured settings. Enables the standard `GetValue` and `GetRequiredValue` methods to retrieve.

The underlying constructor requires an `IConfiguration` instance and zero or more prefixes to use in order of precedence, first through to last, to find the underlying key/value pair. The prefixes enable a probing order, where specific overrides and common setting values can be supported.

For example, in a microservices architecture, there may be multiple domains. So if there is a `Product` domain, then the following prefixes could be used: `Product` and `Common`. The `GetValue` method where passed a key of `ConnectionString`, would the search for the following (in order) stopping where the value has been configured: `Product/ConnectionString`, `Common/ConnectionString` and `ConnectionString`. This allows for shared settings with the opportunity to easily override where applicable. 

_Note:_ This plays nicely with the likes of the equivalent pattern that can used within the likes of [Azure App Configuration](https://learn.microsoft.com/en-us/azure/azure-app-configuration/overview).

<br/>

### Pre-configured settings

There are a number of pre-configured settings within the [`SettingsBase`](./SettingsBase.cs) class that are used directly by _CoreEx_; these are intended to be overridden where applicable. Other `CoreEx` projects may add others leveraging extension methods within where applicable.

<br/>

## Default settings

The [`DefaultSettings`](./DefaultSettings.cs) class provides a basic implementation of `SettingsBase` to be used where no additional prefixing is required. This is also used throughout `CoreEx` as a default where a `SettingsBase` is expected, but not provided.

<br/>

## Deployment info

The [`DeploymentInfo`](./DeploymentInfo.cs) class provides a base line configuration for capturing and recording deployment information when the underlying application is deployed. These can then be accessed at run-time when performing the likes of [health checks](../HealthChecks/README.md) to validate the deployed version.


