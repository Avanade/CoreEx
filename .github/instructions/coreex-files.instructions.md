---
applyTo: "**/*-file.yaml;**/Files/**/*.cs;!**/Files/**/*.g.cs"
description: "CoreEx.IO file ingestion invariants: YAML generation, streamed diagnostics, immutable replay, scoped execution and caller-owned idempotency."
tags: ["files", "csv", "jsonl", "codegen", "io"]
---

<!--
  AI workflow asset — dual-audience notice:
  - In Avanade/CoreEx this is the authored source.
  - Consumer copies are owned by the version-pinned coreex-ai bundle; propose changes upstream.
-->

# CoreEx File Processing

## NuGet / Project References

| Package/tool | Role |
|---|---|
| `CoreEx.IO` | Shared host-neutral lifecycle, CSV/nested JSONL mapping and readers, scoped unit execution, diagnostics and reconciliation. |
| `CoreEx.CodeGen` | Design-time `file` command that generates runtime artifacts from `*-file.yaml`; never add it as a runtime dependency. |

For creation/modification workflow, use [coreex-file](/.github/skills/coreex-file/SKILL.md). Runtime reference: [CoreEx.IO](/.github/docs/coreex/agents/CoreEx.IO.md).

## Required Invariants

- Start from a checked-in top-level `*-file.yaml` in the CodeGen console. Run File commands with that console as the current working directory; `dotnet run --project <path>` does not change it, and discovery is relative to the process working directory. Generate wire models, mappings, validators, contexts, processors, and create-only handler starters; use `name`, `version`, `runtimeProjectPath`, `records`, and choose flat, unordered discriminator routing, or ordered `structure` based on actual input. Run `dotnet run -- file --validate` before generation.
- Change YAML or templates, never generated output. Handler starters are create-only; preserve handwritten logic and `[ScopedService]`. One dynamic assembly scan registers all attributed integrations.
- All processors inherit protected `Services` from `FileProcessor<TUnit>` for coordinator dependencies used by handwritten partial hooks. It is the construction-time provider, not an automatic invocation or unit scope; access without a supplied provider throws explicitly. Keep unit business dependencies in the generated handler's fresh child scope; do not resolve them from the processor provider.
- Protected lifecycle and handler hooks use purpose-specific argument objects with framework-owned construction and get-only properties; cancellation stays separate and last. Unit validation uses `FileUnitValidationArgs<TUnit>` and its awaited `ReportDiagnosticAsync`; execution/observation use `FileUnitArgs<TUnit>`. Generated unordered dispatch narrows via `args.As<TRecord>()`, preserving context/provenance/sink; cast `args.Context` for generated reconciliation members.
- Generated wire values are nullable. `required` emits NotNull, permitting zero/false; `mandatory` rejects defaults. Rules use one fluent chain per property. Reference data remains a string code, with preinitialized lookup collections.
- `ProcessAsync` owns streams opened by a cancellation-aware factory. The caller guarantees identical immutable bytes for validation and replay. No implicit buffering/spooling.
- Whole-file validation, including EOF reconciliation, precedes replay execution. Replay creates fresh models and pass state; business execution uses independent scopes and returns the original Result. `ValidateOnly`, `ValidateThenExecute`, and `ValidateAndExecute` define processing behavior; the optional awaited `OnValidatedUnitAsync` hook is independent of mode and runs after mapping, pure validation, and scoped custom validation succeed, but before EOF acceptance. Outcomes are mode-independent: `Succeeded`, `ContentFailed`, or `ExecutionFailed`. Later errors return `ContentFailed` while preserving any earlier reports or single-pass writes.
- Use `csv` for CSV dialect/routing settings. Property `name` is the exact source field; `property` optionally overrides the derived CLR name, and positional selection belongs under `csv.index`. File-level metadata uses the shared `metadata` collection; do not put header/trailer metadata under `csv.structure`. For unordered CSV, First/Last routes remain executable units unless explicitly referenced by `metadata`; a declared metadata route is counted as a source record, never as a business unit.
- Flat named mappings may select `CsvHeaderPolicy.Exact` to reject extra header columns and data rows with a field count different from the header; it requires all mapped selectors to be named and remains opt-in for backward compatibility.
- JSONL uses `format: Jsonl`, grouped `json` syntax/mapping options, optional `jsonl.discriminator`, `unit.record` when ambiguous, and direct nested `record`/`collection` occurrences. Member names match exactly and case-sensitively; one record type may be reused at independent occurrences. The first JSONL delivery supports one business wire type plus bounded opt-in metadata routes, not heterogeneous business dispatch. JSON document readers/selectors and XML are not implemented.
- JSONL uses UTF-8 with LF/CRLF lines. A UTF-8 BOM is terminal; recover syntax/depth and consumed-string conversion errors under CollectErrors. Use a single JsonDocument parse per line, not a custom tokenizer, UTF-8 prepass, or exhaustive decoded-string walk. Malformed ignored string content can remain unexamined. On .NET 10+ delegate duplicate rejection to AllowDuplicateProperties = false; older targets use a small DOM name check. Duplicate/syntax/depth and invalid member-name decoding errors share jsonl.record.invalid. Preserve distinct CLR property paths, escaped unit-relative JSON Pointers, and physical lines. Source offsets come from consumed bytes, not read-ahead-distorted stream deltas.
- JSON scalar tokens are strict; preserve empty/whitespace strings and absent/null versus empty collections. Null collection elements reject. Validate nested cardinality/maxItems before allocation; mapping failure suppresses downstream validators. CSV and JSON dates use standard invariant parsing when format is omitted; explicit formats use exact parsing without fallback, including bounds/options and scalar collections. DateTimeOffset requires/preserves an explicit offset; JSON DateTime rejects offsets.
- Accepted metadata uses default System.Text.Json serialization into immutable JsonElement snapshots and fresh typed copies; it is unavailable in validation/report hooks and single-pass execution, available from the first two-pass execution and successful completion. Serializable handwritten metadata properties are included; use JsonIgnore for transient fields. Reconciliation state/services/authentication are not metadata.
- One invocation-level errorMode applies to every format; do not add record-level CSV overrides. Shared FileCardinality describes CSV occurrences and JSON unit/collection cardinality.
- Use awaited individual diagnostics and reporting hooks, never retained file-wide error collections. Content errors return `ContentFailed`; operational faults/cancellation propagate.
- Consumer code owns source admission, input size, concurrency, timeouts, and report policy for CSV and JSONL. Do not add generic resource-quota knobs. Retain the built-in parser's maxDepth and business cardinality/optional explicit maxItems. CSV uses strict configured decoding; JSON validates strings only when consumed. No invalid-byte replacement, BOM-driven encoding switching, or guessed line numbers from property paths.
- Source identity is credential-free, attempt is separate from source revision, and neither is authenticated from CSV fields. Durable per-unit deduplication and mutation commit atomically in application persistence.
- For native Functions Blob imports, keep transport in the Functions host, pin both processor passes to the selected revision, and write reports to durable Blob storage. Use the supported application unit of work to commit the business mutation, receipt, and outbox atomically.

## Do Not

- Do not hand-author generated wire models, mappings, or validators when the File CodeGen definition owns them.
- Do not add transport SDKs or report-storage policies to CoreEx.IO; keep native Blob triggers thin and choose revision pinning, report isolation, and retry behavior at the host.
- Do not equate whole-file acceptance, report overwrite, or a transactional outbox write with exactly-once delivery.

## Further Reading

- [File processing skill](/.github/skills/coreex-file/SKILL.md) — generation and runtime workflow.
- [Functions capability skill](/.github/skills/coreex-functions/SKILL.md) — native Blob trigger and runtime-test workflow.
- [CoreEx.IO guide](/.github/docs/coreex/agents/CoreEx.IO.md) — runtime contracts and guarantees.
