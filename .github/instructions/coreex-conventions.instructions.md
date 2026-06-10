---
applyTo: "**/*.cs"
description: "Universal C# coding conventions: nullable, implicit usings, GlobalUsing.cs, file-scoped namespaces, brace style, expression bodies, and private field naming"
tags: ["conventions", "style", "nullable", "usings", "naming"]
---

# C# Coding Conventions

## Project Configuration

Every project must have `Nullable` and `ImplicitUsings` enabled. For consuming projects these are typically set once in a root `Directory.Build.props`:

```xml
<PropertyGroup>
  <Nullable>enable</Nullable>
  <ImplicitUsings>enable</ImplicitUsings>
</PropertyGroup>
```

Nullable warnings are treated as errors. Never suppress a nullable warning with the null-forgiving operator (`!`) without a clear reason in a comment.

## Global Usings

Every project has a single `GlobalUsing.cs` at the project root that declares all namespace imports. Do not add `using` statements to individual source files.

```csharp
// GlobalUsing.cs — all usings for the project declared here (sorted alphabetically)
global using Contoso.Products.Application.Interfaces;
global using CoreEx;
global using Microsoft.Extensions.Logging;
global using System;
global using System.Collections.Generic;
global using System.Threading;
global using System.Threading.Tasks;
```

**Always re-sort after editing**: Whenever you add or change an entry in `GlobalUsing.cs`, re-sort the **entire** file alphabetically (ordinal) so the order stays deterministic — all namespaces sorted equally, with no special grouping for `System.*`, one `global using` per line. Never simply append a new entry at the end.

**Why this matters for code generation**: The `*.CodeGen` project emits no `using` statements in generated files. Every namespace referenced by generated code must already be declared in `GlobalUsing.cs`, or the generated output will not compile. When adding a new namespace dependency, add it to `GlobalUsing.cs` — not to the generated file.

## File-Scoped Namespaces

Use file-scoped namespace declarations. Never use block-scoped namespaces.

```csharp
// Correct — file-scoped
namespace Contoso.Products.Application;

public class ProductService { }
```

```csharp
// Wrong — do not use block-scoped
namespace Contoso.Products.Application
{
    public class ProductService { }
}
```

## Braces on `if` Statements

Single-line `if` bodies do not require braces.

```csharp
// Correct — no braces needed
if (product == null) return null;

// Correct — no braces needed - prefer muli-line bodies even when they fit on one line
if (context.HasErrors)
    return;

// Correct — braces required when body spans multiple lines
if (condition)
{
    DoSomething();
    DoSomethingElse();
}
```

## Expression-Bodied Members

Use `=>` syntax whenever the entire body is a single expression — methods, properties, constructors, operators, and accessors. Use a block body when there are multiple statements. The choice is entirely the developer's; the IDE makes no suggestion in either direction.

```csharp
// Method delegation — use =>
public Task<Product?> GetAsync(string id) => _repository.GetAsync(id);

// Multi-line single expression — use =>
public Task<ItemsResult<ProductLite>> QueryAsync(QueryArgs? query, PagingArgs? paging)
    => _repository.QueryAsync(query, paging);

// Constructor with single expression — use =>
public DataResult(bool wasMutated) => WasMutated = wasMutated;

// Computed property — use =>
public string DisplayName => $"{First} {Last}";

// Multiple statements — block body required
public async Task<Product> UpdateAsync(Product product)
{
    product.ThrowIfNull();
    await ProductValidator.Default.ValidateAndThrowAsync(product).ConfigureAwait(false);
    return await _repository.UpdateAsync(product).ConfigureAwait(false);
}
```

## Line Length and Method Declarations

Keep declarations and statements on a **single line**. Do not wrap a method (or constructor/delegate) declaration across multiple lines by placing each parameter on its own line — keep the signature on one line even when it has several parameters. Only break a line when it would otherwise exceed **250 characters**.

```csharp
// Correct — single line, even with multiple parameters
protected override Task OnValidateAsync(ValidationContext<Contracts.Employee> context, CancellationToken cancellationToken)

// Incorrect — needlessly split across lines while under 250 characters
protected override Task OnValidateAsync(
    ValidationContext<Contracts.Employee> context,
    CancellationToken cancellationToken)
```

## Ambient Runtime (Clock and GUIDs)

Obtain the current time and new `Guid` values from CoreEx's ambient `Runtime` (`CoreEx` namespace) rather than the BCL statics. `Runtime` is `ExecutionContext`-aware and provider-backed (`TimeProvider` / `IdentifierGenerator`), making values consistent across a request and substitutable in tests.

