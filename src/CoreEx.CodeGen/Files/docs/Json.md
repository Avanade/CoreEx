# Shared JSON mapping settings



## Properties
The `Json` object supports a number of properties that control the generated code output. The following properties with a bold name are those that are more typically used (considered more important).

Property | Description
-|-
`unknownMemberPolicy` | Unknown member policy. Valid options are: `Reject`, `Ignore`.<br/>&dagger; Defaults to Reject; applies recursively to units and declared metadata.
`maxDepth` | Positive nesting limit.<br/>&dagger; Defaults to 64; includes ignored objects and arrays.
