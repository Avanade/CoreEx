---
description: "Guide to CoreEx agent skills and their workflow integration"
tags: ["skills", "workflow", "scaffolding", "domains"]
---

# CoreEx Agent Skills Workflow Guide

## Overview

CoreEx provides four complementary skills that support different phases of domain development and codebase exploration. This guide explains when to use each skill and how they integrate into your development workflow.

| Skill | Author | Purpose | Typical Use |
|-------|--------|---------|------------|
| `/generate-domain` | CoreEx Team | Scaffold a new domain from scratch | Greenfield domain creation |
| `/add-capability` | CoreEx Team | Add features to an existing domain | Post-generation feature enhancement |
| `/acquire-codebase-knowledge` | Awesome Copilot | Document and understand existing codebases | Onboarding and architecture discovery |
| `/aspire` | Aspire Team | Orchestrate distributed apps locally | Running and debugging Aspire apps |

---

## Skill Descriptions

### 1. `/generate-domain` — Greenfield Domain Scaffolding

**Purpose:** Create a new CoreEx domain from scratch following established patterns and templates.

**When to use:**
- Starting a new bounded context or microservice
- Creating a new domain entity (e.g., a new product line, tenant model, order type)
- You want to scaffold all five layers (Contracts, Application, Infrastructure, Api, Database) at once
- You prefer guided scaffolding with questions about your domain

**What it generates:**
- Contracts (DTOs with source generation markers)
- Application services with validation and unit-of-work patterns
- Infrastructure repositories with EF Core and data access
- API controllers with WebApi helpers
- Database projects with migrations and DbEx YAML
- Sample validators, mappers, and domain events

**Output characteristics:**
- Minimal viable product (MVP) focused
- Follows CoreEx conventions and patterns
- Ready to run and extend
- Includes standard error handling and validation

**Example workflow:**
```
User: Create a new Orders domain with Order and OrderItem entities
→ /generate-domain scaffolds the complete domain structure
→ User modifies generated code to add business logic
```

---

### 2. `/add-capability` — Post-Generation Enhancement

**Purpose:** Retrofit an existing CoreEx domain with additional capabilities and integrations.

**When to use:**
- Domain already exists (created with `/generate-domain` or manually)
- Adding event outbox and relay support
- Integrating with Azure Service Bus messaging
- Scaffolding event subscribers
- Adding cross-domain integration features
- Aligning an existing domain with messaging patterns

**What it adds:**
- Outbox configuration and migrations
- Service Bus integration
- Event publisher scaffolding
- Subscriber hosts and event handler templates
- Integration wiring in Program.cs files
- Deployment/infrastructure updates

**Prerequisites:**
- Domain must follow CoreEx project structure conventions
- Project naming must align with CoreEx patterns (`*.Contracts`, `*.Application`, etc.)
- If retrofitting existing code: must be compatible with CoreEx layering

**Example workflow:**
```
User has: Orders domain created with /generate-domain
→ /add-capability adds event outbox and Service Bus support
→ User creates domain event contracts
→ User implements event handlers in Subscribe project
```

---

### 3. `/acquire-codebase-knowledge` — Codebase Discovery & Documentation

**Purpose:** Map, document, and understand an existing codebase structure.

**When to use:**
- Onboarding to a new CoreEx repository or codebase
- Creating architectural documentation
- Understanding how an existing domain is structured
- Analyzing cross-domain dependencies
- Documenting layering and separation of concerns
- Planning a refactor or migration

**What it discovers:**
- Project structure and organization
- Dependency graphs and cross-project relationships
- Layer architecture (Contracts → Application → Infrastructure)
- Design patterns and conventions in use
- Integration points and messaging patterns
- Technology stack and framework choices

**Output characteristics:**
- Comprehensive codebase maps
- Architecture documentation
- Pattern identification
- Dependency analysis
- Best practice summaries

**Relationship to other skills:**
- Run this first if inheriting unfamiliar code
- Use output to inform `/add-capability` decisions
- Helps determine if existing code aligns with CoreEx patterns for `/add-capability`

**Example workflow:**
```
User: Onboard to existing Shopping domain
→ /acquire-codebase-knowledge documents the structure
→ User understands layering and patterns
→ User can then use /add-capability to extend the domain
```

---

### 4. `/aspire` — Distributed App Orchestration

**Purpose:** Run, manage, and debug Aspire distributed applications locally.

