# CoreEx Agent Interaction Guide

This guide is for **consulting delivery teams consuming CoreEx from the starter or NuGet packages**. Its purpose is to help you use the agent as an interactive partner for learning the framework, understanding the sample architecture, and implementing features safely.

This is not a framework reference. Use it when you want to know **how to ask**, **what to ask**, and **which skill or prompt to use**.

## Start with the Right Kind of Request

Most unhelpful agent conversations start with a request that is too vague. Tell the agent which of these modes you want:

| If you want to... | Ask for... | Example |
|---|---|---|
| Understand how the repo works. | **Explanation / discovery.** | `Map the Shopping domain and explain its layers.` |
| Understand a pattern. | **Sample-backed explanation.** | `Explain outbox publishing in this repo using the Product sample.` |
| Decide what to build. | **Solution shaping.** | `Given this use case, what is the smallest CoreEx shape I should scaffold?` |
| Create a new domain. | **Greenfield scaffolding.** | `Generate a new Orders domain with validation and SQL Server persistence.` |
| Add capabilities to an existing domain. | **Capability retrofit.** | `Inspect this domain and add the missing messaging pieces.` |
| Change working code. | **Implementation.** | `Inspect the existing implementation first, then add support for X using current CoreEx patterns.` |

## Ask for Repo-Grounded Answers

When you are new to the framework, avoid asking for generic platform advice when what you really want is **how this repo does it**.

Prefer:

- `Explain validation in this repo with examples from the samples.`
- `Compare API + relay versus API-only using the Product and Shopping samples.`
- `Show me where ETag handling belongs in CoreEx service/controller flow.`

Avoid:

- `How should .NET APIs do validation?`
- `What is the best eventing architecture?`

Those broader questions tend to produce generic answers instead of CoreEx-specific guidance.

## Ask Capability Questions Directly

Yes — you should absolutely ask direct capability questions such as:

- `What is the idempotency key feature in CoreEx?`
- `What problem does it solve?`
- `When should I use it?`
- `How would I implement it in my solution?`

That is a good way to learn the framework. In practice, the best version of that question asks for four things together:

1. **What the capability is.**
2. **What problem it solves.**
3. **When to use it versus not use it.**
4. **How it is implemented in this repo or in my current solution.**

Good examples:

- `Explain the idempotency key feature in CoreEx. What problem does it solve, when should I use it, and how is it implemented in this repo?`
- `Explain ETag handling in CoreEx, what risks it addresses, and how I would add it to my current API.`
- `Explain the outbox pattern in this repo, what failure mode it prevents, and what I would need to add to my solution to support it.`

This same pattern works well for:

- idempotency
- ETags
- validation
- reference data orchestration
- FusionCache / Redis
- outbox relay
- subscriber hosts
- orchestration

## Tell the Agent What Outcome You Want

The same topic can produce very different outcomes. State the intended result explicitly:

- **Explanation only** — no code changes.
- **Plan only** — propose the approach before editing.
- **Implement it** — make the changes.
- **Implement it and align to the samples** — make the changes using the current repo conventions.

Examples:

- `Explain how subscribers work here. No code changes.`
- `Plan the smallest safe way to add reliable event publishing to this domain.`
- `Implement this feature using existing CoreEx patterns only.`

For quick capability or pattern questions that you do not want to add to the main conversation flow, use the Copilot CLI **`/ask`** command as a lightweight side question.

Examples:

- `/ask What is the idempotency key feature in CoreEx and what problem does it solve?`
- `/ask When is outbox sufficient and when do I need orchestration?`
- `/ask Does this repo treat ETag support as optional or expected for mutable entities?`

## Ask the Agent to Inspect Before Recommending

For existing solutions, ask the agent to inspect the current state before it suggests changes. This is especially important for consulting work, where a domain may already be partially set up.

Good examples:

- `Inspect this domain and explain what capabilities are already present before recommending changes.`
- `Check whether this service already has outbox, subscribers, or relay support before proposing a retrofit.`
- `Map the current host shape first, then tell me what is missing for this use case.`

This is the safest way to avoid duplicate hosts, redundant packages, or advice that ignores what the project already has.

## Use the Right Skill or Prompt

The repo already exposes several entry points. Use them intentionally.

| Need | Best fit | Why |
|---|---|---|
| Understand an unfamiliar repo or area. | `acquire-codebase-knowledge` | Produces structured codebase documentation and evidence-backed discovery. |
| Create a new solution shape. | `coreex-project-bootstrap` | Best for solution-level bootstrapping from requirements. |
| Create a new custom domain. | `/generate-domain` | Best when the agent needs to reason about fields, validation, and event naming. |
| Create a template-shaped domain quickly. | `/scaffold-domain-from-templates` | Best for deterministic, template-aligned output. |
| Add capabilities to an existing domain. | `/add-capability` | Best for incremental retrofits such as relay, subscribe, and messaging alignment. |
| Start local dependencies or sample runtime. | `init`, `setup`, or Aspire tooling | Best for environment and sample execution workflows. |

## A Good Question Usually Includes Four Things

A strong request usually includes:

1. **The use case.**
2. **The current context.**
3. **The desired outcome.**
4. **Any constraints.**

