# coreex-validator: Workflow

Full workflow for creating or modifying a CoreEx validator in `Application/Validators/`. Follow the path that matches the request.

---

## Phase 1 — Clarify Before Writing

Answer these questions before emitting any code. Batch all questions — do not interrupt per-property.

| Question | Default | Notes |
|---|---|---|
| Does validation need a constructor-injected dependency? | No | Yes → Path B (`Validator<T>`); No → Path A (`Validator<T, TSelf>`) |
| FluentValidation-style `RuleFor` syntax preferred? | No | Yes → Path C (`AbstractValidator<T, TSelf>`) |
| Which properties need rules? | Ask | List all upfront; resolve types before writing any rule |
| Ref-data properties — required or optional? | Ask per field | Required: `.Mandatory().IsValid()`; optional: `.IsValid()` alone |
| Any async checks requiring a dependency (DB lookup, etc.)? | No | Yes → Path B (`Validator<T>` with injection); async-only (no dependency) → override `OnValidateAsync` on Path A |

---

## Path A — `Validator<T, TSelf>` (no injection)

Use when all rules are declarative and no constructor-injected dependency is needed. Exposes a `Default` singleton automatically.

### A1 — Scaffold

```csharp
namespace {Solution}.Application.Validators;

public class {Name}Validator : Validator<Contracts.{Name}, {Name}Validator>
{
    public {Name}Validator()
    {
        Property(x => x.Sku).Mandatory().MaximumLength(50);
        Property(x => x.Text).Mandatory().MaximumLength(250);
        Property(x => x.SubCategory).Mandatory().IsValid();   // ref-data navigation property
        Property(x => x.Price).PrecisionScale(null, 2).GreaterThanOrEqualTo(0, _ => "zero");
    }
}
```

### A2 — Common rule patterns

| Need | Rule chain |
|---|---|
| Required string | `.Mandatory().MaximumLength(n)` |
| Required ref-data | `.Mandatory().IsValid()` |
| Optional ref-data (valid if supplied) | `.IsValid()` |
| Non-negative decimal | `.GreaterThanOrEqualTo(0, _ => "zero")` |
| Decimal precision | `.PrecisionScale(precision, scale)` — pass `null` for precision to skip |
| Required + precision | `.PrecisionScale(null, 2).GreaterThanOrEqualTo(0, _ => "zero")` |
| Run rule only when entity condition met | `.WhenEntity(e => e.StartDate.HasValue)` chained after the rule |
| Run rule only when property value condition met | `.WhenValue(v => v != null)` chained after the rule (predicate on property value, not entity — use `WhenEntity` for entity conditions) |
| Skip when dependent prop invalid | `.DependsOn(x => x.OtherProp)` at end of chain |
| Runtime-computed threshold | `.LessThanOrEqualTo(_ => DateOnly.FromDateTime(Runtime.UtcNow.UtcDateTime.AddYears(-16)), _ => "the minimum age of 16")` |

**Mandatory() on non-nullable value types:**
`Mandatory()` errors when the value equals `default(T)` — `0` for `decimal`/`int`, `DateOnly.MinValue` for `DateOnly`. If `0` is a legitimate value, do **not** use `Mandatory()` — use `.GreaterThanOrEqualTo(0)` (allows `0`) or `.GreaterThan(0)` (requires positive) instead.

**Message text is a `{2}` suffix — not a full sentence:**
The text argument in comparison rules (`"zero"`, `"the minimum age of 16"`) supplies only the value portion of the standard message template (e.g. `"Price must be greater than or equal to zero."`). Use `.Error("...")` to replace the entire message.

### A3 — Invoke

```csharp
// Exception style — throws ValidationException on failure
await {Name}Validator.Default.ValidateAndThrowAsync(value, cancellationToken).ConfigureAwait(false);

// Result style — returns Result<T> for pipeline composition
var result = await {Name}Validator.Default.ValidateWithResultAsync(value, cancellationToken).ConfigureAwait(false);
```

---

## Path B — `Validator<T>` (constructor injection)

