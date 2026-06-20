# CoreEx Agentic Scaffolding
## Skills + Prompts + Context-Aware Generation

Audience: Engineering leadership, platform team, solution architects, implementation teams.
Duration: 20-30 minutes.

---

## Slide ES1 - Executive Summary
### Why This Matters
- CoreEx scaffolding gives deterministic, repeatable delivery foundations.
- Agentic prompting adds speed and flexibility for domain-specific requirements.
- Teams move faster with less architecture and boilerplate friction.

Speaker notes:
This section is designed for executive stakeholders. It focuses on outcomes, risk reduction, and delivery acceleration.

---

## Slide ES2 - Business Value
### Impact on Delivery
- Faster time from idea to a buildable, reviewable solution baseline.
- Consistent architecture and coding conventions across teams.
- Less rework by reducing early structural and wiring defects.
- Higher engineering focus on business features instead of plumbing.
- Move fast early: discovery, prototyping, and proving value with stakeholders.

Speaker notes:
CoreEx reduces setup variability and enables teams to start from proven implementation patterns. It is especially effective when teams need rapid evidence of value in the first phase of a delivery.

---

## Slide ES3 - Deterministic Foundation + Agentic Speed
### Balanced Delivery Model
- Deterministic: templates, instructions, and skills enforce known-good structure.
- Agentic: prompts tailor scaffolding to domains, features, and constraints.
- Result: predictable governance with rapid customization.
- Converge from experimentation to determinism for repeatability, compliance, and supportability.

Speaker notes:
This is the key message: not deterministic versus agentic, but deterministic core plus agentic acceleration. Teams can begin with rapid exploration, then lock into repeatable patterns as delivery matures.

---

## Slide ES4 - What Comes Out of the Box
### CoreEx Opinionated Acceleration
- Modern architecture patterns: microservices, eventing, and DDD-aligned domains.
- Enterprise capabilities ready to pull in as needed: validation, caching, outbox, observability.
- Integrated hosting and API conventions reduce framework integration overhead.

Speaker notes:
CoreEx is not a generic framework toolkit that teams must assemble from scratch. It is an opinionated accelerator with practical defaults.

---

## Slide ES5 - Platform Flexibility
### Enterprise Choice Without Chaos
- Supports multiple backend patterns and provider strategies.
- Messaging approach is not locked to one broker.
- Teams can select infrastructure choices while preserving a consistent architecture model.

Speaker notes:
Executives get both standardization and optionality.

---

## Slide ES6 - Decision and Rollout
### Recommended Path
- Adopt CoreEx agentic scaffolding as the default start for new services.
- Run a phased rollout with KPI tracking on speed, consistency, and rework reduction.
- Govern templates and instructions as platform assets.
- Ensure the delivered solution remains maintainable and operable over time, including after team transition.

Speaker notes:
Treat scaffolding as a strategic product capability, not a one-off project artifact. This improves continuity when ownership transitions to long-term product teams.

---

## Slide ES7 - Transition to Technical Deep Dive
### Section 2: Full Technical Detail
- Detailed architecture, capability inventory, platform options, and roadmap.
- Prompt, skill, and instruction workflow with enforcement guardrails.

Speaker notes:
The next section provides implementation depth for architects and engineering leads.

---

## Slide 1 - Title
### The New Agentic Way to Scaffold CoreEx Solutions
- Generate production-aligned CoreEx domain scaffolding faster.
- Encode architecture and coding standards as reusable prompt and skill assets.
- Keep generated output aligned to CoreEx patterns and sample implementations.

Speaker notes:
This deck explains how we move from manual scaffolding to an agentic, policy-driven workflow that is repeatable, governed, and practical for teams starting new CoreEx implementations.

---

## Slide 2 - Why Change
### Pain in the Previous Approach
- Manual setup was slow and inconsistent across teams.
- Architecture intent was scattered across tribal knowledge and sample code.
- Early project output often drifted from CoreEx conventions.
- Review cycles focused on fixing structure and wiring instead of business value.

Speaker notes:
We are not replacing engineering judgment. We are automating boilerplate and codifying known-good patterns so teams can spend time on domain behavior.

---

