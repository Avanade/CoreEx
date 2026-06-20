# CoreEx.Validation

> Provides a fluent, property-centric validation framework for .NET classes: composable rules, conditional clauses, strongly-typed error messages, and deep integration with the CoreEx execution and exception model.

## Overview

`CoreEx.Validation` fills the gap between primitive data-annotation validation and a fully featured domain validation library. The framework is built around the concept of a `Validator<TEntity>` that owns a set of `PropertyRule` chains — one chain per property. Each chain is declared fluently using `HasProperty`/`HasRuleFor`, and individual rules are appended to the chain with extension methods defined in `ValidationExtensions`.

Every rule invoked on a property chain runs asynchronously and receives a `PropertyContext<TEntity, TProperty>` that carries the current entity, the resolved property value, the accumulated error list, and a reference to the owning `ValidationContext<TEntity>`. When a rule adds an error it does so through the context, which means all rules in the chain continue executing and all errors for all properties are collected before the framework decides to throw or return.

Validation is initiated by calling `ValidateAsync` on a validator instance and inspecting the returned `IValidationContext<TEntity>`, or by calling `ValidateAndThrowAsync` which converts errors to a CoreEx `ValidationException` automatically.

## Motivation

- The standard .NET `DataAnnotations` model is attribute-only, non-composable, and cannot express cross-property rules or async checks cleanly.
- FluentValidation is a popular alternative but requires a separate NuGet dependency and has a different abstraction model; CoreEx.Validation integrates tightly with `ExecutionContext`, `LText` localisation, `IValidatorEx`, and the CoreEx exception hierarchy.
- Property rules and clauses are plain sealed classes; extension methods in `ValidationExtensions` are the only public API surface, making it trivial to discover and extend the rule set.
- Common validators (`CommonValidator<TValue>`) allow validation logic to be extracted from a specific entity and reused across validators without duplication.
- `RuleSet<TEntity>` groups multiple rules under a shared predicate, enabling scenario-specific validation blocks that only activate under defined conditions.

## Key capabilities

- ✅ **Fluent property-rule chains**: properties are declared with `HasProperty`/`HasRuleFor` and rules are appended via extension methods; the chain executes every rule in order, collecting all errors before reporting.
- 📋 **Rich built-in rule set**: mandatory, string (length, regex), numeric, decimal/floating-point precision, between/range, comparison (value, property, values), enum, email, wildcard, null/none/empty, error, collection, dictionary, entity, common, reference data, and interop rules cover the vast majority of domain validation needs.
- 🧩 **Conditional clauses**: `When`/`WhenValue`/`WhenHasValue` and `DependsOn` clauses short-circuit a rule when a condition is not met, enabling context-sensitive rules without branching in the rule body.
- ⚡ **Fully async**: every rule and clause is `async`-capable; `PredicateAsync<TEntity, TProperty>` delegates allow awaiting external services within a clause.
- 🔧 **Common and inline validators**: `CommonValidator<TValue>` encapsulates reusable validation logic; `InlineValidator<TValue>` is the compositional base used by both `CommonValidator` and `CommonRule`.
- 📝 **Localised error messages**: every rule accepts an optional `LText` override; default messages are defined in `ValidatorStrings` and resolved through CoreEx localisation.
- 🔍 **Context-aware property paths**: `PropertyContext` tracks fully qualified property names (both .NET and JSON) so error messages map precisely to the correct field in API responses.
- 💬 **RuleSet grouping**: `RuleSet<TEntity>` collects a block of rules that only execute when a `Predicate<ValidationContext<TEntity>>` returns `true`.
- 🧩 **Base-validator include**: `Validator<TEntity>`/`AbstractValidator<TEntity>` and the fluent `Include` method allow a base validator to be composed into a derived validator.
- **Interop bridge**: `InteropRule` and the `.Interop()` extension forward validation to any `IValidator`/`IValidator<T>` (e.g. a FluentValidation validator) and merge errors into the CoreEx context.

## Key types