Use when the validator needs a repository or other Application-layer dependency. No `Default` singleton — register in DI and inject.

### B1 — Scaffold

```csharp
namespace {Solution}.Application.Validators;

public class {Name}Validator : Validator<Contracts.{Name}>
{
    private readonly I{Dep}Repository _repository;

    public {Name}Validator(I{Dep}Repository repository)
    {
        _repository = repository.ThrowIfNull();

        Property(x => x.Id).Mandatory().MaximumLength(50);
        Property(x => x.Text).Mandatory().MaximumLength(250);
        Property(x => x.Status).Mandatory().IsValid();   // ref-data navigation property
    }
}
```

### B2 — Async validation (`OnValidateAsync`)

Override `OnValidateAsync` for database lookups or cross-field checks requiring I/O.

**Guard rule:** check `context.HasErrors` first — bail if cheap validation already failed (the following logic needs the entity in a consistent state). For single-property layering, gate on `context.HasError(x => x.Prop)` instead.

```csharp
protected async override Task OnValidateAsync(ValidationContext<Contracts.{Name}> context, CancellationToken cancellationToken)
{
    // Bail early if prior rules already failed — I/O assumes a valid entity.
    if (context.HasErrors)
        return;

    var ids = context.Value.Products!.Keys.ToArray();
    var products = await _repository.GetForReservationAsync(ids).ConfigureAwait(false);

    // Use ValidateFurtherAsync to compose declarative rules from runtime data.
    await context.ValidateFurtherAsync(c => c
        .HasProperty(x => x.Products, c => c.Dictionary(c => c
            .WithKeyValidator("Product", k => k
                .NotFound().WhenValue(v => !products.ContainsKey(v))
                .Error("{0} is non-stocked and therefore cannot be transacted.").WhenValue(v => products[v].IsNonStocked)
                .Error("{0} is not active and therefore cannot be transacted.").WhenValue(v => products[v].IsInactive))
        )), cancellationToken).ConfigureAwait(false);
}
```

**Per-property guard (single-property layering):**

```csharp
protected override Task OnValidateAsync(ValidationContext<Employee> context, CancellationToken cancellationToken)
{
    // Gate only on DateOfBirth — an unrelated failure elsewhere should not skip this.
    if (!context.HasError(x => x.DateOfBirth))
    {
        var minDob = DateOnly.FromDateTime(Runtime.UtcNow.UtcDateTime.AddYears(-16));
        if (context.Value.DateOfBirth > minDob)
            context.AddError(x => x.DateOfBirth, "Employee must be at least 16 years old.");
    }
    return Task.CompletedTask;
}
```

**Note:** Prefer the declarative delegate overload when possible — the example above is better written as a rule:
```csharp
Property(x => x.DateOfBirth).Mandatory().LessThanOrEqualTo(_ => DateOnly.FromDateTime(Runtime.UtcNow.UtcDateTime.AddYears(-16)), _ => "the minimum age of 16");
```
Reserve `OnValidateAsync` + `AddError` for logic that genuinely cannot be expressed as a rule (e.g. multi-field conditions or checks requiring async I/O).

### B3 — Invocation

`Validator<T>` has no `Default` singleton. The established pattern is to instantiate at the call site, passing already-injected dependencies:

```csharp
// Exception style
await new {Name}Validator(_repository).ValidateAndThrowAsync(request, cancellationToken).ConfigureAwait(false);

// Result style — for pipeline composition
var result = await new {Name}Validator(_repository).ValidateWithResultAsync(request, cancellationToken).ConfigureAwait(false);
```

DI registration (`services.AddScoped<{Name}Validator>()`) is an option when the validator needs to be mocked in tests or is shared across multiple services, but is not required by default.

---

## Path C — `AbstractValidator<T, TSelf>` (FluentValidation-style)

Use when the team prefers `RuleFor(x => ...)` / `NotEmpty()` syntax. Validation and error handling are still performed by CoreEx — do **not** add the FluentValidation NuGet package.

