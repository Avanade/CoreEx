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

Copy the following from this repository into any project that references CoreEx NuGet packages:

```
.github/
  copilot-instructions.md
  agents/
    coreex-expert.agent.md
  instructions/
    coreex-conventions.instructions.md
    coreex-contracts.instructions.md
    coreex-application-services.instructions.md
    coreex-validators.instructions.md
    coreex-repositories.instructions.md
    coreex-api-controllers.instructions.md
    coreex-event-subscribers.instructions.md
    coreex-host-setup.instructions.md
    coreex-tooling.instructions.md
    coreex-tests.instructions.md
    coreex-domain.instructions.md
  skills/
    coreex-docs-sync/
      SKILL.md
    generate-domain/           # optional — new domain scaffolding
    add-capability/            # optional — retrofit messaging/integration
    acquire-codebase-knowledge/ # optional — repo onboarding docs
```

On first use, run `/coreex-docs-sync` to populate the local cache. Re-run whenever the CoreEx NuGet version is bumped.
