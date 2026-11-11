---
name: coreex-file
description: "Create or modify a CoreEx.IO CSV, JSON document, or nested JSONL ingestion definition, generated processor, scoped handler, streamed reports, or immutable local/Blob adapter. Supports CSV flat/routed/hierarchical input, JSON document array/root-object selection, and JSONL object units with optional metadata routing; not XML readers, HTTP APIs or reference-data generation."
argument-hint: "[file definition or ingestion capability]"
tags: ["files", "csv", "jsonl", "codegen", "io"]
---

# CoreEx File Processing

Build host-neutral typed CSV, JSON document, and nested JSONL integrations generation-first with File CodeGen and explicit application-owned execution.

## When to Use

- Create/change a `*-file.yaml` definition, wire mapping, validation or routed hierarchy.
- Stream a JSON document with literal JSON Pointer unit and metadata selectors.
- Add scoped business execution, reconciliation, or streamed reporting.
- Connect an immutable stream factory to a local/Blob ingestion adapter.
- Generate a CSV/JSON/JSONL contract/processor for a native Azure Functions Blob-triggered import.

## When Not to Use

- Reference-data entity generation: use `coreex-refdata`.
- Empty Functions hosts: use `coreex-scaffold`; native trigger composition/testing: use `coreex-functions`.
- Functions trigger, storage binding, or runtime topology: use `coreex-functions` alongside this skill.
- XML readers: not implemented.

## Quick Reference

```powershell
Set-Location <CodeGen-project-directory>
dotnet run -- file --validate
dotnet run -- file
dotnet run -- file suppliers-file.yaml
dotnet run -- file --expect-no-changes
```

Start with a checked-in top-level `*-file.yaml`; generate wire types, mappings, validators, contexts, processors, and a create-only handler rather than hand-authoring those artifacts. Run commands with the CodeGen console as the current working directory: `dotnet run --project <path>` selects the project to run but does not change the working directory used for definition discovery. Runtime artifacts belong in an existing project referencing CoreEx.IO and CoreEx.Validation; CodeGen is design-time only. Models are nullable wire values, and unimplemented execution fails explicitly.

Reuse the existing console, or use `coreex-scaffold` to add the explicitly requested `coreex-codegen` tool. File generation requires neither reference data nor a database; do not create business layers to acquire tooling. YAML selects the runtime output project, not a tool-to-host ProjectReference.

For the step-by-step workflow, see [references/workflow.md](references/workflow.md); for host/storage boundaries, see [references/patterns.md](references/patterns.md).

## Key References

- [File invariants](/.github/instructions/coreex-files.instructions.md).
- [Runtime guide cache](/.github/docs/coreex/agents/CoreEx.IO.md).
- [Generator guide cache](/.github/docs/coreex/agents/CoreEx.CodeGen.md).
- [File YAML schema](/.github/docs/coreex/schema/coreex-files.json).
- [Configuration reference](/.github/docs/coreex/files/CodeGeneration.md).
- [CSV upgrade guidance](/.github/docs/coreex/files/MIGRATION.md).
- [coreex-functions](../coreex-functions/SKILL.md) — native Blob trigger, SDK storage, and Functions runtime tests.
