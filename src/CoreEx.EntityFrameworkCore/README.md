﻿# CoreEx.EntityFrameworkCore

The `CoreEx.EntityFrameworkCore` namespace provides extended [_Entity Framework Core (EF)_](https://learn.microsoft.com/en-us/ef/core/) capabilities. 

<br/>

## Motivation

The motivation is to provide supporting EF Core capabilities for [CRUD](https://en.wikipedia.org/wiki/Create,_read,_update_and_delete) related access that support standardized _CoreEx_ data access patterns. This for the most part will simplify and unify the approach to ensure consistency of implementation where needed.

<br/>

## Requirements

The requirements for usage are as follows.
- An **entity** (DTO) that represents the data that must as a minimum implement [`IEntityKey`](../CoreEx/Entities/IEntityKey.cs); generally via either the implementation of [`IIdentifier`](../CoreEx/Entities/IIdentifierT.cs) or [`IPrimaryKey`](../CoreEx/Entities/IPrimaryKey.cs).
- A **model** being the underlying configured EF Core [data source model](https://learn.microsoft.com/en-us/ef/core/modeling/).
- An [`IMapper`](../CoreEx/Mapping/IMapper.cs) that contains the mapping logic to map to and from the **entity** and **model**.

The **entity** and **model** are different types to encourage separation between the externalized **entity** representation and the underlying **model**; which may be shaped differently, and have different property to column naming conventions, internalized columns, etc.

<br/>

## Railway-oriented programming

To support [railway-oriented programming](../CoreEx/Results/README.md) whenever a method name includes `WithResult` this indicates that it will return a `Result` or `Result<T>` including the resulting success or failure information. In these instances an `Exception` will only be thrown when considered truly exceptional.

<br/>

## CRUD capabilities

The [`IEfDb`](./IEfDb.cs) and corresponding [`EfDb`](./EfDb.cs) provides the base CRUD capabilities as follows.

<br/>

### Query (read)

A query is actioned using the [`EfDbQuery`](./EfDbQuery.cs) which is ostensibly a lightweight wrapper over an `IQueryable<TModel>` that automatically maps from the **model** to the **entity**.

Queried entities are not tracked by default; internally uses [`AsNoTracking`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.entityframeworkqueryableextensions.asnotracking); this behaviour can be overridden using [`EfDbArgs.QueryNoTracking`](./EfDbArgs.cs).

Note: a consumer should also consider using [`IgnoreAutoIncludes`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.entityframeworkqueryableextensions.ignoreautoincludes) to exclude related data, where not required, to improve query performance.

The following methods provide additional capabilities:

Method | Description
-|-
`WithPaging` | Adds `Skip` and `Take` paging to the query.
`SelectSingleAsync`, `SelectSingleWithResult` | Selects a single item.
`SelectSingleOrDefaultAsync`, `SelectSingleOrDefaultWithResultAsync` | Selects a single item or default.
`SelectFirstAsync`, `SelectFirstWithResultAsync` | Selects first item.
`SelectFirstOrDefaultAsync`, `SelectFirstOrDefaultWithResultAsync` | Selects first item or default.
`SelectQueryAsync`, `SelectQueryWithResultAsync` | Select items into or creating a resultant collection.
`SelectResultAsync`, `SelectResultWithResultAsync` | Select items creating a [`ICollectionResult`](../CoreEx/Entities/ICollectionResultT2.cs) which also contains corresponding [`PagingResult`](../CoreEx/Entities/PagingResult.cs).

<br/>

### Get (Read)

Gets (`GetAsync` or `GetWithResultAsync`) the **entity** for the specified key mapping from the **model**. Uses the [`DbContext.Find`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.dbcontext.find) internally for the **model** and specified key.

Where the data is not found, then a `null` will be returned. Where the **model** implements [`ILogicallyDeleted`](../CoreEx/Entities/ILogicallyDeleted.cs) and `IsDeleted` then this acts as if not found and returns a `null`.

<br/>

### Create

Creates (`CreateAsync` or `CreateWithResultAsync`) the **entity** by firstly mapping to the **model**. Uses the [`DbContext.Add`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.dbcontext.add) to begin tracking the **model** which will be inserted into the database when [`DbContext.SaveChanges`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.dbcontext.savechanges) is called.

Where the **entity** implements [`IChangeLogAuditLog`](../CoreEx/Entities/IChangeLogAuditLog.cs) generally via [`ChangeLog`](../CoreEx/Entities/IChangeLog.cs) or [`ChangeLogEx`](../CoreEx/Entities/Extended/IChangeLogEx.cs), then the `CreatedBy` and `CreatedDate` properties will be automatically set from the [`ExecutionContext`](../CoreEx/ExecutionContext.cs).

Where the **entity** and/or **model** implements [`ITenantId`](../CoreEx/Entities/ITenantId.cs) then the `TenantId` property will be automatically set from the [`ExecutionContext`](../CoreEx/ExecutionContext.cs).

Generally, the [`DbContext.SaveChanges`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.dbcontext.savechanges) is called to perform the _insert_; unless [`EfDbArgs.SaveChanges`](./EfDbArgs.cs) is set to `false` (defaults to `true`).

The inserted **model** is then re-mapped to the **entity** and returned where [`EfDbArgs.Refresh`](./EfDbArgs.cs) is set to `true` (default); this will ensure all properties updated as part of the _insert_ are included in the refreshed **entity**.

<br/>

### Update

Updates (`UpdateAsync` or `UpdateWithResultAsync`) the **entity** by firstly mapping to the **model**. Uses the [`DbContext.Update`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.dbcontext.add) to begin tracking the **model** which will be updated within the database when [`DbContext.SaveChanges`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.dbcontext.savechanges) is called.

First will check existence of the **model** by performing a [`DbContext.Find`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.dbcontext.find). Where the data is not found, then a [`NotFoundException`](../CoreEx/NotFoundException.cs) will be thrown. Where the **model** implements [`ILogicallyDeleted`](../CoreEx/Entities/ILogicallyDeleted.cs) and `IsDeleted` then this acts as if not found and will also result in a `NotFoundException`.

Where the entity implements [`IETag`](../CoreEx/Entities/IETag.cs) this will be checked against the just read version, and where not matched a  [`ConcurrencyException`](../CoreEx/ConcurrencyException.cs) will be thrown. Also, any [`DbUpdateConcurrencyException`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.dbupdateconcurrencyexception) thrown will be converted to a corresponding `ConcurrencyException` for consistency.

Where the **entity** implements [`IChangeLogAuditLog`](../CoreEx/Entities/IChangeLogAuditLog.cs) generally via [`ChangeLog`](../CoreEx/Entities/IChangeLog.cs) or [`ChangeLogEx`](../CoreEx/Entities/Extended/IChangeLogEx.cs), then the `UpdatedBy` and `UpdatedDate` properties will be automatically set from the [`ExecutionContext`](../CoreEx/ExecutionContext.cs).

Where the **entity** and/or **model** implements [`ITenantId`](../CoreEx/Entities/ITenantId.cs) then the `TenantId` property will be automatically set from the [`ExecutionContext`](../CoreEx/ExecutionContext.cs).

Generally, the [`DbContext.SaveChanges`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.dbcontext.savechanges) is called to perform the _update_; unless [`EfDbArgs.SaveChanges`](./EfDbArgs.cs) is set to `false` (defaults to `true`).

The updated **model** is then re-mapped to the **entity** and returned where [`EfDbArgs.Refresh`](./EfDbArgs.cs) is set to `true` (default); this will ensure all properties updated as part of the _update_ are included in the refreshed **entity**.

<br/>

### Delete

Deletes (`DeleteAsync` or `DeleteWithResultAsync`) the **entity** either physically or logically.

First will check existence of the **model** by performing a [`DbContext.Find`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.dbcontext.find). Where the data is not found, then a [`NotFoundException`](../CoreEx/NotFoundException.cs) will be thrown. Where the **model** implements [`ILogicallyDeleted`](../CoreEx/Entities/ILogicallyDeleted.cs) and `IsDeleted` then this acts as if not found and will also result in a `NotFoundException`.

Where the **model** implements [`ILogicallyDeleted`](../CoreEx/Entities/ILogicallyDeleted.cs) then an update will occur after setting `IsDeleted` to `true`. Uses the [`DbContext.Update`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.dbcontext.add) to begin tracking the **model** which will be updated within the database when [`DbContext.SaveChanges`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.dbcontext.savechanges) is called.

Otherwise, will physically delete. Uses the [`DbContext.Remove`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.dbcontext.remove) to begin tracking the **model** which will be deleted from the database when [`DbContext.SaveChanges`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.dbcontext.savechanges) is called.

Generally, the [`DbContext.SaveChanges`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.dbcontext.savechanges) is called to perform the _update_; unless [`EfDbArgs.SaveChanges`](./EfDbArgs.cs) is set to `false` (defaults to `true`).

<br/>

## Usage

To use `EfDB` relationships to the EF Core [`DbContext`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.dbcontext) must be established as follows.

- [`Database`](../CoreEx.Database/Database.cs) must be defined; see [example](../../samples/My.Hr/My.Hr.Business/Data/HrDb.cs).
- `DbContext` and [`Database`](../CoreEx.Database/Database.cs) relationship must be defined; see [example](../../samples/My.Hr/My.Hr.Business/Data/HrDbContext.cs).
- [`EfDb`](./EfDb.cs) and `DbContext` relationship must be defined; see [example](../../samples/My.Hr/My.Hr.Business/Data/HrEfDb.cs).

Alternatively, review the _Beef_ [MyEf.Hr data](https://github.com/Avanade/Beef/tree/master/samples/MyEf.Hr/MyEf.Hr.Business/Data) sample implementation.

<br/>

### MySql

Where leveraging _MySQL_ it is recommended to use the [Pomelo.EntityFrameworkCore.MySql](https://www.nuget.org/packages/Pomelo.EntityFrameworkCore.MySql) package; this has greater uptake and community supporting than the Oracle-enabled [MySql.Data.EntityFrameworkCore](https://www.nuget.org/packages/MySql.Data.EntityFrameworkCore) package.

The `DbContextOptionsBuilder` code within the `DbContext` implementation should be as follows (review version specification) to enable.

```csharp
protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
{
    base.OnConfiguring(optionsBuilder);

    if (!optionsBuilder.IsConfigured)
        optionsBuilder.UseMySql(BaseDatabase.GetConnection(), ServerVersion.Create(new Version(8, 0, 33), Pomelo.EntityFrameworkCore.MySql.Infrastructure.ServerType.MySql));
}
```