# CoreEx.Data

The `CoreEx.Data` namespace provides extended data-related capabilities. 

<br/>

## Motivation

The motivation is to simplify and improve the data access experience.

<br/>

## OData-like Querying

It is not always possible to implement the likes of OData and/or GraphQL on an underlying data source. This could be related to the complexity of the implementation, the desire to hide the underlying data structure, and/or limit the types of operations performed to manage the underlying performance.

However, the desire to provide a similar experience to the client remains. The `CoreEx.Data.Querying` namespace enables the client to perform OData-like queries (limited to [`$filter`](https://docs.oasis-open.org/odata/odata/v4.01/cs01/part2-url-conventions/odata-v4.01-cs01-part2-url-conventions.html#sec_SystemQueryOptionfilter) and [`$orderby`](https://docs.oasis-open.org/odata/odata/v4.01/cs01/part2-url-conventions/odata-v4.01-cs01-part2-url-conventions.html#_Toc505773299)) on an underlying data source, in a structured and controlled manner.

_Note:_ This is **not** intended to be a replacement for [OData](https://learn.microsoft.com/en-us/odata/webapi-8/overview), [GraphQL](https://github.com/graphql-dotnet/graphql-dotnet), etc. but to provide a limited, explicitly supported, dynamic capability to filter an underlying query.

Where this capability is different is that the separation from the API contract and the underlying data source schema is maintained. This is achieved by using configuration to explicitly define the fields that can be filtered and ordered, whilst also defining their relationship to the data source. This is in contrast to OData and GraphQL where the data source schema is largely exposed to the client.

<br/>

### Features

The following features are supported:

- `$filter` - the ability to filter the underlying query based on a set of conditions. The following is supported:
  - `eq` - equal to; expressed as `field eq 'value'`
  - `ne` - not equal to; expressed as `field ne 'value'`
  - `gt` - greater than; expressed as `field gt 'value'`
  - `ge` - greater than or equal to; expressed as `field ge 'value'`
  - `lt` - less than; expressed as `field lt 'value'`
  - `le` - less than or equal to; expressed as `field le 'value'`
  - `in` - in list; expressed as `field in ('value1', 'value2', ...)`
  - `startswith` - starts with; expressed as `startswith(field, 'value')`
  - `endswith` - ends with; expressed as `endswith(field, 'value')`
  - `contains` - contains; expressed as `contains(field, 'value')`
  - `and` - logical and; expressed as `field1 eq 'value1' and field2 eq 'value2'`
  - `or` - logical or; expressed as `field1 eq 'value1' or field2 eq 'value2'`
  - `not` - logical not; expressed as `not field eq 'value'`
  - `null` - is null; expressed as `field eq null`
  - `(` and `)` - grouping; expressed as `(field1 eq 'value1' and field2 eq 'value2') or field3 eq 'value3'`)`

- `$orderby` - the ability to order the underlying query based on a set of fields. The following is supported:
  - `asc` - ascending; expressed as `field asc`
  - `desc` - descending; expressed as `field desc`
  - `,` - multiple fields; expressed as `field1 asc, field2 desc`

Where the `'value'` is expressed as a string it must be enclosed in single quotes. A number, boolean, date, date and time, or `null` should be expressed as a constant, as expected for the underlying field type.

The following are examples of supported queries:

```
$filter=lastname eq 'Doe' and startswith(firstname, 'a')
$filter=salary gt 100000 and salary le 200000
$filter=(lastname eq 'Doe' and firstname eq 'John') or (lastname eq 'Smith' and firstname eq 'Jane')
$filter=state in ('CA', 'NY', 'TX')
$filter=isactive eq true
$filter=terminated eq null
$filter=startdate ge 2020-01-01
$orderby=lastname desc, firstname
```

<br/>

### Configuration

The [`QueryArgsConfig`](./Querying/QueryArgsConfig.cs) provides the means to configure the desired support; the model is an _explicit_ opt-in, versus an opt-out, of the capabilities.

This contains the following key capabilities:

- [`FilterParser`](./Querying/QueryFilterParser.cs) - this is the `$filter` parser and LINQ translator.
- [`OrderByParser`](./Querying/QueryOrderByParser.cs) - this is the `$orderby` parser and LINQ translator.

Each of these properties have the ability to _explicitly_ add fields and their corresponding configuration. An example is as follows:

``` csharp
private static readonly QueryArgsConfig _config = QueryArgsConfig.Create()
    .WithFilter(filter => filter
        .AddField<string>(nameof(Employee.LastName), c => c.WithOperators(QueryFilterOperator.AllStringOperators).WithUpperCase())
        .AddField<string>(nameof(Employee.FirstName), c => c.WithOperators(QueryFilterOperator.AllStringOperators).WithUpperCase())
        .AddReferenceDataField<Gender>(nameof(Employee.Gender), nameof(EfModel.Employee.GenderCode))
        .AddField<DateTime>(nameof(Employee.StartDate))
        .AddNullField(nameof(Employee.Termination), nameof(EfModel.Employee.TerminationDate), c => c.WithDefault(new QueryStatement($"{nameof(EfModel.Employee.TerminationDate)} == null"))))
    .WithOrderBy(orderby => orderby
        .AddField(nameof(Employee.LastName))
        .AddField(nameof(Employee.FirstName))
        .WithDefault($"{nameof(Employee.LastName)}, {nameof(Employee.FirstName)}"));
```

There are a number of different field configurations that can be added:

Method | Description
|-|-|
`AddField<T>` | Adds a field of the specified type `T`. See [`QueryFilterFieldConfig<T>`](./Querying/QueryFilterFieldConfigT.cs).
`AddNullField` | Adds a field that only supports `null` checking operations; limits to `EQ` and `NE`. See [`QueryFilterNullFieldConfig`](./Querying/QueryFilterNullFieldConfig.cs).
`AddReferenceDataField<TRef>` | Adds a reference data field of the specified type `TRef`. Automatically includes the requisite `IReferenceData.Code` validation, and limits operations to `EQ`, `NE` and `IN`. See [`QueryFilterReferenceDataFieldConfig<TRef>`](./Querying/QueryFilterReferenceDataFieldConfig.cs).

Each of the above methods support the following parameters:
- `field` - the name of the field (using the correct casing) that can be referenced within the `$filter`.
- `model` - the optional model name of the field (using the correct casing)  to be used in the underlying LINQ operation (defaults to `field`).
- `configure` - an optional configuration action to further define the field configuration.

Depending on the field type being added (as above), the following related configuration options are available:

Method | Description
|-|-|
`AlsoCheckNotNull` | Indicates that a not-null check should also be performed when performing the operation.
`AsNullable` | Indicates that the field is nullable and therefore supports null equality operations.
`MustBeValid` | Indicates that the reference data field value must exist and be considered valid; i.e. it is `IReferenceData.IsValid`.
`UseIdentifier` | Indicates that the `IReferenceData.Id` should be used in the underlying LINQ operation instead of the `IReferenceData.Code`.
`WithConverter` | Provides the `IConverter<string, T>` to convert the filer value string to the underlying field type of `T`.
`WithDefault` | Provides a default LINQ statement to be used for the field when no filtering is specified by the client.
`WithHelpText` | Provides additional help text for the field to be used where help is requested.
`WithOperators` | Overrides the supported operators for the field. See [`QueryFilterOperator`](./Querying/QueryFilterOperator.cs).
`WithResultWriter` | Provides an opportunity to override the default result writer; i.e. LINQ expression.
`WithUpperCase` | Indicates that the operation should be case-insensitive by performing an explicit `ToUpper()` on the field value.
`WithValue` | Provides an opportunity to override the converted field value when the filter is applied.

<br/>

### Usage

The configuration is then used to parse and apply the filter and/or order-by to the underlying query using the new `IQueryable<T>.Where` and `IQueryable<T>.OrderBy` extension methods.

``` csharp
var query = new QueryArgs
{
    Filter = "LastName eq 'Doe' and startswith(firstname, 'a')",
    OrderBy = "LastName desc, FirstName"
};

return _dbContext.Employees.Where(_queryConfig, query).OrderBy(_queryConfig, query).ToCollectionResultAsync<EmployeeCollectionResult, EmployeeCollection, Employee>(paging);
```

The [`QueryArgs`](../CoreEx/Entities/QueryArgs.cs), demonstrated above, is a simple class that is used to house the `Filter` and `OrderBy` properties in a consistent fashion. Additionally, the [`WebApiRequestOptions`](../CoreEx.AspNetCore/CoreEx.AspNetCore.WebApis.WebApiRequestOptions) automatically creates an instance of this class from the originating query string (i.e. `$filter` and `$orderby`).

``` csharp
public Task<IActionResult> GetAllAsync()
    => _webApi.GetAsync(Request, p => _service.GetAllAsync(p.RequestOptions.Query, p.RequestOptions.Paging));
```

<br/>

### Enablement

The `CoreEx.Data.Querying` capabilities described above essentially parses the OData-like syntax and then translates it into the equivalent dynamic LINQ statements. This statement is then passed through the [Dynamic LINQ](https://dynamic-linq.net/) NuGet [library](https://dynamic-linq.net/).

For example, the following OData-like filters would be translated into the equivalent dynamic LINQ statements:

```
$filter: code eq 'A'
LINQ: Where("Code == @0", ["A"])
---
$filter: startswith(firstName, 'abc'), 
LINQ: Where("FirstName.ToUpper().StartsWith(@0)", ["ABC"])
```

<br/>

### Help

To aid the consumers (clients) of the OData-like endpoints a *help* request can be issued. This is performed by using either `$filter=help` or `$orderby=help` and will result in a `400-BadRequest` with help-like contents similar to the following:

``` json
{
  "$filter": [
    "Filter field(s) are as follows:
     LastName (Type: String, Null: false, Operators: EQ, NE, LT, LE, GE, GT, IN, StartsWith, Contains, EndsWith)
     FirstName (Type: String, Null: false, Operators: EQ, NE, LT, LE, GE, GT, IN, StartsWith, Contains, EndsWith)
     Gender (Type: Gender, Null: false, Operators: EQ, NE, IN)
     StartDate (Type: DateTime, Null: false, Operators: EQ, NE, LT, LE, GE, GT, IN)
     Termination (Type: <none>, Null: true, Operators: EQ, NE)"
  ]
}

{
  "$orderby": [
    "Order-by field(s) are as follows:
    LastName (Direction: Both)
    FirstName (Direction: Both)"
  ]
}

```
