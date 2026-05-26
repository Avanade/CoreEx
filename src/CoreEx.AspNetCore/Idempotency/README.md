# CoreEx.AspNetCore.Idempotency

> Provides end-to-end idempotency key support: `IdempotencyKeyMiddleware` intercepts `[IdempotencyKey]`-marked endpoints, and `HybridCacheIdempotencyProvider` stores and replays prior responses using `IHybridCache`.

## Overview

Idempotency prevents duplicate processing when clients retry `POST` requests due to network failures or timeouts. `CoreEx.AspNetCore.Idempotency` implements this pattern transparently at the middleware layer so individual action methods require no idempotency-specific code.

The flow: a client sends a `POST` with an `x-idempotency-key` header and a `[IdempotencyKey]` attribute is discovered on the endpoint; `IdempotencyKeyMiddleware` delegates to `IIdempotencyProvider.OnInvokeAsync`; the provider checks the cache for a prior response keyed by `(type-name, idempotency-key)`; on a hit it replays the cached HTTP response body and headers; on a miss it executes the action and stores the result before returning.

`HybridCacheIdempotencyProvider` is the built-in implementation backed by `IHybridCache`. It serializes the response body as a `BinaryData` entry and stores response headers, status code, and content type alongside it, enabling a complete response replay.

## Key capabilities

- 🔁 **Transparent replay**: Duplicate requests with the same `x-idempotency-key` on `[IdempotencyKey]`-marked endpoints receive the original response without executing the handler again.
- 🗄️ **HybridCache backing**: `HybridCacheIdempotencyProvider` stores responses in `IHybridCache` with configurable `LocalExpiration` and `DistributedExpiration`.
- 🔒 **In-flight deduplication**: `IdempotencyStatus.Processing` is stamped during execution; concurrent identical requests receive a `409 Conflict` until the first completes, preventing double-execution under parallel retries.
- 🏷️ **Attribute-driven**: `[IdempotencyKey]` is applied per action method; endpoints without the attribute pass through the middleware unchanged.
- 🔌 **Pluggable provider**: `IIdempotencyProvider` is a single-method interface, allowing any storage backend (Redis, SQL, etc.) to be substituted for `HybridCacheIdempotencyProvider`.
- ⚙️ **DI registration**: `AddHybridCacheIdempotencyProvider(services, configure?)` registers both `HybridCacheIdempotencyProvider` and `IdempotencyKeyMiddleware` as scoped services.

## Key types

| Type | Description |
|------|-------------|
| **[`IdempotencyKeyMiddleware`](./IdempotencyKeyMiddleware.cs)** | `IMiddleware` that reads `[IdempotencyKeyAttribute]` from endpoint metadata and dispatches to `IIdempotencyProvider.OnInvokeAsync`. |
| **[`HybridCacheIdempotencyProvider`](./HybridCacheIdempotencyProvider.cs)** | `IIdempotencyProvider` backed by `IHybridCache`; stores response body, headers, status code, and content type; marks in-flight requests as `Processing`. |
| **[`IdempotencyStatus`](./IdempotencyStatus.cs)** | Enum: `None`, `Processing`, `Completed` — tracks the lifecycle of a cached idempotency entry. |
| **[`IdempotencyProviderInvoker`](./IdempotencyProviderInvoker.cs)** | `InvokerBase<HybridCacheIdempotencyProvider>` wrapping provider executions with OpenTelemetry spans. |
| [`IIdempotencyProvider`](./IIdempotencyProvider.cs) | Single-method pluggable interface: `OnInvokeAsync(IdempotencyKeyAttribute, HttpContext, RequestDelegate)`. |
| [`IdempotencyKey`](./IdempotencyKey.cs) | Internal static helpers for cache key construction and response capture/replay. |

## Related Namespaces

- **[`CoreEx.AspNetCore.Mvc`](../Mvc/README.md)** - `[IdempotencyKeyAttribute]` is defined in `Mvc`; it is discovered from endpoint metadata by `IdempotencyKeyMiddleware`.
- **[`CoreEx.Caching`](../../CoreEx/Caching/README.md)** - `IHybridCache` is the cache abstraction used by `HybridCacheIdempotencyProvider`.
- **[`CoreEx.Http`](../../CoreEx/Http/README.md)** - `HttpNames.IdempotencyKeyHeaderName` (`x-idempotency-key`) is the agreed header name read by the provider.
- **[`CoreEx.Invokers`](../../CoreEx/Invokers/README.md)** - `IdempotencyProviderInvoker` extends `InvokerBase` to emit spans for idempotency provider executions.