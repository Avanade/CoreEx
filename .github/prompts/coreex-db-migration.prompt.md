---
description: Add or change a database table for a CoreEx domain — scaffold migration script, update dbex.yaml, apply, and regenerate EF persistence models
---

Guide this workspace through a CoreEx database schema change.

Use `.github/skills/coreex-db-migration/SKILL.md` and its referenced workflow as the authoritative workflow contract when they exist.

Operational contract:
- Inspect the database state before authoring any script — this is a hard gate.
- Use `.github/skills/coreex-db-migration/references/workflow.md` as the sole source of truth for the decision tree, SQL column templates, dbex.yaml rules, and guardrails.
- Ask one question at a time; never batch schema decisions into a single message.
- Surface verbatim error output on any `dotnet run` failure — do not edit generated files to work around failures.
- If any prompt text conflicts with the skill, the skill wins.

Outcome:
- Author and apply the correct migration script for the target table and change type (create, refdata, alter, or non-entity).
- Confirm `*.g.cs` Infrastructure persistence models are regenerated, and the solution builds cleanly.