**When to use:**
- Starting the local development environment
- Debugging multiple services together
- Viewing Aspire dashboard and observability
- Managing service dependencies
- Adding new resources to orchestration
- Checking service health and logs

**What it handles:**
- `aspire start` / `aspire stop` commands
- Aspire dashboard access
- Service dependency resolution
- Container and local resource management
- OpenTelemetry and observability
- Resource status and health checks

**Integration:**
- Used after domain(s) are created and configured
- Works with domains scaffolded by `/generate-domain`
- Coordinates with domains enhanced by `/add-capability`
- Provides feedback on Service Bus, databases, and messaging

**Example workflow:**
```
User: Ready to test Orders and Shopping domains together
→ /aspire start orchestrates all services
→ /aspire opens dashboard to monitor
→ Services interact via Service Bus (added by /add-capability)
```

---

## Typical Development Workflows

### Workflow A: Greenfield Domain from Scratch

**Scenario:** Building a new domain (e.g., Payments, Reporting, Inventory).

```
1. /generate-domain
   ↓ (asks: entity name, business rules, events, database strategy)
   ↓ (scaffolds: all 5 layers with MVP structure)

2. Customize generated code
   ↓ (add business logic, validation rules, database schema refinements)

3. /add-capability (optional)
   ↓ (add: outbox, Service Bus, subscribers if cross-domain events needed)

4. /aspire
   ↓ (test domain in context of other services)
```

**Tools used sequentially:** `/generate-domain` → customize → `/add-capability` → `/aspire`

---

### Workflow B: Extending an Existing Domain

**Scenario:** Orders domain already exists; now add event publishing and subscribers.

```
1. /acquire-codebase-knowledge
   ↓ (understand: current structure, dependencies, patterns)

2. /add-capability
   ↓ (add: outbox events, Service Bus, subscriber scaffold)

3. Implement domain events and handlers
   ↓ (code: event contracts, event handlers, business logic)

4. /aspire
   ↓ (test: event flow, cross-domain integration)
```

**Tools used sequentially:** `/acquire-codebase-knowledge` → `/add-capability` → `/aspire`

---

## Decision Tree: Which Skill to Use

```
START: What do you need?

├─ I have an IDEA for a new domain or service
│  └─ → Use /generate-domain
│
├─ I have an EXISTING domain and want to ADD features
│  ├─ Does it follow CoreEx structure?
│  │  ├─ Yes → Use /add-capability
│  │  └─ No → Use /acquire-codebase-knowledge first, then decide
│  └─ Examples: add messaging, add events, add caching
│
├─ I'm NEW to a codebase and need to UNDERSTAND it
│  └─ → Use /acquire-codebase-knowledge
│
├─ I want to RUN the system locally or DEBUG multiple services
│  └─ → Use /aspire
│
└─ I want to PLAN a refactor or understand dependencies
   └─ → Use /acquire-codebase-knowledge
```

---

## Integration Points

### Between `/generate-domain` and `/add-capability`

- **Handoff:** `/generate-domain` creates the initial domain structure; `/add-capability` enhances it.
- **Compatibility:** `/add-capability` expects domains created by `/generate-domain` to have standard project naming and layering.
- **Timing:** Can be done immediately after generation or deferred until messaging is needed.

### Between `/acquire-codebase-knowledge` and `/add-capability`

- **Prerequisite:** Run `/acquire-codebase-knowledge` first if uncertain whether existing code aligns with CoreEx patterns.
- **Decision Making:** Output from `/acquire-codebase-knowledge` informs whether `/add-capability` is suitable.
- **Retrofit Readiness:** Helps determine if hand-written code can be extended with `/add-capability`.

### Between Domain Skills and `/aspire`

- **Execution Environment:** `/aspire` provides the runtime orchestration for domains created or extended by the other skills.
- **Observability:** `/aspire` dashboard visualizes event flows, messaging, and service health across domains scaffolded by `/generate-domain` and enhanced by `/add-capability`.
- **Testing Loop:** After `/generate-domain` or `/add-capability`, use `/aspire` to validate the implementation.

---

## Key Principles

1. **Layered Approach:** Skills are designed to be used in sequence—start with generation, then add capabilities, then orchestrate.

2. **Conventions Over Configuration:** All skills assume and enforce CoreEx project structure and naming patterns. Code created by these skills is interoperable.

3. **Non-Breaking:** Each skill can be skipped. Using `/generate-domain` does not require later use of `/add-capability`. Running a domain with `/aspire` does not require prior use of `/acquire-codebase-knowledge`.

