# CSV parser and assembly settings



## Property categories
The `Csv` object supports a number of properties that control the generated code output. These properties are separated into a series of logical categories.

Category | Description
-|-
[`Dialect`](#Dialect) | CSV dialect and explicit encoding.
[`Routing`](#Routing) | CSV routing and hierarchy.

The properties with a bold name are those that are more typically used (considered more important).

## Dialect
CSV dialect and explicit encoding.

Property | Description
-|-
`delimiter` | Delimiter.<br/>&dagger; One character; defaults to comma.
`quote` | Quote.<br/>&dagger; One character; defaults to double quote.
`comment` | Comment marker.<br/>&dagger; One character at record start; omitted by default.
`encoding` | Explicit encoding. Valid options are: `Utf8`, `Utf16LE`, `Utf16BE`, `Utf32LE`, `Utf32BE`.<br/>&dagger; Defaults to Utf8. Strict runtime decoding; no BOM-based encoding switching.
`headerPolicy` | Named header policy. Valid options are: `AllowAdditionalColumns`, `Exact`.<br/>&dagger; Flat named-column mappings only; defaults to AllowAdditionalColumns. Exact rejects extra header columns and rows whose field count differs from the header.

## Routing
CSV routing and hierarchy.

Property | Description
-|-
`discriminatorIndex` | Zero-based discriminator index.<br/>&dagger; Selects unordered routing without structure; required with structure; omitted for flat input.
`structure` | Business-root hierarchy. The corresponding [`Structure`](Structure.md) object.<br/>&dagger; Omit for unordered routed records (with discriminatorIndex) or one flat record (without discriminatorIndex).
