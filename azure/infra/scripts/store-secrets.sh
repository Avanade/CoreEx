#!/usr/bin/env bash
# Populates Key Vault with connection secrets needed for E2E testing.
# Runs automatically as a postprovision hook via azure.yaml.
set -euo pipefail

rg="${AZURE_RESOURCE_GROUP:?AZURE_RESOURCE_GROUP is not set}"
sql_password="${AZURE_SQL_ADMIN_PASSWORD:?AZURE_SQL_ADMIN_PASSWORD is not set}"
postgres_password="${AZURE_POSTGRES_ADMIN_PASSWORD:-${AZURE_SQL_ADMIN_PASSWORD}}"
sql_server="${AZURE_SQL_SERVER:-${sqlServerName:-}}"
sql_login="${AZURE_SQL_ADMIN_LOGIN:-coreexadmin}"
sql_db="${AZURE_SQL_DB_NAME:-${sqlDatabaseName:-}}"
postgres_server="${AZURE_POSTGRES_SERVER:-${postgresServerName:-}}"
postgres_login="${AZURE_POSTGRES_ADMIN_LOGIN:-coreexpgadmin}"
postgres_db="${AZURE_POSTGRES_DB_NAME:-${postgresDatabaseName:-}}"

if [[ -z "${sql_server}" ]]; then
  echo "AZURE_SQL_SERVER (or azd output sqlServerName) is not set." >&2
  exit 1
fi

if [[ -z "${sql_db}" ]]; then
  echo "AZURE_SQL_DB_NAME (or azd output sqlDatabaseName) is not set." >&2
  exit 1
fi

if [[ -z "${postgres_server}" ]]; then
  echo "AZURE_POSTGRES_SERVER (or azd output postgresServerName) is not set." >&2
  exit 1
fi

if [[ -z "${postgres_db}" ]]; then
  echo "AZURE_POSTGRES_DB_NAME (or azd output postgresDatabaseName) is not set." >&2
  exit 1
fi

echo "Locating Key Vault in resource group '${rg}'..."
kv_name=$(az keyvault list --resource-group "${rg}" --query "[0].name" -o tsv)
kv_id=$(az keyvault show --name "${kv_name}" --resource-group "${rg}" --query id -o tsv)

# Grant the current signed-in user Key Vault Administrator so secrets can be managed without manual IAM steps.
# Silently skipped when running as a service principal that does not support az ad signed-in-user.
current_oid=$(az ad signed-in-user show --query id -o tsv 2>/dev/null || true)
if [[ -n "${current_oid}" ]]; then
  echo "Assigning Key Vault Administrator role to current user..."
  az role assignment create \
    --role "Key Vault Administrator" \
    --assignee "${current_oid}" \
    --scope "${kv_id}" \
    --only-show-errors 2>/dev/null || true
  # Allow RBAC propagation before writing secrets.
  sleep 15
fi

echo "Storing sql-admin-password..."
az keyvault secret set \
  --vault-name "${kv_name}" \
  --name "sql-admin-password" \
  --value "${sql_password}" \
  --output none

echo "Storing postgres-admin-password..."
az keyvault secret set \
  --vault-name "${kv_name}" \
  --name "postgres-admin-password" \
  --value "${postgres_password}" \
  --output none

echo "Building SQL Server connection string..."
sql_conn="Server=tcp:${sql_server}.database.windows.net,1433;Database=${sql_db};User Id=${sql_login};Password=${sql_password};Encrypt=true;TrustServerCertificate=false;"

echo "Storing sql-connection-string..."
az keyvault secret set \
  --vault-name "${kv_name}" \
  --name "sql-connection-string" \
  --value "${sql_conn}" \
  --output none

echo "Building Postgres connection string..."
postgres_conn="Server=${postgres_server}.postgres.database.azure.com;Port=5432;Database=${postgres_db};User Id=${postgres_login};Password=${postgres_password};Ssl Mode=Require;"

echo "Storing postgres-connection-string..."
az keyvault secret set \
  --vault-name "${kv_name}" \
  --name "postgres-connection-string" \
  --value "${postgres_conn}" \
  --output none

echo "Locating Service Bus namespace..."
sb_name=$(az servicebus namespace list --resource-group "${rg}" --query "[0].name" -o tsv)
sb_conn=$(az servicebus namespace authorization-rule keys list \
  --resource-group "${rg}" \
  --namespace-name "${sb_name}" \
  --name "app" \
  --query primaryConnectionString -o tsv)

echo "Storing service-bus-connection-string..."
az keyvault secret set \
  --vault-name "${kv_name}" \
  --name "service-bus-connection-string" \
  --value "${sb_conn}" \
  --output none

echo "Secrets stored successfully in Key Vault '${kv_name}':"
echo "  - sql-admin-password"
echo "  - postgres-admin-password"
echo "  - sql-connection-string"
echo "  - postgres-connection-string"
echo "  - service-bus-connection-string"
