# CoreEx.Database

The `CoreEx.Database` namespace provides extended [_ADO.NET_](https://learn.microsoft.com/en-Us/dotnet/framework/data/adonet/) capabilities. 

<br/>

## Motivation

The motivation is to simplify and unify the approach to _ADO.NET_ (database) access.

<br/>

## Railway-oriented programming

To support [railway-oriented programming](../CoreEx/Results/README.md) whenever a method name includes `WithResult` this indicates that it will return a `Result` or `Result<T>` including the resulting success or failure information. In these instances an `Exception` will only be thrown when considered truly exceptional.

<br/>

## Database

The [`Database`](./Database.cs) is the base (common) implementation for the [`IDatabase`](./IDatabase.cs) interface that provides the standardized access to the underlying database.

The following additional [`IDatabase`](./IDatabase.cs) key capabilities exist.

Capability | Description
-|-
[`DatabaseColumns`](./Extended/DatabaseColumns.cs) | Enables the specification of special database columns used for extended built-in capabilities.
[`Wildcard`](./DatabaseWildcard.cs) | Provides configuration to manage _wildcard_ transformation.
[`DateTimeTransform`](../CoreEx/Entities/DateTimeTransform.cs) | Specifies the `DateTime` transformation when reading from the database.

<br/>

### Provider specific

The following specific database provider implementations further extend the capabilities.

Database | Implementation
-|-
[Microsoft SQL Server](https://learn.microsoft.com/en-us/sql/sql-server) | [`SqlServerDatabase`](../CoreEx.Database.SqlServer/SqlServerDatabase.cs)
[Oracle MySQL](https://www.oracle.com/mysql/what-is-mysql/) | [`MySqlDatabase`](../CoreEx.Database.MySql/MySqlDatabase.cs)

<br/>

### Usage

To use the `Database` a connection creation function parameter is required that is leveraged at runtime (lazy instantiation) to get (create or provide) the underlying [`DbConnection`](https://learn.microsoft.com/en-us/dotnet/api/system.data.common.dbconnection). The `IDatabase` implements [`IDisposable`](https://learn.microsoft.com/en-us/dotnet/api/system.idisposable); the `Dispose` is the primary mechanism to close the connection where automatically opened.

The following demonstrates [usage](../../samples/My.Hr/My.Hr.Business/Data/HrDb.cs).

``` csharp
    public class HrDb : SqlServerDatabase
    {
        public HrDb(SettingsBase settings) : base(() => new SqlConnection(settings.GetRequiredValue<string>("ConnectionStrings:Database"))) { }
    }
```

Additionally, review the _Beef_ repo [sample](https://github.com/Avanade/Beef/blob/master/samples/My.Hr/My.Hr.Business/Data/HrDb.cs).

<br/>

## Commands

The _CoreEx_ [`IDatabase`](./IDatabase.cs) encapsulates an _ADO.NET_ [`DbCommand`](https://learn.microsoft.com/en-us/dotnet/api/system.data.common.dbcommand) within a [`DatabaseCommand`](./DatabaseCommand.cs); via the following methods:

Method | Description
-|-
`StoredProcedure` | Creates a command for a stored procedure; (see [`CommandType.StoredProcedure`](https://learn.microsoft.com/en-us/dotnet/api/system.data.commandtype#system-data-commandtype-storedprocedure))
`SqlStatement` | Creates a command for a SQL statement; (see [`CommandType.Text`](https://learn.microsoft.com/en-us/dotnet/api/system.data.commandtype#system-data-commandtype-text))
`SqlFromResource` | Creates a command for a SQL statement within the specified embedded resource.

or `IDatabase.SqlStatement` method passing the appropriate content.

The following [`DatabaseCommand`](./DatabaseCommand.cs) methods provide additional capabilities. The query-based methods optionally leverage the rich [Mapping](#Mapping) capabilities.

Method | Description
-|-
`NonQueryAsync`, `NonQueryWithResultAsync` | Executes a non-query command.
`ScalarAsync<T>`, `ScalarWithResultAsync<T>` | Executes the query and returns the first column of the first row in the result set returned by the query.
`SelectSingleAsync`, `SelectSingleWithResultAsync` | Selects a single item.
`SelectSingleOrDefaultAsync` | Selects a single item or default.
`SelectFirstAsync`, `SelectFirstWithResultAsync` | Selects first item.
`SelectFirstOrDefaultAsync`, `SelectFirstOrDefaultWithResultAsync` | Selects first item or default.
`SelectQueryAsync`, `SelectQueryWithResultAsync` | Select items into or creating a resultant collection.
`SelectMultiSetAsync`, `SelectMultiSetWithResulAsync` | Executes a multi-dataset query command with one or more [multi-set arguments](#Multi-set-arguments).

The _DbEx_ [`DatabaseExtensions`](https://github.com/Avanade/DbEx/blob/main/src/DbEx/DatabaseExtensions.cs) class demonstrates usage of the `SelectQueryAsync` (_without_ [Mapping](#Mapping)) within the `SelectSchemaAsync` method.

<br/>

### Query

The [`Extended`](./Extended) namespace provides a `DatabaseCommand.Query<T>` that provides a [`DatabaseQuery<T>`](./Extended/DatabaseQuery.cs) to encapsulate the following.

Method | Description
-|-
`WithPaging` | Adds `Skip` and `Take` paging to the query.
`SelectSingleAsync`, `SelectSingleWithResultAsync` | Selects a single item.
`SelectSingleOrDefaultAsync`, `SelectSingleOrDefaultWithResultAsync` | Selects a single item or default.
`SelectFirstAsync`, `SelectFirstWithResultAsync` | Selects first item.
`SelectFirstOrDefaultAsync`, `SelectFirstOrDefaultWithResultAsync` | Selects first item or default.
`SelectQueryAsync`, `SelectQueryWithResultAsync` | Select items into or creating a resultant collection.
`SelectResultAsync`, `SelectResultWithResultAsync` | Select items creating a [`ICollectionResult`](../CoreEx/Entities/ICollectionResultT2.cs) which also contains corresponding [`PagingResult`](../CoreEx/Entities/PagingResult.cs).

<br/>

### Reference data

The [`Extended`](./Extended) namespace provides a `DatabaseCommand.ReferenceData<TColl, TItem, TId>` that provides a [`RefDataLoader<TColl, TItem, TId>`](./Extended/RefDataLoader.cs) (via the `LoadAsync` and `LoadWithResultAsync` methods) to simplify the loading of a reference data collection.

The [`ReferenceDataService`](../../samples/My.Hr/My.Hr.Business/Services/ReferenceDataService.cs) within the `My.Hr` smaple demonstrates usage.

``` csharp
await _db.ReferenceData<GenderCollection, Gender, Guid>("Hr", "Gender").LoadAsync("GenderId", cancellationToken: cancellationToken).ConfigureAwait(false)
```

<br/>

## Parameters

The [`DatabaseCommand`](./DatabaseCommand.cs) provides a [`Parameters`](./DatabaseParameterCollection.cs) property that primarily enables the following core parameter capabilities.

Method | Description
-|-
`AddParameter` | Adds a [`DbParameter`](); there are a number of overloads enabled.
`AddReturnValueParameter` | Adds an `int` return value [`DbParameter`]() (see [`DatabaseColumns.ReturnValueName`](./Extended/DatabaseColumns.cs)).
`AddReselectRecordParam` | Adds a `bool` reselect record [`DbParameter`]() (see [`DatabaseColumns.ReselectRecordName`](./Extended/DatabaseColumns.cs)).

Additionally, the `DatabaseCommand` supports a set of [extension methods](./IDatabaseParametersExtensions.cs) to further enable, and simplify, the specification of parameters that leverage the aforementioned `Parameters`.

Method | Description
-|-
`Param` | Adds a [`DbParameter`](https://learn.microsoft.com/en-us/dotnet/api/system.data.common.dbparameter); there are a number of overloads enabled.
`ParamWhen` | Adds a [`DbParameter`](https://learn.microsoft.com/en-us/dotnet/api/system.data.common.dbparameter) _when_ the specified condition is `true`; there are a number of overloads enabled.
`ParamWith` | Adds a [`DbParameter`](https://learn.microsoft.com/en-us/dotnet/api/system.data.common.dbparameter) when invoked with a non-default value; there are a number of overloads enabled.
`ParamWithWildcard` | Adds a _wildcard_ [`DbParameter`](https://learn.microsoft.com/en-us/dotnet/api/system.data.common.dbparameter) when invoked with a non-default value; there are a number of overloads enabled.
`RowVersionParam` | Adds a _row version_ [`DbParameter`](https://learn.microsoft.com/en-us/dotnet/api/system.data.common.dbparameter) (see [`DatabaseColumns.RowVersionName`](./Extended/DatabaseColumns.cs)). Note that the underlying implementation is database specific.
`ReselectRecordParam` | Adds a `bool` reselect record [`DbParameter`](https://learn.microsoft.com/en-us/dotnet/api/system.data.common.dbparameter) (see [`DatabaseColumns.ReselectRecordName`](./Extended/DatabaseColumns.cs)).
`ReselectRecordParamWhen` | Adds a `bool` reselect record [`DbParameter`](https://learn.microsoft.com/en-us/dotnet/api/system.data.common.dbparameter) (see [`DatabaseColumns.ReselectRecordName`](./Extended/DatabaseColumns.cs)) _when_ `true`.
`PagingParams` | Adds the [`PagingArgs`](../CoreEx/Entities/PagingArgs.cs) [`DbParameter`](https://learn.microsoft.com/en-us/dotnet/api/system.data.common.dbparameter)(s) being [`DatabaseColumns.PagingSkipName`](./Extended/DatabaseColumns.cs), `DatabaseColumns.PagingTakeName` and `DatabaseColumns.PagingCountName`.

<br/>

## Database record

_CoreEx_ encapsulates an _ADO.NET_ [`DbDataReader`](https://learn.microsoft.com/en-us/dotnet/api/system.data.common.dbdatareader) within a [`DatabaseRecord`](./DatabaseRecord.cs); this primarily provides the `GetValue<T>` method that provides extended capabilites to retrieve a column value from the underlying `DbDataReader`.

<br/>

## Mapping

To support the mapping _from_ and/or _to_ a .NET Type and the underlying database, the [`IDatabaseMapper`](./IDatabaseMapper.cs) and corresponding [`IDatabaseMapper<TSource>`](./IDatabaseMapperT.cs) interface enable (also see [`DatabaseQueryMapper`](./DatabaseQueryMapper.cs) for query only (`MapFromDb`) support).
- `MapToDb` - maps the .NET Type to the database by adding the properties as database [parameters](#Parameters).
- `MapFromDb` - maps the database columns to the properties of a .NET Type.

The [`Mapping` namespace](./Mapping) provides the primary mapping capabilities.

Class | Description
-|-
[`DatabaseMapper`](./Mapping/DatabaseMapper.cs) | Enables the `Create` and `CreateAuto` of a `DatabaseMapper<TSource>`.
[`DatabaseMapper<TSource>`](./Mapping/DatabaseMapperT.cs) | Provides the to/from mapping configuration.
[`PropertyColumnMapper`]() | Provides the property to/from mapping configuration.

The [`ChangeLogDatabaseMapper`](./Mapping/ChangeLogDatabaseMapper.cs) is a _CoreEx_ implementation example. Additionally, see the _Beef_ `My.Hr` sample which further demonstrates usage within the [`EmployeeBaseData.DbMapper`](https://github.com/Avanade/Beef/blob/master/samples/My.Hr/My.Hr.Business/Data/Generated/EmployeeBaseData.cs) class.

<br/>

## Multi-set arguments

To simplify the support for the [retrieval of multiple result sets](https://learn.microsoft.com/en-us/dotnet/framework/data/adonet/retrieving-data-using-a-datareader#retrieving-multiple-result-sets-using-nextresult) the [`IMultiSetArgs`](./IMultiSetArgs.cs) is provided. This is useful where a single command will result in multiple result sets reducing the chattiness between application and database, improving performance, reducing execution latency.

The following `IMultiSetArgs` implementations are provided. The `StopOnNull` property indicates whether to stop further query result set processing where the current set has resulted in a `null` (i.e. no records).

Class | Description
-|-
[`MultiSetCollArgs<TColl, TItem>`](./MultiSetCollArgsT.cs) | Provides the multi-set arguments when expecting a collection of items/records. The `MinRows` and `MaxRows` properties can also be specified to ensure/validate correctness of returned rows.
[`MultiSetSingleArgs<T>`](./MultiSetSingleArgsT.cs) | Provides the multi-set arguments when expecting a single item/record only. The `IsMandatory` property indicates whether the value is mandatory.

The [`DatabaseCommannd.SelectMultiSetAsync`](./DatabaseCommand.cs) method supports one or more [`IMultiSetArgs`](./IMultiSetArgs.cs) when invoked; leveraging the configuration within to create the resulting output. Note also, the `IMultiSetArgs` count must not be less that the number of result sets returned from the database.

The _Beef_ `My.Hr` sample demonstrates usage within the [`EmployeeData`](https://github.com/Avanade/Beef/blob/master/samples/My.Hr/My.Hr.Business/Data/EmployeeData.cs) class.

``` csharp
await db.SelectMultiSetAsync(
    new MultiSetSingleArgs<Employee>(DbMapper.Default, r => employee = r, isMandatory: false, stopOnNull: true),
    new MultiSetCollArgs<EmergencyContactCollection, EmergencyContact>(EmergencyContactData.DbMapper.Default, r => employee!.EmergencyContacts = r)).ConfigureAwait(false);
```