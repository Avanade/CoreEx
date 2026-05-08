---
name: generate-domain
description: "Generate a new CoreEx domain or microservice. Use when: scaffolding a new domain, creating a new microservice, adding a new bounded context, generating sample domain code like shopping or product, creating contracts/application/infrastructure/API/database layers from scratch following CoreEx conventions."
argument-hint: "Optional: solution prefix, domain name, and root entity — e.g. 'Contoso Orders Order'"
tags: ["scaffolding", "microservice", "bounded-context", "code-generation", "layering", "exec-plan"]
---

# Generate Domain

Scaffolds all layers of a new CoreEx domain — Contracts, Application, Infrastructure, API, Database, and baseline Unit/Api tests — aligned to the Contoso sample architecture. Work begins with an approval checkpoint after a domain generation plan is created.

## When to Use

- Scaffolding a new microservice or bounded context from scratch.
- Generating domain code that follows CoreEx conventions (ETag, ChangeLog, Outbox, FusionCache, NSwag).
- Producing code that mirrors the Shopping or Product sample domains.

## Workflow Overview

This skill integrates an exec-plan checkpoint before code generation:

1. **Phase 0: Validate Intent and Create Plan** — Gather inputs, clarify requirements, and scaffold a domain-generation plan in `.agent/execplans/`.
2. **Approval Checkpoint** — Present the plan to the user for review and approval.
3. **Phase 1–8: Execute** — Generate all layers (Contracts through Tests) following the approved plan.
4. **Phase 9: Validate & Document** — Run quality gates, update the plan with results, and mark complete.

For complete step-by-step workflow covering all phases and validation gates, see [`references/workflow.md`](references/workflow.md).

## Inputs Required

The skill will ask for any missing values. Before starting, consider:

| Input | Example |
|-------|---------|
| Solution prefix | `Contoso` |
| Domain name | `Orders` |
| Root entity | `Order` |
| Fields, ref-data codes, operations, event subjects | Confirm before approving plan |

## Key References

- Exec-plan template: `/.github/skills/coreex-exec-plan/assets/templates/PLAN.template.md`
- Domain-generation plan template: `/.github/skills/generate-domain/assets/templates/DOMAIN.PLAN.template.md`
- All instruction files: `/.github/instructions/*.instructions.md`
- Domain templates: `/.github/templates/domain/**`
- Sample domains: `samples/src/Contoso.Products/`, `samples/src/Contoso.Shopping/`
- Active plans index: `.agent/PLANS.md`
