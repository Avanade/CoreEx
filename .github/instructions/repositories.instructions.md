---
applyTo: "**/Infrastructure/**/*.cs"
description: "Repository and infrastructure conventions: EFCore, mapping, typed HTTP clients, adapter implementations, and data-access patterns"
tags: ["repositories", "infrastructure", "data-access", "efcore", "mapping", "adapters"]
---

# Repository & Infrastructure Conventions

## NuGet / Project References

| Package | Key types provided |
|---|---|
| `CoreEx` | `[ScopedService<T>]`, `.ThrowIfNull()`, `EventData` |
| `CoreEx.EntityFrameworkCore` | `EfDb<TContext>`, `EfDbModel<T>`, `EfDbMappedModel<TContract,TModel,TMapper>`, `EfDbOptions`, `.GetAsync()`, `.CreateAsync()`, `.UpdateAsync()`, `.DeleteAsync()`, `.GetWithResultAsync()`, `.CreateWithResultAsync()`, `.UpdateWithResultAsync()`, `.Query()` |
| `CoreEx.Database.SqlServer` | SQL Server outbox publisher, ADO.NET helpers — **Shopping only** |
| `CoreEx.Database.Postgres` | PostgreSQL outbox publisher, ADO.NET helpers — **Products only** |
| `CoreEx.Data` | `DataResult<T>`, `ItemsResult<T>`, `QueryArgsConfig`, `QueryFilterOperator`, `.Where(parsed)`, `.OrderBy(parsed)`, `.ToMappedItemsResultAsync()` |
| `CoreEx.Results` | `Result<T>`, `.GoAsync()`, `.ThenAs()`, `.ThenAsAsync()` |

> **Polyglot data**: Products uses PostgreSQL (`CoreEx.Database.Postgres` + `Npgsql.EntityFrameworkCore.PostgreSQL`); Shopping uses SQL Server (`CoreEx.Database.SqlServer` + `Microsoft.EntityFrameworkCore.SqlServer`). Layers above Infrastructure are database-agnostic.

## Structure

- Define the repository interface in the Application project under `Application/Repositories/`.
- Implement in the Infrastructure project under `Repositories/`. Register with `[ScopedService<IInterface>]`.
- Inject the domain's `*EfDb` class via primary constructor and guard with `.ThrowIfNull()`.

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
| Collection query | `Task<ItemsResult<T>>` | Items + optional total count |
| Domain aggregate (Shopping) | `Task<Result<TAgg>>` | Wraps EF result with domain-model mapping |

## EfDb — Unit-of-Work Facade

The `*EfDb` sub-class (`ProductsEfDb` / `ShoppingEfDb`) is the **unit-of-work facade** over the `DbContext`. It declares a typed property per entity model and configures global options (e.g., logical-delete filters). The `DbContext` delegates connection and transaction management to CoreEx's `IDatabase`, so the same connection is shared across a request.

```csharp
public sealed class ProductsEfDb(ProductsDbContext dbContext) : EfDb<ProductsDbContext>(dbContext, _options)
{
    private static readonly EfDbOptions _options = new EfDbOptions()
        .WithModel<Persistence.Product>(m => m.WithLogicalDeleteFilter());

    public EfDbMappedModel<Contracts.Product, Persistence.Product, ProductMapper> Products
        => Model<Persistence.Product>().ToMappedModel<Contracts.Product, ProductMapper>(ProductMapper.Default);

    public EfDbModel<Persistence.SubCategory> SubCategories => Model<Persistence.SubCategory>();
    public EfDbModel<Persistence.Inventory> Inventory => Model<Persistence.Inventory>();
    // ...
}
```

## EF Delegate Shortcuts

Use the built-in EF delegate methods for single-entity CRUD — do not write raw `DbContext` queries for simple operations:

