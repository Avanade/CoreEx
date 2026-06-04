---
applyTo: "**/*Validator*.cs"
description: "Validator conventions: Validator<T,TSelf>, AbstractValidator, declarative rules, async OnValidateAsync, nested/dictionary validators, and Result-based invocation"
tags: ["validators", "validation", "fluent-api", "rules", "error-handling", "application-layer"]
---

# Validator Conventions

## NuGet / Project References

| Package | Key types provided |
|---|---|
| `CoreEx.Validation` | `Validator<T, TSelf>`, `Validator<T>`, `AbstractValidator<T, TSelf>`, `AbstractValidator<T>`, `Validator.Create<T>()`, `.Mandatory()`, `.MaximumLength()`, `.IsValid()`, `.PrecisionScale()`, `.GreaterThanOrEqualTo()`, `.LessThanOrEqualTo()`, `.Equal()`, `.NotFound()`, `.WhenValue()`, `.Error()`, `.DependsOn()`, `.Entity()`, `.Dictionary()`, `.WithKeyValidator()`, `.WithValueValidator()`, `ValidationContext<T>`, `.ValidateFurtherAsync()`, `.ValidateAndThrowAsync()`, `.ValidateWithResultAsync()` |
| `CoreEx` | `LText` — localised text label for use in `.WithKeyValidator(label, ...)`; `[Localization(...)]` attribute on contract properties |
| `CoreEx.UnitTesting` | `.AssertErrors()` — test-only helper for asserting expected validation errors inline |

## Placement

Validators live in `Application/Validators/`. They belong to the Application layer and may inject Application-layer dependencies (e.g., `IProductRepository`) — they must not reference Infrastructure directly.

## Base Class

Choose the base class based on whether a `Default` singleton and constructor injection are needed:

**`Validator<TEntity, TSelf>`** — use when no constructor injection is required. The two-type-argument form exposes a static `Default` singleton automatically:

```csharp
public class ProductValidator : Validator<Contracts.Product, ProductValidator>
{
    public ProductValidator()
    {
        Property(p => p.Sku).Mandatory().MaximumLength(50);
        Property(p => p.Text).Mandatory().MaximumLength(250);
        Property(p => p.SubCategory).Mandatory().IsValid();
        Property(p => p.UnitOfMeasure).Mandatory().IsValid();
        Property(p => p.Price).PrecisionScale(null, 2).GreaterThanOrEqualTo(0, _ => "zero");
    }
}
```

**`Validator<TEntity>`** — use when constructor injection is required (e.g., a repository dependency). There is no `Default` singleton; register the validator in DI and inject it:

```csharp
public class MovementRequestValidator : Validator<Contracts.MovementRequest>
{
    private readonly IProductRepository _repository;

    public MovementRequestValidator(IProductRepository repository)
    {
        _repository = repository.ThrowIfNull();
        Property(x => x.Id).Mandatory().MaximumLength(50);
        // ...
    }
}
```

**`AbstractValidator<TEntity, TSelf>`** — a FluentValidation-style compatibility alias for `Validator<TEntity, TSelf>`. Use when your team prefers the `RuleFor(x => ...)` / `NotEmpty()` / `GreaterThanOrEqualTo()` syntax. Validation and error handling are still performed by CoreEx:

```csharp
public class ProductValidator : AbstractValidator<Product, ProductValidator>
{
    public ProductValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Sku).NotEmpty();
        RuleFor(x => x.UnitOfMeasure).NotEmpty().IsValid();
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
    }
}
```

Do **not** use the `FluentValidation` NuGet package — `AbstractValidator` here is `CoreEx.Validation.AbstractValidator`, not FluentValidation.

## Invoking Validators

For `Validator<T, TSelf>` (no injection), call via the static `Default` singleton:

```csharp
// Exception style — throws ValidationException on failure
await ProductValidator.Default.ValidateAndThrowAsync(product);

// Result style — returns Result<T> for pipeline composition
var result = await ProductValidator.Default.ValidateWithResultAsync(product);
```

