# CoreEx Expert Agent

The `coreex-expert` agent gives GitHub Copilot and Claude Code authoritative, CoreEx-idiomatic guidance — architecture decisions, pattern selection, layer design, and implementation advice — all aligned to the Contoso sample implementations.

It is defined once in `coreex-expert.agent.md` and works in both tools from the same file.

| Tool | How to invoke |
|------|--------------|
| GitHub Copilot Chat | Switch to **Agent** mode and select **CoreEx Expert** |
| Claude Code | `@coreex-expert` |

---

## How the agent resolves guidance

The agent relies on a local doc cache rather than making live GitHub fetches on every question. The cache is populated by `/coreex-docs-sync` and stored under `.github/docs/coreex/`.

```
Developer asks a CoreEx question
              │
              ▼
  Read .github/docs/coreex/.manifest
              │
        ┌─────┴─────┐
        │           │
   manifest      no manifest
    present        │
        │          └──► offer to run /coreex-docs-sync first
        │                (never runs silently — always asks first)
        │
  check staleness
        │
   ┌────┴────┐
   │         │
 fresh     stale
   │       (>30 days OR coreex-version in manifest
   │        differs from version in project files)
   │         │
   │         └──► recommend running /coreex-docs-sync
   │
   ▼
Use local cache (always preferred over live GitHub fetches)
   │
   ├── .github/docs/coreex/*.md           ← 10 architecture docs
   │
   └── .github/docs/coreex/agents/*.md   ← 16 per-package AI guides
              │
    read manifest referenced-packages
    to distinguish:
      • package already in project  →  guide on current usage
      • package not yet in project  →  recommend adopting it
```

The `referenced-packages` field in the manifest lets the agent distinguish between a package the project already uses and one it would need to add — without restricting which guides get synced.

---

## The local doc cache

`/coreex-docs-sync` fetches from the CoreEx GitHub repository and writes two folders:

**`.github/docs/coreex/`** — 10 architecture docs:

| File | Content |
|------|---------|
| `layers.md` | Full layer dependency diagram and design-time tooling overview |
| `patterns.md` | Pattern catalog — every architectural, application, messaging, and testing pattern |
| `contracts-layer.md` | Generated contracts, `[Contract]`, `[ReferenceData]`, source generation |
| `domain-layer.md` | Aggregates, mutation guards, integration-event accumulation, `Result<T>` pipelines |
| `application-layer.md` | Service orchestration, `TransactionAsync`, validators, policies, adapters |
| `infrastructure-layer.md` | EF Core repositories, mappers, outbox wiring, relay publisher |
| `hosts-layer.md` | API, Subscribe, and Outbox Relay `Program.cs` shapes, middleware ordering |
| `testing.md` | Unit, integration, API, Subscribe, and Relay test patterns |
| `tooling.md` | CodeGen and Database project run order, generated-file ownership |
| `aspire.md` | Aspire orchestration for local distributed development and E2E testing |

**`.github/docs/coreex/agents/`** — 16 per-package AI usage guides, one per CoreEx NuGet package. All 16 are synced unconditionally so the agent can guide on any package — including ones the project hasn't adopted yet.

**`.github/docs/coreex/.manifest`** — records `synced` date, `coreex-version`, and `referenced-packages`.

### Staleness triggers

| Trigger | Agent action |
|---------|-------------|
| Cache absent | Offers to run `/coreex-docs-sync` before the first GitHub fetch |
| `synced` date > 30 days | Recommends a refresh |
| `coreex-version` in manifest ≠ version in project files | Recommends a refresh |

---

## Why sync all 16 package guides unconditionally

An earlier design synced only the packages the project already references. This was changed because:

- The agent cannot recommend adopting a package (e.g. `CoreEx.Caching.FusionCache`) if it has no knowledge of what that package offers.
- All 16 guides are small markdown files — the total download is negligible.
- Syncing all unconditionally removes the need to re-run after adding a new package.
- The `referenced-packages` manifest field preserves the "in project vs. not yet" distinction without making it a gate on what gets synced.

---

## Adopting the agent in a consuming project

Do not copy files by hand. The whole AI workflow set — including this agent — is installed by the `coreex-ai` template. From the **repo root** of the project that references CoreEx NuGet packages:

Resolve the version to pin first — **never run a bare `dotnet new install CoreEx.Template`** (it silently resolves to whatever is latest at that moment, which can desync the AI assets from the project's actual CoreEx version — see [AGENTS.md](../../AGENTS.md), Step 3, for the full resolution rule):

```bash
# Install the pinned template version (skip if already installed at that version)
dotnet new install CoreEx.Template::<version>

# Single-repo project:
dotnet new coreex-ai

# Monorepo — replace <subfolder> with the CoreEx app path (e.g. backend):
dotnet new coreex-ai --app-folder <subfolder>
```

`dotnet new coreex-ai` installs everything the agent depends on:

- `.github/instructions/` — the scoped, auto-injected instruction files
- `.github/prompts/` — the `coreex-scaffold` prompt plus one prompt per per-capability (L1) skill
- `.github/skills/` — the full skill suite (`coreex-docs-sync`, `acquire-codebase-knowledge`, `coreex-solution-scaffolder`, `aspire`, and the 14 L1 skills)
- `.github/agents/coreex-expert.agent.md` — this agent
- `.claude/commands/` — the Claude Code equivalents
- `.github/docs/coreex/` — the local docs cache (architecture docs + per-package guides) the expert reads first, already populated at the pinned version — no separate sync step needed on first install

Later, when the project's `CoreEx` NuGet version is bumped, re-run `/coreex-docs-sync` to refresh the whole bundle (instructions, skills, prompts, this agent, and the docs cache) to the matching `CoreEx.Template` release — the agent recommends this automatically when the cached `coreex-version` no longer matches the project (see the resolution flowchart above).

For deterministic project scaffolding, use the [CoreEx.Template](https://github.com/Avanade/CoreEx/blob/main/src/CoreEx.Template/README.md) `dotnet new coreex*` template pack rather than an agent skill.
