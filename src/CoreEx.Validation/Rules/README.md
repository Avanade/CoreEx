# CoreEx.Validation.Rules

> Provides all built-in property rule implementations for the CoreEx validation framework, together with the `IPropertyRule` interfaces and `PropertyRuleBase` abstract base class that underpin them.

## Overview

`CoreEx.Validation.Rules` contains every built-in rule that can be applied to a property chain in a `Validator<TEntity>`. Rules are the units of validation logic — each one receives a `PropertyContext<TEntity, TProperty>`, inspects the value, and optionally calls `context.AddError(...)` to register a failure. Rules are never instantiated directly; they are created through the fluent extension methods in `ValidationExtensions` and returned as `IPropertyRule<TEntity, TProperty>` so further clauses or chained rules can be appended.

`PropertyRuleBase<TEntity, TProperty>` provides the shared scaffolding: it holds the optional `ErrorText` override, the list of attached `IPropertyClause` guards, the chain pointer to the next rule, and the `ValidateAsync` orchestration that runs clauses first and then delegates to `OnValidateAsync`. The `IPropertyRule<TEntity>`, `IPropertyRule<TEntity, TProperty>`, `IRootPropertyRule<TEntity>`, and `IRootPropertyRule<TEntity, TProperty>` interfaces define the contracts at each layer of the rule infrastructure.

`RuleSet<TEntity>` is a special rule-like container that groups a set of `IRootPropertyRule` chains under a predicate; it is wired into a validator via the `ValidatorBase.RuleSet(predicate, configure)` fluent builder and is stored in the same `Rules` list as ordinary property rules, but its execution delegates to all contained chains when the predicate is satisfied.

## Key capabilities

- 📋 **Comprehensive built-in rule set**: covers mandatory presence, string constraints, numeric/decimal precision, range/between, ordered and equality comparisons against values and sibling properties, enum membership, e-mail format, wildcard patterns, null/none/empty inverse checks, unconditional errors, collection and dictionary cardinality with duplicate detection, child-entity delegation, common/inline validator composition, reference data validity, and external validator interop.
- 🧱 **Consistent base scaffolding**: `PropertyRuleBase<TEntity, TProperty>` enforces a uniform clause-then-rule execution contract; the `ValidateWhenNull` override lets rules like `CommonRule` opt in to executing even when the value is `null`.
- 🔧 **Extensible via `With` inner builders**: `CollectionRule`, `DictionaryRule`, and `EntityRule` each expose a nested `With` fluent builder that configures optional per-rule sub-validators, duplicate-check strategies, and cardinality constraints, keeping the primary extension-method signature clean.
- 💬 **Override-able error text**: every rule inherits `ErrorText` from `PropertyRuleBase` and will use it in place of the default `ValidatorStrings` message when set, enabling per-call customization without subclassing.
- 🧩 **Conditional rule grouping**: `RuleSet<TEntity>` batches multiple root-property rules under a `Predicate<ValidationContext<TEntity>>`, enabling scenario or mode-specific validation blocks that activate only when the predicate passes.

## Key types

