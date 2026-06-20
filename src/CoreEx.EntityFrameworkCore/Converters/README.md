# CoreEx.EntityFrameworkCore.Converters

> Provides EF Core `ValueConverter` bridges that allow CoreEx `IConverter<TModel, TProvider>` implementations to be used directly in EF model configuration, including a built-in `JsonElement` ↔ `string` converter.

## Overview

EF Core's `ValueConverter<TModel, TProvider>` type is the standard extension point for translating .NET property values to and from database column values. The `Converters` namespace wires CoreEx's own `IConverter<TModel, TProvider>` abstraction into that EF pipeline so that any CoreEx converter can be registered in `OnModelCreating` without duplication.

`ValueConverterBridge<TModel, TProvider>` inherits directly from EF Core's `ValueConverter<TModel, TProvider>` and delegates its `ConvertToProvider`/`ConvertFromProvider` expressions to the supplied CoreEx converter. The static `ValueConverterBridge.Create<TModel, TProvider>(converter)` factory removes the need to name the generic type explicitly at the call site.

`JsonElementStringEfConverter` is a ready-made specialization that serializes `JsonElement?` property values to `string?` columns using `JsonElementStringConverter.Default`, covering the common pattern of storing JSON fragments in a single text column.

## Key types

| Type | Description |
|------|-------------|
| **[`ValueConverterBridge<TModel, TProvider>`](./ValueConverterBridgeT2.cs)** | EF Core `ValueConverter<TModel, TProvider>` that delegates to a CoreEx `IConverter<TModel, TProvider>`; use `ValueConverterBridge.Create(converter)` for concise construction. |
| **[`ValueConverterBridge`](./ValueConverterBridge.cs)** | Static factory with two `Create<TModel, TProvider>` overloads — one accepting a typed `IConverter<TModel, TProvider>` and one accepting the non-generic `IConverter` base (validated at runtime). |
| **[`JsonElementStringEfConverter`](./JsonElementStringEfConverter.cs)** | Pre-built `ValueConverterBridge<JsonElement?, string?>` using `JsonElementStringConverter.Default`; exposes a `Default` singleton for direct use in model configuration. |

## Usage

Register a CoreEx converter in `OnModelCreating`:

```csharp
modelBuilder.Entity<Order>()
    .Property(o => o.Metadata)
    .HasConversion(JsonElementStringEfConverter.Default);
```

Or wire any custom CoreEx converter:

```csharp
modelBuilder.Entity<Product>()
    .Property(p => p.Status)
    .HasConversion(ValueConverterBridge.Create(MyStatusConverter.Default));
```

## Related namespaces

- **[`CoreEx.EntityFrameworkCore`](../README.md)** - Parent package; `ValueConverterBridge` types are applied during EF `OnModelCreating`.
- **[`CoreEx.Mapping.Converters`](../../CoreEx/Mapping/README.md)** - `IConverter<TModel, TProvider>` is the CoreEx converter contract wrapped by `ValueConverterBridge`.
