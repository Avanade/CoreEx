---
name: generate-domain
description: "Generate a new CoreEx domain or microservice. Use when: scaffolding a new domain, creating a new microservice, adding a new bounded context, generating sample domain code like shopping or product, creating contracts/application/infrastructure/API/database layers from scratch following CoreEx conventions."
argument-hint: "Optional: solution prefix, domain name, and root entity — e.g. 'Contoso Orders Order'"
tags: ["scaffolding", "microservice", "bounded-context", "code-generation", "layering"]
---

# Generate Domain

Scaffolds all layers of a new CoreEx domain — Contracts, Application, Infrastructure, API, Database, and baseline Unit/Api tests — aligned to the Contoso sample architecture.

## When to Use

- Scaffolding a new microservice or bounded context from scratch.
- Generating domain code that follows CoreEx conventions (ETag, ChangeLog, Outbox, FusionCache, NSwag).
- Producing code that mirrors the Shopping or Product sample domains.

## Inputs Required

Before generating, confirm with user:

| Input | Example |
|-------|---------|
| Solution prefix | `Contoso` |
| Domain name | `Orders` |
| Root entity | `Order` |
| Fields, ref-data codes, operations, event subjects | Confirm before generating |

## Workflow Overview

For complete step-by-step workflow covering all 8 phases (Contracts, Application, Infrastructure, API, Database, Tests, Quality Gates, Naming), see [`references/workflow.md`](references/workflow.md).

## Workflow Overview

For complete step-by-step workflow covering all 8 phases (Contracts, Application, Infrastructure, API, Database, Tests, Quality Gates, Naming), see [`references/workflow.md`](references/workflow.md).

## Key References

- All instruction files: `/.github/instructions/*.instructions.md`
- Templates: `/.github/templates/domain/**`
- Checklist: `DomainScaffold.checklist.md`
- Sample domains: `samples/src/Contoso.Products/`, `samples/src/Contoso.Shopping/`
