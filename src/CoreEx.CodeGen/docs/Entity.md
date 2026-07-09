# Reference-data entity configuration



## Property categories
The `Entity` object supports a number of properties that control the generated code output. These properties are separated into a series of logical categories.

Category | Description
-|-
[`Primary`](#Primary) | Provides the _primary_ configuration.
[`API`](#API) | Provides the configuration for the generated API code.
[`Repository`](#Repository) | Provides the configuration for the generated repository code.
[`Mapping`](#Mapping) | Provides the configuration for the generated mapping code.
[`Exclude`](#Exclude) | Provides the configuration for code generation exclusion.
[`Collections`](#Collections) | Provides the collections configuration.

The properties with a bold name are those that are more typically used (considered more important).

## Primary
Provides the _primary_ configuration.

Property | Description
-|-
**`name`** | The reference-data entity (contract) name. [Mandatory]
**`plural`** | The pluralized reference-data entity (contract) name.<br/>&dagger; Defaults to `{Name}` with the last word pluralized.
`text` | The reference-data entity friendly text.<br/>&dagger; Defaults to `{Name}` converted to sentence case. This is primarily used in generated code comments.
**`idType`** | The reference-data identifier type. Valid options are: `String`, `Guid`, `Int32`, `Int64`.<br/>&dagger; Defaults to root `{IdType}`.
**`collectionSortOrder`** | The collection sort order. Valid options are: `Code`, `Id`, `Text`, `SortOrder`.<br/>&dagger; This is the collection sort order. Defaults to root `{CollectionSortOrder}`.

## API
Provides the configuration for the generated API code.

Property | Description
-|-
**`route`** | The route suffix.<br/>&dagger; Defaults to `{Plural}` and root `{RouteConvention}` configuration.

## Repository
Provides the configuration for the generated repository code.

Property | Description
-|-
**`repository`** | The repository implementation. Valid options are: `None`, `EntityFramework`.<br/>&dagger; Defaults to root `{Repository}`.
**`repositoryName`** | The repository parameter name.<br/>&dagger; This is the .NET repository parameter name that should be used within the generated code. Defaults from root `{Repository}` and related configuration.
**`model`** | The corresponding repository model name.<br/>&dagger; Defaults to `{Name}` (assumes same).

## Mapping
Provides the configuration for the generated mapping code.

Property | Description
-|-
**`mapper`** | The mapper name.<br/>&dagger; This is the .NET mapper name used within the generated code. Defaults to root `{Name}Mapper`.

## Exclude
Provides the configuration for code generation exclusion.

Property | Description
-|-
**`excludeApi`** | Indicates whether to exclude the generation of the API.<br/>&dagger; Defaults to `false`.
**`excludeMapper`** | Indicates whether to exclude the generation of the mapper.<br/>&dagger; Defaults to `false`.

## Collections
Provides the collections configuration.

Property | Description
-|-
`properties` | The corresponding [`Property`](Property.md) collection.

