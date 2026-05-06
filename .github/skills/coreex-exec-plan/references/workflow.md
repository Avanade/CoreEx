# Workflow

Use this workflow when producing `PLAN.md` for a CoreEx change.

## 1. Clarify Before Planning

Ask only for missing decisions that materially change the plan. Typical examples:

- User outcome: what changes for an API consumer, operator, or developer?
- Architecture boundary: which bounded context, host, or layer owns the change?
- Data shape: schema changes, migration needs, compatibility constraints.
- Integration dependencies: downstream APIs, events, Service Bus, database ownership.
- Rollout constraints: backward compatibility, incremental delivery, feature flags.

Do not ask broad discovery questions that can be answered by inspecting the repo.

## 2. Orient Locally

Inspect only enough code to ground the plan:

- Identify the owning solution, projects, and layers.
- Confirm whether similar patterns already exist in samples or framework libraries.
- Capture likely entry points, validators, repositories, contracts, hosts, and tests.
- Note area-specific instruction files that the later implementation must follow.

The plan must reference real projects and likely files, not abstract placeholders, unless the location is genuinely unknown.

## 3. Scaffold the ExecPlan

Create or refresh `PLAN.md` at the repository root using the template.

Fill these sections completely:

- `Purpose / Big Picture`
- `Progress`
- `Surprises & Discoveries`
- `Decision Log`
- `Outcomes & Retrospective`
- `Context and Orientation`
- `Plan of Work`
- `Concrete Steps`
- `Validation and Acceptance`
- `Idempotence and Recovery`
- `Artifacts and Notes`
- `Interfaces and Dependencies`
- `Revision Note`

Write the plan in prose-first Codex style. Outside the `Progress` section, avoid checklist-heavy formatting, tables, and long enumerations unless they materially improve clarity. If the plan is written into `PLAN.md` as the only document content, do not wrap it in triple backticks.

## 4. Make the Plan Executable

The plan should enable later implementation without another planning pass.

Required characteristics:

- State the intended user-visible outcome.
- Explain the current repo structure relevant to the work.
- Break work into ordered stages or milestones with concrete deliverables and observable proof.
- Include likely file and project touchpoints.
- Include exact or near-exact commands for build, test, and validation.
- Capture assumptions, open questions, and decision points explicitly.
- Define non-obvious terms in plain language where they are first used.
- Include idempotence, retry, rollback, or cleanup guidance when steps could fail halfway.
- Prefer behavioral acceptance criteria over purely structural criteria.
- Prefer reversible, incremental changes over large risky jumps.

## 5. Self-Review the Plan

Before finishing, test the draft mentally against this standard:

- Could a repo-new engineer implement this without guessing where to start?
- Would they know what success looks like?
- Are the validation steps sufficient to catch regressions?
- Are unknowns called out instead of hidden in vague wording?
- Does the plan stand on its own without external docs or prior conversation context?

If not, tighten the plan before handing it off.

## 6. Stop at Approval Boundary

Do not implement from this workflow. The next action after writing `PLAN.md` is to ask the user to review and approve it.