4. **Scaffolding as Foundation:** Skills provide scaffolding and templates, not complete implementations. Customization and business logic still require developer input.

5. **CoreEx Alignment:** All four skills assume and respect CoreEx architecture (layering, validation, unit of work, event patterns, etc.).

---

## Common Scenarios and Recommended Skill Sequencing

### New Microservice: E-Commerce Domain

1. **Ideation:** Decide on Orders, Products, Payments domains
2. **Create Products:** `/generate-domain` → Products domain scaffold
3. **Enhance Products:** `/add-capability` → add outbox/events/Service Bus
4. **Create Orders:** `/generate-domain` → Orders domain scaffold
5. **Enhance Orders:** `/add-capability` → add outbox/events/Service Bus
6. **Create Payments:** `/generate-domain` → Payments domain scaffold
7. **Enhance Payments:** `/add-capability` → add outbox/events/Service Bus
8. **Integrate:** `/aspire` → run all three together, validate event flow

**Skills in sequence:** `/generate-domain` × 3, `/add-capability` × 3, `/aspire`

---

### Migrating Existing Codebase to CoreEx

1. **Assess:** `/acquire-codebase-knowledge` → understand current structure
2. **Compare:** Review generated map against CoreEx expectations
3. **Decide:** Retrofit possible? Use `/add-capability`? Or rebuild with `/generate-domain`?
4. **Extend:** `/add-capability` → add messaging/integration if retrofitting works
5. **Validate:** `/aspire` → test migrated domain in new environment

**Skills in sequence:** `/acquire-codebase-knowledge`, (optional: `/add-capability` or `/generate-domain`), `/aspire`

---

### Team Onboarding to Contoso Sample

1. **Orient:** `/acquire-codebase-knowledge` → document Contoso structure
2. **Review:** Generated map shows Products, Shopping, Orders layers
3. **Explore:** `/aspire` → run sample locally, observe services
4. **Propose:** Team proposes new domain → `/generate-domain` → scaffold new domain
5. **Enhance:** New domain needs messaging → `/add-capability` → add event support
6. **Validate:** `/aspire` → new domain integrates with existing services

**Skills in sequence:** `/acquire-codebase-knowledge`, `/aspire`, `/generate-domain`, `/add-capability`, `/aspire`

---

## FAQ

**Q: Can I use `/add-capability` without first using `/generate-domain`?**

A: Yes, if your existing domain follows CoreEx structure conventions:
- Project naming: `*.Contracts`, `*.Application`, `*.Infrastructure`, `*.Api`, `*.Database`
- Layering: Contracts → Application → Infrastructure separation
- Patterns: Uses CoreEx validators, repositories, unit of work, etc.

If uncertain, run `/acquire-codebase-knowledge` first to assess alignment.

---

**Q: Should I use `/acquire-codebase-knowledge` before `/generate-domain`?**

A: Only if you're uncertain about domain design or CoreEx patterns. For greenfield scenarios, `/generate-domain` is a faster start. Use `/acquire-codebase-knowledge` when learning from existing samples or migrating legacy code.

---

**Q: What if I only want to scaffold contracts and test with `/aspire`, skipping the full domain?**

A: `/generate-domain` is all-or-nothing for a complete domain. For partial scaffolding, you'll need to code by hand following CoreEx conventions or ask the CoreEx Expert agent for guidance on minimal structure.

---

**Q: Can multiple domains share a database?**

A: Yes. `/generate-domain` and `/add-capability` default to per-domain databases, but you can manually merge database projects post-generation. Review the Contoso sample for examples of shared infrastructure.

---

**Q: How do I know if my existing code is compatible with `/add-capability`?**

A: Run `/acquire-codebase-knowledge` first and review the output. Look for:
- Standard project structure (5 layers)
- CoreEx naming conventions
- Use of `IUnitOfWork`, validators, repositories
- Absence of conflicting frameworks (e.g., conflicting IoC patterns)

If alignment is unclear, ask the CoreEx Expert agent.

---

## See Also

- [CoreEx Capabilities](capabilities.md) — Detailed feature guide
- [Application Scaffolding Guide](application-scaffolding-guide.md) — Deep dive on domain structure
- [Orchestration Guide](orchestration.md) — Aspire and distributed app patterns
- [Agent Interaction Guide](agent-interaction-guide.md) — How to interact with CoreEx agents effectively
