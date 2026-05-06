---
name: coreex.plan
description: "Create a codex-style ExecPlan in PLAN.md before implementation starts. Use when you want the agent to ask clarifying questions, scaffold a self-contained plan, require user approval, and only then begin execution in a later step."
argument-hint: "Feature or change to plan, e.g. 'Add multi-tenancy to Orders domain'"
agent: agent
tools: ['create', 'read', 'search', 'todo']
---

Create or refresh a repository-root `PLAN.md` as a codex-style ExecPlan for the requested feature.

## Purpose

Use this prompt to produce an implementation-ready plan before any code changes begin. The output must be detailed enough that a capable but repo-new engineer can execute it without guessing at architecture, file locations, validation, or acceptance criteria.

Follow the reusable workflow and template in [coreex-exec-plan](../skills/coreex-exec-plan/SKILL.md).

## Required Workflow

1. Ask targeted clarifying questions only for decisions that materially change architecture, scope, or validation.
2. Inspect the local codebase just enough to orient the plan around the real owning projects, layers, and conventions.
3. Scaffold `PLAN.md` using the template at [PLAN template](../skills/coreex-exec-plan/assets/templates/PLAN.template.md).
4. Fill every required section with concrete repo-specific detail, including exact projects, likely files, commands, tests, risks, and decision points.
5. Validate the plan against the checklist in [self-contained checklist](../skills/coreex-exec-plan/references/checklists.md) and tighten any vague steps.
6. Stop after the plan is written. Do not implement code from this prompt.
7. Tell the user that implementation should start only after they review and approve `PLAN.md`.

## Output Requirements

- The resulting `PLAN.md` must read like an ExecPlan, not a brainstorm.
- Follow the Codex section order and keep narrative sections prose-first; use checklists only in `Progress`.
- Prefer concrete commands, file paths, interfaces, dependencies, and validation steps over general advice.
- Include explicit assumptions where information is still unknown.
- Define non-obvious terms in plain language where they first appear.
- Leave `Progress`, `Decision Log`, `Surprises & Discoveries`, `Outcomes & Retrospective`, and `Revision Note` as live sections ready for later updates.
- Include `Validation and Acceptance`, `Idempotence and Recovery`, `Artifacts and Notes`, and `Interfaces and Dependencies` in the generated plan.
- If the user request is underspecified, ask questions first instead of scaffolding a speculative plan.

## Completion Gate

Finish only when:

- `PLAN.md` exists at the repository root.
- The plan is self-contained per the checklist.
- The final response asks the user to review and approve the plan before any implementation begins.