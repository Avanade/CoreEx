# CoreEx Azure Deployment with azd

This folder contains the Azure Developer CLI (azd) project for deploying the Contoso sample services in this repository.

## What this deploys

Infrastructure (Bicep):
- App Service Plan (Linux).
- 7 Web Apps:
  - aspire-dashboard
  - products-api
  - shopping-api
  - products-outbox-relay
  - shopping-outbox-relay
  - products-subscribe
  - shopping-subscribe
- Azure SQL Database (Shopping and Orders domains).
- Azure Database for PostgreSQL Flexible Server (Products domain).
- Azure Service Bus (Standard).
- Azure Managed Redis.
- Application Insights.
- Key Vault.

## Important behaviors

- Dev parameters are applied automatically by a `preprovision` hook in [azure.yaml](azure.yaml).
- The `postprovision` hook grants the provisioning user `Key Vault Administrator` on the deployed Key Vault and stores E2E connection secrets.
- Deployment `location` is sourced from `AZURE_LOCATION` and injected by the preprovision hook.
- SQL admin password is injected at runtime from `AZURE_SQL_ADMIN_PASSWORD` by [infra/scripts/use-dev-params.sh](infra/scripts/use-dev-params.sh).
- PostgreSQL admin password is injected from `AZURE_POSTGRES_ADMIN_PASSWORD` when set; otherwise it defaults to `AZURE_SQL_ADMIN_PASSWORD`.
- The `predeploy` migration hook runs domain-specific providers: Shopping and Orders on SQL Server, Products on PostgreSQL.
- For Products, the migration hook runs `Migrate` + `Schema` + `ResetAndData` to avoid the DbEx PostgreSQL create-stage issue in Azure Flexible Server.
- Key Vault name is unique per deployment (generated in [infra/main.bicep](infra/main.bicep)).
- Multi-targeted .NET projects use a configurable publish framework via environment variable:
  - `AZD_DOTNET_TARGET_FRAMEWORK` (preferred), or
  - `DOTNET_TARGET_FRAMEWORK`.

## Prerequisites

- Azure CLI (`az`).
- Azure Developer CLI (`azd`).
- .NET SDK installed.
- Access to an Azure subscription.
- Outbound port 1433 and 5432 access to run DB updates.

## One-time setup

From this folder:

```bash
cd ./CoreEx/azure
```

Authenticate:

```bash
az login
azd auth login
```

Create/select azd environment if needed:

```bash
azd env new
# or
azd env select <env-name>
```

Set required values:

```bash
azd env set AZURE_SUBSCRIPTION_ID <subscription-id>
azd env set AZURE_LOCATION eastus2
azd env set AZURE_SQL_ADMIN_PASSWORD '<strong-password>'
azd env set AZURE_POSTGRES_ADMIN_PASSWORD '<strong-password>'  # Optional; defaults to AZURE_SQL_ADMIN_PASSWORD.
azd env set AZD_DOTNET_TARGET_FRAMEWORK 'net10.0'
```

Note: Region availability and quota can vary by SKU. `AZD_DOTNET_TARGET_FRAMEWORK` can be set to `net8.0`, `net9.0`, or `net10.0` depending on your requirements.

Load environment variables into your current bash session:

```bash
set -a && eval "$(azd env get-values)" && set +a
```

> **Important**: This step must be repeated every time you open a new shell session. The `preprovision` hook scripts read directly from shell environment variables, not from the azd environment store alone.

Run the preprovision hook manually to generate `infra/main.parameters.json`:

```bash
./infra/scripts/use-dev-params.sh
```

This script:
- Validates that `AZURE_SQL_ADMIN_PASSWORD` and `AZURE_LOCATION` are set.
- Detects your current public IP for SQL and PostgreSQL firewall rules.
- Maps `AZD_DOTNET_TARGET_FRAMEWORK` to the App Service Linux runtime:
  - `net8.0` -> `DOTNETCORE|8.0`
  - `net9.0` -> `DOTNETCORE|9.0`
  - `net10.0` -> `DOTNETCORE|10.0`
- Writes `infra/main.parameters.json` (used by all subsequent azd and az commands).

## Validate before deploy

> **Note**: `azd provision --preview` does not run the `preprovision` hook, so `infra/main.parameters.json` must already exist (generated above) before previewing.

Preview infra changes:

```bash
azd provision --preview --no-prompt
```

If you need the full ARM what-if output:

```bash
az deployment group what-if --resource-group <your-resource-group> --template-file ./infra/main.bicep --parameters ./infra/main.parameters.json --no-pretty-print
```

Package all services:

```bash
azd package --all --no-prompt
```

## Deploy

Provision infra + deploy services:

```bash
azd up --no-prompt
```

Redeploy code only:

```bash
azd deploy --all --no-prompt
```

Re-run infra only:

```bash
azd provision --no-prompt
```

## Accessing the Aspire Dashboard

After deployment, the Aspire Dashboard is publicly accessible from a dedicated HTTPS-enabled App Service. The six deployed services are configured to export OTLP telemetry to it automatically.

Find the dashboard URL:

```bash
az webapp show --resource-group <your-resource-group> --name <dashboard-app-name> --query defaultHostName -o tsv
```

Open `https://<host-name>` in a browser to view the Aspire Dashboard.

If the dashboard prompts for a browser token, retrieve it from the container logs:

```bash
az webapp log tail --resource-group <your-resource-group> --name <dashboard-app-name>
```

Extract only the token (bash):

