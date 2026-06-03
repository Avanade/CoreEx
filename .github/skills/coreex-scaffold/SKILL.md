---
name: coreex-scaffold
description: "Guide a developer through CoreEx solution shaping for either a new repository or an existing scaffolded solution missing runtime hosts. USE FOR: empty repo greenfield scaffolding, deciding API-only vs API plus relay vs API plus subscriber, choosing SQL Server vs Postgres vs no database, choosing refdata/outbox/DDD/ROP options, installing CoreEx.Template, checking current CoreEx solution shape, and adding missing Api/Outbox.Relay/Subscribe hosts to an existing repo. DO NOT USE FOR: unrelated runtime debugging or forcing root re-scaffolding over an existing solution. INVOKES: workspace inspection, user questions, dotnet new install/list, dry-run validation, and either template generation or manual retrofit work depending on repo shape."
argument-hint: "Optional: proposed solution name, required hosts, database choice, messaging choice, and architectural constraints."
tags: ["coreex", "scaffolding", "retrofit", "template", "hosts"]
---

# CoreEx Scaffold

Guides a repository through the right CoreEx setup path based on its current shape.

## When to Use

- Starting a new CoreEx domain or service from an empty repository.
- Deciding whether the first cut should be API-only, API plus relay, or API plus subscriber.
- Adding missing `Api`, `Outbox.Relay`, or `Subscribe` hosts to an existing scaffolded CoreEx solution.
- Converting project needs into either the matching `dotnet new coreex*` commands or the safest retrofit path.

## When Not to Use

- You are debugging local runtime, container, Aspire, or package restore issues unrelated to project shaping.
- You want architectural guidance for an existing implementation beyond project setup; use `CoreEx Expert` instead.

## Workflow

1. **Inspect the workspace shape first.**
   - Determine whether the repository is effectively empty or already contains real solution content.
   - Use specific evidence such as an existing `.slnx`, populated `src/`, existing `Database` or `CodeGen` projects, or existing test projects.

2. **Branch to the right path.**
   - If the workspace is greenfield, run the normal scaffold interview.
   - If the workspace is already scaffolded, infer the current solution name, providers, and capability flags from local artifacts and ask only what is still missing.

3. **Choose the safest implementation path.**
   - Verify the template pack first with `dotnet new list --tag CoreEx`.
   - If missing, run `dotnet new install CoreEx.Template`.
   - Use `dotnet new ... --dry-run` when helpful to confirm naming and output shape.
   - For existing repos, do not run templates directly into the workspace when they create nested root folders or conflicting layout.
   - Prefer a manual retrofit aligned to the repo structure when template output does not fit.

4. **Apply the right shape.**
   - For greenfield work, run `dotnet new coreex -n <SolutionName>` first, then add `coreex-api`, `coreex-relay`, and `coreex-subscriber` only as needed.
   - For existing solutions, add only the requested or clearly missing runtime hosts.
   - Preserve existing provider and capability choices unless the user explicitly asks to change them.

5. **Summarize the result and next steps.**
   - Call out what was created or added.
   - Call out what was intentionally omitted or deferred.
   - Mention prerequisites still needed, such as package feeds, CodeGen, or database tooling steps.

## Shape Selection Guide

| Need | Recommendation |
|---|---|
| Empty repo, own data, expose HTTP only | `coreex` + `coreex-api` |
| Empty repo, own data, expose HTTP, publish reliable events | `coreex` + `coreex-api` + `coreex-relay` |
| Empty repo, expose HTTP, publish events, consume events | `coreex` + `coreex-api` + `coreex-relay` + `coreex-subscriber` |
| Existing repo missing only runtime hosts | Infer current shape, then add only the missing hosts |
| Facade over another system with no local database | `coreex --data-provider None --messaging-provider None` and add `coreex-api --data-provider None` only if an API is needed |

## Key References

- `../../docs/coreex/application-scaffolding-guide.md`.
- `../../docs/coreex/layers.md`.
- `../../docs/coreex/patterns.md`.
- `https://github.com/Avanade/CoreEx/blob/main/src/CoreEx.Template/README.md`.
