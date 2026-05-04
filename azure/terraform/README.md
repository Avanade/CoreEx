# Terraform Equivalent of Azure Bicep Deployment

This folder provides a Terraform implementation that mirrors the current resources provisioned by `azure/infra` Bicep templates.

> [!NOTE]
> This does NOT deploy any application code or run DB migrations.  It only deploys the base infrastructure.
> The AZD command in azure/infra deploys both the infrastructure and the code.

## Prerequisites

Before running `terraform plan` (or `./apply.sh <env> plan`), ensure all of the following are complete.

### Tooling

- Terraform CLI installed (compatible with [versions.tf](versions.tf)).
- Azure CLI (`az`) installed.
- `curl` available (required by `apply.sh` to resolve public IP).
- Azure Developer CLI (`azd`) installed (required by `apply.sh` to load environment values).

### Azure Authentication and Subscription

- Logged in to Azure CLI:

```bash
az login
```

- Correct subscription selected:

```bash
az account set --subscription <subscription-id>
```

### Required Identity Permissions

The deploying identity must be able to create/update all resources in scope and perform RBAC/secret operations, including:

- Create/update role assignments (for Key Vault Administrator assignment on the deployed vault).
- Set Key Vault secrets.
- Create/update App Service, SQL, Service Bus, Redis, Application Insights, and Key Vault resources.

### Required Input Values

- `AZURE_SQL_ADMIN_PASSWORD` must be set before running `./apply.sh`.
- Environment tfvars file must exist for your target environment (`dev.tfvars`, `test.tfvars`, or `prod.tfvars`).

Example:

```bash
export AZURE_SQL_ADMIN_PASSWORD='<strong-password>'
```

### Framework Selection (for `apply.sh`)

`apply.sh` derives `app_service_linux_fx_version` from one of:

- `AZD_DOTNET_TARGET_FRAMEWORK` (preferred), or
- `DOTNET_TARGET_FRAMEWORK`.

Supported values: `net8.0`, `net9.0`, `net10.0`.

Example:

```bash
export AZD_DOTNET_TARGET_FRAMEWORK='net10.0'
```

### Terraform Initialization

Initialize providers in the Terraform folder before planning:

```bash
cd azure/terraform
terraform init
```

Optional validation:

```bash
terraform fmt -recursive
terraform validate
```

## What It Deploys

- Linux App Service Plan.
- 6 Linux Web Apps:
  - `products-api`
  - `shopping-api`
  - `products-outbox-relay`
  - `shopping-outbox-relay`
  - `products-subscribe`
  - `shopping-subscribe`
- Aspire Dashboard Linux container Web App.
- Application Insights.
- Key Vault.
- Service Bus namespace + topic + subscriptions.
- Azure SQL server + database + firewall rules.
- Azure Managed Redis (redisEnterprise) + default database.

## Environment Files

- `dev.tfvars` matches `azure/infra/main.dev.bicepparam` values.
- `test.tfvars` matches `azure/infra/main.test.bicepparam` values.
- `prod.tfvars` matches `azure/infra/main.prod.bicepparam` values.

## Usage

```bash
cd azure/terraform
./apply.sh dev plan
./apply.sh dev apply
```

Or run Terraform manually with one of the environment files:

```bash
cd azure/terraform
export TF_VAR_sql_admin_password="$AZURE_SQL_ADMIN_PASSWORD"
terraform init
terraform plan -var-file=dev.tfvars
terraform apply -var-file=dev.tfvars
```

## Notes

- This deployment creates/manages the resource group named by `resource_group_name` if it does not already exist.
- The `apply.sh` script loads `azd env` values (if available), resolves current public runner IP, and maps `AZD_DOTNET_TARGET_FRAMEWORK` to `app_service_linux_fx_version`.
- Sensitive values such as `sql_admin_password` should come from secure sources and are injected via `TF_VAR_sql_admin_password`.
- Naming and wiring are aligned with the Bicep setup in `azure/infra`.

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
- `service-bus-connection-string`

Retrieve them:

```bash
# Get Key Vault name
KV=$(az keyvault list --resource-group <your-resource-group> --query '[0].name' -o tsv)

# SQL connection string
az keyvault secret show --vault-name $KV --name sql-connection-string -o tsv --query value

# Service Bus connection string
az keyvault secret show --vault-name $KV --name service-bus-connection-string -o tsv --query value
```

### Update E2E configuration

Edit [../samples/tests/Contoso.E2E.Runner/appsettings.json](../samples/tests/Contoso.E2E.Runner/appsettings.json) with the deployed endpoints and connection strings:

```json
{
  "E2E": {
    "Products": {
      "BaseAddress": "https://app-products-api-dev-{suffix}.azurewebsites.net",
      "ConnectionString": "Server=sql-dev-{suffix}.database.windows.net;Database=Contoso;User Id=sqladmin;Password={password};Encrypt=true;TrustServerCertificate=false;",
      "ServiceBus": "Endpoint=sb://sb-dev-{suffix}.servicebus.windows.net/;SharedAccessKeyName=rootManageSharedAccessKey;SharedAccessKey={key};"
    },
    "Shopping": {
      "BaseAddress": "https://app-shopping-api-dev-{suffix}.azurewebsites.net",
      "ConnectionString": "Server=sql-dev-{suffix}.database.windows.net;Database=Contoso;User Id=sqladmin;Password={password};Encrypt=true;TrustServerCertificate=false;",
      "ServiceBus": "Endpoint=sb://sb-dev-{suffix}.servicebus.windows.net/;SharedAccessKeyName=rootManageSharedAccessKey;SharedAccessKey={key};"
    }
  }
}
```

### Run E2E scenarios

From the repository root:

```bash
cd samples/tests/Contoso.E2E.Runner
dotnet run
```

This launches an interactive CLI menu to select and execute test scenarios or run load simulations against the deployed APIs.
