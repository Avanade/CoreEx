# Hierarchy position



## Property categories
The `Node` object supports a number of properties that control the generated code output. These properties are separated into a series of logical categories.

Category | Description
-|-
[`Structure`](#Structure) | Record ownership and occurrences.
[`Collections`](#Collections) | Ordered descendants.

The properties with a bold name are those that are more typically used (considered more important).

## Structure
Record ownership and occurrences.

Property | Description
-|-
**`record`** | Record name. [Mandatory]
`cardinality` | Occurrences. Valid options are: `Many`, `One`, `Optional`, `OneOrMore`.<br/>&dagger; Defaults to Many.

## Collections
Ordered descendants.

Property | Description
-|-
`children` | The corresponding [`Node`](Node.md) collection.
