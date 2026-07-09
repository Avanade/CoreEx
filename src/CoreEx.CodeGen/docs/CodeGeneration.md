# Reference-data code-generation

This configuration is used for generating reference-data related code. This is a well understood, deterministic pattern, that is best suited for scenarios where reference-data capabilities are required in a consistent manner.

## Property categories
The `CodeGeneration` object supports a number of properties that control the generated code output. These properties are separated into a series of logical categories.

Category | Description
-|-
[`Primary`](#Primary) | Provides the _primary_ configuration.
[`API`](#API) | Provides the configuration for the generated API code.
[`Repository`](#Repository) | Provides the configuration for the generated repository code.
[`Paths`](#Paths) | Provides the configuration for the paths used in code generation.
[`Collections`](#Collections) | Provides the collections configuration.

The properties with a bold name are those that are more typically used (considered more important).

## Primary
Provides the _primary_ configuration.

Property | Description
-|-
**`domain`** | The domain name.<br/>&dagger; This is the .NET domain name. Attempts to default from the underlying data project file path; uses the second to last segment of the child-most sub-directory by convention. For example, `/xxx/yyy/My.App.Sales.CodeGen`, the domain would be `Sales`.
**`idType`** | The default reference data identifier type. Valid options are: `String`, `Guid`, `Int32`, `Int64`.<br/>&dagger; Defaults to `String`.
**`collectionSortOrder`** | The default reference data collection sort order. Valid options are: `Code`, `Id`, `Text`, `SortOrder`.<br/>&dagger; This is the default collection sort order. Defaults to `Code`.

## API
Provides the configuration for the generated API code.

Property | Description
-|-
**`route`** | The route prefix.<br/>&dagger; Defaults to `/api/refdata`.
**`routeConvention`** | The route naming convention where not directly specified. Valid options are: `KebabCase`, `SnakeCase`, `CamelCase`, `Lowercase`.<br/>&dagger; Defaults to `KebabCase`.

## Repository
Provides the configuration for the generated repository code.

Property | Description
-|-
**`repository`** | The default repository implementation. Valid options are: `None`, `EntityFramework`. [Mandatory]
**`entityFrameworkRepositoryName`** | The default Entity Framework (EF) repository identifier/name.<br/>&dagger; This is the .NET Entity Framework (EF) repository identifier/name that should be used within the generated code (often a private field). Defaults to `_ef`.

## Paths
Provides the configuration for the paths used in code generation.

Property | Description
-|-
`contractsProjectPath` | The relative path for the .NET contracts-related project.<br/>&dagger; Defaults to `Contracts`.
`apiProjectPath` | The relative path for the .NET API-related project.<br/>&dagger; Defaults to `Api`.
`applicationProjectPath` | The relative path for the .NET application-related project.<br/>&dagger; Defaults to `Application`.
`dataProjectPath` | The relative path for the .NET data-related project.<br/>&dagger; Defaults to automatic inference using expected name of `Infrastructure`.
`dataRepositoriesPath` | The path to append to the `{DotNetDataProjectPath}` for the .NET generated repository code.<br/>&dagger; Defaults to `Repositories`.
`dataModelsPath` | The path to append to the `{DotNetDataProjectPath}` for the .NET generated models code.<br/>&dagger; Defaults to `Persistence`.
`dataMappingPath` | The path to append to the `{DotNetDataProjectPath}` for the .NET generated mapping code.<br/>&dagger; Defaults to `Mapping`.

## Collections
Provides the collections configuration.

Property | Description
-|-
**`entities`** | The corresponding [`Entity`](Entity.md) collection.

