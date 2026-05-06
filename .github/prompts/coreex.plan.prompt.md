---
name: coreex.plan
description: "Create a codex-style ExecPlan in .agent/execplans/ before implementation starts. Use when you want the agent to ask clarifying questions, scaffold a self-contained plan, require user approval, and only then begin execution in a later step."
argument-hint: "Feature or change to plan, e.g. 'Add multi-tenancy to Orders domain'"
agent: agent
tools: ['create', 'read', 'search', 'todo']
---

Create a codex-style ExecPlan in `.agent/execplans/{plan-name}.md` and maintain an index in `.agent/PLANS.md`.

## Purpose

Use this prompt to produce an implementation-ready plan before any code changes begin. The plan is stored in `.agent/execplans/` alongside an index at `.agent/PLANS.md`. The output must be detailed enough that a capable but repo-new engineer can execute it without guessing at architecture, file locations, validation, or acceptance criteria.

Follow the reusable workflow and template in [coreex-exec-plan](../skills/coreex-exec-plan/SKILL.md).

## Required Workflow

1. If the user's request is underspecified, ask targeted clarifying questions only for decisions that materially change architecture, scope, or validation.
2. Derive a short, URL-friendly plan name from the feature (e.g. "multi-tenancy-orders" from "Add multi-tenancy to Orders domain").
3. Inspect the local codebase just enough to orient the plan around the real owning projects, layers, and conventions.
4. Scaffold the plan file using the template at [PLAN template](../skills/coreex-exec-plan/assets/templates/PLAN.template.md) and save it to `.agent/execplans/{plan-name}.md`.
5. Fill every required section with concrete repo-specific detail, including exact projects, likely files, commands, tests, risks, and decision points.
6. Validate the plan against the checklist in [self-contained checklist](../skills/coreex-exec-plan/references/checklists.md) and tighten any vague steps.
7. Update or create `.agent/PLANS.md` as an index that lists all plans with their descriptions and status.
8. Stop after the plan is written. Do not implement code from this prompt.
9. Tell the user that implementation should start only after they review and approve the plan.

## Output Requirements

- The resulting plan must read like an ExecPlan, not a brainstorm.
- Follow the Codex section order and keep narrative sections prose-first; use checklists only in `Progress`.
- Use repository-relative file paths from repo root (e.g., `samples/src/...`), never absolute paths. Different developers may have the repo cloned in different locations.
- Prefer concrete commands, file paths, interfaces, dependencies, and validation steps over general advice.
- Include explicit assumptions where information is still unknown.
- Define non-obvious terms in plain language where they first appear.
- Leave `Progress`, `Decision Log`, `Surprises & Discoveries`, `Outcomes & Retrospective`, and `Revision Note` as live sections ready for later updates.
- Include `Validation and Acceptance`, `Idempotence and Recovery`, `Artifacts and Notes`, and `Interfaces and Dependencies` in the generated plan.
- Update `.agent/PLANS.md` to include the new plan in the index.

## Completion Gate

Finish only when:

- The plan file exists at `.agent/execplans/{plan-name}.md`.
- `.agent/PLANS.md` has been created or updated with an entry for the new plan.
- The plan is self-contained per the checklist.
- The final response includes the path to the plan file and asks the user to review and approve it before any implementation begins.