# CoreEx.Data

> Provides lightweight data-access primitives for paging, querying, partitioning, multi-tenancy, logical deletion, type discrimination, and model preparation — shared across all CoreEx data-layer packages.

## Overview

`CoreEx.Data` defines the foundational contracts and utilities that database and ORM packages (`CoreEx.Database`, `CoreEx.EntityFrameworkCore`, etc.) depend on. It extends the entity contract interfaces from `CoreEx.Entities` with data-specific concerns: `IPrimaryKey` for key-based access, `IPartitionKey` for partitioned storage, `ITenantId` for multi-tenancy, `ILogicallyDeleted` for soft-delete, and `ITypeDiscriminator` for polymorphic persistence.

`PagingArgs` and `PagingResult` are the universal paging contract across all query-returning operations, carrying `Skip`/`Take` and optionally the total count. `ItemsResult<T>` bundles a collection of results with its associated `PagingResult` for transport from repository to API layer. `QueryArgs` provides a lightweight OData-like `$filter` and `$orderby` for explicitly supported dynamic querying — not a full OData stack, but sufficient for controlled dynamic search scenarios.

The `Model` static utility class automates the most common model preparation tasks on create and update: setting tenant ID, populating type discriminator, and stamping the audit change-log from the ambient `ExecutionContext`.

## Key capabilities

- 📄 **Paging**: `PagingArgs` (skip/take with configurable defaults) and `PagingResult` (total count, was-paged flag) provide a uniform paging contract across all list operations.
- 📦 **Paged results**: `ItemsResult<T>` pairs a typed item collection with `PagingResult`, conveying both data and paging metadata from repository to API without extra wrapper types.
- 🔍 **Dynamic querying**: `QueryArgs` carries OData-like `$filter`, `$orderby`, and include/exclude field lists for explicitly supported, limited dynamic search.
- 🏢 **Multi-tenancy**: `ITenantId` / `IReadOnlyTenantId` mark entities that carry a tenant identifier; `Model.PrepareTenantId` stamps the current tenant from `ExecutionContext`.
- 🗂️ **Partitioning**: `IPartitionKey` / `IReadOnlyPartitionKey` and `PartitionKey` utility compute deterministic hash-based partition IDs for partitioned storage systems; `PartitionPicker` selects a partition from a pool.
- 🗑️ **Logical deletion**: `ILogicallyDeleted` / `IReadOnlyLogicallyDeleted` mark entities that are soft-deleted rather than physically removed.
- 🔠 **Type discrimination**: `ITypeDiscriminator` / `IReadOnlyTypeDiscriminator` carry a discriminator value for polymorphic entity persistence.
- 🛠️ **Model preparation**: `Model` static utility stamps tenant ID, type discriminator, and audit change-log fields for create and update operations from the ambient `ExecutionContext`.

## Key types

| Type | Description |
|------|-------------|
| **[`ItemsResult<T>`](./ItemsResultT.cs)** | Sealed record pairing a typed item collection with a `PagingResult`; the standard return type for paginated list operations. |
| **[`Model`](./Model.cs)** | Static utility preparing models for create/update by stamping tenant ID, type discriminator, and audit change-log from the ambient `ExecutionContext`. |
| **[`PagingArgs`](./PagingArgs.cs)** | Record carrying `Skip`, `Take`, and a `IsCountRequested` flag; configurable global `DefaultTake` and `MaximumTake` via `IConfiguration`. |
| **[`PagingResult`](./PagingResult.cs)** | Record carrying `Skip`, `Take`, `TotalCount` (optional), and `IsSkipped` / `IsPaged` convenience properties. |
| **[`PartitionKey`](./PartitionKey.cs)** | Static utility computing a deterministic hash-based partition identifier from a string key and a partition size. |
| **[`PartitionPicker`](./PartitionPicker.cs)** | Selects a partition key value from a configured pool using `PartitionKey.GetPartitionId`. |
| **[`QueryArgs`](./QueryArgs.cs)** | Carries OData-like `$filter`, `$orderby`, and include/exclude field lists for controlled dynamic querying. |
| [`IItemsResult`](./IItemsResult.cs) | Non-generic interface for `ItemsResult<T>` providing `Items` as `IEnumerable` and `Paging` as `PagingResult`. |
| [`IItemsResult<T>`](./IItemsResultT.cs) | Generic extension of `IItemsResult` adding strongly-typed `Items`. |
| [`ILogicallyDeleted`](./ILogicallyDeleted.cs) | Mutable interface adding `IsDeleted` for soft-delete; counterpart to `IReadOnlyLogicallyDeleted`. |
| [`IPartitionKey`](./IPartitionKey.cs) | Mutable interface adding `PartitionKey` for partitioned storage; counterpart to `IReadOnlyPartitionKey`. |
| [`IPrimaryKey`](./IPrimaryKey.cs) | Interface marking an entity as having a primary key; typically combined with `IEntityKey`. |
| [`ITenantId`](./ITenantId.cs) | Mutable interface adding `TenantId` for multi-tenant entities; counterpart to `IReadOnlyTenantId`. |
| [`ITotalCount`](./ITotalCount.cs) | Interface adding a nullable `TotalCount` property; implemented by `PagingResult` and `ItemsResult<T>`. |
| [`ITypeDiscriminator`](./ITypeDiscriminator.cs) | Mutable interface adding a `TypeDiscriminator` string for polymorphic persistence; counterpart to `IReadOnlyTypeDiscriminator`. |

## Related Namespaces

- **[`CoreEx.Entities`](../Entities/README.md)** - Defines the upstream entity contract interfaces (`IIdentifier<T>`, `IETag`, `IChangeLog`, `CompositeKey`) that data-layer interfaces here extend.
- **[`CoreEx.Database`](../../CoreEx.Database/README.md)** - Database package built on these primitives; uses `PagingArgs`, `QueryArgs`, `IPartitionKey`, `ILogicallyDeleted`, and `Model`.
- **[`CoreEx.EntityFrameworkCore`](../../CoreEx.EntityFrameworkCore/README.md)** - EF Core integration consuming `PagingArgs`, `QueryArgs`, and the entity contract interfaces from this namespace.
- **[`CoreEx.Data`](../../CoreEx.Data/README.md)** - Separate `CoreEx.Data` NuGet package providing higher-level query filtering and ordering utilities built on top of these primitives.