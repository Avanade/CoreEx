# File record



## Property categories
The `Record` object supports a number of properties that control the generated code output. These properties are separated into a series of logical categories.

Category | Description
-|-
[`Primary`](#Primary) | Record identity.
[`Structure`](#Structure) | Routing and continuation.
[`Collections`](#Collections) | Mapped properties.

The properties with a bold name are those that are more typically used (considered more important).

## Primary
Record identity.

Property | Description
-|-
**`name`** | Logical record name. [Mandatory]

## Structure
Routing and continuation.

Property | Description
-|-
`match` | Exact discriminator.<br/>&dagger; Required for hierarchical files; forbidden for flat files.
`errorMode` | Route error policy. Valid options are: `FailFast`, `CollectErrors`.

## Collections
Mapped properties.

Property | Description
-|-
**`properties`** | The corresponding [`Property`](Property.md) collection. [Mandatory]
