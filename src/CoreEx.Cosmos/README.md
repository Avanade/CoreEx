# CoreEx.Cosmos

The `CoreEx.Cosmos` namespace provides extended [_Azure Cosmos DB_](https://learn.microsoft.com/en-us/azure/cosmos-db/) capabilities, specifically focused on the [API for NoSQL](https://learn.microsoft.com/en-us/azure/cosmos-db/choose-api#api-for-nosql).

<br/>

## Motivation

The motivation is to provide supporting Cosmos DB capabilities for [CRUD](https://en.wikipedia.org/wiki/Create,_read,_update_and_delete) related access that support standardized _CoreEx_ data access patterns. This for the most part will simplify and unify the approach to ensure consistency of implementation where needed.

<br/>

## Requirements

The requirements for usage are as follows.
- An **entity** (DTO) that represents the data that must as a minimum implement [`IEntityKey`](../CoreEx/Entities/IEntityKey.cs); generally via either the implementation of [`IIdentifier`](../CoreEx/Entities/IIdentifierT.cs) or [`IPrimaryKey`](../CoreEx/Entities/IPrimaryKey.cs).
- A **model** being the underlying data representation that will be persisted within CosmosDB itself..
- An [`IMapper`](../CoreEx/Mapping/IMapper.cs) that contains the mapping logic to map to and from the **entity** and **model**.

The **entity** and **model** are different types to encourage separation between the externalized **entity** representation and the underlying **model**; which may be shaped differently, and have different property naming conventions, internalized properties, etc.

<br/>

## Railway-oriented programming

To support [railway-oriented programming](../CoreEx/Results/README.md) whenever a method name includes `WithResult` this indicates that it will return a `Result` or `Result<T>` including the resulting success or failure information. In these instances an `Exception` will only be thrown when considered truly exceptional.

<br/>

## Resource model

This article provides a good overview of the [Azure Cosmos DB resource model](https://learn.microsoft.com/en-us/azure/cosmos-db/resource-model); these concepts are important to understand when working with Cosmos DB.

_CoreEx_ provides encapsulated capabilities for each of the following:
- [Databases](#Database) - contains one-or-more Containers.
- [Containers](#Containers) (Collections) - contains one-or-more Items.
- [Items](#Items) (Documents) - the JSON object being persisted.

Each of the above are further described, in reverse order, as this is intended to make it easier to understand.

<br/>

## Items

From a Cosmos DB perspective, an Item (aka Document) is a JSON object that represents the data that is being persisted. The are two key patterns for persisting a Document that _CoreEx_ enables:

- **Untyped** - where a single Document _type_ is persisted within the Container; being one or more Documents of the same _type_ (schema/structure). This is the simpliest pattern, and requires no special support to enable; i.e. works out-of-the-box.
- **Typed** - where one or more Document _types_ are persisted within the Container; being one or more Documents of different _types_ (schema/structure). This is a more complex pattern, and requires additional support to enable - this is enabled in a consistent manner via the [`CosmosDbValue<TModel>`](./CosmosDbValue.cs) class.

The **Typed** document JSON structure is as follows (standard Cosmos DB properties have been removed for brevity purposes):

``` json
{
  "type": "document-type-name",  # The unique name of the document type; used to query/filter the documents.
  "value": {                     # The actual document _model_ data.
    "property1": "value1",
    "property2": "value2"
  }
}
```

<br/>

## Containers

From a Cosmos DB perspective, a [Container](https://learn.microsoft.com/en-us/azure/cosmos-db/resource-model#azure-cosmos-db-containers) is a logical entity that represents a collection of items. The are two key patterns for interacting with a container that _CoreEx_ enables:
- **Entity** - pattern in which there is separation between the externalized **entity** and the underlying **model**, and the requisite mapping between the two is fully integrated. This is the preferred pattern as it allows for a clear separation of concerns. These capabilities largely exist in the root `CoreEx.Cosmos` namespace.
- **Model** - pattern in which the persisted **model** is directly interacted with, with the expectation that the developer would handle any mapping manually. This is useful in scenarios where the full **entity** is an overhead to the operations that needs to be performed. These capabilities largely exist in the `CoreEx.Cosmos.Model` namespace.

A Cosmos DB Container is encapsulated within one of the following _CoreEx_ capabilities depending on the patterns required:

Type | Container Pattern | Document Pattern | [`IMapper`](../CoreEx/Mapping/IMapper.cs) support
-|-|-
[`CosmosDbContainer`](CosmosDbContainer.cs) | Entity | Untyped | Yes
[`CosmosDbValueContainer`](CosmosDbValueContainer.cs) | Entity | Typed | No
[`CosmosDbModelContainer`](Model/CosmosDbModelContainer.cs) | Model | Untyped | Yes
[`CosmosDbValueModelContainer`](Model/CosmosDbValueModelContainer.cs) | Model | Typed | No

Where more advanced CosmosDB capabilities are required, for example, Partitioning, etc., then the [`CosmosDbArgs`](./CosmosDbArgs.cs) enables the configuration of these capabilities, as well as other extended _CoreEx_ capabilities such as multi-tenancy support.

<br/>

## Database

From a Cosmos DB perspective, a [Database](https://learn.microsoft.com/en-us/azure/cosmos-db/resource-model#azure-cosmos-db-databases) is a means to group one-or-more Containers. 

The [`ICosmoDb`](./ICosmosDb.cs) and corresponding [`CosmosDb`](./CosmosDb.cs) provides the base Database capabilities:
- `Container<T, TModel>()` - instantiates a `CosmosDbContainer` instance.
- `ValueContainer<T, TModel>()` - instantiates a `CosmosDbValueContainer` instance.
- `ModelContainer<TModel>()` - instantiates a `CosmosDbModelContainer` instance.
- `ValueModelContainer<TModel>()` - instantiates a `CosmosDbValueModelContainer` instance.
- `UserAuhtorizeFilter<TModel>()` - enables an authorization filter to be applied to a specified Container.

The following represents an example usage of the [`CosmosDb`](https://github.com/Avanade/Beef/blob/master/samples/Cdr.Banking/Cdr.Banking.Business/Data/CosmosDb.cs) class:

``` csharp
public class CosmosDb : CoreEx.Cosmos.CosmosDb
{
    private readonly Lazy<CosmosDbContainer<Account, Model.Account>> _accounts;
    private readonly Lazy<CosmosDbContainer<AccountDetail, Model.Account>> _accountDetails;
    private readonly Lazy<CosmosDbContainer<Transaction, Model.Transaction>> _transactions;

    /// <summary>
    /// Initializes a new instance of the <see cref="CosmosDb"/> class.
    /// </summary>
    public CosmosDb(Mac.Database database, IMapper mapper) : base(database, mapper)
    {
        // Apply an authorization filter to all operations to ensure only the valid data is available based on the users context; i.e. only allow access to Accounts within list defined on ExecutionContext.
        UseAuthorizeFilter<Model.Account>("Account", (q) => ((IQueryable<Model.Account>)q).Where(x => ExecutionContext.Current.Accounts.Contains(x.Id!)));
        UseAuthorizeFilter<Model.Account>("Transaction", (q) => ((IQueryable<Model.Transaction>)q).Where(x => ExecutionContext.Current.Accounts.Contains(x.AccountId!)));

        // Lazy create the containers.
        _accounts = new(() => Container<Account, Model.Account>("Account"));
        _accountDetails = new(() => Container<AccountDetail, Model.Account>("Account"));
        _transactions = new(() => Container<Transaction, Model.Transaction>("Transaction"));
    }

    /// <summary>
    /// Exposes <see cref="Account"/> entity from <b>Account</b> container.
    /// </summary>
    public CosmosDbContainer<Account, Model.Account> Accounts => _accounts.Value;

    /// <summary>
    /// Exposes <see cref="AccountDetail"/> entity from <b>Account</b> container.
    /// </summary>
    public CosmosDbContainer<AccountDetail, Model.Account> AccountDetails => _accountDetails.Value;

    /// <summary>
    /// Exposes <see cref="AccountDetail"/> entity from <b>Account</b> container.
    /// </summary>
    public CosmosDbContainer<Transaction, Model.Transaction> Transactions => _transactions.Value;
}
```

<br/>

## CRUD capabilities

The **entity** [`ICosmosDbContainer<T, TModel>`](./ICosmosDbContainerT.cs) and **model** [`CosmosDbModelContainer<TModel>`](./Model/CosmosDbModelContainer.cs) provides the base CRUD capabilities as follows.

<br/>

### Query (Read)

A query is actioned using the [`CosmosDbQuery<T, TModel>`](./CosmosDbQuery.cs) and [`CosmosDbModelQuery<TModel>`](./Model/CosmosDbModelQuery.cs) which is obstensibly a lightweight wrapper over an `IQueryable<TModel>` that automatically maps from the **model** to the **entity** (where applicable).

Uses the [`Container.GetItemLinqQueryable`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.azure.cosmos.container.getitemlinqqueryable?view=azure-dotnet) internally to create.

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
`ToArrayAsync`, `ToArrayWithResultAsync` | Select items into a resulting array.

<br/>

### Get (Read)

Gets (`GetAsync` or `GetWithResultAsync`) the **entity** for the specified key mapping from the **model**. Uses the [`Container.ReadItemAsync`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.azure.cosmos.container.readitemasync?view=azure-dotnet) internally for the **model** and specified key.

Where the data is not found, then a `null` will be returned. Where the **model** implements [`ILogicallyDeleted`](../CoreEx/Entities/ILogicallyDeleted.cs) and `IsDeleted` then this acts as if not found and returns a `null`.

<br/>

### Create

Creates (`CreateAsync` or `CreateWithResultAsync`) the **entity** by firstly mapping to the **model**. Uses the [`Container.CreateItemAsync`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.azure.cosmos.container.createitemasync?view=azure-dotnet) internally to create.

Where the **entity** implements [`IChangeLogAuditLog`](../CoreEx/Entities/IChangeLogAuditLog.cs) generally via [`ChangeLog`](../CoreEx/Entities/IChangeLog.cs) or [`ChangeLogEx`](../CoreEx/Entities/Extended/IChangeLogEx.cs), then the `CreatedBy` and `CreatedDate` properties will be automatically set from the [`ExecutionContext`](../CoreEx/ExecutionContext.cs).

Where the **entity** and/or **model** implements [`ITenantId`](../CoreEx/Entities/ITenantId.cs) then the `TenantId` property will be automatically set from the [`ExecutionContext`](../CoreEx/ExecutionContext.cs).

The inserted **model** is then re-mapped to the **entity** and returned; this will ensure all properties updated as part of the _insert_ are included in the refreshed **entity**.

<br/>

### Update

Updates (`UpdateAsync` or `UpdateWithResultAsync`) the **entity** by firstly mapping to the **model**. Uses the [`Container.ReplaceItemAsync`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.azure.cosmos.container.replaceitemasync?view=azure-dotnet) internally to update.

First will check existence of the **model** by performing a [`Container.ReadItemAsync`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.azure.cosmos.container.readitemasync?view=azure-dotnet). Where the data is not found, then a [`NotFoundException`](../CoreEx/NotFoundException.cs) will be thrown. Where the **model** implements [`ILogicallyDeleted`](../CoreEx/Entities/ILogicallyDeleted.cs) and `IsDeleted` then this acts as if not found and will also result in a `NotFoundException`.

Where the entity implements [`IETag`](../CoreEx/Entities/IETag.cs) this will be checked against the just read version, and where not matched a  [`ConcurrencyException`](../CoreEx/ConcurrencyException.cs) will be thrown. Also, any [`CosmosException`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.azure.cosmos.cosmosexception?view=azure-dotnet) with a `HttpStatusCode.PreconditionFailed` thrown will be converted to a corresponding `ConcurrencyException` for consistency.

Where the **entity** implements [`IChangeLogAuditLog`](../CoreEx/Entities/IChangeLogAuditLog.cs) generally via [`ChangeLog`](../CoreEx/Entities/IChangeLog.cs) or [`ChangeLogEx`](../CoreEx/Entities/Extended/IChangeLogEx.cs), then the `UpdatedBy` and `UpdatedDate` properties will be automatically set from the [`ExecutionContext`](../CoreEx/ExecutionContext.cs).

Where the **entity** and/or **model** implements [`ITenantId`](../CoreEx/Entities/ITenantId.cs) then the `TenantId` property will be automatically set from the [`ExecutionContext`](../CoreEx/ExecutionContext.cs).

The updated **model** is then re-mapped to the **entity** and returned; this will ensure all properties updated as part of the _update_ are included in the refreshed **entity**.

<br/>

### Delete

Deletes (`DeleteAsync` or `DeleteWithResultAsync`) the **entity**/**model** either physically or logically.

First will check existence of the **model** by performing a [`Container.ReadItemAsync`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.azure.cosmos.container.readitemasync?view=azure-dotnet). Where the data is not found, then a [`NotFoundException`](../CoreEx/NotFoundException.cs) will be thrown. Where the **model** implements [`ILogicallyDeleted`](../CoreEx/Entities/ILogicallyDeleted.cs) and `IsDeleted` then this acts as if not found and will also result in a `NotFoundException`.

Where the **model** implements [`ILogicallyDeleted`](../CoreEx/Entities/ILogicallyDeleted.cs) then an update will occur after setting `IsDeleted` to `true`. Uses the [`Container.ReplaceItemAsync`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.azure.cosmos.container.replaceitemasync?view=azure-dotnet) internally to update.

Otherwise, will physically delete. Uses the [`Container.DeleteItemAsync`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.azure.cosmos.container.deleteitemasync?view=azure-dotnet) internally to delete.


## Usage

Review the unit tests and/or _Beef_ [Cdr.Banking](https://github.com/Avanade/Beef/tree/master/samples/Cdr.Banking) sample implementation.