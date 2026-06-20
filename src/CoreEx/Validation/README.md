# CoreEx.Validation

> Provides the `IValidator<T>` / `IValidationResult<T>` contracts, `MultiValidator`, and the `Validation` static helper — the foundational validation interfaces shared between the `CoreEx` and `CoreEx.Validation` packages.

## Overview

`CoreEx.Validation` (within `src\CoreEx`) defines the contracts and entry-point utilities that the full validation package (`CoreEx.Validation` NuGet) implements. Separating the interfaces from the implementation means that application-layer code (services, repositories) can depend on `IValidator<T>` and `IValidationResult<T>` without pulling in the full validation rule engine.

`Validation` is a static helper providing the standard error message format strings (`MandatoryFormat`, `InvalidValueFormat`) and factory methods (`Validation.CreateResult<T>()`) used when constructing lightweight validation outcomes without the full rule pipeline. `MultiValidator` orchestrates multiple `IValidator<T>` implementations in sequence and merges their results into a single `ValidationException`.

## Key capabilities

- 📋 **Validator contract**: `IValidator<T>` defines `ValidateAsync(T value, CancellationToken)` returning `Task<IValidationResult<T>>`; consumed by application services for both manual and DI-resolved validation.
- ✅ **Validation result contract**: `IValidationResult<T>` carries the validated `Value`, a `MessageCollection`, and `ThrowOnError()` / `ToResult()` helpers to integrate naturally with both exception-based and `Result`-based service flows.
- 🔗 **Multi-validator**: `MultiValidator` runs a sequence of validators, merges `MessageItem` collections, and produces a single result — used when a single entity must pass multiple independent validation passes.
- 📝 **Standard message formats**: `Validation.MandatoryFormat` and `Validation.InvalidValueFormat` are `LText` values defining the standard required and invalid-value error strings, customizable globally at startup.
- ⚡ **Extension methods**: `ValidatorExtensions.Requires(...)` and related extension methods provide concise null/empty guards that produce `ValidationException` with standardized messages.

## Key types

| Type | Description |
|------|-------------|
| **[`Validation`](./Validation.cs)** | Static helper: `MandatoryFormat`, `InvalidValueFormat`, `ValueName`, `ValueText` — configurable standard validation message strings; `CreateResult<T>()` factory. |
| **[`MultiValidator`](./MultiValidator.cs)** | Composes multiple `IValidator<T>` instances, executing in sequence and merging their `MessageCollection` into a single result. |
| **[`MultiValidatorResult`](./MultiValidatorResult.cs)** | `IValidationResult` returned by `MultiValidator.ValidateAsync()`; aggregates messages from all constituent validators. |
| [`IValidator<T>`](./IValidatorT.cs) | Validation contract: `ValidateAsync(T value, CancellationToken) → Task<IValidationResult<T>>`. |
| [`IValidationResult<T>`](./IValidationResultT.cs) | Typed result: `Value`, `Messages` (`MessageCollection`), `HasErrors`, `ThrowOnError()`, `ToResult()`. |
| [`IValidationResult`](./IValidationResult.cs) | Non-generic base of `IValidationResult<T>` for use in heterogeneous result collections. |
| [`ValidatorExtensions`](./ValidatorExtensions.cs) | Extension methods: `Requires(...)` guards and other fluent helper methods producing `ValidationException` on failure. |

## Related Namespaces

- **[`CoreEx`](../README.md)** - `ValidationException` is the exception type thrown by `IValidationResult.ThrowOnError()`; `MessageItem` and `MessageCollection` carry the field-level error details.
- **[`CoreEx.Localization`](../Localization/README.md)** - `Validation.MandatoryFormat` and `InvalidValueFormat` are `LText` values resolved at runtime via `TextProvider.Current`.
- **[`CoreEx.Validation`](../../CoreEx.Validation/README.md)** - The full validation package implements `IValidator<T>` with a rich rule engine (fluent property rules, collection rules, reference data rules, common rule library).