## Slide 3 - What Is New
### Agentic Scaffolding Stack
- Prompt workflows define user intent capture and step sequencing.
- Skills define capability packs with selection logic and generation strategy.
- Instruction files enforce file-scope coding conventions.
- Templates and scripts provide repeatable project materialization.
- Sample implementations provide concrete behavioral and architectural examples.
- CoreEx scaffolding introduces deterministic outputs, reducing non-deterministic variance from pure free-form agentic generation.

Speaker notes:
The key idea is layered context. Prompts ask for what we need, skills decide what to generate, instructions govern how to write code, and templates provide deterministic structure.

---

## Slide 4 - Core Building Blocks
### Assets in the Repository
- Deterministic template pack: CoreEx.Template with `dotnet new coreex*`.
- Agent guidance: CoreEx Expert for shape and capability decisions.
- Environment and startup prompts: init, setup.
- File-scoped instructions for controllers, services, repositories, validators, tests, and host setup.
- Starter docs for architecture, conventions, and domains.

Speaker notes:
Everything needed is shipped as repository assets. Teams do not start from a blank prompt.

---

## Slide 5 - Operating Model
### Human + Agent Collaboration
- Human provides bounded context and domain intent.
- Agent asks only minimal clarification questions for critical decisions.
- Agent scaffolds layered projects and hosts.
- Agent validates quality gates and resolves diagnostics.
- Human reviews domain correctness and business rules.
- Deterministic CoreEx scaffolding provides predictable baselines; agentic prompting provides fast customization on top.

Speaker notes:
This is controlled autonomy. The agent handles deterministic work while still adapting to requested features and domains. Humans remain accountable for correctness, semantics, and trade-off decisions.

---

## Slide 6 - End-to-End Flow
### From Prompt to Running Solution
1. Capture request: domains, hosts, persistence, behaviors.
2. Select skill and package set.
3. Materialize solution and project templates.
4. Generate layer artifacts: Contracts, Application, Infrastructure, API, Database.
5. Apply conventions from scoped instruction files.
6. Run diagnostics and fix generation errors.
7. Build and verify starter functionality.

Speaker notes:
The flow is intentionally explicit so we can audit and improve each stage.

---

## Slide 7 - Context Hierarchy
### How the Agent Stays Aligned to CoreEx
- Copilot instructions define global CoreEx-first behavior.
- Scoped instruction files enforce local patterns by file type.
- Skill logic maps requested capabilities to concrete package choices.
- Prompts capture required inputs and enforce completion checklists.
- Sample projects and docs provide reference implementations.

Speaker notes:
This hierarchy reduces ambiguity. If a rule exists in multiple places, the narrower scope wins for generation behavior.

---

## Slide 8 - Generated Architecture
### Standard Layered Output
- Contracts project for DTOs and source-generation annotations.
- Application project for services, validation, exceptions, and orchestration.
- Infrastructure project for repository and adapter implementations.
- API host with CoreEx middleware and OpenAPI conventions.
- Database project with migrations, outbox tables, and procedures.
- Optional subscribe and outbox relay hosts when integration requires them.
- Domain-first modeling aligned to DDD (aggregates, value objects, bounded contexts).
- Modern architecture support out of the box: eventing, microservice host separation, and integration patterns.

Speaker notes:
The generated shape matches CoreEx reference architecture, keeps dependencies layered and predictable, and gives teams a ready-to-use microservices/event-driven baseline.

---

## Slide 8A - Products and Shopping Sample Topology
### Architecture Shown in the Diagram
- Two bounded domains, each with its own API and Application layer.
- Each domain application encapsulates contracts and infrastructure concerns behind its service boundary.
- Unit-of-Work sits between application logic and persistence.
- Each domain has isolated Data and Outbox stores.
- Outbox Relay and Message Subscriber are deployed per domain.
- Domains communicate asynchronously through a shared queue or stream backbone.
- Cache is shown as a shared cross-domain optimization layer.

Speaker notes:
This diagram demonstrates the CoreEx pattern in practice: independent domain services with local transaction boundaries and outbox-driven integration. Product and Shopping remain decoupled operationally, while events over queue or stream coordinate cross-domain workflows.

---

