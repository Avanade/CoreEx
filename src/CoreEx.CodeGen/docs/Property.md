# Reference-data property configuration



## Property categories
The `Property` object supports a number of properties that control the generated code output. These properties are separated into a series of logical categories.

Category | Description
-|-
[`Primary`](#Primary) | Provides the _primary_ configuration.
[`Repository`](#Repository) | Provides the configuration for the generated repository code.
[`Exclude`](#Exclude) | Provides the configuration for code generation exclusion.

The properties with a bold name are those that are more typically used (considered more important).

## Primary
Provides the _primary_ configuration.

Property | Description
-|-
**`name`** | The property name. [Mandatory]
**`type`** | The property type.<br/>&dagger; Defaults to `string?`. Supports the following conventions: `^`-prefix for reference-data and `?`-suffix for nullable.
`text` | The property text.<br/>&dagger; Defaults to `{Name}` converted to sentence case. This is primarily used in generated code comments.

## Repository
Provides the configuration for the generated repository code.

Property | Description
-|-
`model` | The corresponding data model property name.<br/>&dagger; Defaults to `{Name}` (assumes same).

## Exclude
Provides the configuration for code generation exclusion.

Property | Description
-|-
`excludeContract` | Indicates whether to exclude the property from the generated contract code.<br/>&dagger; Defaults to `false`.
`excludeMapping` | Indicates whether to exclude the property from the generated mapping code.<br/>&dagger; Defaults to `false`.

