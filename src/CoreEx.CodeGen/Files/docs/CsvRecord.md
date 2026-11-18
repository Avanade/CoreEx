# CSV record routing



## Properties
The `CsvRecord` object supports a number of properties that control the generated code output. The following properties with a bold name are those that are more typically used (considered more important).

Property | Description
-|-
`match` | Exact discriminator.<br/>&dagger; Required for routed files; forbidden for flat files.
`cardinality` | File cardinality. Valid options are: `Many`, `One`, `Optional`, `OneOrMore`.<br/>&dagger; Unordered routed files only; defaults to Many. Counts include recognized invalid records. Hierarchical cardinality belongs on structure nodes.
`position` | File position. Valid options are: `Any`, `First`, `Last`.<br/>&dagger; Unordered routed files only; defaults to Any. First/Last require One or Optional cardinality.
