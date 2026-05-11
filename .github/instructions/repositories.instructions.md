---
applyTo: "**/Infrastructure/**/*.cs"
description: "Repository and infrastructure conventions: EFCore, ADO.NET patterns, ScopedService registration, and data access layers"
tags: ["repositories", "infrastructure", "data-access", "efcore", "ado-net"]
---

# Repository & Infrastructure Conventions

## NuGet / Project References

| Package | Key types provided |
|---|---|
| `CoreEx` | `[ScopedService<T>]`, `.ThrowIfNull()` |
| `CoreEx.EntityFrameworkCore` | `EfDb`, `EfDbSet<T>`, `.GetAsync()`, `.CreateAsync()`, `.UpdateAsync()`, `.DeleteAsync()`, `.GetWithResultAsync()`, `.CreateWithResultAsync()`, `.UpdateWithResultAsync()` |
| `CoreEx.Database.SqlServer` | SQL Server outbox publisher, ADO.NET command/parameter helpers |
| `CoreEx.Data` | `DataResult<T>`, `ItemsResult<T>`, `QueryArgsConfig`, `QueryFilterOperator`, `.Where(parsed)`, `.OrderBy(parsed)`, `.ToMappedItemsResultAsync()` |
| `CoreEx.Results` | `Result<T>`, `.GoAsync()`, `.ThenAs()`, `.ThenAsAsync()` |

## Structure

- Define the interface in the Application project under `Application/Repositories/`.
- Implement in the Infrastructure project. Register with `[ScopedService<IInterface>]` attribute.
- Inject the EF `*EfDb` (or ADO.NET database) via primary constructor and guard with `.ThrowIfNull()`.

```csharp
[ScopedService<IProductRepository>]
public class ProductRepository(ProductsEfDb ef) : IProductRepository
{
    private readonly ProductsEfDb _ef = ef.ThrowIfNull();
}
```

## Return Types

| Operation | Return type | Notes |
|---|---|---|
| Single entity lookup | `Task<TEntity?>` | Returns `null` when not found; service checks |
| Create / Update | `Task<DataResult<TEntity>>` | Includes mutation flag for event decisions |
| Delete | `Task<DataResult>` | Carries mutation flag only |
| Collection query | `Task<ItemsResult<T>>` | Includes items + optional total count |
| Domain aggregate | `Task<Result<TAgg>>` | Shopping-style — wraps `DataResult` with mapping |

## Dynamic Query Configuration

Define a `static readonly QueryArgsConfig _queryConfig` per repository for OData-style filtering and ordering. Build it once at class (not method) level:

```csharp
private static readonly QueryArgsConfig _queryConfig = QueryArgsConfig.Create()
    .WithFilter(filter => filter
        .WithDefaultModelPrefix("Product")
        .AddField<string>(nameof(ProductBase.Sku), c => c
            .WithOperators(QueryFilterOperator.EqualityOperators | QueryFilterOperator.StartsWith)
            .AsUpperCase())
        .AddField<string>(nameof(ProductBase.Text), c => c
            .WithOperators(QueryFilterOperator.StringFunctions)
            .AsUpperCase())
        .AddReferenceDataField<Category>(nameof(ProductBase.Category), "CategoryCode",
            c => c.WithModelPrefix(null)))
    .WithOrderBy(orderby => orderby
        .WithDefaultModelPrefix("Product")
        .AddField(nameof(ProductBase.Sku), c => c.WithDefault().WithAlwaysInclude())
        .AddField(nameof(ProductBase.Text))
        .AddField(nameof(ProductBase.Brand)));
```

Apply in the query method:

```csharp
public async Task<ItemsResult<ProductLite>> QueryAsync(QueryArgs? query, PagingArgs? paging)
{
    var parsed = _queryConfig.Parse(query).ThrowOnError();

    var products = _ef.Products.Model.Query();

    return await products
        .Where(parsed)
        .OrderBy(parsed)
        .ToMappedItemsResultAsync(x => new ProductLite
        {
            Id = x.Product.Id,
            Sku = x.Product.Sku,
            Text = x.Product.Text,
        }, paging);
}
```

## EF Delegate Shortcuts

Use the built-in EF delegate methods for single-entity CRUD — do not write raw `DbContext` queries for simple operations:

```csharp
public Task<Product?> GetAsync(string id) => _ef.Products.GetAsync(id);
public Task<DataResult<Product>> CreateAsync(Product product) => _ef.Products.CreateAsync(product);
public Task<DataResult<Product>> UpdateAsync(Product product) => _ef.Products.UpdateAsync(product);
public Task<DataResult> DeleteAsync(string id) => _ef.Products.DeleteAsync(id);
```

## Domain-Aggregate Repositories (Result Pattern)

For Shopping-style aggregate repositories, chain `Result` operations using `.GoAsync` / `.ThenAs` / `.ThenAsAsync`. Map between persistence models and domain aggregates using explicit mappers:

```csharp
public Task<Result<Domain.Basket>> GetAsync(string id) => Result
    .GoAsync(() => _ef.Baskets.GetWithResultAsync(id))
    .ThenAs(model => BasketMapper.Map(model));

public Task<Result<Domain.Basket>> CreateAsync(Domain.Basket basket) => Result
    .Go(() =>
    {
        var model = new Persistence.Basket();
        BasketIntoMapper.MapInto(basket, model);
        return SynchronizeItems(basket, model);
    })
    .ThenAsAsync(model => _ef.Baskets.CreateWithResultAsync(model))
    .ThenAs(b => BasketMapper.Map(b));
```

## Explicit Mapping — No AutoMapper

Write explicit mapper classes or static methods. Do not introduce AutoMapper:

```csharp
public static class BasketMapper
{
    public static Domain.Basket Map(Persistence.Basket model)
    {
        // explicit property assignment
    }
}

public static class BasketIntoMapper
{
    public static void MapInto(Domain.Basket src, Persistence.Basket dest)
    {
        dest.Id = src.Id;
        dest.CustomerId = src.CustomerId;
        // ...
    }
}
```

## ConfigureAwait

Always call `.ConfigureAwait(false)` on every awaited call inside repository methods.

## HTTP Client Adapters

Infrastructure adapters that wrap downstream APIs should use a typed `HttpClient` registered under a named key. The adapter interface lives in Application; the implementation lives in Infrastructure:

```csharp
[ScopedService<IProductAdapter>]
public class ProductAdapter(ProductsHttpClient httpClient) : IProductAdapter
{
    private readonly ProductsHttpClient _httpClient = httpClient.ThrowIfNull();

    public Task<Product?> GetAsync(string id)
        => _httpClient.GetAsync<Product>($"api/products/{id}");

    public Task<MovementCollection> ReserveInventoryAsync(MovementRequest request)
        => _httpClient.PostAsync<MovementRequest, MovementCollection>("api/inventory/reserve", request);
}
```