| Type | Description |
|------|-------------|
| [IPropertyRule<TEntity>](./IPropertyRuleT.cs) | Root property-rule contract; exposes `AddClause`, `Chain`, `SetText`, and `ErrorText`. |
| [IPropertyRule<TEntity, TProperty>](./IPropertyRuleT2.cs) | Typed property-rule contract; extends `IPropertyRule<TEntity>` with the typed `ValidateAsync` method. |
| [IRootPropertyRule<TEntity>](./IRootPropertyRuleT.cs) | Marker interface for rules stored directly in a validator's `Rules` list; implemented by `RootPropertyRule<TEntity, TProperty>` and `RuleSet<TEntity>`. |
| _[`PropertyRuleBase<TEntity, TProperty>`](./PropertyRuleBase.cs)_ | Abstract base for all rule implementations; holds `ErrorText`, clause list, chain pointer; drives the clause-check-then-`OnValidateAsync` execution contract. |
| **[`RootPropertyRule<TEntity, TProperty>`](./RootPropertyRule.cs)** | Concrete `IRootPropertyRule` entry point created by `HasProperty`/`HasRuleFor`; owns the property metadata and dispatches to the chained rule sequence. |
| **[`RuleSet<TEntity>`](./RuleSet.cs)** | Groups a set of `IRootPropertyRule` chains under a `Predicate<ValidationContext<TEntity>>`; chains execute only when the predicate returns `true`. |
| **[`MandatoryRule<TEntity, TProperty>`](./MandatoryRule.cs)** | Validates that a value is not `null`, not default, and/or not empty; wired via `Mandatory()`, `NotNull()`, `NotEmpty()`. |
| **[`NullNoneEmptyRule<TEntity, TProperty>`](./NullNoneEmptyRule.cs)** | Inverse of mandatory — validates that a value _is_ `null`, default, or empty; wired via `Null()`, `None()`, `Empty()`. |
| **[`StringRule<TEntity>`](./StringRule.cs)** | Validates string length (min/max/exact) and optional regex; wired via `String()`, `Matches()`, `Length()`, `MinimumLength()`, `MaximumLength()`. |
| **[`NumericRule<TEntity, TProperty>`](./NumericRule.cs)** | Validates sign constraint on any `INumber<T>`; wired via `Numeric()`, `Positive()`. |
| **[`DecimalRule<TEntity>`](./DecimalRule.cs)** | Validates `decimal` precision and scale; `PrecisionScale` covers generic `IFloatingPoint<T>`; wired via `Decimal()`, `PrecisionScale()`. |
| **[`BetweenRule<TEntity, TProperty>`](./BetweenRule.cs)** | Validates a comparable value lies between two bounds; wired via `Between()`, `InclusiveBetween()`, `ExclusiveBetween()`. |
| _[`CompareRuleBase<TEntity, TProperty>`](./CompareRuleBase.cs)_ | Abstract base for `CompareValueRule` and `ComparePropertyRule`; holds the `CompareOperator` and optional `IComparer<T>`. |
| **[`CompareValueRule<TEntity, TProperty>`](./CompareValueRule.cs)** | Compares property value against a constant or function value using a `CompareOperator`; wired via `CompareValue()`, `Equal()`, `NotEqual()`, `LessThan()`, `LessThanOrEqualTo()`, `GreaterThan()`, `GreaterThanOrEqualTo()`. |
| **[`ComparePropertyRule<TEntity, TProperty, TCompareProperty>`](./ComparePropertyRule.cs)** | Compares property value against another property on the same entity; skips if the target property has an error; wired via `CompareProperty(op, expression)`. |
| **[`CompareValuesRule<TEntity, TProperty>`](./CompareValuesRule.cs)** | Validates equality against a set of allowed values, optionally replacing the property value with the matched entry; wired via `CompareValues(values)`. |
| **[`EnumRule<TEntity, TProperty>`](./EnumRule.cs)** | Validates a `struct Enum` is defined or within an allowed subset; wired via `Enum()`, `Enum(allowed[])`. |
| **[`EnumStringRule<TEntity>`](./EnumStringRule.cs)** | Validates a `string` can be parsed to an `Enum` type, configured via the `EnumWith` nested builder; wired via `Enum(Func<EnumWith, EnumWith>)`. |
| **[`EmailRule<TEntity>`](./EmailRule.cs)** | Validates a `string` is a valid e-mail address (`MailAddress.TryCreate`) and optionally within a max length; wired via `Email()`. |
| **[`WildcardRule<TEntity>`](./WildcardRule.cs)** | Validates a `string` conforms to a CoreEx `Wildcard` pattern; wired via `Wildcard()`. |
| **[`ErrorRule<TEntity, TProperty>`](./ErrorRule.cs)** | Always emits the supplied error; intended for use with a `When` clause; wired via `Error()`, `Duplicate()`, `NotFound()`, `Invalid()`, `Immutable()`. |
| **[`CollectionRule<TEntity, TProperty, TItem>`](./CollectionRule.cs)** | Validates an `IEnumerable<T>` for min/max count and duplicate detection; wired via `Collection()`; `With` helpers: `WithDuplicateIdCheck()`, `WithDuplicateKeyCheck()`, `WithDuplicatePropertyCheck()`, `WithDuplicateCheck()`. |
| **[`DictionaryRule<TEntity, TProperty, TKey, TValue>`](./DictionaryRule.cs)** | Validates an `IDictionary<TKey,TValue>` for min/max count with optional per-entry validation; wired via `Dictionary()`. |
| **[`EntityRule<TEntity, TProperty>`](./EntityRule.cs)** | Delegates validation of a child entity to an `IValidatorEx<TProperty>`; merges child errors into the parent context with prefixed names; wired via `Entity()`. |
| **[`CommonRule<TEntity, TProperty>`](./CommonRule.cs)** | Applies a `CommonValidator<TProperty>` to a property, enabling reuse of shared rule logic; wired via `Common(commonValidator)`. |
| **[`ReferenceDataRule<TEntity, TProperty>`](./ReferenceDataRule.cs)** | Validates an `IReferenceData` property is valid (and optionally active); wired via `ReferenceData()`, `IsValid()`. |
| **[`ReferenceDataCodeRule<TEntity>`](./ReferenceDataCodeRule.cs)** | Validates a `string` as a reference data code via the orchestrator, configured with `ReferenceDataWith`; wired via `ReferenceData(Func<ReferenceDataWith, ReferenceDataWith>)`. |
| **[`ReferenceDataCodeCollectionRule<TEntity, TProperty>`](./ReferenceDataCodeCollectionRule.cs)** | Validates an `IReferenceDataCodeCollection` contains no invalid or inactive items; wired via `ReferenceDataCodes()`, `AreValid()`. |
| **[`InteropRule<TEntity, TProperty>`](./InteropRule.cs)** | Delegates validation to any external `IValidator`/`IValidator<T>` and merges results into the CoreEx context; wired via `Interop()`. |
| **[`IncludeBaseRule<TEntity>`](./IncludeBaseRule.cs)** | Internal pass-through that injects a base `IValidatorEx` into a derived validator's `Rules` list; used by the `Include` fluent method on `ValidatorBase`. |

## Related namespaces

- **[`CoreEx.Validation`](../README.md)** - Root namespace; `ValidationExtensions` partial classes are the sole public entry point for creating and attaching all rule types defined here.
- **[`CoreEx.Validation.Abstractions`](../Abstractions/README.md)** - Defines `IPropertyRule`, `PropertyRuleBase`, `IPropertyContext`, and `IValidatorEx` consumed by every rule implementation.
- **[`CoreEx.Validation.Clauses`](../Clauses/README.md)** - Clause implementations that guard rule execution; clauses are attached to `IPropertyRule` instances defined here.
- **[`CoreEx.RefData`](../../CoreEx.RefData/README.md)** - Provides `IReferenceData` and `IReferenceDataCodeCollection` validated by `ReferenceDataRule`, `ReferenceDataCodeRule`, and `ReferenceDataCodeCollectionRule`.