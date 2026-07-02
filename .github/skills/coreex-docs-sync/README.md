# CoreEx Docs Sync

Fetches the CoreEx architecture docs and per-package AI guides from GitHub and caches them locally. Once cached, the `coreex-expert` agent uses the local copies instead of making live GitHub fetches on every question.

## When to run

- **After bumping a CoreEx NuGet version** — refreshes the guides to match the version in use.
- **When the CoreEx Expert recommends it** — the agent checks the manifest on every session and flags if the cache is older than 30 days or the recorded version doesn't match the project's current packages.
- **First-time population** — `dotnet new coreex-ai` already ships the cache at install; run this only if the cache is absent (e.g. the assets were copied in manually) or to pull the latest `main`.

Do not run this inside the CoreEx repository itself — the docs are already present locally at `samples/docs/` and `src/*/AGENTS.md`.

## How to invoke

**Claude Code:**
```
/coreex-docs-sync
```

**GitHub Copilot Chat:**
```
#file:.github/skills/coreex-docs-sync/SKILL.md  sync the CoreEx docs
```

No arguments required.

## What gets written

```
.github/docs/coreex/
  .manifest                    ← sync date, CoreEx version, referenced packages
  local-dev.md
  layers.md
  patterns.md
  contracts-layer.md
  domain-layer.md
  application-layer.md
  infrastructure-layer.md
  hosts-layer.md
  testing.md
  tooling.md
  aspire.md
  agents/
    CoreEx.md                  ← one guide per package, always synced
    CoreEx.AspNetCore.md
    CoreEx.AspNetCore.NSwag.md
    CoreEx.Azure.Messaging.ServiceBus.md
    CoreEx.Caching.FusionCache.md
    CoreEx.CodeGen.md
    CoreEx.Data.md
    CoreEx.Database.md
    CoreEx.Database.Postgres.md
    CoreEx.Database.SqlServer.md
    CoreEx.DomainDriven.md
    CoreEx.EntityFrameworkCore.md
    CoreEx.Events.md
    CoreEx.RefData.md
    CoreEx.UnitTesting.md
    CoreEx.Validation.md
```

All 16 package guides are synced unconditionally — not just the packages the project currently references. This lets the expert recommend adopting a new package with full knowledge of what it offers. The `referenced-packages` field in the manifest records which packages are actually in the project so the expert can distinguish the two.

## Reference

- [SKILL.md](./SKILL.md) — full detail on detection logic, manifest format, and re-run triggers.
- [CoreEx Expert agent README](../agents/README.md) — how the agent uses the cache and when it suggests a refresh.
