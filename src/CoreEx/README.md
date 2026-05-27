# CoreEx

> The foundational `CoreEx` package providing the core runtime primitives, patterns, and abstractions used across all other CoreEx libraries and consuming services.

## Overview

`CoreEx` is the shared kernel of the CoreEx framework. It defines the types that all other packages depend on and that consuming applications interact with every day: semantic exception types, execution context, railway-oriented result types, entity contracts, mapping, JSON utilities, hosting infrastructure, caching abstractions, localization, and more.

The package is deliberately non-opinionated — nothing forces a particular architectural style and every capability is opt-in. Teams can adopt individual namespaces incrementally, composing only what they need without taking on the full framework surface area at once.

All other CoreEx packages (`CoreEx.Events`, `CoreEx.Database`, `CoreEx.AspNetCore`, etc.) depend on this package, making it the natural starting point when working with the CoreEx ecosystem.

## Motivation

- **Reduce boilerplate** — recurring patterns such as exception-to-HTTP-status mapping, entity identity, ETag handling, and audit change-log tracking are defined once and reused everywhere rather than reimplemented per project.
- **Standardize errors** — a rich hierarchy of semantic exception types with built-in HTTP status codes and structured error payloads removes the need for bespoke error-handling middleware.
- **Explicit, expected errors** — the `Result`/`Result<T>` types bring railway-oriented programming to service code, allowing known business errors to be propagated without the overhead and noise of exceptions.
- **Execution context** — a consistent, `AsyncLocal`-based ambient context carries user identity, tenant, timestamps, and operation type across the full call chain without threading them explicitly through every method signature.
- **Composability** — DI registration attributes, invoker tracing, and host-settings primitives are intentionally thin so they integrate into any host model without dictating structure.

## Key capabilities

- 🚨 **Semantic exceptions**: A hierarchy of typed exceptions (`ValidationException`, `NotFoundException`, `BusinessException`, `ConcurrencyException`, etc.) each with a pre-mapped HTTP status code, structured error type, and configurable logging enablement.
- 🔐 **Execution context**: An `AsyncLocal`-scoped `ExecutionContext` that carries user identity (`AuthenticationUser`), tenant ID, operation type, request timestamp, and arbitrary attributes throughout the lifetime of a request.
- 🚂 **Railway-oriented results**: `Result` and `Result<T>` value types that represent success or a typed error without throwing, enabling functional pipeline-style error propagation for expected business outcomes.
- 📦 **Entity contracts**: Interfaces and types for identifiers (`IIdentifier<T>`), ETags (`IETag`), audit change logs (`IChangeLog`), composite keys (`CompositeKey`), and entity cleaning/transformation — used as the common contract shape across all CoreEx layers.
- 🗺️ **Explicit mapping**: Uni- and Bi-directional mapper base classes and a `Mapper` utility for propagating standard contract properties (identity, ETags, change logs, tenant, partition) between source and destination types without a reflection-heavy mapping library.
- 🔄 **JSON utilities**: RFC 7396 JSON Merge Patch (`JsonMergePatch`), include/exclude JSON property filtering (`JsonFilter`), and custom converters for common CoreEx types.
- 💉 **DI lifetime attributes**: `[ScopedService]`, `[SingletonService]`, and `[TransientService]` attributes that mark implementation types for automatic discovery and registration via `AddDynamicServicesUsing<T>`.
- ⏱️ **Hosting and work orchestration**: Base classes for timer-driven and synchronized hosted services, plus `WorkOrchestrator` for tracking long-running distributed work states with pluggable storage.
- ⚡ **Caching abstractions**: `IHybridCache` and `ICacheKeyProvider` decouple cache consumption from provider implementation, supporting both in-process and distributed cache strategies.
- 🌐 **Localization**: `LText` and `TextProvider` provide a lightweight, key-based localization abstraction used throughout validation messages, exception text, and reference data.
- 🃏 **Wildcard parsing**: `Wildcard` provides standardized `*` and `?` wildcard parsing, validation, and `Regex` compilation for consistent wildcard-based filtering.
- 🏷️ **Reference data interfaces**: Core `IReferenceData`, `IReferenceDataCollection`, and `ReferenceDataOrchestrator` types establish the reference data contract used by `CoreEx.RefData` and data-layer packages.
- 📐 **Schema versioning**: `SchemaAttribute` and `Schema` utilities for associating a semantic version with entity types, supporting schema-aware messaging and validation.

## Key types

