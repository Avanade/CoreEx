# CoreEx.Database — AI Usage Guide

Provides the `IDatabase` ADO.NET abstraction, `DatabaseCommand` fluent builder, explicit column mapping, and the transactional outbox relay base infrastructure.

## IDatabase and DatabaseCommand

Use `DatabaseCommand` for all ADO.NET queries. Never write raw `DbCommand` code.

```csharp
// Single row
var order = await _db.SqlStatement("SELECT * FROM orders WHERE id = @id")
    .Param("@id", id)
    .SelectSingleAsync(OrderMapper.Default)
    .ConfigureAwait(false);

// Collection
var orders = await _db.StoredProcedure("spGetOrders")
    .SelectQueryAsync(OrderMapper.Default)
    .ConfigureAwait(false);
```

## Explicit Mappers

Extend `DatabaseMapper<T>` for each entity. Hand-write column assignments — do not use reflection or AutoMapper.

```csharp
public class OrderMapper : DatabaseMapper<Order>
{
    public static readonly OrderMapper Default = new();

    protected override Order OnMapFromDb(DatabaseRecord r, OperationType operationType)
    {
        var order = new Order
        {
            Id   = r.GetValue<Guid>("order_id"),
            Code = r.GetValue<string>("code")!,
        };
        MapStandardFromDb(r, order);   // reads RowVersion→ETag, change-log columns
        return order;
    }

    protected override void OnMapToDb(Order value, DatabaseParameterCollection p, OperationType operationType)
    {
        p.AddParameter("@code", value.Code);
        MapStandardToDb(value, p, operationType);
    }
}
```

## Error Number Convention

Stored procedures raise user error numbers 56001–56007/56010 to signal domain exceptions — no application-layer switch statements required.

| Error number | CoreEx exception |
|---|---|
| 56001 | `ValidationException` |
| 56002 | `BusinessException` |
| 56004 | `ConcurrencyException` |
| 56005 | `NotFoundException` |
| 56006 | `ConflictException` |
| 56007 | `DuplicateException` |

## Do Not

- Do not write raw `DbCommand` or `SqlCommand` code — use `DatabaseCommand`.
- Do not use reflection-based mappers — extend `DatabaseMapper<T>` with explicit column reads.

## Further Reading

- [README](./README.md) — full `IDatabase`, `DatabaseCommand`, and outbox relay API reference.
- [CoreEx.Database.SqlServer](../CoreEx.Database.SqlServer/README.md) / [CoreEx.Database.Postgres](../CoreEx.Database.Postgres/README.md) — concrete implementations.
- [Infrastructure layer](../../samples/docs/infrastructure-layer.md) — repository implementation, mapper patterns, and outbox table wiring in real sample code.
- [Tooling](../../samples/docs/tooling.md) — how `*.Database` projects (DbEx) generate outbox infrastructure, schema, and seed scripts.
