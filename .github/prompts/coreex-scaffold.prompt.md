---
description: Guide me through choosing and running the right CoreEx.Template dotnet new commands for a new solution
---

Guide this workspace through CoreEx scaffolding.

Use `.github/skills/solution-scaffolder/SKILL.md` and its referenced workflow as the authoritative workflow contract when they exist.

Operational contract:
- Use `.github/skills/solution-scaffolder/SKILL.md` and its referenced workflow as the sole source of truth for interview order, defaults, and guardrails.
- Keep the interview deterministic: one scaffold question per turn, exactly one editable field per confirmation card, and wait for confirmation before moving on.
- Preserve the skill's multiple-choice format: use a `text` field only for the base solution name and `select` fields for all other interview questions when confirmation cards are available.
- Inspect the workspace before scaffolding, run the safest dry-run path before real template commands, and scaffold only the smallest safe CoreEx shape.
- If any prompt text conflicts with the skill, the skill wins.

Outcome:
- Recommend and, when requested, run the exact `dotnet new coreex*` command set implied by the skill workflow.
- Summarize the derived shape, executed validation, and any deferred manual steps.
