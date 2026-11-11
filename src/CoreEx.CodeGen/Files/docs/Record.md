# File record



## Property categories
The `Record` object supports a number of properties that control the generated code output. These properties are separated into a series of logical categories.

Category | Description
-|-
[`Primary`](#Primary) | Record identity.
[`Csv`](#Csv) | CSV routing.
[`Jsonl`](#Jsonl) | JSONL routing.
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

## Jsonl
JSONL routing.

Property | Description
-|-
`jsonl` | JSONL root discriminator match. The corresponding [`JsonlRecord`](JsonlRecord.md) object.

## Collections
Mapped properties.

Property | Description
-|-
**`properties`** | The corresponding [`Property`](Property.md) collection. [Mandatory]
