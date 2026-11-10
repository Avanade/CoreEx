# File record



## Property categories
The `Record` object supports a number of properties that control the generated code output. These properties are separated into a series of logical categories.

Category | Description
-|-
[`Primary`](#Primary) | Record identity.
[`Csv`](#Csv) | CSV routing.
[`Structure`](#Structure) | Route continuation policy.
[`Collections`](#Collections) | Mapped properties.

The properties with a bold name are those that are more typically used (considered more important).

## Primary
Record identity.

Property | Description
-|-
**`name`** | Logical record name. [Mandatory]

## Csv
CSV routing.

Property | Description
-|-
`csv` | CSV routing settings. The corresponding [`CsvRecord`](CsvRecord.md) object.

## Structure
Route continuation policy.

Property | Description
-|-
`errorMode` | Route error policy. Valid options are: `FailFast`, `CollectErrors`.

## Collections
Mapped properties.

Property | Description
-|-
**`properties`** | The corresponding [`Property`](Property.md) collection. [Mandatory]
