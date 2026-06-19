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

Inject `EfDb<TDbContext>` (or your `IEfDb<TDbContext>`) into repositories. Access typed CRUD via `Model<TModel>()`.

```csharp
[ScopedService<IProductRepository>]
public class ProductRepository(EfDb<MyDbContext> efDb) : IProductRepository
{
    private readonly EfDbModel<ProductModel> _model = efDb.Model<ProductModel>();

    public Task<Product?> GetAsync(Guid id, CancellationToken ct = default) =>
        _model.GetAsync(new EfDbArgs(OperationType.Get), id, ProductMapper.Default.MapToEntity, ct);

    public Task<Product> CreateAsync(Product product, CancellationToken ct = default) =>
        _model.CreateAsync(new EfDbArgs(OperationType.Create), product, ProductMapper.Default, ct);
}
```

## Mapped Model (Separate EF Model Type)

Use `EfDbMappedModel<TValue, TModel, TMapper>` when the domain entity type differs from the EF persistence model type.

```csharp
private readonly EfDbMappedModel<Order, OrderModel, OrderMapper> _model =
    efDb.Model<Order, OrderModel, OrderMapper>();
```

## Dynamic Query with Paging

Use `Query(args)` for paged, filtered list endpoints. Combine with `EfDbExtensions.ToItemsResultAsync`.

```csharp
private static readonly QueryArgsConfig _queryConfig = QueryArgsConfig.Create()
    .WithFilter(f => f.AddField<string>(nameof(ProductModel.Status)))
    .WithOrderBy(o => o.AddField(nameof(ProductModel.Name)).WithDefault("Name"));

public Task<ItemsResult<Product>> GetAllAsync(QueryArgs? args, PagingArgs? paging,
    CancellationToken ct = default)
    => _model.Query(new EfDbArgs(args ?? new QueryArgs(), _queryConfig, paging))
             .ToMappedItemsResultAsync(ProductMapper.Default.MapToEntity, ct);
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
