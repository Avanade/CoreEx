---
description: "Operational guidance for AI agents deploying Contoso sample services to Azure via azd/Bicep"
scope: "azure/"
tags: ["azure", "deployment", "iac", "bicep"]
---

# AGENTS.md — Azure Deployment

Operational guide for AI agents working in the `azure/` folder of this repository. This deploys the Contoso sample services (under `samples/src/`) to Azure using **Azure Developer CLI (azd) + Bicep**.

## Scope

This file applies to anything under `azure/`. For application code, see the relevant `samples/` projects. Companion human-facing docs:

- [README.md](README.md) — azd + Bicep workflow.

## Folder layout

- [azure.yaml](azure.yaml) — azd project manifest. Declares the 8 services and the pre/post hooks.
- [infra/](infra/) — Bicep templates (primary IaC for `azd`).
  - [infra/main.bicep](infra/main.bicep) — Entry template.
  - [infra/modules/](infra/modules/) — Per-resource modules (`app-service-plan`, `app-services`, `aspire-dashboard`, `database`, `postgres-database`, `service-bus`, `redis`, `key-vault`, `application-insights`).
  - [infra/scripts/](infra/scripts/) — Hook scripts (`use-dev-params.*`, `store-secrets.*`).
  - `main.{dev,test,prod}.bicepparam` — Environment parameter files.
- [scripts/](scripts/) — Higher-level deployment helper scripts (DB migrations, SQL firewall, packaging).

## What gets deployed

The Bicep deployment provisions the following resource set:

- Linux App Service Plan.
- 7 Web Apps: `aspire-dashboard`, `products-api`, `shopping-api`, `products-outbox-relay`, `shopping-outbox-relay`, `products-subscribe`, `shopping-subscribe`.
- Azure SQL Server + Database (Shopping and Orders domains, with firewall rules).
- Azure Database for PostgreSQL Flexible Server + Database (Products domain).
- Azure Service Bus (Standard) — namespace + topic + subscriptions.
- Azure Managed Redis.
- Application Insights.
- Key Vault (per-deployment unique name; stores E2E secrets).

## Conventions

- Comments end with a period/fullstop (repo-wide rule from [.github/copilot-instructions.md](../.github/copilot-instructions.md)).
- Resource naming, SKUs, and per-environment values are driven by the parameter files — do not hardcode environment-specific values in templates.
- Key Vault name is generated uniquely per deployment in [infra/main.bicep](infra/main.bicep). Do not assume a fixed name.
- Multi-targeted .NET projects must be published with a single TFM. The TFM is sourced from `AZD_DOTNET_TARGET_FRAMEWORK` (preferred) or `DOTNET_TARGET_FRAMEWORK` and mapped to App Service Linux `DOTNETCORE|<version>` by the preprovision hook.

## azd hooks (defined in [azure.yaml](azure.yaml))

- `preprovision` → `infra/scripts/use-dev-params.{sh,ps1}` — selects the dev parameter file, injects `AZURE_LOCATION` and `AZURE_SQL_ADMIN_PASSWORD` into `main.parameters.json`, maps the .NET TFM to the App Service runtime.
- `predeploy` → `scripts/run-products-db-migrations.{sh,ps1}` — runs domain-specific DB migrations before app code deploys: Shopping/Orders on SQL; Products on PostgreSQL. Products uses `Migrate` + `Schema` + `ResetAndData` sequence.
- `postprovision` → `infra/scripts/store-secrets.{sh,ps1}` — grants the provisioning user `Key Vault Administrator` and stores `sql-admin-password`, `sql-connection-string`, `postgres-admin-password`, `postgres-connection-string`, `service-bus-connection-string` in Key Vault.

When editing hook scripts, keep the bash and PowerShell variants behaviorally identical.

## Required environment variables

Set in the azd environment (`azd env set <KEY> <VALUE>`):

- `AZURE_SUBSCRIPTION_ID` — target subscription.
- `AZURE_LOCATION` — e.g. `eastus2`.
- `AZURE_SQL_ADMIN_PASSWORD` — strong password; consumed by hooks, never committed.
- `AZURE_POSTGRES_ADMIN_PASSWORD` — optional; if omitted, hooks default to `AZURE_SQL_ADMIN_PASSWORD`.
- `AZD_DOTNET_TARGET_FRAMEWORK` — one of `net8.0`, `net9.0`, `net10.0`.

Load into the current shell before running ad-hoc `az` commands:

```bash
set -a && eval "$(azd env get-values)" && set +a
```

## Common workflows

### azd + Bicep

```bash
cd azure
azd provision --preview --no-prompt   # plan.
azd package --all --no-prompt          # build & package services.
azd up --no-prompt                     # full provision + deploy.
azd deploy --all --no-prompt           # code-only redeploy.
azd down --force --purge --no-prompt   # tear down.
```

## Helper scripts

