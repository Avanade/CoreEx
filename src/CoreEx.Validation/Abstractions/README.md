# CoreEx.Validation.Abstractions

> Provides the core interfaces, abstract base classes, and infrastructure types that underpin the CoreEx validation framework.

## Overview

`CoreEx.Validation.Abstractions` defines the foundational contracts and base implementations that all validators, property rules, and validation contexts build upon. Consumer code rarely references these types directly — they are surfaced through the fluent API on `Validator<TEntity>` and `ValidationExtensions` — but they are the authoritative extension points for anyone building custom rules, custom validators, or integrating the framework into a new host.

`ValidatorBase<TEntity, TSelf>` is the shared abstract root for both `Validator<TEntity>` and `Validator<TEntity, TParent>`. It owns the `Rules` list, the `HasProperty`/`HasRuleFor` fluent entry points, the `RuleSet` and `Include` configuration helpers, and the internal `ValidateInternalAsync` orchestration loop. `InlineValidator<TValue>` is a lower-level building block that underpins `CommonValidator<TValue>`; it stores a single `Validator` (an inner `Validator<ValidationValue<TValue>>`) that validates the wrapped value as though it were an entity property.

`IValidatorEx` and `IValidatorEx<T>` are the public validator interfaces consumed by `EntityRule`, `InteropRule`, and DI registration helpers. `IValidationContext<T>` is returned from every `ValidateAsync` call and carries the entity value, the accumulated error list, and the `ThrowOnError()` helper. `IPropertyContext<TEntity>` and `IPropertyContext<TEntity, TProperty>` are the interfaces through which clause and rule implementations access the property value, entity, and owning context without depending on the concrete `PropertyContext` struct.

## Key capabilities

- 🧱 **Validator lifecycle base**: `ValidatorBase<TEntity, TSelf>` wires up `HasProperty`/`HasRuleFor`, `RuleSet`, `Include`, and the ordered async execution loop for all property rule chains.
- 📋 **Inline and common validator base**: `InlineValidator<TValue>` enables a `Validator<ValidationValue<TValue>>` to validate a raw value (not an entity), which is the foundation for `CommonValidator<TValue>` and `CommonRule`.
- 🔧 **Extensible rule and clause interfaces**: `IPropertyRule<TEntity>`, `IPropertyRule<TEntity, TProperty>`, `IRootPropertyRule<TEntity>`, `IPropertyClause<TEntity>`, and `IPropertyClause<TEntity, TProperty>` provide the contracts for custom rules and clauses.
- 💬 **Structured validation messages**: `ValidationMessageItem` extends `MessageItem` with an internal `FullyQualifiedPropertyName` that maps validation errors to precise .NET and JSON property paths.
- ⚡ **Async-first contract**: `IValidatorEx<T>.ValidateAsync` and `IPropertyClause.CheckAsync` are async throughout; synchronous surfaces are explicit opt-ins layered on top.
- **Value-level validation**: `ValueValidator<T>` and `IValueValidator<T>` allow a raw value to be validated outside of an entity context — used by `Validator.CreateFor<T>(value, ...)` entry points.
- **Runtime metadata**: `ISelfRuntimeMetadata` and `SelfRuntimeMetadata` provide the property name, JSON name, and value accessor abstraction used by rule and clause implementations without reflection at validation time.

## Key types

| Type | Description |
|------|-------------|
| _[`ValidatorBase<TEntity, TSelf>`](./ValidatorBase.cs)_ | Abstract root for all entity validators; owns the `Rules` list and the `HasProperty`/`HasRuleFor`/`RuleSet`/`Include` fluent builder surface; drives the `ValidateInternalAsync` execution loop. |
| _[`InlineValidator<TValue>`](./InlineValidator.cs)_ | Abstract base for `CommonValidator<TValue>`; wraps a single inner `Validator<ValidationValue<TValue>>` so a plain value can be validated using the full rule chain machinery. |
| **[`ValueValidator<T>`](./ValueValidator.cs)** | Concrete `IValueValidator<T>` that validates a raw value (not an entity property) by wrapping it in a `ValidationValue<T>` and executing the configured rule chain. |
| **[`ValidationMessageItem`](./ValidationMessageItem.cs)** | Extends `MessageItem` with an internal fully-qualified property-name field used to correlate validation errors with their source properties during context accumulation. |
| **[`ValidationValue<T>`](./ValidationValue.cs)** | Lightweight wrapper that presents a raw value as a single-property `class` entity so it can be validated by the entity-centric rule machinery. |
| **[`ValueFormatter`](./ValueFormatter.cs)** | Static helper that formats a property value into a human-readable `LText` for use in comparison-rule error messages. |
| [IValidatorEx](./IValidatorEx.cs) | Non-generic marker interface for all CoreEx validators; used for DI registration and type-safe discovery. |
| [IValidatorEx<T>](./IValidatorExT.cs) | Generic validator interface; defines `ValidateAsync(T, ValidationArgs?, CancellationToken)` and `ValidateAndThrowAsync`; implemented by all concrete validators. |
| [IValidationContext](./IValidationContext.cs) | Non-generic validation-result contract; exposes the error list, `HasErrors`, and `ThrowOnError()`. |
| [IValidationContext<T>](./IValidationContextT.cs) | Generic validation result returned from `ValidateAsync`; extends `IValidationContext` with the typed entity value. |
| [IPropertyContext](./IPropertyContext.cs) | Non-generic property-context contract exposing the owning `IValidationContext`, property name, JSON name, and `AddError`. |
| [IPropertyContext<TEntity>](./IPropertyContextT.cs) | Entity-typed property-context interface; gives clause and rule implementations access to the owning `IValidationContext<TEntity>` and the entity reference. |
| [IPropertyContext<TEntity, TProperty>](./IPropertyContextT2.cs) | Fully typed property-context interface; adds the resolved `TProperty` value. |

## Related namespaces

- **[`CoreEx.Validation`](../README.md)** - Root namespace containing `Validator<TEntity>`, `CommonValidator<TValue>`, `ValidationContext`, `ValidationArgs`, `PropertyContext`, and all `ValidationExtensions` that build on top of these abstractions.
- **[`CoreEx.Validation.Rules`](../Rules/README.md)** - All built-in rule implementations; each rule receives an `IPropertyContext<TEntity, TProperty>` defined here.
- **[`CoreEx.Validation.Clauses`](../Clauses/README.md)** - `WhenClause` and `DependsOnClause` implement `IPropertyClause<TEntity, TProperty>` defined here.