For `Validator<T>` (with injection), the instance is resolved from DI and invoked the same way:

```csharp
await _movementRequestValidator.ValidateAndThrowAsync(request);
```

## Common Rules

| Rule | Method |
|---|---|
| Required | `.Mandatory()` |
| Max string length | `.MaximumLength(n)` |
| Reference data validity | `.IsValid()` |
| Decimal precision | `.PrecisionScale(precision, scale)` |
| Greater than or equal to | `.GreaterThanOrEqualTo(value)` |
| Less than or equal to | `.LessThanOrEqualTo(value)` |
| Equals | `.Equal(value)` |
| Not found (for key lookup) | `.NotFound()` |
| Conditional rule | `.WhenValue(predicate)` |
| Custom error text | `.Error("message")` |

### Full rule and clause reference

The table above lists the most common rules; the full set of fluent extension methods is below. All are part of `CoreEx.Validation` — consult the [CoreEx.Validation README](https://github.com/Avanade/CoreEx/blob/main/src/CoreEx.Validation/README.md) for complete detail and overloads before hand-rolling logic in `OnValidateAsync` (most needs are already covered by a rule).

| Category | Rules (extension methods) |
|---|---|
| Presence (required) | `Mandatory()`, `NotNull()`, `NotEmpty()` |
| Absence (must be unset) | `Null()`, `None()`, `Empty()` |
| Strings | `MaximumLength(n)`, `MinimumLength(n)`, `Length(exact)`, `String(maxLength)`, `String(min, max, regex)`, `Matches(regex)`, `Wildcard()`, `Email()` / `Email(maxLength)` |
| Numbers / decimals | `Numeric(allowNegatives)`, `Positive()`, `Decimal(precision, scale)`, `PrecisionScale(precision, scale)` |
| Comparisons (value) | `Equal(v)`, `NotEqual(v)`, `LessThan(v)`, `LessThanOrEqualTo(v)`, `GreaterThan(v)`, `GreaterThanOrEqualTo(v)`, `Compare(op, v)` — each with a delegate overload for runtime values (see below) |
| Comparisons (other) | `CompareProperty(op, x => x.Other)`, `CompareValues(values)`, `Between(min, max)`, `InclusiveBetween(min, max)`, `ExclusiveBetween(min, max)` |
| Enums | `Enum()`, `Enum(allowed[])`, string `Enum(c => c....)` |
| Reference data | `IsValid(allowInactive)` / `ReferenceData(allowInactive)` (typed property), `ReferenceDataCodes()` / `AreValid()` (code collection), string `ReferenceData(c => c....)` |
| Collections | `Collection(...)`, with `WithDuplicateIdCheck()` / `WithDuplicateKeyCheck()` / `WithDuplicatePropertyCheck()` / `WithDuplicateCheck()` |
| Dictionaries | `Dictionary(...)` (with `WithKeyValidator(...)` / `WithValueValidator(...)`) |
| Child / shared / external | `Entity(validator)` (child entity), `Common(commonValidator)` (shared value rules), `Interop(validator)` (external/FluentValidation) |
| Always-error (guard with a clause) | `Error(text)`, `Duplicate()`, `NotFound()`, `Invalid()`, `Immutable()` |
| Clauses (conditional execution) | `When(...)`, `WhenValue(pred)`, `WhenHasValue()`, `WhenEntity(pred)`, `DependsOn(x => x.Other)` |

### Comparisons

Prefer the dedicated comparison rules — `.GreaterThanOrEqualTo(value)`, `.LessThanOrEqualTo(value)`, `.GreaterThan(value)`, `.LessThan(value)`, `.Equal(value)`. For the general form use the **`.Compare(...)`** extension with a `CompareOperator` value:

```csharp
// ✅ Idiomatic — dedicated rule (preferred)
Property(x => x.Salary).GreaterThanOrEqualTo(0m, _ => "zero").PrecisionScale(18, 2);

// ✅ Equivalent — general Compare extension
Property(x => x.Salary).Compare(CompareOperator.GreaterThanOrEqualTo, 0m, "zero").PrecisionScale(18, 2);

// ❌ Wrong — no such method `CompareValue`, and `GreaterThanEqual` is not a valid CompareOperator
Property(x => x.Salary).CompareValue(CompareOperator.GreaterThanEqual, 0m, "zero").PrecisionScale(18, 2);
```

The extension is **`Compare`** (not `CompareValue`), and the `CompareOperator` members are `Equal`, `NotEqual`, `LessThan`, `LessThanOrEqualTo`, `GreaterThan`, `GreaterThanOrEqualTo` (there is no `GreaterThanEqual`).

#### Runtime-computed values (delegate overloads)

Most comparison rules (and many others) provide a **delegate overload** — `Func<PropertyContext<TEntity, TProperty>, TProperty>` — so the value can be computed at runtime. Prefer a declarative rule with a delegate over an imperative check in `OnValidateAsync` whenever the rule can express it. For example, "at least 16 years old" is a comparison rule, not hand-written logic.

Mind the direction: the threshold is `today − 16 years` — the **latest** acceptable birth date — so being at least 16 means `DateOfBirth <= threshold`. A rule passes when its condition holds, so the correct rule is **`LessThanOrEqualTo`** (it errors when `DateOfBirth` is *after* the threshold, i.e. younger than 16):

```csharp
// ✅ Preferred — declarative rule with a computed threshold (no OnValidateAsync needed)
Property(x => x.DateOfBirth)
    .Mandatory()
    .LessThanOrEqualTo(_ => DateOnly.FromDateTime(Runtime.UtcNow.UtcDateTime.AddYears(-16)), _ => "the minimum age of 16");
```

The delegate parameter is the `PropertyContext` (use `ctx => ctx.Entity...` to compute relative to other properties; ignore it with `_ =>` for an absolute value such as one derived from `Runtime.UtcNow`). Always sanity-check the comparison direction so the rule *fails* on the invalid case, matching the equivalent imperative check (`if (DateOfBirth > threshold) AddError(...)`).

#### Message text is a suffix, not a full sentence

The optional text argument on these rules (e.g. `"zero"`, `_ => "the minimum age of 16"`) supplies **only the value substitution** (`{2}`) in the standard message template — it is **not** a complete error message. For instance `CompareLessThanEqualFormat` is `"{0} must be less than or equal to {2}."`, so `.LessThanOrEqualTo(..., _ => "the minimum age of 16")` renders *"Date Of Birth must be less than or equal to the minimum age of 16."* Keep the text short (the value rendering only); the standard wrapper supplies the rest. The default templates live in `ValidatorStrings.cs` (each is an overridable, localizable `LText`) — consult it rather than re-inventing full messages. To override the *entire* message, use `.Error("...")` instead.

## Reference Data Fields

Use `.IsValid()` on the **typed reference-data navigation property** to validate that the value is a known active item — **not** the serialized `*Code` string property. For a contract with `GenderCode`, validate the generated `Gender` property; for `SubCategoryCode`, validate `SubCategory`; and so on.

```csharp
// ✅ Correct — validate the typed navigation property
Property(p => p.SubCategory).Mandatory().IsValid();
Property(p => p.UnitOfMeasure).Mandatory().IsValid();
Property(x => x.Gender).IsValid();

// ❌ Wrong — do not apply .IsValid() to the *Code string property
Property(x => x.GenderCode).IsValid();
```

## Nested / Collection Validators

For entities with nested objects, create a separate `Validator.Create<T>()` for the nested type and reference it via `.Entity(validator)` or `.Dictionary(c => c.WithKeyValidator(...).WithValueValidator(...))`.

Use `LText` to provide a localised label for dictionary keys in error messages:

```csharp
private static readonly LText _productText = "Product";

private static readonly Validator<MovementRequestProduct> _productValidator = Validator.Create<MovementRequestProduct>()
    .HasProperty(x => x.UnitOfMeasure, c => c.Mandatory().IsValid())
    .HasProperty(x => x.Quantity, c => c.GreaterThanOrEqualTo(0).DependsOn(x => x.UnitOfMeasure));

// In parent validator constructor:
Property(x => x.Products).Mandatory().Dictionary(c => c
    .WithKeyValidator(_productText, k => k.Mandatory().MaximumLength(50))
    .WithValueValidator(v => v.Mandatory().Entity(_productValidator)));
```

When the value validator needs to access the dictionary key (e.g., to look up data keyed by that value), use `ctx.GetDictionaryKey<T>()` inside the rule lambda:

```csharp
var dv = Validator.Create<MovementRequestProduct>()
    .HasProperty(x => x.UnitOfMeasure, c => c.Equal(
        ctx => products[ctx.GetDictionaryKey<string>()].UnitOfMeasureCode));
```

## Async Validation (Database Checks)

Override `OnValidateAsync` for validators that need to query the database. Check `context.HasErrors` first to skip expensive async work if earlier rules already failed — this global guard is appropriate here because the I/O assumes a valid entity. (When a check merely augments a single property, gate on that property with `context.HasError(x => x.Prop)` instead — see [Adding Errors Manually](#adding-errors-manually).)

```csharp
protected async override Task OnValidateAsync(ValidationContext<MovementRequest> context, CancellationToken cancellationToken)
{
    if (context.HasErrors)
        return;

    var ids = context.Value.Products!.Keys.ToArray();
    var products = await _repository.GetForReservationAsync(ids).ConfigureAwait(false);

    await context.ValidateFurtherAsync(c => c
        .HasProperty(x => x.Products, c => c.Dictionary(c => c
            .WithKeyValidator("Product", k => k
                .NotFound().WhenValue(v => !products.ContainsKey(v))
                .Error("{0} is non-stocked.").WhenValue(v => products[v].IsNonStocked))
        )), cancellationToken).ConfigureAwait(false);
}
```

## Adding Errors Manually

**Prefer a declarative rule first.** Most checks — including those needing runtime-computed values — are expressible as rules via the delegate overloads (see [Runtime-computed values](#runtime-computed-values-delegate-overloads)); the "at least 16 years old" check below is better written as `Property(x => x.DateOfBirth).LessThanOrEqualTo(_ => DateOnly.FromDateTime(Runtime.UtcNow.UtcDateTime.AddYears(-16)), _ => "the minimum age of 16")`. Reserve manual `OnValidateAsync` + `AddError` for logic that genuinely cannot be a rule (e.g. multi-field conditions or checks requiring async I/O).

When you do add an error directly, identify the property using the **member-access expression** overload of `context.AddError` — `context.AddError(x => x.Property, ...)`. Never pass a property-name string such as `nameof(...)`; the expression resolves the label, JSON name, and metadata automatically.

**Choose the right guard — `HasErrors` vs `HasError(x => x.Prop)`:**
- `context.HasErrors` (global) — bail early only when the *following logic needs the whole entity in a valid state*, e.g. before async I/O / database lookups (see [Async Validation](#async-validation-database-checks)).
- `context.HasError(x => x.Prop)` (per-property) — when you are simply layering an additional rule onto a single property, gate on **that property only**. This still runs your check when unrelated properties have failed, and skips it only when the property itself is already in error (avoiding a misleading second message).

```csharp
protected override Task OnValidateAsync(ValidationContext<Employee> context, CancellationToken cancellationToken)
{
    // Only gate on DateOfBirth — an unrelated failure elsewhere should not skip this check.
    if (!context.HasError(x => x.DateOfBirth))
    {
        // Use Runtime.UtcNow (the ambient, ExecutionContext-aware clock) — never DateTime.UtcNow / DateTimeOffset.UtcNow.
        var minDob = DateOnly.FromDateTime(Runtime.UtcNow.UtcDateTime.AddYears(-16));
        if (context.Value.DateOfBirth > minDob)
            context.AddError(x => x.DateOfBirth, "Employee must be at least 16 years old.");   // expression — not nameof(...)
    }

    return Task.CompletedTask;
}
```

## Localization Labels

Property names in error messages use an auto-derived label by default (the PascalCase property name split into words, e.g. `DateOfBirth` → "Date Of Birth"). Override with `[Localization("...")]` on the contract property (or a custom label in the rule) **only when the default is undesired** — do not add `[Localization("Salary")]` to a `Salary` property, as it merely repeats the default:

```csharp
// Contract
[Localization("Sub-category")]
public partial string? SubCategoryCode { get; set; }

// Produces: "Sub-category is required." (not "SubCategoryCode is required.")
```

## DependsOn for Conditional Precision

Use `.DependsOn(x => x.OtherProp)` to skip a rule when a dependent property is already invalid. This prevents misleading cascading errors:

```csharp
Property(x => x.Quantity, c => c
    .GreaterThanOrEqualTo(0)
    .PrecisionScale(
        ctx => ctx.Entity.UnitOfMeasure!.Precision,
        ctx => ctx.Entity.UnitOfMeasure!.Scale)
    .DependsOn(x => x.UnitOfMeasure));
```

## Do Not

- Do not use the `FluentValidation` NuGet package — `AbstractValidator` here is `CoreEx.Validation.AbstractValidator`, not FluentValidation.
- Do not perform I/O in `OnValidateAsync` without first checking `context.HasErrors` — always fail fast.
- Do not reference Infrastructure assemblies from validators — inject Application-layer repository interfaces only.
- Do not instantiate validators with `new` at the call site when a `Default` singleton is available.
- Do not add logic that requires async I/O to the constructor — use `OnValidateAsync` for that.
- Do not pass a property-name string (e.g. `nameof(...)`) to `context.AddError` — use the member-access expression overload, `context.AddError(x => x.Property, ...)`.
- Do not apply `.IsValid()` to a `*Code` string property — validate the typed reference-data navigation property instead (e.g. `Gender`, not `GenderCode`).
- Do not use `CompareValue(...)` or a `CompareOperator.GreaterThanEqual` value — the extension is `.Compare(...)` and the operator is `CompareOperator.GreaterThanOrEqualTo` (or use the dedicated `.GreaterThanOrEqualTo(...)` rule).
- Do not hand-write logic in `OnValidateAsync` for something expressible as a rule — use the delegate overloads for runtime-computed values (e.g. `.LessThanOrEqualTo(_ => DateOnly.FromDateTime(Runtime.UtcNow.UtcDateTime.AddYears(-16)), _ => "the minimum age of 16")`). Sanity-check the comparison direction so the rule fails on the *invalid* case.
- Do not put a full sentence in a rule's text argument — it is only the `{2}` value substitution in the standard message template; override the whole message with `.Error("...")`, and consult `ValidatorStrings.cs` for the defaults.
- Do not add a redundant `[Localization]` whose value equals the auto-derived label (e.g. `[Localization("Salary")]` on `Salary`) — only annotate to change the label.

## Further Reading

- [Application Layer Guide — Validators](https://github.com/Avanade/CoreEx/blob/main/samples/docs/application-layer.md) — full validator walkthrough including declarative and programmatic phases.
- [Pattern Catalog](https://github.com/Avanade/CoreEx/blob/main/samples/docs/patterns.md) — Validator pattern entry with cross-links.
- [CoreEx.Validation README](https://github.com/Avanade/CoreEx/blob/main/src/CoreEx.Validation/README.md) — `Validator<T>`, rule set, `OnValidateAsync`, `ValidateFurtherAsync`, and `AbstractValidator`.
