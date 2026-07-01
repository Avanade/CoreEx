---
mode: agent
description: "Add or modify a DDD domain object (aggregate root, entity, or value object) in a CoreEx Domain layer. Interviews the developer to determine which applies, then follows the CoreEx.DomainDriven pattern."
---

Use the `coreex-aggregate` skill to add or modify a domain object in this CoreEx `*.Domain` project.

Read `.github/skills/coreex-aggregate/SKILL.md` first, confirm a Domain layer is warranted (Phase 0),
interview to determine aggregate root vs entity vs value object (Phase 1), then follow
`.github/skills/coreex-aggregate/references/workflow.md` step by step.

User request: ${input}