| Type | Description |
|------|-------------|
| **[`ExecutionContext`](./ExecutionContext.cs)** | `AsyncLocal`-scoped ambient context carrying user identity, tenant ID, operation type, timestamp, and attributes for the lifetime of a request. |
| **[`Result`](./Results/Result.cs)** | Value type representing a successful or failed operation with no return value; the basis of railway-oriented programming in CoreEx. |
| **[`Result<T>`](./Results/ResultT.cs)** | Value type representing a successful operation with a value, or a typed error — used for functional pipeline-style error propagation. |
| **[`DataConsistencyException`](./DataConsistencyException.cs)** | Non-error exception used to signal a potential data consistency issue without treating it as an application error. |
| **[`PrecisionTimeProvider`](./PrecisionTimeProvider.cs)** | `TimeProvider` implementation that truncates timestamps to a configurable fractional-second precision for database compatibility. |
| **[`OperationType`](./OperationType.cs)** | Enum representing the CRUD operation type (Get, Create, Update, Delete, Query) carried on the `ExecutionContext`. |
| _[`ExtendedException`](./Abstractions/ExtendedException.cs)_ | Abstract base for all CoreEx semantic exceptions; provides HTTP status, error type, error code, detail, and configurable logging. |
| [`IExtendedException`](./Abstractions/IExtendedException.cs) | Interface defining the extended exception contract: `StatusCode`, `ErrorType`, `ErrorCode`, `IsError`, and `ShouldBeLogged`. |

