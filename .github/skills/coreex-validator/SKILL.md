---
name: coreex-validator
description: "Create or modify a CoreEx validator in the Application layer. USE FOR: new Validator<T,TSelf> (no injection), new Validator<T> with constructor injection, AbstractValidator<T,TSelf> (FluentValidation-style), adding rules to an existing validator, nested entity/collection/dictionary validators. DO NOT USE FOR: domain invariants in aggregates, FluentValidation NuGet package, Infrastructure-layer checks."
argument-hint: "Optional: contract type, validator name, list of properties to validate, any async/database checks needed"
tags: ["validators", "validation", "application-layer", "coreex", "fluent-rules", "async"]
---

# CoreEx: Validator

Guides you through creating or modifying a CoreEx validator (`Application/Validators/`) for a contract or request type. Covers declarative rules, ref-data validation, async database checks, collection and dictionary validators.

## When to Use

- New validator for a contract or request type (no database calls needed)
- New validator that requires a repository or other Application-layer dependency
- Adding property rules or an async check to an existing validator
- Nested validator for a sub-property (`.Entity()`), a collection, or a dictionary
- Switching to FluentValidation-compatible `AbstractValidator` syntax

## When Not to Use

- Domain invariants (aggregates, entities, value objects) — those belong in the Domain layer
- Infrastructure-level data checks that are not accessed via an Application-layer interface
- `FluentValidation` NuGet package — `AbstractValidator` here is `CoreEx.Validation.AbstractValidator`

## Quick Reference

**Clarifying questions to ask before writing any code:**
1. Does the validator need a constructor-injected dependency (e.g. a repository)? (determines base class)
2. Which properties need validation? (list upfront — batch type resolution, don't interrupt per-property)
3. Are any ref-data properties required? Optional? (`.Mandatory().IsValid()` vs `.IsValid()`)
4. Are any checks async (e.g. confirming an entity exists in the database)?
5. Does the user prefer FluentValidation-style `RuleFor(x => ...)` syntax?

**Base class decision:**

| Situation | Base class | Notes |
|---|---|---|
| No injection needed | `Validator<T, TSelf>` | Exposes `Default` singleton; invoke via `.Default.ValidateAndThrowAsync(...)` |
| Constructor injection needed | `Validator<T>` | No `Default`; register in DI; invoke via injected instance |
| FluentValidation-style preferred | `AbstractValidator<T, TSelf>` | `RuleFor(x => ...)` / `NotEmpty()` / `IsValid()` — still CoreEx; has `Default` |

**Key rules at a glance:**
- Ref-data: `.IsValid()` on the **navigation property** (`Gender`), never on `*Code` string
- `Mandatory()` on a non-nullable value type errors on `default` (`0`, `MinValue`) — use a range rule if `0` is valid
- Runtime-computed thresholds: use delegate overloads (`.LessThanOrEqualTo(_ => ...)`) — prefer over `OnValidateAsync` imperative logic
- `context.HasErrors` guard before async I/O; `context.HasError(x => x.Prop)` for per-property guards
- `context.AddError(x => x.Prop, ...)` — member-access expression, never `nameof(...)`
- Message text argument is a `{2}` suffix substitution — not a full sentence; use `.Error("...")` to override the whole message
- Always offer to create or update the matching `{Validator}Tests` in `*.Test.Unit/Validators/`

For full workflow, rule reference, and code examples see [`references/workflow.md`](references/workflow.md).

## Key References

- [`/.github/instructions/coreex-validators.instructions.md`](/.github/instructions/coreex-validators.instructions.md) — full rule set, comparison operators, localization, DependsOn, DI registration
- [`/.github/instructions/coreex-tests.instructions.md`](/.github/instructions/coreex-tests.instructions.md) — validator unit test conventions: `Test.Scoped`, `AssertErrors`, expected message text
- [`/samples/src/Contoso.Products.Application/Validators/`](/samples/src/Contoso.Products.Application/Validators/) — `ProductValidator` (simple), `MovementRequestValidator` (injection + dictionary + async)
- [`/samples/src/Contoso.Shopping.Application/Validators/`](/samples/src/Contoso.Shopping.Application/Validators/) — `AbstractValidator` style, plain request validators
- [`/samples/src/Contoso.Orders.Application/Validators/`](/samples/src/Contoso.Orders.Application/Validators/) — `OrderValidator` (collection validator)
