# CoreEx.Database — AI Usage Guide

Provides the `IDatabase` ADO.NET abstraction, `DatabaseCommand` fluent builder, explicit column mapping, and the transactional outbox relay base infrastructure.

## IDatabase and DatabaseCommand

Use `DatabaseCommand` for all ADO.NET queries. Never write raw `DbCommand` code. Prefer `EfDb` for ordinary CRUD
and LINQ queries; put application-owned static SQL in an embedded `.sql` / `.pgsql` resource, regardless of length,
and load it once with `SqlStatement.FromResource<TResource>`.

```csharp
private static readonly SqlStatement _getOrder =
    SqlStatement.FromResource<OrderRepository>("GetOrderById.sql");

var order = await _db.Statement(_getOrder)
    .Param("OrderId", id)
    .SelectSingleAsync(OrderMapper.Default)
    .ConfigureAwait(false);

// Stored procedure
var orders = await _db.Statement(SqlStatement.StoredProcedure("spGetOrders"))
    .SelectQueryAsync(OrderMapper.Default)
    .ConfigureAwait(false);
```

Explicitly embed SQL files in the Infrastructure project's `.csproj` (for example,
`<EmbeddedResource Include="Repositories\Sql\*.sql" />`). Bind values with `.Param(...)`; never interpolate data
values into SQL. Resource lookup matches the end of the fully qualified name, so keep resource filenames unique
within the assembly. Use `SqlStatement.FromText(...)` only when SQL must genuinely be generated at runtime (for
example, when an identifier is dynamic); bind data values as parameters and validate or safely quote dynamic
identifiers. Schema changes belong in the `*.Database` project's DbEx migrations, not in runtime SQL resources.

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

## Outbox partition rollout

Writers and relays share `CoreEx:Host:Outbox:PartitionSize`. Preserve the effective size when migrating from the removed relay-only key. Changing the size does not remap persisted outbox rows; shrinking the relay range can leave older partition IDs unprocessed. Pause affected producers and drain the outbox with the old configuration before changing the mapping, then resume with consistent writer/relay settings. Keyless events now map deterministically using `$none`, so old/new placement must not be assumed to preserve ordering.

Service Bus session mapping is a separate boundary. Coordinate destination policies across all producers, including direct broker publishers; drain affected broker work and in-flight handlers before switching mappings where ordering matters.

## Operations store base

`DatabaseOperationStoreBase<TDatabase>` (`CoreEx.Database.Operations`) is the provider-neutral relational `IOperationStore`; its primary key is `OperationStoreBase.ComputeKeyHash`, the `CompositeKey.ToCanonicalHash` of the complete identity. Identifiers are delimited via `IDatabase.QuoteIdentifier` (ANSI double quotes by default; SQL Server overrides with brackets), rows are read through `DatabaseRecord`, and the ETag uses the provider `RowVersionConverter`. Providers supply the column names, embedded statements and the operation database (`CreateOperationDatabase`/`OperationDatabaseFactory`) used for independent operations. Mutations join an existing `IDatabase.CurrentTransaction` or create and own an explicit `DbTransaction` in the provider-neutral base. Claims, renewals, releases, cancellation and cleanup use a separate connection; do not override them to share the business transaction.

Transaction participation is managed in C# using explicit `DbTransaction` via `IDatabase.UseTransaction` or `IUnitOfWork.TransactionAsync`; SQL resources do not begin, commit or roll back transactions. Mutations without an existing transaction create one in the base, while mutations with an existing transaction join it. Independent operations use a separate connection and transaction without the business transaction. Do not introduce `System.Transactions` or `TransactionScope`; ambient enlistment management and distributed transactions are not supported.

## Do Not

- Do not write raw `DbCommand` or `SqlCommand` code — use `DatabaseCommand`.
- Do not use reflection-based mappers — extend `DatabaseMapper<T>` with explicit column reads.
- Do not inline application-owned static SQL in C# — keep it in an embedded resource and load it with `SqlStatement.FromResource<TResource>`. Reserve `SqlStatement.FromText(...)` for genuinely dynamic SQL.

## Further Reading

- [README](./README.md) — full `IDatabase`, `DatabaseCommand`, and outbox relay API reference.
- [CoreEx.Database.SqlServer](../CoreEx.Database.SqlServer/README.md) / [CoreEx.Database.Postgres](../CoreEx.Database.Postgres/README.md) — concrete implementations.
- [Infrastructure layer](../../samples/docs/infrastructure-layer.md) — repository implementation, mapper patterns, and outbox table wiring in real sample code.
- [Tooling](../../samples/docs/tooling.md) — how `*.Database` projects (DbEx) generate outbox infrastructure, schema, and seed scripts.
