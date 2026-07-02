---
name: coreex-docs-sync
description: "Fetch the CoreEx sample architecture docs and all per-package AI usage guides from GitHub and cache them locally under .github/docs/coreex/. Guides for all CoreEx packages are synced regardless of which ones the project currently references, enabling the CoreEx Expert to guide on and recommend any package."
argument-hint: No arguments required. Run to set up or refresh the local doc cache.
tags: ["docs", "cache", "sync", "coreex-expert", "offline"]
---

# Sync CoreEx Docs

Fetches the CoreEx sample architecture docs and AI usage guides for all CoreEx packages from GitHub, writing them to `.github/docs/coreex/`. All 16 package guides are synced unconditionally — this enables the CoreEx Expert to give authoritative guidance on any package, including ones not yet adopted by the project. Writes a manifest recording which packages the project currently references so the expert can distinguish "already in use" from "you'd need to add this."

## When to Use

- **Refresh** the cache after bumping a CoreEx NuGet package version.
- When the CoreEx Expert flags the cache stale (older than 30 days, or a version mismatch).
- To pull the latest CoreEx `main` branch docs.
- **First-time population** only when the cache is absent — e.g. a project that installed the AI assets manually. `dotnet new coreex-ai` already ships the cache at install, so most consumers use this skill to refresh, not populate.

## When Not to Use

- In the CoreEx repository itself — the docs are already present at `samples/docs/` and `src/*/AGENTS.md`.
- When you only need one specific doc — fetch the GitHub URL directly instead.

## What It Does

1. **Detects** the current `CoreEx` NuGet version from `Directory.Packages.props`, `*.csproj`, or `Directory.Build.props`.
2. **Detects** all `CoreEx.*` package references currently in the project — recorded in the manifest for the expert's awareness, not used to limit what is synced.
3. **Creates** `.github/docs/coreex/` and `.github/docs/coreex/agents/` if they do not exist.
4. **Fetches** the 11 sample architecture docs and writes them to `.github/docs/coreex/`.
5. **Fetches** all 16 per-package `AGENTS.md` guides and writes them to `.github/docs/coreex/agents/`.
6. **Writes** `.github/docs/coreex/.manifest` with sync date, CoreEx version, and the list of packages currently referenced in the project.
7. **Reports** success, any fetch failures, and a reminder of when to re-run.

## Cache Layout

```
.github/docs/coreex/
  .manifest                              # synced date, coreex-version, referenced-packages
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
    CoreEx.md                            # always present after sync
    CoreEx.AspNetCore.md                 # always present after sync
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

## Manifest Format

```
synced: YYYY-MM-DD
coreex-version: <detected version, or "unknown">
referenced-packages: CoreEx, CoreEx.Validation, CoreEx.EntityFrameworkCore, ...
```

The `referenced-packages` field lets the CoreEx Expert distinguish between packages already in the project and packages it might recommend adding.

## Re-run Triggers

- CoreEx NuGet version bumped in the project.
- CoreEx Expert reports cache is older than 30 days.
- CoreEx Expert reports a version mismatch between the manifest and the project's current package version.
