# CoreEx.RefData

The `CoreEx.RefData` namespace provides a rich, first class, experience for _reference data_ given its key role within an application.

<br/>

## Motivation

To provide a consistent pattern to the treatment of _reference data_ within an application, simplifying usage whilst maintaining flexibility of implementation.

<br/>

## Types of data

At a high-level a typical application deals with different types of data:

- **Reference Data** is data that is managed within an application primarily used to provide lists of valid values. These values provide contextual information that are generally used to trigger business processes, workflows and/or used for grouping / filtering.
This data has a low level of volatility, in that it remains largely static for significant periods of time. There are low volumes of this data within an application. It is a very good candidate for the likes of caching. Reference Data is generally never deleted; instead it may become inactive. Example: Country, Gender, Payment Type, etc. 

- **Master data** is data that is captured and continuously maintained to reflect a current known understanding; there is no historical context other than that provided by an audit process providing a version history over time. This data has a moderate level of volatility, in that changes generally occur infrequently. There are moderate volumes of this data within an application.
Master data can be deleted (or logically deleted) as required; typically the latter. Example: Customer, Vendor, Product, GL Account, etc. 

- **Transactional data** is data that is recorded to capture/manage an event or action, tied to specific business rules, at a point in time. The data will typically have a high level of volatility at inception decreasing significantly over time. Once the corresponding workflow has completed the data becomes immutable and serves the purpose of providing a historical context. Transactional data is generally never deleted as it provides an auditable recording; it may be archived. There are high volumes of this type of data within an application. Example: Purchase Order, Sales Invoice, GL Posting, etc. 

<br/>

## Base capabilities

The [`IReferenceData`](./IReferenceData.cs) provides for the core (standard) properties. The [`ReferenceDataBase`](./ReferenceDataBaseT.cs) or [`ReferenceDataBaseEx`](./Extended/ReferenceDataBaseEx.cs) provide the base implementation depending on the level of functionality required out-of-the-box. The latter is targeted for internal usage only with additional capabilities included that are often useful.

The *primary* properties are as follows.

Property | Description
-|-
`Id` | The internal unique identifier as either an `int`, `long`, `Guid` or `string`.
`Code` | The unique (immutable) code as a `string`. This is primarily the value that would be used by external parties (applications) to consume. Additionally, it could be used to store the reference in the underlying data source if the above `Id` is not suitable.
`Text` | The textual `string` used for display within an application; e.g. within a drop-down. 
`SortOrder` | Defines the sort order (integer) within the underlying reference data collection.
`IsActive` | Indicates whether the value is active or not. It is up to the application what to do when a value is not considered active.

Additional _secondary_ properties are as follows.

Property | Description
-|-
`StartDate` | The `IsValid` validity start date (`null` indicates not defined) where [`IReferenceDataContext`](./IReferenceDataContext.cs)
`EndDate` | The `IsValid` validity end date (`null` indicates not defined).
`ETag` | The entity tag ([`IETag`](../Entities/IETag.cs)) for optimistic concurrency and [`IF-MATCH`](https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/If-Match) checking.

Finally, there is a feature to enable multiple code mappings; i.e. where two (or more) systems have a different codes for the same value. The `SetMapping`, `GetMapping` and `TryGetMapping`, etc. methods enable.

Additional developer-defined properties can be, and should be, added where required extending on the base class. The _reference data_ framework will then make these available within the application to enable simple usage/access by a developer. 

The [`ReferenceDataCollection`](./ReferenceDataCollection.cs) provides the base capabilities for a reference data collection. Including the adding, ensuring uniqueness, sorting and additional filtering (e.g. `ActiveList`).

<br/>

## Orchestration and caching

The [`ReferenceDataOrchestrator`](./ReferenceDataOrchestrator.cs) provides the centralized reference data orchestration. Primarily responsible for the management of one or more `IReferenceDataProvider` instances. 

An [`IReferenceDataProvider`](./IReferenceDataProvider.cs) defines the list of _reference data_ `Types` that are provided/supported. The `GetAsync` method is then responsible for performing the load for the specified _reference data_ type. 

Each `IReferenceDataProvider` (typically only one) instance is registered via the orchestrator's `Register` method. The orchestrator then provides a number of utility methods to access the various _reference data_ types. The orchestrator will lazy-load each type (using provider's `GetAsync`) on first access and automatically cache using an [`IMemoryCache`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.caching.memory.imemorycache) to improve performance. Out-of-the-box `IMemoryCache` policies can be set to manage cache lifetimes.

<br/>

### Cache policy configuration

