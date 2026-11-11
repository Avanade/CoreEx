# Nested collection occurrence



## Properties
The `Collection` object supports a number of properties that control the generated code output. The following properties with a bold name are those that are more typically used (considered more important).

Property | Description
-|-
`record` | Element record.<br/>&dagger; Exactly one of record/type is required.
`type` | Scalar element type. Valid options are: `String`, `Integer`, `Int64`, `Decimal`, `Double`, `Boolean`, `Guid`, `DateOnly`, `DateTime`, `DateTimeOffset`.<br/>&dagger; Exactly one of record/type is required. Null elements are rejected.
`cardinality` | Per-parent cardinality. Valid options are: `Many`, `One`, `Optional`, `OneOrMore`.<br/>&dagger; Defaults to Many; missing/null counts as zero.
`maxItems` | Optional maximum elements.<br/>&dagger; No maximum by default. An explicit positive count is a business constraint checked before element mapping.
