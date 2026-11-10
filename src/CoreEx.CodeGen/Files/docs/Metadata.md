# File metadata



## Properties
The `Metadata` object supports a number of properties that control the generated code output. The following properties with a bold name are those that are more typically used (considered more important).

Property | Description
-|-
**`name`** | Logical metadata name. [Mandatory]
**`record`** | Wire record. [Mandatory]
`required` | Required metadata.<br/>&dagger; Defaults to false; duplicate occurrences are always errors.
`csv` | CSV metadata routing. The corresponding [`CsvMetadata`](CsvMetadata.md) object.
