---
name: coreex-onboard
description: "Scaffold CoreEx Copilot instruction and agent markdown files into a repository from bundled plugin templates."
argument-hint: "Optional: mode (safe or force) and whether to overwrite existing files."
tags: ["coreex", "onboarding", "bootstrap", "instructions", "scaffolding"]
---

# CoreEx Onboard

Scaffolds CoreEx AI guidance files into a repository so Copilot behavior aligns to CoreEx standards from day one.

## When to Use

- A repository is empty or missing CoreEx Copilot guidance files.
- You installed `coreex-agent-pack` and want `.github` guidance files materialized in-repo.
- You want a repeatable way to bootstrap CoreEx instructions across multiple repositories.

## When Not to Use

- You only need one file tweaked manually; edit that file directly instead.
- You are upgrading repository code, not onboarding Copilot guidance.
- You need to scaffold solution/domain hosts; use `solution-scaffolder` after onboarding.

## Workflow Overview

1. Detect the repository root and verify write access to `.github/`.
2. Compare bundled templates with existing target files.
3. Apply in **safe mode** by default: create missing files only; do not overwrite.
4. If the user requests overwrite, apply **force mode** for selected files.
5. Write/update `.github/coreex-bootstrap.json` with source plugin and timestamp.
6. Summarize created, skipped, and overwritten files.

For step-by-step guidance, see [the workflow guide](references/workflow.md).
For completion gates, see [the checklist guide](references/checklists.md).

## Quick Reference

| Source template path | Target path |
|---|---|
| `assets/templates/.github/copilot-instructions.md` | `.github/copilot-instructions.md` |
| `assets/templates/.github/instructions/*.instructions.md` | `.github/instructions/*.instructions.md` |
| `assets/templates/.github/agents/coreex-expert.agent.md` | `.github/agents/coreex-expert.agent.md` |

Bootstrap marker format:

```json
{
  "bootstrapVersion": "1.1.0",
  "plugin": "coreex-agent-pack",
  "skill": "coreex-onboard",
  "mode": "safe",
  "updatedAtUtc": "2026-01-01T00:00:00Z"
}
```

## Key References

- [CoreEx Copilot Instructions](assets/templates/.github/copilot-instructions.md)
- [CoreEx Instruction Templates](assets/templates/.github/instructions/)
- [CoreEx Expert Agent Template](assets/templates/.github/agents/coreex-expert.agent.md)
- [Skill workflow](references/workflow.md)