```csharp
public Task<Contracts.Product?> GetAsync(string id) => _ef.Products.GetAsync(id);
public Task<DataResult<Contracts.Product>> CreateAsync(Contracts.Product product) => _ef.Products.CreateAsync(product);
public Task<DataResult<Contracts.Product>> UpdateAsync(Contracts.Product product) => _ef.Products.UpdateAsync(product);
public Task<DataResult> DeleteAsync(string id) => _ef.Products.DeleteAsync(id);
```

## Dynamic Query Configuration

Define a `static readonly QueryArgsConfig _queryConfig` once at class level for OData-style filtering and ordering:

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

In the query method, compose the full base query first (including any required joins), then apply `Where(parsed)` and `OrderBy(parsed)`:

```csharp
public async Task<ItemsResult<Contracts.ProductLite>> QueryAsync(QueryArgs? query, PagingArgs? paging)
{
    var parsed = _queryConfig.Parse(query).ThrowOnError();

    // Compose the base query with required joins before applying parsed filters.
    var q =
        from p in _ef.Products.Model.Query()
        join sc in _ef.SubCategories.Query() on p.SubCategoryCode equals sc.Code into scg
        from sc in scg.DefaultIfEmpty()
        join i in _ef.Inventory.Query() on p.Id equals i.Id into ig
        from i in ig.DefaultIfEmpty()
        select new { Product = p, sc.CategoryCode, QtyOnHand = i == null ? 0 : i.QtyOnHand };

    return await q
        .Where(parsed)
        .OrderBy(parsed)
        .ToMappedItemsResultAsync(x => new Contracts.ProductLite
        {
            Id = x.Product.Id,
            Sku = x.Product.Sku,
            CategoryCode = x.CategoryCode,
            QtyOnHand = x.QtyOnHand
        }, paging);
}
```

Expose the query schema for the `$query` endpoint via `ToJsonSchema()`:

```csharp
public Task<JsonElement> QuerySchemaAsync() => Task.FromResult(_queryConfig.ToJsonSchema());
```

## Domain-Aggregate Repositories (Result Pattern)

For Shopping-style aggregate repositories, chain `Result` operations using `.GoAsync` / `.ThenAs` / `.ThenAsAsync`. Map between persistence models and domain aggregates using the explicit infrastructure mappers:

```csharp
public Task<Result<Domain.Basket>> GetAsync(string id) => Result
    .GoAsync(() => _ef.Baskets.GetWithResultAsync(id))
    .ThenAs(model => BasketMapper.Map(model));

public Task<Result<Domain.Basket>> CreateAsync(Domain.Basket basket) => Result
    .Go(() =>
    {
        var model = new Persistence.Basket();
        BasketIntoMapper.MapInto(basket, model);
        return model;
    })
    .ThenAsAsync(model => _ef.Baskets.CreateWithResultAsync(model))
    .ThenAs(b => BasketMapper.Map(b));
```

## Mapping

The `Mapping/` sub-folder contains **bidirectional mappers** between Contract types and Persistence model types. Extend `BiDirectionMapper<TFrom, TTo, TSelf>` — do not use AutoMapper or reflection-based conventions:

```csharp
// Infrastructure/Mapping/ProductMapper.cs
public class ProductMapper : BiDirectionMapper<Contracts.Product, Persistence.Product, ProductMapper>
{
    protected override Persistence.Product OnMap(Contracts.Product source) => new()
    {
        Id = source.Id!,
        Sku = source.Sku!,
        SubCategoryCode = source.SubCategory?.Code!,
        Price = source.Price
    };

    protected override Contracts.Product OnMap(Persistence.Product source) => new()
    {
        Id = source.Id,
        Sku = source.Sku,
        SubCategoryCode = source.SubCategoryCode,
        Price = source.Price
    };
}
```

Infrastructure-level mapping is always Contract ↔ Persistence. Application-level mapping (Domain aggregate ↔ Contract) lives in `Application/Mapping/` and uses `Mapper<TSource, TDest, TSelf>`. Do not conflate the two.

## External Clients and Adapter Implementations

