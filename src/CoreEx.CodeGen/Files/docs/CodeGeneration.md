# File processing code-generation



## Property categories
The `CodeGeneration` object supports a number of properties that control the generated code output. These properties are separated into a series of logical categories.

Category | Description
-|-
[`Primary`](#Primary) | Definition identity.
[`Format`](#Format) | Input format.
[`Processing`](#Processing) | Default invocation policy.
[`Csv`](#Csv) | CSV parser, routing, and assembly settings.
[`Json`](#Json) | JSON mapping and JSONL framing.
[`Paths`](#Paths) | Destination project and subfolder.
[`Namespaces`](#Namespaces) | Explicit generated namespace.
[`Collections`](#Collections) | Typed wire records.

The properties with a bold name are those that are more typically used (considered more important).

## Primary
Definition identity.

Property | Description
-|-
**`name`** | Integration name. [Mandatory]
**`version`** | Definition revision. [Mandatory]<br/>&dagger; Not a package/source version, layout selector, or business deduplication key.

## Format
Input format.

Property | Description
-|-
`format` | Input format. Valid options are: `Csv`, `Jsonl`.<br/>&dagger; Defaults to Csv. JSON document and XML readers are not yet supported.

## Processing
Default invocation policy.

Property | Description
-|-
`metadata` | The corresponding [`Metadata`](Metadata.md) collection.
`mode` | Default processing mode. Valid options are: `ValidateOnly`, `ValidateThenExecute`, `ValidateAndExecute`.<br/>&dagger; Defaults to ValidateOnly. Valid-unit reporting is an optional processor hook in every mode; ValidateAndExecute validates and executes each unit in one pass.
`errorMode` | Default content policy. Valid options are: `FailFast`, `CollectErrors`.<br/>&dagger; Defaults to FailFast.

## Csv
CSV parser, routing, and assembly settings.

Property | Description
-|-
`csv` | CSV parser, routing, and assembly settings. The corresponding [`Csv`](Csv.md) object.

## Json
JSON mapping and JSONL framing.

Property | Description
-|-
`json` | Shared JSON mapping settings. The corresponding [`Json`](Json.md) object.<br/>&dagger; JSONL only in this release; kind/unitPath/document selection are not supported.
`jsonl` | JSONL framing and routing. The corresponding [`Jsonl`](Jsonl.md) object.
`unit` | Business object selection. The corresponding [`Unit`](Unit.md) object.

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

## Collections
Typed wire records.

Property | Description
-|-
**`records`** | The corresponding [`Record`](Record.md) collection. [Mandatory]
