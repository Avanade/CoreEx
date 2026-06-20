# CoreEx.Results

> Provides `Result` and `Result<T>` — readonly value types that represent either a successful outcome or a typed error, enabling railway-oriented programming patterns for explicit, exception-free error propagation.

## Overview

`CoreEx.Results` implements the [Railway Oriented Programming](https://fsharpforfunandprofit.com/posts/recipe-part2/) pattern, drawing on [monad-based error handling](https://en.wikipedia.org/wiki/Monad_(functional_programming)) to represent the outcome of an operation as a first-class value rather than relying on thrown exceptions.

The core principle is a clear distinction between *expected* errors and *unexpected* errors. Exceptions remain the right tool for truly unexpected conditions — the sort that should not occur in a correctly functioning system. For *known* business outcomes (e.g. "not found", "validation failed", "concurrency conflict"), propagating a `Result` is more explicit, carries no stack-trace overhead, and produces cleaner, more predictable code paths.

`Result` (no value) and `Result<T>` (with value) are both `readonly struct` types, making them allocation-free on the success path. A rich set of extension methods covers the full railway pipeline: `Then`, `When`, `OnFailure`, `Match`, `Any`, and `Bind` — all with synchronous and `async` overloads.

## Key capabilities

- 🚂 **Railway-oriented pipeline**: Chain operations with `Then`, `When`, and `Bind` — each step executes only when the previous step succeeded; the first failure short-circuits the remainder of the chain.
- 🔀 **Conditional branching**: `When` applies a condition predicate before executing a step; `Match` provides explicit success/failure branches for terminal handling.
- ⛔ **Failure-path handling**: `OnFailure` executes only when the result is in a failure state, enabling error recovery or transformation without breaking the chain.
- 🔄 **Unconditional steps**: `Any` executes regardless of result state — useful for side-effects such as logging that must always run.
- 🔗 **Bind and type transformation**: `Bind` unwraps a `Result<T>` and maps it to a `Result<U>` via a delegate, enabling typed pipeline composition across different value types.
- ▶️ **Chain entry point**: `Result.Go(...)` provides a clean, readable starting point for a new pipeline from a value, action, or function.
- ⚡ **Async-first**: Every pipeline extension has both synchronous and `Task<Result>`/`Task<Result<T>>` overloads, including `async`/`await` variants, so pipelines compose naturally with `async` service code.
- 🪶 **Zero-allocation success path**: Both `Result` and `Result<T>` are `readonly struct` types; the success case carries no heap allocation.

## Key types

| Type | Description |
|------|-------------|
| **[`Result`](./Result.cs)** | Readonly struct representing a successful outcome or a typed error with no return value. Includes `IsSuccess`, `IsFailure`, `Error`, `ThrowOnError()`, and implicit conversion from `Exception`. |
| **[`Result<T>`](./ResultT.cs)** | Readonly struct representing a successful outcome with a `Value`, or a typed error. Includes all `Result` members plus a strongly-typed `Value` property. |
| **[`ResultsExtensions`](./ResultsExtensions.cs)** | Static partial class providing the full set of pipeline extension methods (`Then`, `When`, `OnFailure`, `Match`, `Any`, `Bind`) for both `Result` and `Result<T>`, synchronously and asynchronously. |
| [`IResult`](./Abstractions/IResult.cs) | Core interface defining `IsSuccess`, `IsFailure`, `Error`, `Value`, and `ToFailure` — the minimum contract for result types. |
| [`IResult<T>`](./Abstractions/IResultT.cs) | Generic extension of `IResult` adding a strongly-typed `Value` property. |
| [`IToResult`](./Abstractions/IToResult.cs) | Interface for types that can project themselves to a `Result`. |
| [`IToResult<T>`](./Abstractions/IToResultT.cs) | Interface for types that can project themselves to a `Result<T>`. |

## Related Namespaces

- **[`CoreEx`](../README.md)** - Defines the semantic exception types (`ValidationException`, `NotFoundException`, etc.) that are used as the `Error` payload within a failed `Result`.
- **[`CoreEx.AspNetCore`](../../CoreEx.AspNetCore/README.md)** - `WebApi` helpers consume `Result` and `Result<T>` directly, translating failure errors to the appropriate HTTP response.
- **[`CoreEx.Validation`](../../CoreEx.Validation/README.md)** - Validators return `Result` and `Result<T>` to compose naturally within a service pipeline.

## Additional Resources

- [Railway Oriented Programming — Scott Wlaschin](https://fsharpforfunandprofit.com/posts/recipe-part2/) - The foundational article describing the pattern that `CoreEx.Results` implements.
- [Monad (functional programming) — Wikipedia](https://en.wikipedia.org/wiki/Monad_(functional_programming)) - Background on the monad pattern underlying `Result` types.
- [Error management — Microsoft F# guide](https://learn.microsoft.com/en-us/dotnet/fsharp/style-guide/conventions#error-management) - Microsoft's guidance on when to use result types versus exceptions.