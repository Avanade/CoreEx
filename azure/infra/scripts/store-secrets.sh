#!/usr/bin/env bash
# Populates Key Vault with connection secrets needed for E2E testing.
# Runs automatically as a postprovision hook via azure.yaml.
set -euo pipefail

rg="${AZURE_RESOURCE_GROUP:?AZURE_RESOURCE_GROUP is not set}"
sql_password="${AZURE_SQL_ADMIN_PASSWORD:?AZURE_SQL_ADMIN_PASSWORD is not set}"

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

echo "Locating SQL Server..."
sql_server=$(az sql server list --resource-group "${rg}" --query "[0].name" -o tsv)
sql_login=$(az sql server show --resource-group "${rg}" --name "${sql_server}" --query administratorLogin -o tsv)
sql_db=$(az sql db list --resource-group "${rg}" --server "${sql_server}" --query "[?name!='master'].name | [0]" -o tsv)
sql_conn="Server=tcp:${sql_server}.database.windows.net,1433;Database=${sql_db};User Id=${sql_login};Password=${sql_password};Encrypt=true;TrustServerCertificate=false;"

echo "Storing sql-connection-string..."
az keyvault secret set \
  --vault-name "${kv_name}" \
  --name "sql-connection-string" \
  --value "${sql_conn}" \
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
echo "  - sql-connection-string"
echo "  - service-bus-connection-string"