The default implementation for the `IMemoryCache` is that each _reference data_ type's collection [`ICacheEntry`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.caching.memory.icacheentry) is set in a standardized manner; being `AbsoluteExpirationRelativeToNow` and `SlidingExpiration` properties defaulted to `02:00:00` (2 hours) and `00:30:00` (30 minutes) respectively.

These defaults can be overridden within the configuration [settings](../Configuration/SettingsBase.cs); as can specific _reference data_ types. The following is an example of setting the defaults and then specifically overridding the `Gender` policy (the `Type.Name` is used as the property name).

``` json
{
  "RefDataCache": {
    "AbsoluteExpirationRelativeToNow": "01:45:00",
    "SlidingExpiration": "00:15:00",
    "Gender": {
      "AbsoluteExpirationRelativeToNow": "03:00:00",
      "SlidingExpiration": "00:45:00"
    }
  }
}
```

Where the above is not sufficient then the virtual `OnCreateCacheEntry` method can be overridden to fully customize (override) the default behaviour.

<br/>

## Reference data properties and serialization

It is recommended that the rich _reference data_ types themselves where included within an entity are not JSON serialized/deserialized over the wire from the likes of an API; in these instances only the `Code` is generally used. This is to minimize the resulting payload size of the owning entity. 

For example a `Person` entity would be defined with the rich `Gender` _reference data_ type:

``` csharp
public class Person
{
    public string? Name { get; set; }
    public Gender? Gender { get; set; }
}
```

The resulting serialized JSON should be as follows. By default the [`JsonSerializer`](../Text/Json/JsonSerializer.cs) will automatically detect a property of `IReferenceData` and serialize the `Code` only, whereby ensuring the minimized serialization. Where full serialization is required use the alternate [`ReferenceDataContentJsonSerializer`](../Text/Json/ReferenceDataContentJsonSerializer.cs).

``` json
{ "name": "sarah", "gender": "F" }
```

<br/>

## Reference data APIs

The [orchestrator](#Orchestration-and-caching) encapsulates all the requisite functionality to enables rich API endpoints. By default only the active (`IReferenceData.IsActive`) items will be returned. To get both the active and inactive the [`$inactive=true`](../Http/HttpConsts.cs) URL query string must be used.

<br/>

### Per reference data endpoints

Each reference data entity should have an API endpoint; similar to `/ref/Xxx`. This will by default return all of the active reference data entries. This can also be invoked passing additional URL query string parameters:

Parameter | Description
-|-
`code` | Zero or more codes can be passed; e.g: `?code=m,f` or `?code=m&code=f` (case insensitive).
`text` | A single text with wildcards can be passed; e.g: `?text=m*` (case insensitive).

To expose per _reference data_ type, [code similar to](../../../samples/My.Hr/My.Hr.Api/Controllers/ReferenceDataController.cs) the following should be adopted. The [orchestrator](#Orchestration-and-caching) encapsulates all the requisite functionality to filter, etc.

``` csharp
[HttpGet("genders")]
[ProducesResponseType(typeof(IEnumerable<Gender>), (int)HttpStatusCode.OK)]
public Task<IActionResult> GenderGetAll([FromQuery] IEnumerable<string>? codes = default, string? text = default) =>
    _webApi.GetAsync(Request, x => _orchestrator.GetWithFilterAsync<Gender>(codes, text, x.RequestOptions.IncludeInactive));
```

<br/>

### Root reference data endpoint

Additionally, a root `/ref`-style endpoint can be used to return multiple reference data values in a single request; designed to reduce chattiness from a consuming channel to the above _per_ endpoints. This must be passed at least a single URL query string parameter to function.

The parameter is either just the named reference data entity which will result in all corresponding entries being returned (e.g: `?gender` or `?gender&country`). Otherwise, specific codes can be specified (e.g" `?gender=m,f`, `?gender=m&gender=f`, `?gender=m,f&country=au,nz`). The options can be mixed and matched (e.g: `?gender&country=au,nz`).

To expose, [code similar to](../../../samples/My.Hr/My.Hr.Api/Controllers/ReferenceDataController.cs) the following should be adopted. The [orchestrator](#Orchestration-and-caching) encapsulates all the requisite functionality to function.

``` csharp
[HttpGet()]
[ProducesResponseType(typeof(IEnumerable<CoreEx.RefData.ReferenceDataMultiItem>), (int)HttpStatusCode.OK)]
public Task<IActionResult> GetNamed() => _webApi.GetAsync(Request, p => _orchestrator.GetNamedAsync(p.RequestOptions));
```