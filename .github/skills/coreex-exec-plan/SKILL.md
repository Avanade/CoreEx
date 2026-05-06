---
name: coreex-exec-plan
description: "Produce codex-style ExecPlans for CoreEx changes. Use when planning a feature, refactor, migration, or cross-layer change that should be captured in PLAN.md before implementation begins."
argument-hint: "Feature or outcome to plan, e.g. 'Add multi-tenancy to Orders domain'"
tags: ["planning", "exec-plan", "workflow", "documentation", "coreex"]
user-invocable: false
---

# CoreEx Exec Plan

Creates a repository-specific ExecPlan in `PLAN.md` before any implementation starts.

## When to Use

- Planning a non-trivial feature, refactor, migration, or architecture change.
- You want the agent to ask clarifying questions before writing the plan.
- You want a durable `PLAN.md` that can drive later implementation and progress tracking.
- The change spans multiple layers, hosts, projects, or validation steps.

## When Not to Use

- Tiny one-file edits that do not need a durable plan.
- Pure codebase onboarding or architecture mapping with no implementation outcome.
- Immediate implementation work where the user explicitly does not want a planning pass.

## Workflow Overview

1. Clarify the user outcome, major constraints, and dependencies.
2. Inspect the local codebase just enough to anchor the plan in real files and conventions.
3. Scaffold `PLAN.md` from [the template](./assets/templates/PLAN.template.md).
4. Fill every required section with concrete implementation and validation details.
5. Check the draft against [the self-contained checklist](./references/checklists.md).
6. Hand the plan to the user for approval before implementation starts.

For the detailed authoring procedure, see [workflow](./references/workflow.md).

## Key References

- [Application Service Instructions](/.github/instructions/application-services.instructions.md)
- [Repository Instructions](/.github/instructions/repositories.instructions.md)
- [Host Setup Instructions](/.github/instructions/host-setup.instructions.md)
- [Tests Instructions](/.github/instructions/tests.instructions.md)
- [CoreEx capabilities](../../../docs/capabilities.md)