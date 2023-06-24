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
[`Caching`](./Caching) | Provides addition caching capabilities.
[`Configuration`](./Configuration) | Extends [`IConfiguration`](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.configuration.iconfiguration) to enable a more flexible means to get and override configuration values.
[`Entities`](./Entities) | Provides standardized and enriched capabilities for entities and data models.
[`Events`](./Events) | Provides standardized and enriched capabilities for event (message) declaration, publishing and subscribing.
[`Globalization`](./Globalization) | Provides extended globalization capabilities.
[`HealthChecks`](./HealthChecks) | Provides extended health checks capabilities.
[`Hosting`](./Hosting) | Provides extended [hosted service (worker)](https://learn.microsoft.com/en-us/dotnet/core/extensions/workers) capabilities. 
[`Http`](./Http) | Provides extended [`HttpClient`](https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httpclient) capabilities.
[`Invokers`](./Invokers) | Provides extended invocation capabilities.
[`Json`](./Json) | Provides implementation agnostic [JSON](https://en.wikipedia.org/wiki/JSON)-related capabilities.
[`Localization`](./Localization) | Provided extended localization capabilities.
[`Mapping`](./Mapping) | Provides implementation agnostic mapping capabilities.
[`RefData`](./RefData) | Provides standardized and enriched capabilities for reference data.
[`Results`](./Results) | Provides [monadic](https://en.wikipedia.org/wiki/Monad_(functional_programming)) error-handling, often referred to as [Railway-oriented programming](https://swlaschin.gitbooks.io/fsharpforfunandprofit/content/posts/recipe-part2.html) via `Result` and `Result<T>` types.
[`Security`](./Security) | Provides extended security capabilities.
[`Serialization`](./Serialization) | Provides implementation agnostic serialization capabilities.
[`Text.Json`](./Text/Json) | Provides [`System.Text.Json`](https://docs.microsoft.com/en-us/dotnet/api/system.text.json) implementation of the [`IJsonSerializer`](./Json/IJsonSerializer.cs).
[`Validation`](./Validation) | Provides implementation agnostic validation capabilities.
[`Wildcards`](./Wildcards) | Provides standardized approach to parsing and validating [`Wildcard`](./Wildcards/Wildcard.cs) text.`
[`Text.Json`](./Text/Json) | Provides [`System.Text.Json`](https://docs.microsoft.com/en-us/dotnet/api/system.text.json) implementation of the [`IJsonSerializer`](./Json/IJsonSerializer.cs).
[`Validation`](./Validation) | Provides implementation agnostic validation capabilities.
[`Wildcards`](./Wildcards) | Provides standardized approach to parsing and validating [`Wildcard`](./Wildcards/Wildcard.cs) text. 

<br/>

## Execution context

The [`ExecutionContext`](./ExecutionContext.cs) is a foundational class that is integral to the underlying execution within _CoreEx_. It represents a thread-bound (request) execution context - enabling the availability of the likes of `Username` at at runtime via `ExecutionContext.Current`. Additionally, the context is passed between executing threads for the owning request (see [`AsyncLocal`](https://learn.microsoft.com/en-us/dotnet/api/system.threading.asynclocal-1)).

An implementor may choose to inherit from this class and add additional capabilities as required.

<br/>

## Exceptions

There are a number of key exceptions that have a specific built in behaviour; these all implement [`IExtendedException`](./Abstractions/IExtendedException.cs).

Exception | Description | HTTP Status | [`ErrorType`](./Abstractions/ErrorType.cs)
-|-|-|-
[`AuthenticationException`](./AuthenticationException.cs) | Represents an **Authentication** exception. | 401 Unauthorized | 8 AuthenticationError 
[`AuthorizationException`](./AuthorizationException.cs) | Represents an **Authorization** exception. | 403 Forbidden | 3 AuthorizationError 
[`BusinessException`](./BusinessException.cs) | Represents a **Business** exception whereby the message returned should be displayed directly to the consumer. | 400 BadRequest | 2 BusinessError 
[`ConcurrencyException`](./ConcurrencyException.cs) | Represents a data **Concurrency** exception; generally as a result of an errant [ETag](./Entities/IETag.cs). | 412 PreconditionFailed | 4 ConcurrencyError 
[`ConflictException`](./ConflictException.cs) | Represents a data **Conflict** exception; for example creating an entity that already exists. | 409 Conflict | 6 ConflictError 
[`DuplicateException`](./DuplicateException.cs) | Represents a **Duplicate** exception; for example updating a code on an entity where the value is already used. | 409 Conflict | 7 DuplicateError 
[`NotFoundException`](./NotFoundException.cs) | Represents a **NotFound** exception; for example getting an entity that does not exist. | 404 NotFound | 5 NotFoundError 
[`TransientException`](./TransientException.cs) | Represents a **Transient** exception; failed but is a candidate for a retry. | 503 ServiceUnavailable | 9 TransientError 
[`ValidationException`](./ValidationException.cs) | Represents a **Validation** exception with a corresponding `Messages` [collection](./Entities/MessageItemCollection.cs). | 400 BadRequest | 1 ValidationError 