```csharp
namespace {Solution}.Application.Validators;

public class {Name}Validator : AbstractValidator<Contracts.{Name}, {Name}Validator>
{
    public {Name}Validator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Sku).NotEmpty();
        RuleFor(x => x.UnitOfMeasure).NotEmpty().IsValid();   // ref-data navigation property
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
    }
}
```

Has a `Default` singleton (same as `Validator<T, TSelf>`). Invocation is identical to Path A.

---

## Path D — Add Rules to an Existing Validator

### D1 — Add a property rule

Locate the constructor and add the rule in property declaration order:

```csharp
Property(x => x.NewField).Mandatory().MaximumLength(100);
```

Apply the property type resolution from `coreex-contracts.instructions.md` (name-pattern inference → explicit type → ref-data check) for type decisions.

### D2 — Add an async check

**If no additional dependency is needed** (e.g. a cross-field check or conditional logic): override `OnValidateAsync` directly on the existing `Validator<T, TSelf>` class — no base class change required. Both `Validator<T, TSelf>` and `Validator<T>` support async overrides.

**If the async check requires a constructor-injected dependency** (e.g. a repository):
1. Change base class to `Validator<T>` (removes `Default` singleton — update all call sites).
2. Add a constructor parameter and inject the dependency.
3. Override `OnValidateAsync` and apply the B2 guard pattern.

If the class already uses `Validator<T>`, add to or extend the existing `OnValidateAsync` override.

### D3 — Extend `OnValidateAsync`

When adding to an existing override, respect the existing `context.HasErrors` guard — do not duplicate it. Add new per-property checks after existing ones, using `context.HasError(x => x.Prop)` guards where the new check applies to a single property.

---

## Nested Validators

### Entity

Use `.Entity()` to run a separate validator against a complex sub-property. Errors from the sub-validator are merged into the parent result under the sub-property's JSON path (e.g. `"address.street"`).

**Form 1 — Direct validator instance** (most common — sub-type has a `Validator<T, TSelf>` `Default` singleton):

```csharp
// OrderValidator constructor:
Property(x => x.ShippingAddress).Mandatory().Entity(AddressValidator.Default);
```

**Form 2 — DI-resolved validator** (sub-type uses `Validator<T>` with injection — no `Default` available):

```csharp
Property(x => x.ShippingAddress).Mandatory().Entity(w => w.WithValidator<AddressValidator>());
```

This calls `Validator.Get<AddressValidator>(serviceProvider)` at validation time — the validator is resolved from the DI container scoped to the current request.

**Form 3 — Inline rules** (quick one-off validation without a dedicated validator class):

```csharp
// Simple scalar property with inline length rule:
Property(x => x.Name).Entity(w => w.WithValidator(v => v.MaximumLength(4)));
```

**Key behaviour:** validation errors propagate with the full nested path. If `ShippingAddress.Street` fails, the error key is `"shippingAddress.street"` — not `"street"` alone.

### Collection

```csharp
private static readonly Validator<OrderItem> _itemValidator = Validator.Create<OrderItem>()
    .HasProperty(x => x.Id, p => p.Mandatory().MaximumLength(50))
    .HasProperty(x => x.Quantity, p => p.GreaterThanOrEqualTo(0m).PrecisionScale(null, 4));

// In constructor:
Property(x => x.Items).Collection(c => c.WithItemValidator(_itemValidator));
```

**Accessing the collection index from an item rule:**
When the item validator needs to look up data keyed by position (e.g. a per-index minimum quantity), use `ctx.GetCollectionIndex()` in a rule's delegate overload:

```csharp
// minimumQuantities is captured from the outer scope (e.g. fetched in OnValidateAsync)
var itemValidator = Validator.Create<OrderItem>()
    .HasProperty(x => x.Quantity, p => p
        .GreaterThanOrEqualTo(ctx => minimumQuantities[ctx.GetCollectionIndex()]));
```

`GetCollectionIndex()` returns the zero-based integer index set by `CollectionRule` during enumeration. It throws `IndexOutOfRangeException` if called outside a collection validation context. Note: `.Error()` takes a plain string (`LText`) — it does not accept a delegate, so dynamic index-based error messages must be composed via `context.AddError(...)` in `OnValidateAsync` instead.