Template:

```text
I am working on <domain/use case>.
Please inspect <existing code / domain / sample> first.
I want <explanation / plan / implementation>.
Use existing CoreEx patterns and align to <sample or host shape>.
```

Example:

```text
I am working on an Orders domain.
Inspect the current domain first.
I want a plan for adding reliable integration-event publishing.
Use existing CoreEx patterns and align to the Product sample.
```

## Questions That Work Well for New Developers

### Learn the architecture

- `Map the Products sample and explain the role of each project.`
- `Explain how Contracts, Application, Infrastructure, Api, Subscribe, and Outbox.Relay fit together here.`
- `Show me the request flow for a Product create in the samples.`

### Learn a practice

- `Explain where validation belongs in CoreEx and show the sample pattern.`
- `Explain why this repo uses outbox relay instead of publishing directly from the API.`
- `Show me how reference data is modeled and consumed.`

### Learn a capability feature

- `What is the idempotency key feature in CoreEx, what problem does it solve, and how would I implement it in my solution?`
- `What is ETag support in CoreEx, what issue does it prevent, and where does it belong in the API flow?`
- `What is FusionCache used for here, when should I add it, and what would the minimal implementation look like?`
- `What is the reference data orchestration feature, when is it worth adding, and how would it fit in this solution shape?`

### Decide what to scaffold

- `Given this use case, do I need API only, API + relay, API + subscribe, or orchestration?`
- `What is the smallest CoreEx shape that supports this requirement?`
- `Should this domain use /generate-domain or /add-capability?`

### Prepare to implement

- `Inspect the current domain and list what is already set up before recommending changes.`
- `Compare my use case to Product, Shopping, and Order.Workflow and tell me which pattern is closest.`
- `Plan the feature in terms of Contracts, Application, Infrastructure, Api, and hosts.`

## When to Ask for Comparisons

Comparative questions are especially useful when you are still learning the framework.

Examples:

- `Compare API-only versus API + relay for this use case.`
- `Compare subscriber host versus orchestration worker for this business flow.`
- `Compare /generate-domain versus /scaffold-domain-from-templates for this new domain.`
- `Compare adding capability retroactively versus scaffolding the full host shape from day one.`

This helps you learn the framework decision points instead of only getting one recommendation.

## Ask for Sample Alignment Explicitly

If you want implementation help that matches repo conventions, say so directly.

Useful phrases:

- `Align to the Product sample.`
- `Use the same host wiring pattern as Shopping.Subscribe.`
- `Follow the existing CoreEx instructions and sample conventions.`
- `Prefer the current repo pattern over generic alternatives.`

## Tell the Agent How Conservative to Be

In consulting projects, you often want the **smallest safe change**, not a broad redesign.

Useful phrases:

- `Prefer the smallest safe change.`
- `Do not restructure the domain unless required.`
- `Preserve the current layering and naming unless there is a clear mismatch.`
- `Add only the missing capability pieces.`

## Good Framing for Feature Requests

When asking for implementation help, frame the feature in business terms first, then let the agent translate that into CoreEx capabilities.

Good:

- `This service needs to publish an event after a successful write. What CoreEx capabilities should I add?`
- `This domain must react to upstream product updates. How should that be implemented in this repo?`
- `This process waits for external approval. Is this a subscriber problem or an orchestration problem?`

Less effective:

- `Add Service Bus.`
- `Use Redis.`
- `Add CoreEx.Events.`

Package-driven questions are less useful than use-case-driven questions.

## Ask for Boundaries, Not Just Answers

When you are uncertain, ask the agent to explain **what is in scope and what is out of scope** for a pattern.

Examples:

- `When is outbox sufficient and when do I need orchestration?`
- `When should I stop at API-only instead of adding subscribers?`
- `What does /add-capability handle today, and what would still need manual work?`

That helps you understand the framework’s decision boundaries instead of only the happy path.

For capability questions, useful follow-ups are:

- `When should I avoid this feature?`
- `What is the smallest version of this capability I can add first?`
- `Does my current solution already have part of this set up?`
- `Which files or hosts would change if I add it?`

## Common Anti-Patterns

Avoid these when interacting with the agent:

- Asking for a package before stating the use case.
- Asking for implementation without asking the agent to inspect the current state first.
- Asking for generic .NET advice when you need CoreEx-specific guidance.
- Asking for “the best architecture” instead of comparing concrete CoreEx shapes.
- Asking for a full redesign when you only need a capability retrofit.

## Suggested Learning Sequence

If you are new to CoreEx, this sequence works well:

1. Read `README.md`.
2. Read `docs/application-scaffolding-guide.md`.
3. Read `docs/capabilities.md`.
4. Use the agent to map one sample domain.
5. Ask the agent to explain one end-to-end request flow.
6. Ask the agent to compare two implementation shapes for your real use case.
7. Move to planning or implementation only after that.

## Where to Go Next

- Use `docs/agent-prompt-recipes.md` for copy/paste prompt starters.
- Use `docs/application-scaffolding-guide.md` to choose the right host/capability shape.
- Use `docs/capabilities.md` for deeper pattern explanations.
- Use `docs/orchestration.md` when the use case goes beyond request/response plus outbox.