| Type | Description |
|------|-------------|
| **[`Validator`](./Validator.cs)** | Static factory and service-locator entry point: `Create<TEntity>()`, `Get<TValidator>()`, and `CreateCommon<TValue>()`. |
| **[`Validator<TEntity>`](./ValidatorT.cs)** | Concrete entity validator; owns rule chains declared fluently via `HasProperty`/`HasRuleFor`; exposes `ValidateAsync` and `ValidateAndThrowAsync`. |
| _[`AbstractValidator<TEntity>`](./AbstractValidator.cs)_ | Convenience base class for defining reusable named validators without generic constraints; derives from `Validator<TEntity>`. |
| _[`AbstractValidator<TEntity, TParent>`](./AbstractValidatorT2.cs)_ | Convenience base class for validators with a parent-entity context; enables cross-entity validation. |
| **[`CommonValidator<TValue>`](./CommonValidatorT.cs)** | Reusable value-level validator; wraps an `InlineValidator<TValue>`; used with the `Common`/`.Common()` rule extension to apply shared logic across properties. |
| **[`ValidationContext<TEntity>`](./ValidationContext.cs)** | The live context for a single validation execution; accumulates `ValidationMessageItem` errors across all property chains; implements `IValidationContext<TEntity>`. |
| **[`ValidationArgs`](./ValidationArgs.cs)** | Immutable options for a validation run: `UseJsonNames`, `FullyQualifiedEntityName`, `FullyQualifiedJsonEntityName`, `ServiceProvider` and `Parameters`. |
| **[`PropertyContext<TEntity, TProperty>`](./PropertyContext.cs)** | Per-rule execution context; carries entity, value, property metadata, and the owning `ValidationContext`; passed to every rule and clause. |
| **[`CompareOperator`](./CompareOperator.cs)** | Enum: `Equal`, `NotEqual`, `LessThan`, `LessThanOrEqualTo`, `GreaterThan`, `GreaterThanOrEqualTo`; used by the compare rules. |
| **[`ValidationExtensions`](./ValidationExtensions.cs)** | Static partial class aggregating all rule and clause extension methods into a single discoverable surface. |

## Clauses

Clauses are conditional guards that run _before_ the rule body. When a clause returns `false` the rule is skipped and no error is added. Clauses are appended to any `IPropertyRule` using the extension methods listed below.

| Clause Class | Description | Extensions |
|---|---|---|
| **[`DependsOnClause<TEntity, TProperty, TDependsOnProperty>`](./Clauses/DependsOnClause.cs)** | Skips the rule when a specified sibling property has its default value or has already reported a validation error. | `DependsOn(Expression<Func<TEntity, TDependsOnProperty>>)` |
| **[`WhenClause<TEntity, TProperty>`](./Clauses/WhenClause.cs)** | Skips the rule when a boolean condition or async predicate evaluates to `false`. | `When(bool)`, `When(Func<bool>)`, `When(Func<PropertyContext,bool>)`, `When(PredicateAsync)`, `WhenValue(Predicate<TProperty>)`, `WhenHasValue()`, `WhenEntity(Predicate<TEntity>)` |

## Rules

Rules apply the actual validation logic to a property value. Each rule class is typically instantiated via the corresponding `ValidationExtensions` method(s). Rules inherit from `PropertyRuleBase<TEntity, TProperty>` and implement `OnValidateAsync`.