## Slide 9 - Determinism vs Pure Agentic Variance
### Why CoreEx Scaffolding Matters
- Pure agent-only generation can vary between runs, prompts, and model behavior.
- CoreEx templates, instructions, and skills constrain generation into known-good patterns.
- Teams get deterministic project structure, wiring, and conventions from day one.
- Agentic prompting still adds flexibility for domain-specific features and behavior.

Speaker notes:
This is not a choice between deterministic and agentic. CoreEx combines both: deterministic scaffolding for foundation, agentic adaptation for domain acceleration.

---

## Slide 10 - Guardrails and Quality Gates
### Built-In Enforcement
- Constructor dependency null guards.
- ConfigureAwait(false) in async service and repository flows.
- Mutation operations wrapped in Unit of Work.
- Event emission inside mutation-aware blocks.
- CoreEx WebApi helper usage and response conventions.
- Validation before persistence using CoreEx validators.
- Diagnostics check before completion.

Speaker notes:
Guardrails shift many common review comments from late discovery to immediate generation-time enforcement.

---

## Slide 11 - Prompt Sequence for Teams
### Suggested Delivery Workflow
1. Run init prompt to verify machine prerequisites.
2. Run setup prompt to start dependencies and local runtime.
3. Run solution bootstrap skill for initial project layout.
4. Run domain generation prompt for new bounded contexts.
5. Run template-scaffold prompt for fast repeatable domain cloning.
6. Execute tests and E2E checks.

Speaker notes:
Teams can adopt this as a standard onboarding and delivery playbook.

---

## Slide 12 - Example Ask
### Example Prompt to Kick Off Scaffolding
Create a new CoreEx solution.
I need a Web API and Worker service.
Domains: Product and Shopping.
Include validation and behaviors.
Use SQL Server persistence.
Use Kafka as message broker.
Scaffold full repository structure with tests.

Speaker notes:
This level of intent is enough for the agent to scaffold an entire repo, including domains, hosts, and tests, while honoring architecture constraints.

---

## Slide 13 - Benefits Realized
### Expected Outcomes
- Faster time from idea to compilable baseline.
- Higher consistency with CoreEx coding and architecture conventions.
- Reduced architecture drift during early implementation.
- Lower cognitive load for new teams onboarding to CoreEx.
- Better review quality by focusing reviews on business semantics.
- Opinionated patterns out of the box reduce friction versus composing a generic framework from scratch.
- Teams pull in only needed capabilities (validation, events, caching, messaging) instead of hand-integrating foundations.
- Smoother transition from initial delivery teams to long-lived ownership with maintainable, standards-aligned code.

Speaker notes:
CoreEx accelerates teams by providing a pre-wired, opinionated path that removes setup churn and allows focus on business capabilities. It also reduces operational risk by leaving behind predictable, supportable implementation assets.

---

## Slide 14 - Platform Flexibility
### Not Locked to One Backend or Broker
- Data persistence can target multiple backend types depending on provider and project needs.
- Messaging integration is pluggable; Azure Service Bus is supported, but not mandatory.
- Agentic prompts can specify preferred database and broker choices per solution.
- Deterministic scaffolding still applies even when platform selections vary.

Speaker notes:
CoreEx gives an opinionated architecture, not a hard platform lock. Teams can keep consistency while selecting infrastructure that fits enterprise constraints.

---

## Slide 15 - CoreEx Capability Inventory (General)
### Included Components and Features
- Error-based exceptions: NotFound, Validation, Concurrency, and related exception types.
- Dynamic dependency injection patterns.
- Entities: Identifier and CompositeKey support.
- ETag support for optimistic concurrency.
- Change log support for created and updated metadata.
- Deep-compare capabilities for entity state comparison.
- Roslyn source-generation support.
- Instrumentation and health checks.
- Hybrid cache (L1 and L2) with FusionCache, backplane support, and Redis integration.
- Hosted services for timer-driven and synchronized workloads.
- Reference data orchestration, including caching support.
- System.Text.Json support for filtering and merge-patch workflows.
- Validation pipeline as an alternative to FluentValidation.
- Mapping helpers with explicit mapping patterns (no AutoMapper).
- Globalization and localization primitives.
- Result-based railway-oriented programming composition.