When a domain calls another domain's API over HTTP, split the concern across two focused classes:

- **Typed HTTP client** (`Clients/`) — thin wrapper around `HttpClient` handling serialization and response mapping to `Result` types. One class per external service.
- **Adapter implementation** (`Adapters/`) — implements the Application-layer `IXxxAdapter` interface. May combine the typed client with local EF reads (e.g., reading from the local event-replicated store) and event publication.

```csharp
// Infrastructure/Clients/ProductsHttpClient.cs
public class ProductsHttpClient(HttpClient httpClient)
{
    private readonly HttpClient _httpClient = httpClient.ThrowIfNull();

    public async Task<Result> CreateReservationAsync(MovementRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/inventory/reserve", request, JsonDefaults.SerializerOptions);
        return await response.ToResultAsync();
    }
}

// Infrastructure/Adapters/ProductAdapter.cs
[ScopedService<IProductAdapter>]
public class ProductAdapter(ShoppingEfDb ef, ProductsHttpClient client, IEventPublisher eventPublisher) : IProductAdapter
{
    // GetAsync — reads from the local event-replicated EF store (eventually consistent).
    public Task<Result<Product>> GetAsync(string id) => Result
        .GoAsync(() => _ef.Products.GetWithResultAsync(id))
        .ThenAs(p => ProductMapper.From.Map(p));

    // ReserveInventoryAsync — calls the Products API in real time (synchronous integration).
    public async Task<Result> ReserveInventoryAsync(Domain.Basket basket)
        => await _client.CreateReservationAsync(BuildRequest(basket)).ConfigureAwait(false);
}
```

Keep the typed HTTP client and the adapter orchestration in separate, independently testable classes.

## Generated Code

Persistence model classes (`Persistence/*.g.cs`) and the EF `DbContext` partial (`Repositories/*DbContext.g.cs`) are generated by the domain's `*.Database` project. Never create or edit these files directly.

| File pattern | Generator | Change instead |
|---|---|---|
| `Persistence/*.g.cs` | `*.Database` project (DbEx) | DbEx YAML config or SQL migration scripts |
| `*DbContext.g.cs` | `*.Database` project (DbEx) | DbEx YAML config |

## ConfigureAwait

Always call `.ConfigureAwait(false)` on every `await` inside repository and adapter methods.

## Do Not

- Do not reference the Infrastructure project from the Application layer — Infrastructure implements Application interfaces, not the other way around.
- Do not use AutoMapper or reflection-based mappers — use `BiDirectionMapper<TFrom, TTo, TSelf>` with explicit `OnMap` overrides.
- Do not call `HttpClient` directly in adapter methods — use the typed HTTP client class in `Clients/`.
- Do not conflate Application-level mapping (aggregate ↔ contract) with Infrastructure-level mapping (contract ↔ persistence model).
- Do not write raw `DbContext` queries for standard CRUD — use the `EfDb` delegate methods.
- Do not edit `*.g.cs` persistence or DbContext files directly — regenerate via the `*.Database` tooling project.

## Further Reading

- [`samples/docs/infrastructure-layer.md`](../../../samples/docs/infrastructure-layer.md) — full walkthrough of persistence models, repositories, mapping, and external client/adapter patterns.
- [`samples/docs/patterns.md`](../../../samples/docs/patterns.md) — Adapter, Repository, Mapper, Persistence, and HTTP Client pattern entries with cross-links.
- [`samples/docs/layers.md`](../../../samples/docs/layers.md) — layer dependency rules and the role of the Infrastructure layer.
- [`samples/docs/tooling.md`](../../../samples/docs/tooling.md) — `*.Database` project: schema, persistence-model generation, and outbox provisioning.
- [`src/CoreEx.EntityFrameworkCore/README.md`](../../../src/CoreEx.EntityFrameworkCore/README.md) — `EfDb`, `EfDbModel`, `EfDbMappedModel`, and `EfDbOptions`.
