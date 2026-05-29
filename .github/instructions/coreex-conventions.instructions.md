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
// GlobalUsing.cs — all usings for the project declared here
global using System;
global using System.Collections.Generic;
global using System.Threading;
global using System.Threading.Tasks;
global using Microsoft.Extensions.Logging;
global using CoreEx;
global using Contoso.Products.Application.Interfaces;
```

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

## Private Field Naming

Private instance fields are always prefixed with `_`. No exceptions.

```csharp
private readonly IProductRepository _repository;
private readonly IUnitOfWork _unitOfWork;
private readonly ILogger<ProductService> _logger;
```

## Do Not

- Do not add `using` statements to individual `.cs` files — declare all imports in `GlobalUsing.cs`.
- Do not use block-scoped namespace declarations.
- Do not add braces to single-line `if` bodies.
- Do not suppress nullable warnings with `!` without a comment explaining why.
- Do not name private fields without the `_` prefix.
- Do not replace a private backing field with an auto-property simply because it could be one — backing fields are a valid developer choice.