| Rule Class | Description | Extensions |
|---|---|---|
| **[`BetweenRule<TEntity, TProperty>`](./Rules/BetweenRule.cs)** | Validates that a comparable value lies between two bounds (inclusive or exclusive). | `Between(min, max)`, `InclusiveBetween(min, max)`, `ExclusiveBetween(min, max)` |
| **[`CollectionRule<TEntity, TProperty, TItem>`](./Rules/CollectionRule.cs)** | Validates an `IEnumerable<T>` collection: min/max count, duplicate detection (by Id, key, property, or equality), and optional per-item entity validation. | `Collection(with)`, `Collection(maxCount)`, `Collection(minCount, maxCount)`; `With` helpers: `WithDuplicateIdCheck()`, `WithDuplicateKeyCheck()`, `WithDuplicatePropertyCheck()`, `WithDuplicateCheck()` |
| **[`CommonRule<TEntity, TProperty>`](./Rules/CommonRule.cs)** | Applies a `CommonValidator<TProperty>` (or `InlineValidator<TProperty>`) to a property, enabling shared rule logic to be reused across multiple properties or validators. | `Common(commonValidator)` |
| **[`ComparePropertyRule<TEntity, TProperty, TCompareProperty>`](./Rules/ComparePropertyRule.cs)** | Compares the property value against another property on the same entity using a `CompareOperator`; skips if the target property already has an error. | `CompareProperty(op, expression)` |
| **[`CompareValueRule<TEntity, TProperty>`](./Rules/CompareValueRule.cs)** | Compares the property value against a supplied constant or function value using a `CompareOperator`. | `CompareValue(op, value)`, `Equal(value)`, `NotEqual(value)`, `LessThan(value)`, `LessThanOrEqualTo(value)`, `GreaterThan(value)`, `GreaterThanOrEqualTo(value)` |
| **[`CompareValuesRule<TEntity, TProperty>`](./Rules/CompareValuesRule.cs)** | Validates that the property value is equal to one of a supplied set of values; optionally replaces the value with the matched entry. | `CompareValues(values)`, `CompareValues(Func<context, values>)` |
| **[`DecimalRule<TEntity>`](./Rules/DecimalRule.cs)** | Validates `decimal` precision and scale, and optional sign constraint. Also covers `IFloatingPoint<T>` via `PrecisionScale`. | `Decimal(precision, scale)`, `PrecisionScale(precision, scale)` |
| **[`DictionaryRule<TEntity, TProperty, TKey, TValue>`](./Rules/DictionaryRule.cs)** | Validates an `IDictionary<TKey,TValue>`: min/max count and optional per-entry value validation. | `Dictionary(with)`, `Dictionary(maxCount)`, `Dictionary(minCount, maxCount)` |
| **[`EmailRule<TEntity>`](./Rules/EmailRule.cs)** | Validates that a `string` is a valid e-mail address (via `MailAddress.TryCreate`) and optionally within a maximum length. | `Email()`, `Email(maxLength)` |
| **[`EntityRule<TEntity, TProperty>`](./Rules/EntityRule.cs)** | Delegates validation of a complex child-entity property to a separate `IValidatorEx<TProperty>`; merges child errors into the parent context with prefixed property names. | `Entity(with)`, `Entity(validator)` |
| **[`EnumRule<TEntity, TProperty>`](./Rules/EnumRule.cs)** | Validates that a `struct Enum` value is defined (or within an optional allowed set). | `Enum()`, `Enum(allowed[])` |
| **[`EnumStringRule<TEntity>`](./Rules/EnumStringRule.cs)** | Validates that a `string` property can be parsed to a specified `Enum` type; configured via `EnumWith` fluent builder. | `Enum(Func<EnumWith, EnumWith>)` (string overload) |
| **[`ErrorRule<TEntity, TProperty>`](./Rules/ErrorRule.cs)** | Always emits the supplied error text; intended for use with a `When`/`DependsOn` clause to guard its execution. | `Error(text)`, `Duplicate()`, `NotFound()`, `Invalid()`, `Immutable()` |
| **[`InteropRule<TEntity, TProperty>`](./Rules/InteropRule.cs)** | Delegates validation to an external `IValidator`/`IValidator<T>` (e.g. a FluentValidation validator) and merges results into the CoreEx `ValidationContext`. | `Interop(getValidator)`, `Interop(validator)` |
| **[`MandatoryRule<TEntity, TProperty>`](./Rules/MandatoryRule.cs)** | Validates that the value is not `null`, not the default value, and/or not empty (string/collection). | `Mandatory()`, `NotNull()`, `NotEmpty()` |
| **[`NullNoneEmptyRule<TEntity, TProperty>`](./Rules/NullNoneEmptyRule.cs)** | Validates that the value _is_ `null`, default, or empty — the inverse of mandatory; configurable per mode. | `Null()`, `None()`, `Empty()` |
| **[`NumericRule<TEntity, TProperty>`](./Rules/NumericRule.cs)** | Validates that a numeric value satisfies sign constraints (allows/disallows negatives). | `Numeric(allowNegatives)`, `Positive()` |
| **[`ReferenceDataCodeCollectionRule<TEntity, TProperty>`](./Rules/ReferenceDataCodeCollectionRule.cs)** | Validates that an `IReferenceDataCodeCollection` contains no invalid or inactive items. | `ReferenceDataCodes(allowInactive)`, `AreValid(allowInactive)` |
| **[`ReferenceDataCodeRule<TEntity>`](./Rules/ReferenceDataCodeRule.cs)** | Validates that a `string` property represents a valid reference data code, resolving through the orchestrator; configured via `ReferenceDataWith` fluent builder. | `ReferenceData(Func<ReferenceDataWith, ReferenceDataWith>)` (string overload) |
| **[`ReferenceDataRule<TEntity, TProperty>`](./Rules/ReferenceDataRule.cs)** | Validates that an `IReferenceData` property is valid (and optionally active). | `ReferenceData(allowInactive)`, `IsValid(allowInactive)` |
| **[`RuleSet<TEntity>`](./Rules/RuleSet.cs)** | Groups a block of `IRootPropertyRule` chains that only execute when a `Predicate<ValidationContext<TEntity>>` is `true`; wired into a validator via `RuleSet(predicate, configure)` on the fluent builder. | (used via `ValidatorBase<TEntity, TSelf>.RuleSet(predicate, configure)`) |
| **[`StringRule<TEntity>`](./Rules/StringRule.cs)** | Validates string length (min/max), regex pattern matching, or an exact length. | `String(maxLength)`, `String(min, max, regex)`, `Matches(regex)`, `Length(exact)`, `MinimumLength()`, `MaximumLength()` |
| **[`WildcardRule<TEntity>`](./Rules/WildcardRule.cs)** | Validates that a `string` conforms to a CoreEx `Wildcard` pattern. | `Wildcard()`, `Wildcard(wildcard)` |

