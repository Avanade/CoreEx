---
description: "Guide to CoreEx templates, agent skills, and how they fit together"
tags: ["skills", "workflow", "templates", "domains"]
---

# CoreEx Templates and Agent Workflow Guide

## Overview

CoreEx now uses the `CoreEx.Template` `dotnet new` template pack for deterministic scaffolding. Agent skills remain useful for codebase discovery, architecture guidance, and local orchestration. This guide explains which tool to use for each part of the workflow.

| Tool | Type | Purpose | Typical Use |
|------|------|---------|------------|
| `CoreEx.Template` | `dotnet new` template pack | Scaffold solution and host projects deterministically | Greenfield solution and host creation |
| `CoreEx Expert` | Agent | Review architecture choices and recommend CoreEx-aligned patterns | Shape selection, design review, capability planning |
| `/acquire-codebase-knowledge` | Skill | Document and understand an existing codebase | Onboarding and architecture discovery |
| `/aspire` | Skill | Orchestrate distributed apps locally | Running and debugging Aspire apps |

---

## Tool Descriptions

### 1. `CoreEx.Template` — Deterministic Scaffolding

**Purpose:** Create a new CoreEx solution and optional API, relay, or subscriber hosts using deterministic templates.

**When to use:**
- Starting a new bounded context or microservice
- Creating the initial layered project structure
- Adding a standard API, relay, or subscriber host to a new solution
- You want repeatable output that matches the template pack exactly

**What it creates:**
- `dotnet new coreex` scaffolds the shared solution shape
- `dotnet new coreex-api` adds an API host
- `dotnet new coreex-relay` adds an outbox relay host
- `dotnet new coreex-subscribe` adds a subscriber host

**Output characteristics:**
- Deterministic and repeatable
- Aligned to current CoreEx conventions
- Best for greenfield scaffolding, not for retrofitting existing code

**Example workflow:**
```
dotnet new install CoreEx.Template
dotnet new coreex -n Avanade.Erp.Orders
dotnet new coreex-api -n Avanade.Erp.Orders.Api
```

---

### 2. `CoreEx Expert` — Shape and Capability Guidance

**Purpose:** Review a requirement or existing implementation and recommend the right host shape, capability set, and implementation path.

**When to use:**
- Deciding between API-only, API plus relay, API plus subscriber, or orchestration
- Planning a new domain before running `dotnet new`
- Retrofitting messaging or subscriber capabilities into an existing domain
- Comparing your use case to Products, Shopping, or other samples

**What it helps with:**
- Determines which `coreex*` templates you need
- Identifies what can be scaffolded versus what must be implemented manually
- Produces sample-aligned plans for retrofit work
- Calls out risks, tradeoffs, and missing prerequisites

**Best practice:**
- Ask it to inspect the current domain first
- Ask for the smallest safe change
- Ask it to cite the closest sample or instruction file

**Example workflow:**
```
User: Inspect this domain and tell me whether I need API only, API + relay, or API + subscribe.
→ CoreEx Expert reviews the current code and samples
→ User runs the needed `dotnet new` templates or makes the recommended manual changes
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
- Use output to inform retrofit decisions and template selection
- Helps determine if existing code aligns with CoreEx patterns before manual extension

**Example workflow:**
```
User: Onboard to existing Shopping domain
→ /acquire-codebase-knowledge documents the structure
→ User understands layering and patterns
→ User can then plan manual extensions or new hosts with better context
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
- Works with solutions scaffolded by `CoreEx.Template`
- Validates domains that were extended manually using repo conventions
- Provides feedback on Service Bus, databases, and messaging

**Example workflow:**
```
User: Ready to test Orders and Shopping domains together
→ /aspire start orchestrates all services
→ /aspire opens dashboard to monitor
→ Services interact via Service Bus after the required wiring is implemented
```

---

## Typical Development Workflows

### Workflow A: Greenfield Domain from Scratch

**Scenario:** Building a new domain (e.g., Payments, Reporting, Inventory).

```
1. Ask CoreEx Expert for shape guidance
   ↓ (decide: API-only, relay, subscriber, orchestration)

2. Run `dotnet new coreex` and the required `coreex*` host templates
   ↓ (scaffold the deterministic baseline)

3. Customize generated code
   ↓ (add business logic, contracts, validation rules, and persistence details)

4. /aspire
   ↓ (test domain in context of other services)
```

**Tools used sequentially:** `CoreEx Expert` → `dotnet new coreex*` → customize → `/aspire`

---

### Workflow B: Extending an Existing Domain

**Scenario:** Orders domain already exists; now add event publishing and subscribers.

```
1. /acquire-codebase-knowledge
   ↓ (understand: current structure, dependencies, patterns)

2. Ask CoreEx Expert for the smallest retrofit plan
   ↓ (identify: outbox events, Service Bus wiring, subscriber host needs)

3. Implement domain events and handlers
   ↓ (code: event contracts, event handlers, business logic)

4. /aspire
   ↓ (test: event flow, cross-domain integration)
```

**Tools used sequentially:** `/acquire-codebase-knowledge` → `CoreEx Expert` → implement → `/aspire`

---

