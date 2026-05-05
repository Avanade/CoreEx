# AGENTS.md — Azure Deployment

Operational guide for AI agents working in the `azure/` folder of this repository. This deploys the Contoso sample services (under `samples/src/`) to Azure using either **Azure Developer CLI (azd) + Bicep** or **Terraform**.

## Scope

This file applies to anything under `azure/`. For application code, see the relevant `samples/` projects. Companion human-facing docs:

- [README.md](README.md) — azd + Bicep workflow.
- [terraform/README.md](terraform/README.md) — Terraform workflow.

## Folder layout

- [azure.yaml](azure.yaml) — azd project manifest. Declares the 6 services and the pre/post hooks.
- [infra/](infra/) — Bicep templates (primary IaC for `azd`).
  - [infra/main.bicep](infra/main.bicep) — Entry template.
  - [infra/modules/](infra/modules/) — Per-resource modules (`app-service-plan`, `app-services`, `aspire-dashboard`, `database`, `service-bus`, `redis`, `key-vault`, `application-insights`).
  - [infra/scripts/](infra/scripts/) — Hook scripts (`use-dev-params.*`, `store-secrets.*`).
  - `main.{dev,test,prod}.bicepparam` — Environment parameter files.
- [terraform/](terraform/) — Terraform implementation that mirrors the Bicep deployment (parity must be maintained when changing one or the other).
- [scripts/](scripts/) — Higher-level deployment helper scripts (DB migrations, SQL firewall, packaging).

## What gets deployed

Both Bicep and Terraform provision the same resource set:

- Linux App Service Plan.
- 7 Web Apps: `aspire-dashboard`, `products-api`, `shopping-api`, `products-outbox-relay`, `shopping-outbox-relay`, `products-subscribe`, `shopping-subscribe`.
- Azure SQL Server + Database (with firewall rules).
- Azure Service Bus (Standard) — namespace + topic + subscriptions.
- Azure Managed Redis.
- Application Insights.
- Key Vault (per-deployment unique name; stores E2E secrets).

## Conventions

- Comments end with a period/fullstop (repo-wide rule from [.github/copilot-instructions.md](../.github/copilot-instructions.md)).
- Keep Bicep and Terraform in sync. Any new resource, parameter, or wiring change in `infra/` must have an equivalent change in `terraform/` (and vice versa). Cross-reference by env: `main.dev.bicepparam` ↔ `dev.tfvars`, etc.
- Resource naming, SKUs, and per-environment values are driven by the parameter/tfvars files — do not hardcode environment-specific values in templates.
- Key Vault name is generated uniquely per deployment in [infra/main.bicep](infra/main.bicep). Do not assume a fixed name.
- Multi-targeted .NET projects must be published with a single TFM. The TFM is sourced from `AZD_DOTNET_TARGET_FRAMEWORK` (preferred) or `DOTNET_TARGET_FRAMEWORK` and mapped to App Service Linux `DOTNETCORE|<version>` by the preprovision hook.

## azd hooks (defined in [azure.yaml](azure.yaml))

- `preprovision` → `infra/scripts/use-dev-params.{sh,ps1}` — selects the dev parameter file, injects `AZURE_LOCATION` and `AZURE_SQL_ADMIN_PASSWORD` into `main.parameters.json`, maps the .NET TFM to the App Service runtime.
- `predeploy` → `scripts/run-products-db-migrations.{sh,ps1}` — runs DB migrations against the provisioned SQL DB before app code deploys.
- `postprovision` → `infra/scripts/store-secrets.{sh,ps1}` — grants the provisioning user `Key Vault Administrator` and stores `sql-admin-password`, `sql-connection-string`, `service-bus-connection-string` in Key Vault.

When editing hook scripts, keep the bash and PowerShell variants behaviorally identical.

## Required environment variables

Set in the azd environment (`azd env set <KEY> <VALUE>`):

- `AZURE_SUBSCRIPTION_ID` — target subscription.
- `AZURE_LOCATION` — e.g. `eastus2`.
- `AZURE_SQL_ADMIN_PASSWORD` — strong password; consumed by hooks, never committed.
- `AZD_DOTNET_TARGET_FRAMEWORK` — one of `net8.0`, `net9.0`, `net10.0`.

Load into the current shell before running ad-hoc `az` / `terraform` commands:

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

### Terraform

```bash
cd azure/terraform
./apply.sh dev plan
./apply.sh dev apply
```

`apply.sh` loads `azd env` values, resolves the runner public IP for SQL firewall, and maps `AZD_DOTNET_TARGET_FRAMEWORK` to `app_service_linux_fx_version`.

## Validating changes

Before declaring an infra change complete:

1. **Bicep**: `azd provision --preview --no-prompt` from `azure/`, or run `az deployment group what-if` against `infra/main.bicep` with `infra/main.parameters.json` after the preprovision hook has populated it.
2. **Terraform**: `./apply.sh <env> plan` and confirm no unintended destroy/replace operations.
3. **Parity**: diff the resource set between Bicep `what-if` and Terraform `plan` when changing either side.
4. **Hooks**: if you touched `infra/scripts/` or `scripts/`, run the bash and PowerShell variants (or read them carefully) to confirm parity.

Do not run `azd up`, `azd down`, `terraform apply`, or `terraform destroy` without explicit user approval — these touch live Azure resources.

## Secrets and safety

- Never commit `AZURE_SQL_ADMIN_PASSWORD`, generated parameter files containing secrets, `terraform.tfstate*`, or any Key Vault content.
- The post-provision hook is the canonical source for runtime secrets in Key Vault. Do not duplicate secret-storage logic elsewhere.
- Treat `terraform.tfstate` as sensitive; do not print or echo it.
- Avoid destructive Azure operations (`az group delete`, `azd down`, `terraform destroy`, dropping SQL DBs) unless the user has confirmed in this turn.

## Troubleshooting cheatsheet

- **Multi-target publish error (NETSDK1129)** — set `AZD_DOTNET_TARGET_FRAMEWORK` and reload env.
- **SQL password missing** — set `AZURE_SQL_ADMIN_PASSWORD` before `azd provision` / `terraform apply`.
- **API returns 404 at `/`** — expected; probe `/api/...`, `/health/ready/detailed`, or `/swagger`.
- **Aspire Dashboard requires token** — fetch from `az webapp log tail` (see [README.md](README.md#accessing-the-aspire-dashboard)).
- **`azd init` says no project** — run from `azure/`, not the repo root.

## E2E tests against deployed services

Endpoints, SQL, and Service Bus connection strings can be wired into [../samples/tests/Contoso.E2E.Runner/appsettings.json](../samples/tests/Contoso.E2E.Runner/appsettings.json). Pull connection strings from Key Vault (populated by the postprovision hook) rather than reconstructing them. See the "Running E2E Tests" section of [README.md](README.md) for exact commands.
