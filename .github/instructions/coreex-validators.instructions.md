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

## Reference Data Fields

Use `.IsValid()` on `ReferenceData`-typed properties to validate that the code is a known active value:

```csharp
Property(p => p.SubCategory).Mandatory().IsValid();
Property(p => p.UnitOfMeasure).Mandatory().IsValid();
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

Override `OnValidateAsync` for validators that need to query the database. Check `context.HasErrors` first to skip expensive async work if earlier rules already failed:

```csharp
protected async override Task OnValidateAsync(
    ValidationContext<MovementRequest> context,
    CancellationToken cancellationToken)
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

## Localization Labels

Property names in error messages use the property name by default. Override with `[Localization("...")]` on the contract property or pass a custom label into the rule:

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

## Further Reading

- [Application Layer Guide — Validators](https://github.com/Avanade/CoreEx/blob/main/samples/docs/application-layer.md) — full validator walkthrough including declarative and programmatic phases.
- [Pattern Catalog](https://github.com/Avanade/CoreEx/blob/main/samples/docs/patterns.md) — Validator pattern entry with cross-links.
- [CoreEx.Validation README](https://github.com/Avanade/CoreEx/blob/main/src/CoreEx.Validation/README.md) — `Validator<T>`, rule set, `OnValidateAsync`, `ValidateFurtherAsync`, and `AbstractValidator`.