```csharp
DateTimeOffset now = Runtime.UtcNow;             // DateTimeOffset
DateTime utc       = Runtime.UtcNow.UtcDateTime; // DateTime (when a DateTime is required)
Guid id            = Runtime.NewGuid();          // new Guid
```

- Need a `DateTimeOffset` → `Runtime.UtcNow` (**never** `DateTimeOffset.UtcNow`).
- Need a `DateTime` → `Runtime.UtcNow.UtcDateTime` (**never** `DateTime.UtcNow`).
- Need a `Guid` → `Runtime.NewGuid()` (**never** `Guid.NewGuid()`).

## XML Documentation Comments

Document the public surface with XML doc comments, and **never duplicate** a description that an interface already provides:

- **Interfaces** — give **every** member (method, property, event) a `<summary>` describing the operation. Document parameters/returns (`<param>`, `<returns>`) where it adds clarity.
- **Contract / DTO properties** — every property on a contract (including `[Contract]` partial classes) gets a `<summary>`. (Standard `Id`/`ETag`/`ChangeLog` members that implement `IIdentifier`/`IETag`/`IChangeLog` may use `<inheritdoc/>`.)
- **Implementing / overriding members** — on the concrete class member that implements an interface member (or overrides a base member), use **`<inheritdoc/>`** rather than repeating the summary.
- **Everything else** — a public type or member that is **not** an interface implementation/override gets its own `<summary>` (classes, standalone methods, properties, etc.).

> **Do not invert this** (a common mistake): the `<summary>` goes on the **interface** member and on **contract properties**; the concrete class member that *implements* an interface gets **`<inheritdoc/>`**, not a fresh summary. Leaving interfaces or contract properties undocumented while summarising the implementing class is **backwards** — fix it the right way round.

```csharp
public interface IEmployeeService
{
    /// <summary>Gets the <see cref="Employee"/> for the specified <paramref name="id"/>.</summary>
    Task<Employee?> GetAsync(string id);

    /// <summary>Creates a new <see cref="Employee"/>.</summary>
    Task<DataResult<Employee>> CreateAsync(Employee employee);
}

[ScopedService<IEmployeeService>]
public class EmployeeService(IUnitOfWork unitOfWork, IEmployeeRepository repository) : IEmployeeService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork.ThrowIfNull();
    private readonly IEmployeeRepository _repository = repository.ThrowIfNull();

    /// <inheritdoc/>
    public Task<Employee?> GetAsync(string id) => _repository.GetAsync(id);

    /// <inheritdoc/>
    public Task<DataResult<Employee>> CreateAsync(Employee employee) => /* ... */;
}
```

## Private Field Naming

Private instance fields are always prefixed with `_`. No exceptions.

```csharp
private readonly IProductRepository _repository;
private readonly IUnitOfWork _unitOfWork;
private readonly ILogger<ProductService> _logger;
```

## Do Not

- Do not emit `#nullable enable` or `#nullable restore` pragma directives in hand-authored files — nullable is enabled project-wide via `<Nullable>enable</Nullable>` in `Directory.Build.props`. These pragmas are reserved for auto-generated `.g.cs` files produced by code generators.
- Do not add `using` statements to individual `.cs` files — declare all imports in `GlobalUsing.cs`.
- Do not leave `GlobalUsing.cs` unsorted after editing — re-sort the whole file alphabetically (all namespaces equally, no `System.*` grouping) rather than appending new entries at the end.
- Do not use block-scoped namespace declarations.
- Do not add braces to single-line `if` bodies.
- Do not suppress nullable warnings with `!` without a comment explaining why.
- Do not name private fields without the `_` prefix.
- Do not split method/constructor declarations (one parameter per line) or otherwise wrap statements across lines unless the line would exceed 250 characters.
- Do not use `DateTime.UtcNow` or `DateTimeOffset.UtcNow` — use `Runtime.UtcNow` (or `Runtime.UtcNow.UtcDateTime` for a `DateTime`).
- Do not use `Guid.NewGuid()` — use `Runtime.NewGuid()`.
- Do not replace a private backing field with an auto-property simply because it could be one — backing fields are a valid developer choice.
- Do not leave interface members or contract properties undocumented — each gets a `<summary>`.
- Do not invert the doc convention — summaries go on **interfaces and contract properties**; the **implementing** class member gets `<inheritdoc/>` (not a fresh summary). Summarising the concrete class while leaving the interface/contract undocumented is backwards.
