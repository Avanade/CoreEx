# File processing code-generation



## Property categories
The `CodeGeneration` object supports a number of properties that control the generated code output. These properties are separated into a series of logical categories.

Category | Description
-|-
[`Primary`](#Primary) | Definition identity.
[`Format`](#Format) | CSV dialect and explicit encoding.
[`Processing`](#Processing) | Default invocation policy and operating budgets.
[`Paths`](#Paths) | Destination project and subfolder.
[`Namespaces`](#Namespaces) | Explicit generated namespace.
[`Structure`](#Structure) | Routing and hierarchy.
[`Collections`](#Collections) | Typed wire records.

The properties with a bold name are those that are more typically used (considered more important).

## Primary
Definition identity.

Property | Description
-|-
**`name`** | Integration name. [Mandatory]
**`version`** | Definition revision. [Mandatory]<br/>&dagger; Not a package/source version, layout selector, or business deduplication key.

## Format
CSV dialect and explicit encoding.

Property | Description
-|-
`delimiter` | Delimiter.<br/>&dagger; One character; defaults to comma.
`quote` | Quote.<br/>&dagger; One character; defaults to double quote.
`comment` | Comment marker.<br/>&dagger; One character at record start; omitted by default.
`encoding` | Explicit encoding. Valid options are: `Utf8`, `Utf16LE`, `Utf16BE`, `Utf32LE`, `Utf32BE`.<br/>&dagger; Defaults to Utf8. Strict runtime decoding; no BOM-based encoding switching.

## Processing
Default invocation policy and operating budgets.

Property | Description
-|-
`mode` | Default processing mode. Valid options are: `ValidateOnly`, `ValidateThenExecute`.<br/>&dagger; Defaults to ValidateOnly. Execution without handwritten behavior fails explicitly.
`errorMode` | Default content policy. Valid options are: `FailFast`, `CollectErrors`.<br/>&dagger; Defaults to FailFast.
`maxInputBytes` | Positive input-byte budget.<br/>&dagger; Defaults to 67108864 (64 MiB).
`maxGroupRecords` | Positive records-per-root budget.<br/>&dagger; Defaults to 10000, including the root.

## Paths
Destination project and subfolder.

Property | Description
-|-
**`runtimeProjectPath`** | Runtime project directory. [Mandatory]<br/>&dagger; Relative to the CodeGen output/project directory, or absolute. No Functions project is required.
`runtimePath` | Runtime subfolder.<br/>&dagger; Defaults to Files/<Name>; must remain inside runtimeProjectPath.

## Namespaces
Explicit generated namespace.

Property | Description
-|-
`runtimeNamespace` | Generated namespace.<br/>&dagger; Defaults to project directory name plus runtimePath segments. Explicit override avoids assumptions about MSBuild RootNamespace.

## Structure
Routing and hierarchy.

Property | Description
-|-
`discriminatorIndex` | Zero-based discriminator index.<br/>&dagger; Required with structure; forbidden for flat input.
`structure` | Business-root hierarchy. The corresponding [`Structure`](Structure.md) object.<br/>&dagger; Without structure, exactly one flat record must be declared.

## Collections
Typed wire records.

Property | Description
-|-
**`records`** | The corresponding [`Record`](Record.md) collection. [Mandatory]
