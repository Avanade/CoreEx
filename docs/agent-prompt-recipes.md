# CoreEx Agent Prompt Recipes

This guide gives **copy/paste prompt patterns** for consultant delivery teams using CoreEx from the starter or NuGet packages. Adapt the wording to your domain, but keep the structure.

If you want to ask one of these as a quick side question in Copilot CLI without adding it to the main conversation flow, you can also prefix it with **`/ask`**.

## 1. Understand the Codebase

### Map a domain

```text
Map the <Domain> area of this repo for me.
Explain the role of Contracts, Application, Infrastructure, Api, Database, Subscribe, and Outbox.Relay if present.
Use concrete file references from this repo.
No code changes.
```

### Explain a request flow

```text
Show me the end-to-end request flow for <operation> in the samples.
Use the actual sample code and explain which layer owns each step.
No code changes.
```

### Compare two samples

```text
Compare the Product and Shopping samples for <topic>.
Focus on the differences in host shape, messaging, and domain behavior.
No code changes.
```

## 2. Learn a Pattern

### Capability explainer template

```text
Explain the <capability> feature in CoreEx.
Tell me:
- what it is
- what problem it solves
- when to use it and when not to use it
- how it is implemented in this repo
- how I would add it to my solution

Use sample-backed guidance where possible.
No code changes.
```

### Idempotency key

```text
What is the idempotency key feature in CoreEx?
What problem does it solve?
When should I use it?
How would I implement it in my solution?
Use the repo patterns and samples where relevant.
No code changes.
```

Quick CLI variant:

```text
/ask What is the idempotency key feature in CoreEx, what problem does it solve, and when should I use it?
```

### Reference data orchestration

```text
Explain the reference data orchestration feature in CoreEx.
What problem does it solve?
When is it worth adding?
How would it fit into my current solution shape?
```

### FusionCache / Redis

```text
Explain the FusionCache and Redis pattern used in this repo.
What problem does it solve?
When should I add it?
What is the smallest implementation I could add first?
```

### Validation

```text
Explain how validation is implemented in this repo.
Show me where the validator lives, where it is called, and how that differs from controller validation.
Use sample-backed examples only.
```

### Outbox

```text
Explain how reliable event publishing works in this repo.
Use the Product sample and describe API host, outbox tables, relay host, and subscriber flow.
No code changes.
```

### ETag and concurrency

```text
Explain ETag and optimistic concurrency handling in this repo.
Show me the contract, service, and API implications using an existing sample.
```

### Orchestration

```text
Compare request/response plus outbox versus orchestration for this use case:
<describe business process>.
Use the Order.Workflow sample where relevant.
No code changes.
```

## 3. Shape a New Solution

### Choose the smallest CoreEx shape

```text
Given this use case:
<describe use case>

Recommend the smallest CoreEx application shape that supports it.
Tell me whether I need API only, API + relay, API + subscribe, or orchestration.
Explain why.
```

### Choose the right scaffolding entry point

```text
I need to implement this:
<describe requirement>

Tell me whether I should use coreex-project-bootstrap, /generate-domain, /scaffold-domain-from-templates, or /add-capability.
Explain the tradeoffs using the current repo guidance.
```

## 4. Plan a Feature

### Plan before coding

```text
Inspect the current <Domain> implementation first.
Then create a plan for adding <feature>.
Use existing CoreEx patterns only and align to the closest sample in this repo.
Do not implement yet.
```

### Ask for layer-by-layer impact

```text
For this feature:
<describe feature>

Tell me what should change in Contracts, Application, Infrastructure, Api, and any hosts.
Use the current repo conventions.
No code changes yet.
```

### Ask for capability guidance

```text
This feature needs:
<business requirement>

Tell me which CoreEx capabilities are actually needed and which should be deferred.
Prefer the smallest safe change.
```

## 5. Implement a Feature Safely

### Smallest safe change

