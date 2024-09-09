# CoreEx.Data

The `CoreEx.Data` namespace provides extended data-related capabilities. 

<br/>

## Motivation

The motivation is to simplify and improve the data access experience.

<br/>

## OData-like Querying

It is not always possible to implement the likes of OData and/or GraphQL on an underlying data source. This could be related to the complexity of the implementation, the desire to hide the underlying data structure, and/or limit the types of operations performed to manage the underlying performance.

However, the desire to provide a similar experience to the client remains. The `CoreEx.Data.Querying` namespace enables the client to perform OData-like queries (limited to `$filter` and `$orderby`) on an underlying data source, in a structured and controlled manner.

_Note:_ This is **not** intended to be a replacement for OData, GraphQL, etc. but to provide a limited, explicitly supported, dynamic capability to filter an underlying query.

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
  - `in` - in list; expressed as `field in('value1', 'value2', ...)`
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

The `'value'` is expressed as a string (must be enclosed in single quotes), number, boolean, or date, or `null` depending on the field type.

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

- [`FilterParser`](./Querying/QueryFilterParser.cs) - this is the `$filter` parser.
- [`OrderByParser`](./Querying/QueryOrderByParser.cs) - this is the `$orderby` parser.

Each of these properties have the ability to _explicitly_ add fields and their corresponding configuration. An example is as follows:

``` csharp
private static readonly QueryArgsConfig _queryConfig = QueryArgsConfig.Create()
    .WithFilter(filter => filter
        .AddField<string>(nameof(Employee.LastName), c => c.Operators(QueryFilterTokenKind.AllStringOperators).UseUpperCase())
        .AddField<string>(nameof(Employee.FirstName), c => c.Operators(QueryFilterTokenKind.AllStringOperators).UseUpperCase())
        .AddReferenceDataField<Gender>(nameof(Employee.Gender), nameof(EfModel.Employee.GenderCode))
        .AddField<DateTime>(nameof(Employee.StartDate))
        .AddNullField(nameof(Employee.Termination), nameof(EfModel.Employee.TerminationDate), c => c.Default(new QueryStatement($"{nameof(EfModel.Employee.TerminationDate)} == null"))))
    .WithOrderBy(orderby => orderby
        .AddField(nameof(Employee.LastName))
        .AddField(nameof(Employee.FirstName))
        .WithDefault($"{nameof(Employee.LastName)}, {nameof(Employee.FirstName)}"));
```

<br/>

### Usage

The configuration is then used to parse and apply the filter and/or order-by to the underlying query using the new `Where` and `OrderBy` extension methods.

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