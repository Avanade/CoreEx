---
applyTo: "**/*Validator*.cs"
---

# Validator Conventions

## NuGet / Project References

| Package | Key types provided |
|---|---|
| `CoreEx.Validation` | `Validator<T, TSelf>`, `Validator.Create<T>()`, `.Mandatory()`, `.MaximumLength()`, `.IsValid()`, `.PrecisionScale()`, `.GreaterThanOrEqualTo()`, `.LessThanOrEqualTo()`, `.Equal()`, `.NotFound()`, `.WhenValue()`, `.Error()`, `.DependsOn()`, `.Entity()`, `.Dictionary()`, `ValidationContext<T>`, `.ValidateFurtherAsync()`, `.ValidateAndThrowAsync()`, `.ValidateWithResultAsync()`, `.AssertErrors()` (test helper) |
| `CoreEx.Localization` | `[Localization(...)]` attribute on contract properties |

## Base Class

Use `Validator<TEntity, TSelf>` from `CoreEx.Validation`. Expose a static `Default` singleton instance:

```csharp
public class ProductValidator : Validator<Product, ProductValidator>
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

Do **not** use FluentValidation unless the project already depends on it.

## Static Default Instance

The `Validator<T, TSelf>` base provides a `Default` singleton. Call `ValidateAndThrowAsync` or `ValidateWithResultAsync` without instantiating manually:

```csharp
// Exception style (services)
await ProductValidator.Default.ValidateAndThrowAsync(product);

// Result style (domain-aggregate services)
var result = await ProductValidator.Default.ValidateWithResultAsync(product);
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

For entities with nested objects, create a separate `Validator.Create<T>()` for the nested type and reference it via `.Entity(validator)` or `.Dictionary(...)`:

```csharp
private static readonly Validator<MovementRequestProduct> _productValidator = Validator.Create<MovementRequestProduct>()
    .HasProperty(x => x.UnitOfMeasure, c => c.Mandatory().IsValid())
    .HasProperty(x => x.Quantity, c => c.GreaterThanOrEqualTo(0).DependsOn(x => x.UnitOfMeasure));

// In parent validator:
Property(x => x.Products).Mandatory().Dictionary(c => c
    .WithKeyValidator("Product", k => k.Mandatory().MaximumLength(50))
    .WithValueValidator(v => v.Mandatory().Entity(_productValidator)));
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

Use `.DependsOn(x => x.OtherProp)` to skip a rule when a dependent property is already invalid:

```csharp
Property(x => x.Quantity, c => c
    .GreaterThanOrEqualTo(0)
    .PrecisionScale(
        ctx => ctx.Entity.UnitOfMeasure!.Precision,
        ctx => ctx.Entity.UnitOfMeasure!.Scale)
    .DependsOn(x => x.UnitOfMeasure));
```
