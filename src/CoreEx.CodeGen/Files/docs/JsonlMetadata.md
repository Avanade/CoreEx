# JSONL metadata placement



## Properties
The `JsonlMetadata` object supports a number of properties that control the generated code output. The following properties with a bold name are those that are more typically used (considered more important).

Property | Description
-|-
`position` | Metadata position. Valid options are: `Any`, `First`, `Last`.<br/>&dagger; Defaults to Any; malformed/unroutable non-skipped lines participate.
