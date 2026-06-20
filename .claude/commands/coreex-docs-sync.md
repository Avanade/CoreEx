---
description: "Fetch the CoreEx sample architecture docs and all per-package AI usage guides from GitHub and cache them locally under .github/docs/coreex/ for offline, faster expert guidance."
allowed-tools: [Read, Write, Glob, Grep, WebFetch]
---

Sync the CoreEx docs to `.github/docs/coreex/`. Follow these steps exactly.

## Step 1 — Detect current CoreEx version

Search for the `CoreEx` NuGet package version in this order, stopping at the first match:
1. `Directory.Packages.props` — look for `<PackageVersion Include="CoreEx" Version="..." />`
2. Any `*.csproj` file — look for `<PackageReference Include="CoreEx" Version="..." />`
3. `Directory.Build.props`

Record the version (or `unknown` if not found). Use it in the manifest.

## Step 2 — Detect referenced CoreEx packages (for manifest only)

Scan `Directory.Packages.props` and all `*.csproj` files for `PackageVersion` or `PackageReference` entries whose `Include` attribute starts with `CoreEx`. Collect the distinct package names. This list goes into the manifest so the CoreEx Expert knows which packages the project currently uses — it does not limit which guides are synced.

## Step 3 — Create cache directories

Ensure both directories exist. Create them if absent:
- `.github/docs/coreex/`
- `.github/docs/coreex/agents/`

## Step 4 — Fetch and write the sample architecture docs

Fetch each URL below and write to the corresponding local path. Report each as it completes.

| URL | Local path |
|---|---|
| `https://raw.githubusercontent.com/Avanade/CoreEx/main/samples/docs/local-dev.md` | `.github/docs/coreex/local-dev.md` |
| `https://raw.githubusercontent.com/Avanade/CoreEx/main/samples/docs/layers.md` | `.github/docs/coreex/layers.md` |
| `https://raw.githubusercontent.com/Avanade/CoreEx/main/samples/docs/patterns.md` | `.github/docs/coreex/patterns.md` |
| `https://raw.githubusercontent.com/Avanade/CoreEx/main/samples/docs/contracts-layer.md` | `.github/docs/coreex/contracts-layer.md` |
| `https://raw.githubusercontent.com/Avanade/CoreEx/main/samples/docs/domain-layer.md` | `.github/docs/coreex/domain-layer.md` |
| `https://raw.githubusercontent.com/Avanade/CoreEx/main/samples/docs/application-layer.md` | `.github/docs/coreex/application-layer.md` |
| `https://raw.githubusercontent.com/Avanade/CoreEx/main/samples/docs/infrastructure-layer.md` | `.github/docs/coreex/infrastructure-layer.md` |
| `https://raw.githubusercontent.com/Avanade/CoreEx/main/samples/docs/hosts-layer.md` | `.github/docs/coreex/hosts-layer.md` |
| `https://raw.githubusercontent.com/Avanade/CoreEx/main/samples/docs/testing.md` | `.github/docs/coreex/testing.md` |
| `https://raw.githubusercontent.com/Avanade/CoreEx/main/samples/docs/tooling.md` | `.github/docs/coreex/tooling.md` |
| `https://raw.githubusercontent.com/Avanade/CoreEx/main/samples/docs/aspire.md` | `.github/docs/coreex/aspire.md` |

## Step 5 — Fetch and write all per-package guides

Fetch the `AGENTS.md` for every CoreEx package listed below — regardless of whether the project currently references them. This allows the CoreEx Expert to guide on and recommend any package, including ones not yet adopted.

| URL | Local path |
|---|---|
| `https://raw.githubusercontent.com/Avanade/CoreEx/main/src/CoreEx/AGENTS.md` | `.github/docs/coreex/agents/CoreEx.md` |
| `https://raw.githubusercontent.com/Avanade/CoreEx/main/src/CoreEx.AspNetCore/AGENTS.md` | `.github/docs/coreex/agents/CoreEx.AspNetCore.md` |
| `https://raw.githubusercontent.com/Avanade/CoreEx/main/src/CoreEx.AspNetCore.NSwag/AGENTS.md` | `.github/docs/coreex/agents/CoreEx.AspNetCore.NSwag.md` |
| `https://raw.githubusercontent.com/Avanade/CoreEx/main/src/CoreEx.Azure.Messaging.ServiceBus/AGENTS.md` | `.github/docs/coreex/agents/CoreEx.Azure.Messaging.ServiceBus.md` |
| `https://raw.githubusercontent.com/Avanade/CoreEx/main/src/CoreEx.Caching.FusionCache/AGENTS.md` | `.github/docs/coreex/agents/CoreEx.Caching.FusionCache.md` |
| `https://raw.githubusercontent.com/Avanade/CoreEx/main/src/CoreEx.CodeGen/AGENTS.md` | `.github/docs/coreex/agents/CoreEx.CodeGen.md` |
| `https://raw.githubusercontent.com/Avanade/CoreEx/main/src/CoreEx.Data/AGENTS.md` | `.github/docs/coreex/agents/CoreEx.Data.md` |
| `https://raw.githubusercontent.com/Avanade/CoreEx/main/src/CoreEx.Database/AGENTS.md` | `.github/docs/coreex/agents/CoreEx.Database.md` |
| `https://raw.githubusercontent.com/Avanade/CoreEx/main/src/CoreEx.Database.Postgres/AGENTS.md` | `.github/docs/coreex/agents/CoreEx.Database.Postgres.md` |
| `https://raw.githubusercontent.com/Avanade/CoreEx/main/src/CoreEx.Database.SqlServer/AGENTS.md` | `.github/docs/coreex/agents/CoreEx.Database.SqlServer.md` |
| `https://raw.githubusercontent.com/Avanade/CoreEx/main/src/CoreEx.DomainDriven/AGENTS.md` | `.github/docs/coreex/agents/CoreEx.DomainDriven.md` |
| `https://raw.githubusercontent.com/Avanade/CoreEx/main/src/CoreEx.EntityFrameworkCore/AGENTS.md` | `.github/docs/coreex/agents/CoreEx.EntityFrameworkCore.md` |
| `https://raw.githubusercontent.com/Avanade/CoreEx/main/src/CoreEx.Events/AGENTS.md` | `.github/docs/coreex/agents/CoreEx.Events.md` |
| `https://raw.githubusercontent.com/Avanade/CoreEx/main/src/CoreEx.RefData/AGENTS.md` | `.github/docs/coreex/agents/CoreEx.RefData.md` |
| `https://raw.githubusercontent.com/Avanade/CoreEx/main/src/CoreEx.UnitTesting/AGENTS.md` | `.github/docs/coreex/agents/CoreEx.UnitTesting.md` |
| `https://raw.githubusercontent.com/Avanade/CoreEx/main/src/CoreEx.Validation/AGENTS.md` | `.github/docs/coreex/agents/CoreEx.Validation.md` |

If any fetch fails, record the failure, skip that file, and continue.

## Step 6 — Write the manifest

Write `.github/docs/coreex/.manifest` with this exact format:

```
synced: YYYY-MM-DD
coreex-version: <version from Step 1, or "unknown">
referenced-packages: <comma-separated list of packages detected in Step 2, or "none detected">
```

## Step 7 — Report

Summarise:
- How many architecture docs were written successfully.
- How many package guides were written successfully (out of 16).
- Any files that failed to fetch (with the error).
- The CoreEx version and referenced packages recorded in the manifest.
- A reminder: *"Re-run `/coreex-docs-sync` after bumping the CoreEx NuGet version or when the CoreEx Expert suggests the cache is stale."*
