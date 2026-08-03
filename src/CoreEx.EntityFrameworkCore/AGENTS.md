# CoreEx.EntityFrameworkCore — AI Usage Guide

Wraps EF Core's `DbContext` with the CoreEx data conventions: `ETag`/concurrency checking, multi-tenancy, logical delete, change-log stamping, paging, and `QueryArgsConfig` dynamic filter/orderby.

## Registration

```csharp
// Program.cs
builder.Services
    .AddDbContext<MyDbContext>(o => o.UseNpgsql(connectionString))
    .AddEfDb<MyEfDb>();               // registers EfDb<MyDbContext> and bridges IDatabase
```

## EfDb — Entry Point

Inject `EfDb<TDbContext>` (or your `IEfDb<TDbContext>`) into repositories. Access typed CRUD via `Model<TModel>()` — this is for the case where the domain/contract type **is** the EF persistence model type (no separate mapper needed); `GetAsync`/`CreateAsync`/`UpdateAsync`/`DeleteAsync` take no mapper parameter:

```csharp
[ScopedService<IProductRepository>]
public class ProductRepository(EfDb<MyDbContext> efDb) : IProductRepository
{
    private readonly EfDbModel<ProductModel> _model = efDb.Model<ProductModel>();

    public Task<ProductModel?> GetAsync(Guid id, CancellationToken ct = default) => _model.GetAsync(id, ct);

    public Task<DataResult<ProductModel>> CreateAsync(ProductModel product, CancellationToken ct = default) => _model.CreateAsync(product, ct);
}
```

## Mapped Model (Separate EF Model Type)

Use `EfDbMappedModel<TValue, TModel, TMapper>` when the domain/contract type (`TValue`) differs from the EF persistence model type (`TModel`). Construct it once — typically as a property on your `EfDb<TDbContext>` subclass — via `Model<TModel>().ToMappedModel<TValue, TMapper>(mapper)`. `TMapper` must be an `IBiDirectionMapper<TValue, TModel>` (see [`CoreEx.Mapping`](../CoreEx/Mapping/README.md)); once mapped, `GetAsync`/`CreateAsync`/`UpdateAsync`/`DeleteAsync` still take **no mapper parameter** — the mapper is already bound via the generic type:

```csharp
public sealed class MyEfDb(MyDbContext dbContext) : EfDb<MyDbContext>(dbContext, _options)
{
    private static readonly EfDbOptions _options = new EfDbOptions().WithModel<ProductModel>(m => m.WithLogicalDeleteFilter());

    public EfDbMappedModel<Product, ProductModel, ProductMapper> Products => Model<ProductModel>().ToMappedModel<Product, ProductMapper>(ProductMapper.Default);
}

public class ProductRepository(MyEfDb ef) : IProductRepository
{
    public Task<Product?> GetAsync(Guid id, CancellationToken ct = default) => ef.Products.GetAsync(id, ct);

    public Task<DataResult<Product>> CreateAsync(Product product, CancellationToken ct = default) => ef.Products.CreateAsync(product, ct);
}
```

## Dynamic Query with Paging

Get the underlying `IQueryable<TModel>` via `Model.Query()`, apply the parsed `QueryArgsConfig<TSelf>` filter/order (see [`CoreEx.Data`](../CoreEx.Data/AGENTS.md)), then materialize with `ToMappedItemsResultAsync`.

> **Always pass `cancellationToken` as a named argument.** `ToMappedItemsResultAsync`'s signature is `(mapper, paging = null, autoCount = true, cancellationToken = default)` — `autoCount` (`bool`) sits *before* `cancellationToken`. A bare positional `CancellationToken` argument in the third slot lands on `autoCount` and fails to compile (`CS1503`). Use `cancellationToken: cancellationToken` explicitly, every time.

```csharp
public async Task<ItemsResult<ProductLite>> QueryAsync(QueryArgs? query, PagingArgs? paging, CancellationToken cancellationToken = default)
{
    var parsed = ProductQueryArgsConfig.Default.Parse(query).ThrowOnError();

    return await ef.Products.Model.Query()
        .Where(parsed)
        .OrderBy(parsed)
        .ToMappedItemsResultAsync(m => ProductMapper.From.Map(m), paging, cancellationToken: cancellationToken)
        .ConfigureAwait(false);
}
```

## ValueConverter Bridge

Use `ValueConverterBridge<TModel, TProvider>` in `OnModelCreating` to reuse CoreEx `IConverter<T, U>` instances as EF value converters.

```csharp
protected override void OnModelCreating(ModelBuilder builder)
{
    builder.Entity<ProductModel>()
           .Property(p => p.Status)
           .HasConversion(new ValueConverterBridge<StatusEnum, string>(new StatusEnumConverter()));
}
```

## Do Not

- Do not inject `DbContext` directly into application services — use the repository behind an interface.
- Do not use EF `DbContext.Add`/`Update`/`Remove` directly — use `EfDbModel` methods so CoreEx cross-cutting (ETag, change-log, logical delete) runs correctly.
- Do not use AutoMapper — use explicit `IBiDirectionMapper<TValue, TModel>` implementations.

## Further Reading

- [README](./README.md) — full `EfDb`, `EfDbModel`, `EfDbArgs`, `EfDbOptions`, and extension-method API reference.
- [CoreEx.Data](../CoreEx.Data/README.md) — `IUnitOfWork`, `QueryArgsConfig`, and `DataResult`.
- [CoreEx.Database](../CoreEx.Database/README.md) — `IDatabase` bridged into `EfDb<TDbContext>` for transaction sharing.
- [Infrastructure layer](../../samples/docs/infrastructure-layer.md) — EF Core repository implementation, `IBiDirectionMapper` usage, `DbContext` configuration, and dynamic query wiring in real sample code.
- [Patterns](../../samples/docs/patterns.md) — repository patterns, explicit mapping conventions, and paged query construction.