Speaker notes:
This slide is the CoreEx baseline inventory. It communicates the practical accelerator: teams pull in tested primitives instead of recreating them per project.

---

## Slide 16 - ASP.NET Core and Data Capabilities
### Included Web, Data, and Database Features
- Web API styles: Http-style minimal APIs and MVC controller APIs.
- application/merge-patch+json support.
- Response JSON filtering support.
- Error handling middleware aligned with ProblemDetails.
- IF-MATCH ETag semantics for GET and PUT or PATCH workflows.
- Idempotency-Key support for POST.
- Health check endpoints.
- OpenAPI generation via NSwag.
- CQRS read and write separation support.
- Unit-of-Work with integrated Outbox.
- Paging support using skip/take with total count.
- Dynamic OData-like query support for filtering and ordering.
- Multi-tenancy behavior support.
- Type discriminator support where required.
- Database support: SQL Server and PostgreSQL (provider dependent).
- ADO.NET command, record, and parameter extensions.
- Entity Framework integration support.

Speaker notes:
This is where friction drops versus generic frameworks: CoreEx packages wire these conventions directly so teams avoid repeated plumbing decisions.

---

## Slide 17 - Messaging and Domain-Driven Capabilities
### Included Eventing and DDD Features
- EventData as an agnostic message representation.
- CloudEvent conversion and interoperability support.
- Publish and subscribe patterns with per-message subscription behavior from stream.
- Azure Service Bus integration patterns.
- Outbox relay support with partition-aware patterns.
- Domain-driven modeling support for aggregates and entities.
- ValueObject modeling using C# record class patterns.
- Integration-events only guidance.
- Explicitly no domain-events and no MediatR-based in-process orchestration by default.

Speaker notes:
CoreEx aligns eventing and domain modeling so distributed services remain explicit, testable, and consistent across teams.

---

## Slide 18 - Current Upgrade Status and Roadmap
### What Is Next
To be upgraded:
- Azure Functions.
- Cosmos (CRUD and Query).
- Dataverse.
- OData.
- Solace messaging integration.

Roadmap:
- MongoDB.
- DocumentDB (new).
- Kafka.

Aspire enabled (done):
- Leverages component runtime libraries.
- Sample uses console for logging, tracing, and metrics visualization.

Speaker notes:
This makes current maturity and future direction explicit for stakeholders. It also reinforces that platform portability is planned and active, not theoretical.

---

## Slide 19 - Risks and Mitigations
### What to Watch
- Risk: over-trusting generated output.
  Mitigation: enforce mandatory human review and test gates.
- Risk: stale templates or instruction drift.
  Mitigation: version and periodically validate scaffolding assets against samples.
- Risk: ambiguous prompts produce wrong shape.
  Mitigation: require minimal clarifying questions for orchestration, CQRS, and integration topology.

Speaker notes:
Agentic does not remove governance. It improves it when paired with clear controls.

---

## Slide 20 - Adoption Plan
### 30-60-90 Day Rollout
- 30 days: pilot with one domain and baseline metrics.
- 60 days: codify team prompt playbooks and update templates from findings.
- 90 days: make agentic scaffolding the default start path for new CoreEx implementations.

Suggested KPIs:
- Time to first successful build.
- Number of post-generation architecture corrections.
- Defect rate in generated boilerplate.
- Onboarding time for new engineers.

Speaker notes:
Measure both speed and quality. The objective is not just faster generation, but better initial correctness.

---

## Slide 21 - Evidence Pointers
### Source Anchors Used for This Deck
- .github prompts for init, setup, and domain generation.
- coreex-starter bootstrap skill and script.
- coreex-starter architecture, conventions, and domain guidance docs.
- scoped instruction files for controllers, application services, repositories, tests, validators, and host setup.

Speaker notes:
These slides are grounded in repository assets and can be updated as those assets evolve.

---

## Appendix - Presenter Q and A
### Likely Questions
- How much can we customize generated domains?
- How do we prevent template sprawl?
- Can this flow support non-SQL providers?
- How do we handle major CoreEx version upgrades?
- What is the approval model for changing instructions and skills?

Speaker notes:
Use these as discussion prompts for architecture review and platform governance forums.
