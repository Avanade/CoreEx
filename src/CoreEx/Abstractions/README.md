# CoreEx.Abstractions

The `CoreEx.Abstractions` namespace provides key abstractions, or other largely internal capabilities.

<br/>

## Motivation

To enable other capabilities generally leveraged internally within _CoreEx_.

<br/>

## Reflection

The `Reflection` namespace is used internally whenever [reflection](https://learn.microsoft.com/en-us/dotnet/framework/reflection-and-codedom/reflection) is needed; this provides extended reusable capabilities, and caching thereof to improve overall performance. The key classes are as follows: [`TypeReflector`](./Reflection/TypeReflector.cs) [`PropertyReflector`](./Reflection/PropertyReflector.cs), and [`PropertyExpression`](./Reflection/PropertyExpression.cs). 

<br/>

## Extension methods

A number of [extensions methods](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/extension-methods) are defined to enable additional features. The naming convention for these is such that the file suffix is `Extentions`; e.g. [`ObjectExtensions`](./ObjectExtensions.cs). 

_Note:_ This convention is used in other namespaces as required to house additional extension methods where applicable.

<br/>

## ETag generation

The [`ETagGenerator`](./ETagGenerator.cs) is used, primary within the [`WebApis`](../WebApis/README.md) capabilities, to generate an [ETag](https://en.wikipedia.org/wiki/HTTP_ETag) value where not provided by the underlying data source. Essentially this is implemented by serializing the payload and hashing with [SHA256](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.sha256) to get a mostly unique value to be used for caching and/or concurrency.
