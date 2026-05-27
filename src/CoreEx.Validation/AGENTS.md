# CoreEx.Validation — AI Usage Guide

Fluent, property-centric validation framework tightly integrated with `ExecutionContext`, `LText` localisation, and the CoreEx exception model.

## Define a Validator

Extend `Validator<TEntity>` and declare property chains in the constructor. Assign a `Default` singleton to avoid repeated instantiation.

```csharp
public class ProductValidator : Validator<Product>
{
    public static readonly ProductValidator Default = new();

    public ProductValidator()
    {
        Property(p => p.Sku)
            .Mandatory()
            .String(maxLength: 20);

        Property(p => p.Name)
            .Mandatory()
            .String(maxLength: 100);

        Property(p => p.Price)
            .Mandatory()
            .CompareValue(CompareOperator.GreaterThan, 0m);
    }
}
```

## Call from a Service

Always call `ValidateAndThrowAsync` before mutating state. This produces a `ValidationException` with all field-level errors at once.

```csharp
public async Task<Product> CreateAsync(Product product, CancellationToken ct = default)
{
    await ProductValidator.Default.ValidateAndThrowAsync(product, ct).ConfigureAwait(false);

    return await _uow.TransactionAsync(async () =>
    {
        // ... persist and publish
    }).ConfigureAwait(false);
}
```

## Common Rules Reference

```csharp
Property(p => p.Email).Mandatory().Email();
Property(p => p.Status).Mandatory().Enum();            // validates enum is defined
Property(p => p.Quantity).Mandatory().Numeric(allowNegatives: false);
Property(p => p.Description).String(maxLength: 500);
Property(p => p.Tags).Collection(maxCount: 10);
Property(p => p.Address).Entity(AddressValidator.Default);  // child entity
```

## Conditional Rules

Use `When`/`WhenHasValue`/`DependsOn` to guard the declarative rules.

```csharp
Property(p => p.DiscountCode)
    .Mandatory()
    .WhenEntity(product => product.HasDiscount);

Property(p => p.ExpiresOn)
    .CompareValue(CompareOperator.GreaterThan, () => Runtime.UtcNow)
    .WhenHasValue();
```

## Reusable Validators

Use `CommonValidator<TValue>` for logic reused across multiple entity validators.

```csharp
public static readonly CommonValidator<string> SkuValidator = Validator.CreateCommon<string>(v =>
    v.String(minLength: 3, maxLength: 20));

// Usage
HasRuleFor(p => p.Sku).Common(SkuValidator);
```

## Do Not

- Do not call `ValidateAsync` and ignore errors — always call `ValidateAndThrowAsync` or check `HasErrors` and throw `ValidationException` explicitly.
- Do not add try/catch around `ValidateAndThrowAsync` to rethrow as a different exception type — `ValidationException` maps to HTTP 400 automatically.
- Do not validate inside `TransactionAsync` — validate first (before the transaction) so failed validation never opens a database transaction.
- Do not use `DataAnnotations` attributes alongside CoreEx validators — pick one approach per entity and stay consistent.
- Do not use FluentValidation alongside CoreEx validators unless bridging with `InteropRule` is explicitly needed.

## Further Reading

- [README](./README.md) — full rule catalogue, clause types, `CommonValidator`, `RuleSet`, and `ValidationExtensions` API reference.
- [CoreEx](../CoreEx/README.md) — `ValidationException`, `MessageItemCollection`, and `LText` localisation.
- [CoreEx.AspNetCore](../CoreEx.AspNetCore/README.md) — `ValidationException` is translated to `400 application/problem+json` with field-level errors by `WebApi`.
- [Application layer](../../samples/docs/application-layer.md) — where and how validators are called in application services, including the validate-before-transaction pattern.
- [Patterns](../../samples/docs/patterns.md) — validation patterns, conditional rules, reusable `CommonValidator` usage, and the relationship between validators and policies.