See the [Extended Exceptions](#extended-exceptions) section below for the full list of semantic exception types.

## Errors vs Exceptions

`CoreEx` distinguishes between expected and unexpected errors:

| Aspect | **Errors** (Expected) | **Exceptions** (Unexpected) |
|--------|------------------------|----------------------------|
| **Use For** | Validation failures, business rules, "not found" | System failures, infrastructure errors |
| **Types** | `CoreEx` semantic exceptions (implement [`IExtendedException`](./Abstractions/IExtendedException.cs))  | Standard .NET, and non-semantic, exceptions |
| **Benefits** | Explicit error handling | Rich diagnostics (stack traces) |
| **Example** | Customer not found | Database connection timeout |

## Extended Exceptions

Semantic (error-oriented) exception types with automatic HTTP status mapping:

| Exception | Description | HTTP Status | Error Type |
|-----------|-------------|-------------|------------|
| [`AuthenticationException`](./AuthenticationException.cs) | User not authenticated. | 401-Unauthorized | `authentication` |
| [`AuthorizationException`](./AuthorizationException.cs) | User lacks permissions. | 403-Forbidden | `authorization` |
| [`BusinessException`](./BusinessException.cs) | Business rule violation (message shown to consumer). | 400-Bad Request | `business` |
| [`ConcurrencyException`](./ConcurrencyException.cs) | Data concurrency conflict (ETag mismatch). | 412-Precondition Failed | `concurrency` |
| [`ConflictException`](./ConflictException.cs) | Data conflict (e.g., identifier already exists on create). | 409-Conflict | `conflict` |
| [`DuplicateException`](./DuplicateException.cs) | Duplicate value (e.g., unique code already in use). | 409-Conflict | `duplicate` |
| [`NotFoundException`](./NotFoundException.cs) | Entity not found. | 404-Not Found | `not-found` |
| [`TransientException`](./TransientException.cs) | Transient failure (retry candidate). | 503-Service Unavailable | `transient` |
| [`ValidationException`](./ValidationException.cs) | Validation failure with message collection. | 400-Bad Request | `validation` |

All inherit from [`ExtendedException<TSelf>`](./Abstractions/ExtendedExceptionT.cs) implementing [`IExtendedException`](./Abstractions/IExtendedException.cs).

Additionally, these support `With*`-style methods to add additional context that is added to the resulting `ProblemDetails`:

``` csharp
throw new BusinessException($"Product '{movement.ProductId}' does not have sufficient quantity on hand.")
    .WithErrorCode("insufficient-quantity")
    .WithKey(movement.ProductId);
```

The above would result in the following `ProblemDetails`:

``` json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Product \u002700000001-0000-0000-0000-000000000000\u0027 does not have sufficient quantity on hand.",
  "status": 400,
  "traceId": "00-a8e8623ef74c2b53820d0ff5d799850d-df28b0bc787429f5-01",
  "errorType": "business",
  "errorCode": "insufficient-quantity",
  "key": "00000001-0000-0000-0000-000000000000"
}
```


## Namespaces

| Namespace | Description | Documentation |
|-----------|-------------|---------------|
| **`CoreEx.Caching`** | `IHybridCache` abstraction and `ICacheKeyProvider` for decoupled local and/or distributed caching. | [📖 README](./Caching/README.md) |
| **`CoreEx.Data`** | Lightweight data-access primitives: paging, query arguments, partition keys, tenant IDs, logical deletion, and `ItemsResult<T>`. | [📖 README](./Data/README.md) |
| **`CoreEx.DependencyInjection`** | Service lifetime attributes (`[ScopedService]`, `[SingletonService]`, `[TransientService]`) for attribute-driven DI registration. | [📖 README](./DependencyInjection/README.md) |
| **`CoreEx.Entities`** | Entity contract interfaces and types: identifiers, ETags, change logs, composite keys, string transforms, and entity cleaning. | [📖 README](./Entities/README.md) |
| **`CoreEx.Globalization`** | `TextInfoCasing` and globalization extensions for culture-aware string casing transformations. | [📖 README](./Globalization/README.md) |
| **`CoreEx.HealthChecks`** | Health check extensions and `HealthCheckTags` constants for standardizing CoreEx service health reporting. | [📖 README](./HealthChecks/README.md) |
| **`CoreEx.Hosting`** | Timer and synchronized hosted service base classes, `HostSettings`, `WorkOrchestrator`, and distributed work state tracking. | [📖 README](./Hosting/README.md) |
| **`CoreEx.Http`** | HTTP primitives: `ProblemDetails`, `IdempotencyKeyHandler`, `HttpNames`, and HTTP request/response extensions. | [📖 README](./Http/README.md) |
| **`CoreEx.Invokers`** | Invocation pipeline abstractions providing automatic OpenTelemetry tracing and structured logging around service operations. | [📖 README](./Invokers/README.md) |
| **`CoreEx.Json`** | JSON Merge Patch (RFC 7396), JSON property filtering, and custom converters for CoreEx entity types. | [📖 README](./Json/README.md) |
| **`CoreEx.Localization`** | `LText` localization-agnostic text value and `TextProvider` for key-based message resolution. | [📖 README](./Localization/README.md) |
| **`CoreEx.Mapping`** | Explicit bi-directional mapper base classes, `Mapper` standard-property utilities, and `IConverter` value converters. | [📖 README](./Mapping/README.md) |
| **`CoreEx.Metadata`** | `RuntimeMetadata` and `IPropertyRuntimeMetadata` for compile-time and reflection-based property introspection on contract types. | [📖 README](./Metadata/README.md) |
| **`CoreEx.RefData`** | Core reference data interfaces, `ReferenceDataOrchestrator`, and provider abstractions consumed by `CoreEx.RefData`. | [📖 README](./RefData/README.md) |
| **`CoreEx.Results`** | `Result` and `Result<T>` railway-oriented types with a full suite of functional extension methods. | [📖 README](./Results/README.md) |
| **`CoreEx.Schemas`** | `SchemaAttribute` and `Schema` utilities for associating semantic version metadata with entity types. | [📖 README](./Schemas/README.md) |
| **`CoreEx.Security`** | `AuthenticationUser` record and `AuthenticationType` enum representing the authenticated user on the `ExecutionContext`. | [📖 README](./Security/README.md) |
| **`CoreEx.Text`** | `SentenceCase` utility for converting identifier-style strings into human-readable sentence-case text. | [📖 README](./Text/README.md) |
| **`CoreEx.Validation`** | Lightweight validation interfaces (`IValidator<T>`, `MultiValidator`) used as the validation contract across CoreEx. | [📖 README](./Validation/README.md) |
| **`CoreEx.Wildcards`** | `Wildcard` parser, validator, and `Regex` compiler for standardized `*` and `?` wildcard-based text matching. | [📖 README](./Wildcards/README.md) |

## Related Namespaces

- **[`CoreEx.AspNetCore`](../CoreEx.AspNetCore/README.md)** - ASP.NET Core integration: `WebApi` HTTP helpers, `IExtendedException` middleware, and `ExecutionContext` middleware binding.
- **[`CoreEx.Validation`](../CoreEx.Validation/README.md)** - Full validation rule engine built on top of the `IValidator<T>` contract defined in this package.
- **[`CoreEx.Events`](../CoreEx.Events/README.md)** - Event publishing and subscribing built on top of the `ExecutionContext`, `Result`, and entity contracts from this package.
- **[`CoreEx.Database`](../CoreEx.Database/README.md)** - Database access layer built on the data primitives (`PagingArgs`, `QueryArgs`, `IIdentifier`) defined here.
- **[`CoreEx.EntityFrameworkCore`](../CoreEx.EntityFrameworkCore/README.md)** - Entity Framework Core integration consuming the entity contracts and mapping types from this package.
- **[`CoreEx.RefData`](../CoreEx.RefData/README.md)** - Full reference data implementation extending the `IReferenceData` and `ReferenceDataOrchestrator` types defined here.
- **[`CoreEx.UnitTesting`](../CoreEx.UnitTesting/README.md)** - Test helpers and fluent assertion extensions targeting the types and patterns from this package. _(test only)_

## AI Usage Guide

An [`AGENTS.md`](./AGENTS.md) file is included with this package. AI coding assistants (GitHub Copilot, Claude, Cursor, etc.) that support workspace-injected package documentation will automatically surface concise usage guidance, code examples, and `Do Not` rules for this package without requiring a local CoreEx checkout.
