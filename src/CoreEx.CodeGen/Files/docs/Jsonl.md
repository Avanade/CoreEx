# JSONL framing and routing



## Properties
The `Jsonl` object supports a number of properties that control the generated code output. The following properties with a bold name are those that are more typically used (considered more important).

Property | Description
-|-
`discriminator` | Direct string discriminator.<br/>&dagger; Omit for ordinary object units. Required for metadata routing; no implicit type-member convention.
`blankLinePolicy` | Blank-line policy. Valid options are: `Reject`, `Skip`.<br/>&dagger; Defaults to Reject. Skip excludes blank lines from records/units but not bytes/physical lines.
