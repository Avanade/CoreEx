# CoreEx.Validation.Clauses

> Provides the conditional clause implementations that guard whether a property rule executes — allowing rules to be skipped based on boolean conditions, async predicates, or sibling-property state.

## Overview

`CoreEx.Validation.Clauses` contains the two built-in `IPropertyClause<TEntity, TProperty>` implementations used by the CoreEx validation framework. A clause is evaluated _before_ the rule body; when its `CheckAsync` returns `false` the rule is skipped and no error is added, enabling context-sensitive validation without branching logic inside rule implementations.

Clauses are never instantiated directly by consumers. They are created and attached to a rule chain via the extension methods in `ValidationExtensions` (`When*` and `DependsOn` families). Multiple clauses can be chained on a single rule and are evaluated left-to-right; the first clause that returns `false` stops evaluation.

## Key capabilities

- ⏸ **Boolean/predicate guard (`WhenClause`)**: skips the rule when a static `bool`, a `Func<bool>`, a context-aware predicate, or a fully async `PredicateAsync` evaluates to `false`; covers entity-level, property-value-level, and presence-check variants.
- 🔗 **Sibling-property guard (`DependsOnClause`)**: skips the rule when a specified sibling property has its default value _or_ has already produced a validation error, preventing cascading errors when an upstream field is missing.
- ⚡ **Async-native**: both clause types are fully async; `WhenClause` accepts a `PredicateAsync<TEntity, TProperty>` delegate so external lookups can be awaited inside a clause without blocking.

## Key types

| Type | Description |
|------|-------------|
| [IPropertyClause<TEntity>](./IPropertyClauseT.cs) | Non-generic-property clause contract; defines `CheckAsync(IPropertyContext<TEntity>, CancellationToken)`; used when clause implementation does not need the resolved property type. |
| [IPropertyClause<TEntity, TProperty>](./IPropertyClauseT2.cs) | Fully typed clause contract; defines `CheckAsync(PropertyContext<TEntity, TProperty>, CancellationToken)`; the standard implementation target for built-in clauses. |
| **[`WhenClause<TEntity, TProperty>`](./WhenClause.cs)** | Evaluates a `PredicateAsync<TEntity, TProperty>` delegate; returns `false` (skip rule) when the delegate returns `false`. Wired via `When()`, `WhenValue()`, `WhenHasValue()`, `WhenEntity()`. |
| **[`DependsOnClause<TEntity, TProperty, TDependsOnProperty>`](./DependsOnClause.cs)** | Returns `false` (skip rule) when the depends-on sibling property is at its default value or already carries a validation error. Wired via `DependsOn(expression)`. |

## Related namespaces

- **[`CoreEx.Validation`](../README.md)** - Root namespace; `ValidationExtensions.WhenClause` and `ValidationExtensions.DependsOnClause` are the public entry points that create and attach these clause instances.
- **[`CoreEx.Validation.Abstractions`](../Abstractions/README.md)** - Defines `IPropertyClause<TEntity>` and `IPropertyClause<TEntity, TProperty>` that both clause types implement.