### Dictionary

```csharp
private static readonly LText _productText = "Product";

private static readonly Validator<MovementRequestProduct> _productValidator = Validator.Create<MovementRequestProduct>()
    .HasProperty(x => x.UnitOfMeasure, c => c.Mandatory().IsValid())
    .HasProperty(x => x.Quantity, c => c
        .GreaterThanOrEqualTo(0)
        .PrecisionScale(ctx => ctx.Entity.UnitOfMeasure!.Precision, ctx => ctx.Entity.UnitOfMeasure!.Scale)
        .DependsOn(x => x.UnitOfMeasure));

// In constructor:
Property(x => x.Products).Mandatory().Dictionary(c => c
    .WithKeyValidator(_productText, k => k.Mandatory().MaximumLength(50))
    .WithValueValidator(v => v.Mandatory().Entity(_productValidator)));
```

**Accessing the dictionary key from a value rule:**
When the value validator needs to look up data by key, use `ctx.GetDictionaryKey<T>()`:

```csharp
var dv = Validator.Create<MovementRequestProduct>()
    .HasProperty(x => x.UnitOfMeasure, c => c.Equal(
        ctx => products[ctx.GetDictionaryKey<string>()].UnitOfMeasureCode));
```

---

## Phase 2 — Validate and Test

1. `dotnet build` — no errors or warnings.
2. Verify the validator is invoked correctly from the service: exception-style (`ValidateAndThrowAsync`) or Result-style (`ValidateWithResultAsync`).
3. **Offer to create or update the matching test class** in `*.Test.Unit/Validators/`. The test class must:
   - Use non-generic `Test.Scoped(test => { ... })` with `XxxValidator.Default` (or `new XxxValidator(mockDep)` for injected validators).
   - Cover **every rule** — both error and success cases.
   - Use `(jsonName, "Full expected message.")` tuples in `AssertErrors(...)`.
   - Use camelCase JSON property path; ref-data rules key on the navigation name (`"gender"`, not `"genderCode"`).
   - Use sentence-case labels in expected messages (`"First name is required."` not `"FirstName is required."`).
4. If a ref-data type is used in the test host that is not yet registered, add the corresponding case to `EntryPoint.ReferenceDataServiceDecorator.GetAsync`.

---

## Guardrails

- **Never use the `FluentValidation` NuGet package.** `AbstractValidator` is `CoreEx.Validation.AbstractValidator`. Installing the FluentValidation package causes namespace collisions.
- **Ref-data: validate the navigation property, not the `*Code` string.** `.IsValid()` on `GenderCode` does nothing useful; use `Gender`.
- **`Mandatory()` on non-nullable value types errors on `default`.** Use a range rule when `0` is a legitimate value.
- **`context.HasErrors` is a global bail-out for I/O.** Do not use it to skip per-property checks unrelated to the I/O path — use `context.HasError(x => x.Prop)` instead.
- **`context.AddError` takes a member-access expression.** Never pass a property-name string or `nameof(...)`.
- **Message text is `{2}` substitution only.** It is appended to the standard message template. Use `.Error("...")` to replace the entire message.
- **Do not reference Infrastructure assemblies.** Inject Application-layer repository/adapter interfaces only.
- **Do not add logic requiring async I/O to the constructor.** Constructor is for rule wiring only; all async work goes in `OnValidateAsync`.
- **Do not use `CompareValue(...)` or `CompareOperator.GreaterThanEqual`.** The method is `.Compare(...)` and the operator is `CompareOperator.GreaterThanOrEqualTo`. Prefer the dedicated `.GreaterThanOrEqualTo(...)` / `.LessThanOrEqualTo(...)` rules.
- **Never instantiate the validator at the call site with `new`** when a `Default` singleton is available.
- **Comparison direction: verify the rule fails on the invalid case.** For `LessThanOrEqualTo`, the rule passes when `value <= threshold` and fails when `value > threshold`. Match this against the equivalent `if (value > threshold) AddError(...)`.
