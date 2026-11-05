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
- Generated processors expose protected `Services` for invocation-scoped coordinator dependencies used by handwritten partial hooks. Keep unit business dependencies in the generated handler's fresh child scope; do not resolve them from the processor provider.
- Generated wire values are nullable. `required` emits NotNull, permitting zero/false; `mandatory` rejects defaults. Rules use one fluent chain per property. Reference data remains a string code, with preinitialized lookup collections.
- `ProcessAsync` owns streams opened by a cancellation-aware factory. The caller guarantees identical immutable bytes for validation and replay. No implicit buffering/spooling.
- Whole-file validation, including EOF reconciliation, precedes every execution. Replay creates fresh models and pass state; business execution uses independent scopes and returns the original Result. `ValidateAndReport` is report-only: its awaited valid-unit hook runs after mapping, pure validation, and scoped custom validation succeed, but before EOF acceptance; later errors yield unsuccessful `ReportedWithErrors`.
- Flat named mappings may select `CsvHeaderPolicy.Exact` to reject extra header columns; it requires all mapped selectors to be named and remains opt-in for backward compatibility.
- Use awaited individual diagnostics and reporting hooks, never retained file-wide error collections. Content errors reject validation (or produce the unsuccessful `ReportedWithErrors` report-only outcome); operational faults/cancellation propagate.
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