```text
Inspect the current domain first.
Implement <feature> using the smallest safe change.
Preserve the current layering and naming unless a restructure is required.
Use existing CoreEx patterns only.
```

### Sample-aligned implementation

```text
Implement <feature> in this domain.
Align to the closest existing sample in this repo.
Explain briefly which sample you followed and why.
```

### Conservative enhancement

```text
Enhance the existing implementation to support <feature>.
Do not regenerate the domain.
Do not add unrelated capabilities.
Inspect what is already present before editing.
```

## 6. Retrofit Existing Domains

### Add reliable event publishing

```text
Inspect this domain and determine whether it already has outbox, relay, or event publisher wiring.
Then add the missing pieces required for reliable integration-event publishing.
Use current CoreEx messaging patterns only.
```

### Add subscriber support

```text
Inspect this domain for any existing Subscribe host or Service Bus wiring.
Then add the missing pieces required to consume <event/command>.
Keep subscriber logic thin and aligned to repo conventions.
```

### Use the retrofit skill intentionally

```text
Use /add-capability for this existing domain.
Inspect the current state first, then add <relay / subscribe / subscriber scaffolding>.
Treat SQL Server and Azure Service Bus as defaults unless you find evidence otherwise.
```

## 7. Review an Existing Design

### Convention check

```text
Review this implementation against the current repo conventions.
Focus on layering, CoreEx usage, validation placement, host wiring, and messaging patterns.
Ignore style-only feedback.
```

### Compare against the samples

```text
Compare this implementation to the closest sample in the repo.
Tell me what is aligned, what is drifting, and what matters functionally.
```

### Ask for missing capabilities

```text
Inspect this domain and list which CoreEx capabilities appear to be missing for this use case:
<describe use case>

Explain which are required now versus optional later.
```

## 8. Debug Architecture or Modeling Uncertainty

### Is this CRUD, messaging, or orchestration?

```text
I am not sure whether this requirement should be modeled as:
- a normal API write
- API + outbox event publishing
- subscriber-driven reaction
- orchestration

Use the current repo patterns to compare those options for this use case:
<describe use case>
```

### Should I add a new host?

```text
Inspect the current domain shape.
Tell me whether this requirement justifies adding a new host or can be handled in the existing ones.
Prefer the smallest safe architecture.
```

## 9. Learn by Asking Better Follow-Ups

When the first answer is not enough, use follow-ups like these:

- `Show me the actual files that demonstrate that pattern.`
- `Which sample is the closest fit for this advice?`
- `What is the smallest version of this that I can implement first?`
- `What would you defer until later?`
- `What files or hosts would change if I added this capability?`
- `Does my current solution already have part of this capability set up?`
- `What would change if this were an existing domain instead of a new one?`
- `What should I ask you next if I want you to implement this safely?`

## 10. Prompt Framing Patterns That Usually Work

### Explanation only

```text
Explain <topic> in the context of this repo.
Use sample-backed evidence.
No code changes.
```

### Plan only

```text
Inspect the current implementation first.
Then create a plan for <feature>.
Do not implement yet.
```

### Implement

```text
Inspect the current implementation first.
Then implement <feature> using existing CoreEx conventions and the closest sample pattern.
Prefer the smallest safe change.
```

### Implement and verify scope

```text
Implement <feature>.
Before changing code, tell me which hosts/layers you expect to modify and why.
Then proceed with the smallest safe change.
```

## 11. What to Include in Your Prompt

For the best results, include:

- the **use case**
- whether the code already exists
- whether you want **explanation**, **plan**, or **implementation**
- whether the result should align to a specific sample
- any constraints such as “smallest safe change”, “no new host unless necessary”, or “use current SQL Server/Service Bus defaults”

## Where to Go Next

- Use `docs/agent-interaction-guide.md` to understand how to interact with the agent effectively.
- Use `docs/application-scaffolding-guide.md` to choose the right CoreEx shape.
- Use `docs/capabilities.md` to dive deeper into the underlying patterns.