## Namespaces

| Namespace | Description | Documentation |
|-----------|-------------|---------------|
| **`CoreEx.Validation.Abstractions`** | Core interfaces, abstract base classes, and infrastructure types that underpin the validation framework: `IValidatorEx`, `ValidatorBase`, `InlineValidator`, `ValueValidator`, `IPropertyContext`, `IValidationContext`, and `ValidationMessageItem`. | [📖 README](./Abstractions/README.md) |
| **`CoreEx.Validation.Clauses`** | Conditional clause implementations (`WhenClause`, `DependsOnClause`) and the `IPropertyClause` interface. | [📖 README](./Clauses/README.md) |
| **`CoreEx.Validation.Rules`** | All built-in rule implementations (`MandatoryRule`, `StringRule`, `CollectionRule`, etc.) and the `IPropertyRule` / `PropertyRuleBase` infrastructure. | [📖 README](./Rules/README.md) |

## Related namespaces

- **[`CoreEx`](../CoreEx/README.md)** - Defines `ValidationException`, `MessageItem`, `IValidationResult`, `LText`, `ExecutionContext`, and `Wildcard` consumed throughout.
- **[`CoreEx.RefData`](../CoreEx.RefData/README.md)** - Provides `IReferenceData` and `IReferenceDataCodeCollection` validated by `ReferenceDataRule`, `ReferenceDataCodeRule`, and `ReferenceDataCodeCollectionRule`.
- **[`CoreEx.AspNetCore`](../CoreEx.AspNetCore/README.md)** - Converts `ValidationException` raised by `ValidateAndThrowAsync` into HTTP 400 responses with a structured error body.

## AI Usage Guide

An [`AGENTS.md`](./AGENTS.md) file is included with this package. AI coding assistants (GitHub Copilot, Claude, Cursor, etc.) that support workspace-injected package documentation will automatically surface concise usage guidance, code examples, and `Do Not` rules for this package without requiring a local CoreEx checkout.