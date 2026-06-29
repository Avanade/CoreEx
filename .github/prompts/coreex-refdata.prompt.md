---
description: Add or modify a reference data type in a CoreEx domain — database migration, seed rows, dbex.yaml, CoreEx CodeGen, and contract wiring
---

Guide this workspace through a CoreEx reference data change.

Use `.github/skills/coreex-refdata/SKILL.md` and its referenced workflow as the authoritative workflow contract when they exist.

Operational contract:
- There are two completely separate `dotnet run` steps in two different projects: `*.Database` (schema + EF models) and `*.CodeGen` (contracts + services + repos). Never conflate them.
- Bring the database up to date (`dotnet run -- database`) and inspect the table before authoring any script — this is a hard gate.
- Never include the `{Name}Id` identifier in seed rows — the `$^` prefix auto-generates it.
- Never edit `.g.cs` files — fix `ref-data.yaml` or `dbex.yaml` and regenerate.
- Surface verbatim error output on any `dotnet run` failure — do not work around it by editing generated files.
- If any prompt text conflicts with the skill, the skill wins.

Outcome:
- The ref-data type exists in the database (migration applied), has seed rows, is registered in `dbex.yaml`, and has fully generated `.g.cs` artefacts across Contracts, Application, Infrastructure, and Api layers.
- The solution builds cleanly.