```bash
TOKEN=$(az webapp log tail --resource-group <your-resource-group> --name <dashboard-app-name> 2>&1 | grep -oE 'login\?t=[A-Za-z0-9]+' | head -n1 | cut -d= -f2)
echo "$TOKEN"
```

Print a ready-to-open login URL:

```bash
HOST=$(az webapp show --resource-group <your-resource-group> --name <dashboard-app-name> --query defaultHostName -o tsv)
TOKEN=$(az webapp log tail --resource-group <your-resource-group> --name <dashboard-app-name> 2>&1 | grep -oE 'login\?t=[A-Za-z0-9]+' | head -n1 | cut -d= -f2)
echo "https://${HOST}/login?t=${TOKEN}"
```

The standalone dashboard displays telemetry views such as:
- Service topology and dependencies
- Health status and logs
- Traces and metrics received over OTLP

Note: standalone Aspire Dashboard mode does not provide the full Aspire resource model UI that the local AppHost provides.

## Running E2E Tests

After deploying with `azd up`, you can run the E2E test runner against the deployed services.

### Get deployed endpoint URLs

Retrieve the deployed App Service endpoints:

```bash
az webapp list --resource-group <your-resource-group> --query "[].hostNames[0]" -o tsv
```

This will show URLs like:
- `app-products-api-dev-{suffix}.azurewebsites.net`
- `app-shopping-api-dev-{suffix}.azurewebsites.net`

Validate the deployed APIs using API/health/swagger paths (not root `/`):

```bash
# Products API
curl -i "https://app-products-api-dev-{suffix}.azurewebsites.net/api/products"

# Shopping API
curl -i "https://app-shopping-api-dev-{suffix}.azurewebsites.net/api/baskets"

# Common liveness and docs paths
curl -i "https://app-products-api-dev-{suffix}.azurewebsites.net/health/ready/detailed"
curl -i "https://app-products-api-dev-{suffix}.azurewebsites.net/swagger"
```

### Retrieve connection strings

After `azd provision` or `azd up`, the postprovision hook automatically stores the following secrets in Key Vault:
- `sql-admin-password`
- `sql-connection-string`
- `postgres-admin-password`
- `postgres-connection-string`

Retrieve them:

```bash
# Get Key Vault name
KV=$(az keyvault list --resource-group <your-resource-group> --query '[0].name' -o tsv)

# SQL connection string
az keyvault secret show --vault-name $KV --name sql-connection-string -o tsv --query value

# PostgreSQL connection string
az keyvault secret show --vault-name $KV --name postgres-connection-string -o tsv --query value
```

### Update E2E configuration

Edit [../samples/tests/Contoso.E2E.Runner/appsettings.json](../samples/tests/Contoso.E2E.Runner/appsettings.json) with the deployed endpoints and connection strings:

```json
{
  "E2E": {
    "Products": {
      "BaseAddress": "https://app-products-api-dev-{suffix}.azurewebsites.net",
      "ConnectionString": "Server=pg-dev-{suffix}.postgres.database.azure.com;Port=5432;Database=coreexdev;User Id={postgres-admin};Password={password};Ssl Mode=Require;Trust Server Certificate=true;"
    },
    "Shopping": {
      "BaseAddress": "https://app-shopping-api-dev-{suffix}.azurewebsites.net",
      "ConnectionString": "Server=sql-dev-{suffix}.database.windows.net;Database=Contoso;User Id=sqladmin;Password={password};Encrypt=true;TrustServerCertificate=false;"
    }
  }
}
```

### Run E2E scenarios

From the repository root:

```bash
cd samples/tests/Contoso.E2E.Runner
dotnet run --framework "${AZD_DOTNET_TARGET_FRAMEWORK:-$DOTNET_TARGET_FRAMEWORK}"
```

This launches an interactive CLI menu to select and execute test scenarios or run load simulations against the deployed APIs.


## Troubleshooting

No project exists / run azd init:

```bash
cd ./CoreEx/azure
azd init
```

No subscriptions found:

```bash
azd auth login --tenant-id <tenant-id>
```

SQL password missing:
- Ensure `AZURE_SQL_ADMIN_PASSWORD` is set before `azd provision` or `azd up`.

PostgreSQL password missing:
- Set `AZURE_POSTGRES_ADMIN_PASSWORD` if your SQL and PostgreSQL admin passwords differ.
- If omitted, deployment hooks default PostgreSQL admin password to `AZURE_SQL_ADMIN_PASSWORD`.

`predeploy` hook fails with missing database outputs:
- Run `azd provision --no-prompt` first to refresh azd environment outputs (`sqlServerName`, `sqlDatabaseName`, `postgresServerName`, `postgresDatabaseName`) before `azd deploy --all --no-prompt`.

Multi-target publish error (NETSDK1129):
- Ensure `AZD_DOTNET_TARGET_FRAMEWORK` is set in your azd environment: `azd env set AZD_DOTNET_TARGET_FRAMEWORK net10.0`.
- Load it into your current shell: `et -a && eval "$(azd env get-values)" && set +a`.

`command not found` while loading environment values:
- This usually means `azd env get-values` was run outside the azd project folder and returned `ERROR: no project exists...`.
- Run from `./CoreEx/azure`, or use `azd -C /path/to/CoreEx/azure env get-values`.

API returns 404 at `/`:
- This is expected for these services.
- Use `/api/...`, `/health`, or `/swagger` paths to validate the deployment.

## Useful commands

Show current environment values:

```bash
azd env get-values
```

List environments:

```bash
azd env list
```

Delete deployed resources:

```bash
azd down --force --purge --no-prompt
```
