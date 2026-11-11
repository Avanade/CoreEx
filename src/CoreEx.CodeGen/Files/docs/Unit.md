# Business unit selection



## Properties
The `Unit` object supports a number of properties that control the generated code output. The following properties with a bold name are those that are more typically used (considered more important).

Property | Description
-|-
`record` | Business root record.<br/>&dagger; Inferred only when exactly one non-metadata, non-child record is eligible.
`cardinality` | File unit cardinality. Valid options are: `Many`, `One`, `Optional`, `OneOrMore`.<br/>&dagger; Defaults to One for root Object input; otherwise Many.
`kind` | JSON document value kind. Valid options are: `Array`, `Object`.<br/>&dagger; Defaults to Array; applies only to JSON documents.
`unitPath` | JSON document unit path.<br/>&dagger; Defaults to the root pointer; applies only to JSON documents.
