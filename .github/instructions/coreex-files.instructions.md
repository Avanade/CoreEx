---
applyTo: "**/*-file.yaml;**/Files/**/*.cs;!**/Files/**/*.g.cs"
description: "CoreEx.IO file ingestion invariants: YAML generation, streamed diagnostics, immutable replay, scoped execution and caller-owned idempotency."
tags: ["files", "csv", "codegen", "io"]
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
| `CoreEx.IO` | Host-neutral processing contracts, CSV mapping/runtime, scoped unit execution, diagnostics and reconciliation. |
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
- Use `limits` for common budgets and `csv` for CSV dialect/routing settings. Property `name` is the exact source field; `property` optionally overrides the derived CLR name, and positional selection belongs under `csv.index`. File-level metadata uses the shared `metadata` collection; do not put header/trailer metadata under `csv.structure`. For unordered CSV, First/Last routes remain executable units unless explicitly referenced by `metadata`; a declared metadata route is counted as a source record, never as a business unit.
- Flat named mappings may select `CsvHeaderPolicy.Exact` to reject extra header columns and data rows with a field count different from the header; it requires all mapped selectors to be named and remains opt-in for backward compatibility.
- Use awaited individual diagnostics and reporting hooks, never retained file-wide error collections. `MaxDiagnostics` caps emitted diagnostic events (errors, warnings, and information), not unit-report callbacks. Content errors return `ContentFailed`; operational faults/cancellation propagate.
- Set explicit input/group budgets and strict encoding. No invalid-byte replacement, BOM-driven encoding switching, or guessed line numbers from property paths.
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