The `azure/scripts/` folder includes two helper scripts that should be preferred over manual command chains when validating a deployment.

### get-aspire-dashboard-login

- Files: `scripts/get-aspire-dashboard-login.sh`, `scripts/get-aspire-dashboard-login.ps1`.
- Purpose: prints the Aspire Dashboard URL and a ready-to-open login URL when a dashboard token can be found.
- Required argument: `--resource-group` / `-ResourceGroup`.
- Optional arguments: dashboard app name and token timeout.
- Discovery behavior: auto-detects the dashboard app when not explicitly provided.
- Token retrieval order:
  1. SCM/Kudu command API query of runtime `container.log` (last 60 minutes).
  2. SCM/Kudu log archive scan.
  3. Live `az webapp log tail` fallback with timeout.
- Output always includes dashboard app name and dashboard URL; login URL is printed only when token extraction succeeds.

### setup-e2e-runner

- Files: `scripts/setup-e2e-runner.sh`, `scripts/setup-e2e-runner.ps1`.
- Purpose: wires `samples/tests/Contoso.E2E.Runner/appsettings.json` to deployed Azure endpoints and connection strings.
- Required argument: `--resource-group` / `-ResourceGroup`.
- Optional arguments: appsettings path, key vault name, Products app name, Shopping app name, skip-validation, insecure validation mode.
- Discovery behavior: auto-detects Products and Shopping app names plus Key Vault when omitted.
- Secret retrieval: reads `postgres-connection-string` and `sql-connection-string` from Key Vault.
- Validation behavior (unless skipped):
  - `GET /api/products` for Products.
  - `POST /api/customers/test/baskets` for Shopping.
  - health and swagger endpoints for both services.
- Update behavior: creates `appsettings.json.bak` before writing updated `E2E.Products` and `E2E.Shopping` values.

### refresh-keyvault-appsettings

- Files: `scripts/refresh-keyvault-appsettings.sh`, `scripts/refresh-keyvault-appsettings.ps1`.
- Purpose: refreshes App Service Key Vault appsetting references and restarts targeted web apps.
- Required argument: `--resource-group` / `-ResourceGroup`.
- Optional arguments: one or more app names and wait-after-restart seconds.
- Discovery behavior: when app names are omitted, all web apps in the resource group are targeted.
- Primary use case: recover from startup failures immediately after deploy when Key Vault-backed settings have not been fully applied yet.

When editing either helper script, keep the bash and PowerShell variants behaviorally identical.

## Validating changes

Before declaring an infra change complete:

1. **Bicep**: `azd provision --preview --no-prompt` from `azure/`, or run `az deployment group what-if` against `infra/main.bicep` with `infra/main.parameters.json` after the preprovision hook has populated it.
2. **Hooks**: if you touched `infra/scripts/` or `scripts/`, run the bash and PowerShell variants (or read them carefully) to confirm parity.

Do not run `azd up` or `azd down` without explicit user approval — these touch live Azure resources.

## Secrets and safety

- Never commit `AZURE_SQL_ADMIN_PASSWORD`, generated parameter files containing secrets, or any Key Vault content.
- The post-provision hook is the canonical source for runtime secrets in Key Vault. Do not duplicate secret-storage logic elsewhere.
- Avoid destructive Azure operations (`az group delete`, `azd down`, dropping SQL DBs) unless the user has confirmed in this turn.

## Troubleshooting cheatsheet

- **Multi-target publish error (NETSDK1129)** — set `AZD_DOTNET_TARGET_FRAMEWORK` and reload env.
- **SQL password missing** — set `AZURE_SQL_ADMIN_PASSWORD` before `azd provision`.
- **PostgreSQL password missing** — set `AZURE_POSTGRES_ADMIN_PASSWORD` when different from SQL admin password.
- **Predeploy missing output keys** — run `azd provision --no-prompt` before `azd deploy --all --no-prompt` to refresh `sql*` and `postgres*` output values in azd env.
- **API returns 404 at `/`** — expected; probe `/api/...`, `/health/ready/detailed`, or `/swagger`.
- **Startup fails with invalid Service Bus/connection string right after deploy** — run `./scripts/refresh-keyvault-appsettings.sh --resource-group <rg>` (or the PowerShell variant) to refresh Key Vault references and restart apps.
- **Aspire Dashboard requires token** — fetch from `az webapp log tail` (see [README.md](README.md#accessing-the-aspire-dashboard)).
- **`azd init` says no project** — run from `azure/`, not the repo root.

## E2E tests against deployed services

Endpoints, SQL, and Service Bus connection strings can be wired into [../samples/tests/Contoso.E2E.Runner/appsettings.json](../samples/tests/Contoso.E2E.Runner/appsettings.json). Pull connection strings from Key Vault (populated by the postprovision hook) rather than reconstructing them. See the "Running E2E Tests" section of [README.md](README.md) for exact commands.
