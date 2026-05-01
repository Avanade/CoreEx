---
applyTo: "**/*.Database/**"
---

# Database Project Conventions

## NuGet / Project References

| Package | Key types provided |
|---|---|
| `DbEx.SqlServer` | `SqlServerMigrationConsole`, migration host runner, YAML data parsing |
| `CoreEx.Database` | `SqlStatement` helpers, outbox integration support |

## Project Shape

Each domain database project must contain:

- `Program.cs` with `ConfigureMigrationArgs`.
- `dbex.yaml` listing reference and transactional tables.
- `Migrations/` ordered SQL scripts.
- `Schema/Stored Procedures/` outbox relay procedures.
- `Data/ref-data.yaml` seed reference data.

## Program.cs Pattern

- Use `SqlServerMigrationConsole.Create<Program>(defaultConnectionString)`.
- Configure `.IncludeExtendedSchemaScripts()`.
- Add default ref-data columns:
  - `SortOrder = 0`.
  - `Scale = 0`.
- Set `DataResetFilterPredicate` to the domain schema only.

```csharp
args.DataResetFilterPredicate = ts => ts.Schema == "{Domain}";
```

## Migration Naming

Use timestamp-prefixed, ordered scripts:

- `20260101-000001-create-{domain}-schema.sql`.
- `20260101-000101-create-{domain}-<refdata>.sql`.
- `20260101-000201-create-{domain}-<aggregate>.sql`.
- `20260101-000202-create-{domain}-<child>.sql`.
- `20260101-000301-create-{domain}-outbox-tables.sql`.

## SQL Conventions

- Wrap each migration in `BEGIN TRANSACTION ... COMMIT TRANSACTION`.
- Use explicit schema-qualified names (`[{Domain}].[Table]`).
- Include `CreatedBy`, `CreatedOn`, `UpdatedBy`, `UpdatedOn` columns on aggregate and reference-data tables.
- Use `TIMESTAMP`/`ROWVERSION` for concurrency columns mapped to `ETag`.
- Add FK constraints for child tables.

## Outbox Requirements

Create both tables:

- `[{Domain}].[Outbox]`.
- `[{Domain}].[OutboxLease]`.

Create all required procedures:

- `spOutboxEnqueue.g.sql`.
- `spOutboxLeaseAcquire.g.sql`.
- `spOutboxLeaseRelease.g.sql`.
- `spOutboxBatchClaim.g.sql`.
- `spOutboxBatchComplete.g.sql`.
- `spOutboxBatchCancel.g.sql`.

Procedure naming and schema must match the domain schema and outbox publisher configuration in Infrastructure.

## Data Seed Conventions

- Keep reference data in `Data/ref-data.yaml`.
- Root node should be the schema/domain name.
- Use concise status/code values with clear text.
- Include required reference data used by validators.

Example:

```yaml
Orders:
  - $^OrderStatus:
    - P: Pending
    - C: Confirmed
    - X: Cancelled
```