## Decision Tree: Which Tool to Use

```
START: What do you need?

├─ I need to CREATE a new solution or host
│  └─ → Use CoreEx.Template (`dotnet new coreex*`)
│
├─ I have an EXISTING domain and want to ADD features
│  ├─ Unsure what shape or files should change?
│  │  └─ → Use CoreEx Expert first
│  └─ If the codebase is unfamiliar → Use /acquire-codebase-knowledge first
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

### Between `CoreEx.Template` and `CoreEx Expert`

- **Handoff:** `CoreEx Expert` helps choose the smallest viable shape; `CoreEx.Template` creates the deterministic baseline.
- **Compatibility:** Templates are for greenfield scaffolding; the agent is for decisions and manual retrofit guidance.
- **Timing:** Ask for advice first when the host shape is unclear.

### Between `/acquire-codebase-knowledge` and `CoreEx Expert`

- **Prerequisite:** Run `/acquire-codebase-knowledge` first if you are inheriting unfamiliar code.
- **Decision Making:** Its output makes retrofit recommendations more concrete and evidence-based.
- **Retrofit Readiness:** Helps determine whether existing code can be extended cleanly or needs more restructuring.

### Between Templates or Manual Changes and `/aspire`

- **Execution Environment:** `/aspire` provides the runtime orchestration for domains created or extended by the other tools.
- **Observability:** `/aspire` dashboard visualizes event flows, messaging, and service health across templated and manually extended domains.
- **Testing Loop:** After scaffolding or retrofit work, use `/aspire` to validate the implementation.

---

## Key Principles

1. **Deterministic Scaffolding:** New project structure comes from `CoreEx.Template`, not chat-driven code generation.

2. **Conventions Over Configuration:** Templates, instructions, and samples work together around the same CoreEx project structure and naming patterns.

3. **Inspect Before Retrofitting:** For existing domains, inspect first and then make the smallest safe manual change.

4. **Scaffolding Is a Baseline:** Templates provide structure, not finished business logic. Customization is still required.

5. **CoreEx Alignment:** Agents and skills should continue to anchor recommendations to samples, instructions, and CoreEx patterns.

---

## Common Scenarios and Recommended Sequencing

### New Microservice: E-Commerce Domain

1. **Ideation:** Decide on Orders, Products, Payments domains
2. **Choose shape:** `CoreEx Expert` → decide which hosts each domain needs
3. **Create Products:** `dotnet new coreex` + needed hosts
4. **Create Orders:** `dotnet new coreex` + needed hosts
5. **Create Payments:** `dotnet new coreex` + needed hosts
6. **Customize:** implement business logic and messaging details
8. **Integrate:** `/aspire` → run all three together, validate event flow

**Tools in sequence:** `CoreEx Expert`, `dotnet new coreex*` × N, customize, `/aspire`

---

### Migrating Existing Codebase to CoreEx

1. **Assess:** `/acquire-codebase-knowledge` → understand current structure
2. **Compare:** Review generated map against CoreEx expectations
3. **Decide:** Retrofit manually, add a new host with a template, or rebuild using `CoreEx.Template`?
4. **Extend:** make the smallest manual CoreEx-aligned changes that fit the current code
5. **Validate:** `/aspire` → test migrated domain in new environment

**Tools in sequence:** `/acquire-codebase-knowledge`, `CoreEx Expert`, optional `dotnet new coreex*`, `/aspire`

---

### Team Onboarding to Contoso Sample

1. **Orient:** `/acquire-codebase-knowledge` → document Contoso structure
2. **Review:** Generated map shows Products, Shopping, Orders layers
3. **Explore:** `/aspire` → run sample locally, observe services
4. **Propose:** Team proposes new domain → `CoreEx Expert` → pick the right template set
5. **Scaffold:** run `dotnet new coreex*` and then add the required business logic
6. **Validate:** `/aspire` → new domain integrates with existing services

**Tools in sequence:** `/acquire-codebase-knowledge`, `/aspire`, `CoreEx Expert`, `dotnet new coreex*`, `/aspire`

---

## FAQ

**Q: When should I use `CoreEx.Template` versus ask an agent for help first?**

A: Use `CoreEx.Template` whenever you are creating a new solution or host shape. Ask `CoreEx Expert` first when you are unsure which host combination to scaffold or when you want an evidence-backed plan before making changes.

---

**Q: Should I use `/acquire-codebase-knowledge` before retrofitting an existing domain?**

A: Yes, when the codebase is unfamiliar or when you are not sure whether it already aligns to CoreEx patterns. It gives you the evidence needed to make a safer retrofit plan.

---

**Q: What if I only want part of the scaffolded shape?**

A: Use only the templates you actually need. Start with `dotnet new coreex` for the shared solution shape, then add `coreex-api`, `coreex-relay`, or `coreex-subscribe` only if the use case requires them.

---

**Q: Can multiple domains share a database?**

A: Yes. The templates assume a conventional per-domain shape, but you can adapt that after scaffolding if your architecture requires shared infrastructure. Review the Contoso sample before diverging.

---

**Q: How do I know if my existing code is compatible with a manual CoreEx retrofit?**

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
