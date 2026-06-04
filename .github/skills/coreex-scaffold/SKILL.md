---
name: coreex-scaffold
description: "Guide a developer through CoreEx solution shaping after the repository has already been bootstrapped, or for an existing scaffolded solution missing runtime hosts. USE FOR: bootstrap-only repos, deciding API-only vs API plus relay vs API plus subscriber, choosing SQL Server vs Postgres vs no database, choosing refdata/outbox/DDD/ROP options, installing CoreEx.Template, checking current CoreEx solution shape, and adding missing Api/Outbox.Relay/Subscribe hosts to an existing repo. DO NOT USE FOR: unrelated runtime debugging, bootstrap creation, or forcing root re-scaffolding over an existing solution. INVOKES: workspace inspection, user questions, dotnet new install/list, dry-run validation, and either template generation or manual retrofit work depending on repo shape."
argument-hint: "Optional: proposed solution name, required hosts, database choice, messaging choice, and architectural constraints."
tags: ["coreex", "scaffolding", "retrofit", "template", "hosts"]
---

# CoreEx Scaffold

Guides a repository through the right CoreEx setup path based on its current shape.

## When to Use

- Starting a new CoreEx domain or service from a repository that already contains only the bootstrap AI assets.
- Deciding whether the first cut should create just the API host or also include the outbox relay and/or subscriber hosts.
- Adding missing `Api`, `Outbox.Relay`, or `Subscribe` hosts to an existing scaffolded CoreEx solution.
- Converting project needs into either the matching `dotnet new coreex*` commands or the safest retrofit path.

## When Not to Use

- You are debugging local runtime, container, Aspire, or package restore issues unrelated to project shaping.
- You still need to create the initial bootstrap repository; run `coreex-bootstrap` before this skill is used.
- You want architectural guidance for an existing implementation beyond project setup; use `CoreEx Expert` instead.

## Workflow

### Prerequisite

Assume the repository is already in one of these states before this skill runs:
- a bootstrap-only shell created earlier by `coreex-bootstrap`; or
- an existing CoreEx solution that is missing some runtime hosts.

### Main Workflow Steps

1. **Inspect the workspace shape first.**
   - Determine whether the repository is a bootstrap-only shell or already contains real solution content.
   - Use specific evidence such as an existing `.slnx`, populated `src/`, existing `Database` or `CodeGen` projects, or existing test projects.

2. **Validate the naming shape before any template command.**
   - For `coreex`, `coreex-api`, `coreex-relay`, `coreex-subscriber` (all domain templates): Require the canonical format `[Company].[Product].[Domain]`.
   - Do not pass host-specific names such as `Company.Product.Domain.Api` to the host templates; those templates derive the suffixes automatically.
   - The existing repository may use a non-canonical root such as `Company.Product`.  Before calling any template, stop and ask what the domain name segment should be.

3. **Branch to the right path.**
   - If the workspace is bootstrap-only, run the scaffold interview and choose the smallest safe domain shape.
   - If the workspace is already scaffolded, infer the current solution name, providers, and capability flags from local artifacts and ask only what is still missing.

4. **Choose the safest implementation path.**
   - Verify the template pack first with `dotnet new list --tag CoreEx`.
   - If missing, run `dotnet new install CoreEx.Template`.
   - Always use `dotnet new ... --dry-run` before the first real template invocation unless the workspace is empty and the command shape is already obvious.
   - Inspect the dry-run file actions and stop if they show nested root folders, incorrect host/test project names, or any conflicting layout.
   - For existing repos, do not run templates directly into the workspace when they create nested root folders or conflicting layout.
   - Prefer a manual retrofit aligned to the repo structure when template output does not fit.

5. **Apply the right shape.**
   - For a bootstrap-only repo, run `coreex` first, then add host templates (`coreex-api`, `coreex-relay`, `coreex-subscriber`) only as needed.
   - For domain templates (`coreex` and host templates), pass the canonical three-part base solution name to `-n` and use `-o .` at the repository root.
   - For all host templates, keep `-n <SolutionName>` equal to the same base solution name used for `coreex`.
   - For existing solutions, add only the requested or clearly missing runtime hosts.
   - Preserve existing provider and capability choices unless the user explicitly asks to change them.

6. **Summarize the result and next steps.**
   - Call out what was created or added.
   - Call out what was intentionally omitted or deferred.
   - Mention prerequisites still needed, such as package feeds, CodeGen, or database tooling steps.

## Naming Rules

**Domain and Host Templates:**
- All of `coreex`, `coreex-api`, `coreex-relay`, `coreex-subscriber` require the canonical format: `[Company].[Product].[Domain]`.
- `coreex -n Company.Product.Domain` establishes the canonical base solution name.
- `coreex-api -n Company.Product.Domain` generates the API host and its tests from that same base name.
- `coreex-relay -n Company.Product.Domain` generates the outbox relay host and its tests from that same base name.
- `coreex-subscriber -n Company.Product.Domain` generates the subscriber host and its tests from that same base name.
- Do not substitute the host project name into `-n`; the templates already append or derive the host-specific suffixes.
- If the user gives only a two-part name for domain templates, ask for the missing segment before running the templates.
- If the repository already exists with a non-canonical root name (e.g., `Avanade.Books`), prefer a manual retrofit unless the user explicitly wants to rename the solution first.

## Command Patterns

### Bootstrap-Only Repository

```text
# Run the core domain template first, then add only the required hosts.
dotnet new coreex -n Avanade.Product.Books --data-provider SqlServer --messaging-provider ServiceBus --refdata-enabled true --outbox-enabled true --domain-driven-enabled false --rop-enabled false -o .

# Add host templates in sequence (only those needed)
dotnet new coreex-api -n Avanade.Product.Books -o . --data-provider SqlServer --refdata-enabled true --outbox-enabled true
dotnet new coreex-relay -n Avanade.Product.Books -o . --data-provider SqlServer --messaging-provider ServiceBus
dotnet new coreex-subscriber -n Avanade.Product.Books -o . --messaging-provider ServiceBus
```

### Existing Repository Retrofit (Skip Bootstrap)

```text
# If the repo already has a bootstrap or partial scaffold, run only the missing domain/host templates
# First confirm the canonical three-part base name, then run coreex and/or host templates
dotnet new coreex -n Company.Product.Domain ...
dotnet new coreex-api -n Company.Product.Domain ...
# etc.
```

## Existing Repository Guardrails

- **Bootstrap is a prereq:** This skill assumes any bootstrap creation has already happened before the workflow starts.
- **For retrofit work:** If the repo already contains a solution, `src/`, tooling, or tests, do not re-run the root scaffold unless the current shape is still only the bootstrap shell.
- **Pre-flight validation:** Always run a dry-run with the intended base solution name before any real domain/host template invocation.
- **Watch for nested roots:** If dry-run output shows paths like `src\\Company.Product.Domain.Api\\src\\Company.Product.Domain.Api\\...`, the output root is wrong; stop and change strategy.
- **Naming mismatch:** If dry-run output shows incorrect project names because the repo uses a non-canonical root name, stop and recommend either renaming the solution first or manually creating the missing hosts.
- **No force:** Do not use `--force` to push through a mismatched layout. The mismatch itself indicates an unsafe operation.

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
