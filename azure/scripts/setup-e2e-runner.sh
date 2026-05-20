#!/usr/bin/env bash
set -euo pipefail

usage() {
  cat <<'EOF'
Usage:
  ./scripts/setup-e2e-runner.sh --resource-group <resource-group> [--appsettings-path <path>] [--key-vault-name <name>] [--products-app-name <name>] [--shopping-app-name <name>] [--skip-validation]

Description:
  Discovers the deployed Products and Shopping API endpoints, retrieves the
  database connection strings from Key Vault, validates the deployed APIs, and
  updates the E2E runner appsettings.json for Products and Shopping.

Options:
  --resource-group, -g      Azure resource group name (required).
  --appsettings-path, -a    Path to the E2E runner appsettings.json file.
                            Defaults to samples/tests/Contoso.E2E.Runner/appsettings.json.
  --key-vault-name, -k      Key Vault name. Auto-detected when omitted.
  --products-app-name, -p   Products API web app name. Auto-detected when omitted.
  --shopping-app-name, -s   Shopping API web app name. Auto-detected when omitted.
  --skip-validation         Skip the endpoint validation checks.
  --help, -h                Show this help.
EOF
}

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "${script_dir}/../.." && pwd)"

resource_group=""
appsettings_path="${repo_root}/samples/tests/Contoso.E2E.Runner/appsettings.json"
key_vault_name=""
products_app_name=""
shopping_app_name=""
skip_validation="false"

while [[ $# -gt 0 ]]; do
  case "$1" in
    --resource-group|-g)
      resource_group="${2:-}"
      shift 2
      ;;
    --appsettings-path|-a)
      appsettings_path="${2:-}"
      shift 2
      ;;
    --key-vault-name|-k)
      key_vault_name="${2:-}"
      shift 2
      ;;
    --products-app-name|-p)
      products_app_name="${2:-}"
      shift 2
      ;;
    --shopping-app-name|-s)
      shopping_app_name="${2:-}"
      shift 2
      ;;
    --skip-validation)
      skip_validation="true"
      shift
      ;;
    --help|-h)
      usage
      exit 0
      ;;
    *)
      echo "Unknown argument: $1" >&2
      usage
      exit 1
      ;;
  esac
done

if [[ -z "${resource_group}" ]]; then
  echo "Missing required argument: --resource-group" >&2
  usage
  exit 1
fi

if ! command -v az >/dev/null 2>&1; then
  echo "Azure CLI 'az' is not installed or not on PATH." >&2
  exit 1
fi

if ! command -v curl >/dev/null 2>&1; then
  echo "curl is required but was not found on PATH." >&2
  exit 1
fi

if [[ ! -f "${appsettings_path}" ]]; then
  echo "The E2E runner appsettings file was not found at '${appsettings_path}'." >&2
  exit 1
fi

get_webapp_host() {
  local app_name="$1"
  az webapp show --resource-group "${resource_group}" --name "${app_name}" --query defaultHostName -o tsv
}

validate_request() {
  local label="$1"
  local url="$2"
  local method="${3:-GET}"
  local status_code

  status_code="$(curl -k -sS -o /dev/null -w '%{http_code}' -X "${method}" "${url}" || true)"
  if [[ ! "${status_code}" =~ ^[23] ]]; then
    echo "Validation failed for ${label}: ${method} ${url} returned HTTP ${status_code:-unknown}." >&2
    exit 1
  fi

  echo "Validated ${label}: ${method} ${url} (${status_code})"
}

if [[ -z "${products_app_name}" ]]; then
  products_app_name="$(az webapp list --resource-group "${resource_group}" --query "[?contains(name, 'products-api')].name | [0]" -o tsv)"
fi

if [[ -z "${shopping_app_name}" ]]; then
  shopping_app_name="$(az webapp list --resource-group "${resource_group}" --query "[?contains(name, 'shopping-api')].name | [0]" -o tsv)"
fi

if [[ -z "${products_app_name}" || -z "${shopping_app_name}" ]]; then
  echo "Unable to auto-detect the Products or Shopping API app names in resource group '${resource_group}'." >&2
  echo "Re-run with --products-app-name and --shopping-app-name." >&2
  exit 1
fi

products_host="$(get_webapp_host "${products_app_name}")"
shopping_host="$(get_webapp_host "${shopping_app_name}")"

if [[ -z "${products_host}" || -z "${shopping_host}" ]]; then
  echo "Unable to resolve one or more app host names." >&2
  exit 1
fi

if [[ -z "${key_vault_name}" ]]; then
  key_vault_name="$(az keyvault list --resource-group "${resource_group}" --query '[0].name' -o tsv)"
fi

if [[ -z "${key_vault_name}" ]]; then
  echo "Unable to auto-detect the Key Vault in resource group '${resource_group}'." >&2
  echo "Re-run with --key-vault-name <name>." >&2
  exit 1
fi

postgres_connection_string="$(az keyvault secret show --vault-name "${key_vault_name}" --name postgres-connection-string --query value -o tsv)"
sql_connection_string="$(az keyvault secret show --vault-name "${key_vault_name}" --name sql-connection-string --query value -o tsv)"

if [[ -z "${postgres_connection_string}" || -z "${sql_connection_string}" ]]; then
  echo "Unable to retrieve one or more connection strings from Key Vault '${key_vault_name}'." >&2
  exit 1
fi

if [[ "${skip_validation}" != "true" ]]; then
  validate_request "Products API" "https://${products_host}/api/products"
  validate_request "Shopping API" "https://${shopping_host}/api/customers/test/baskets" POST
  validate_request "Products health" "https://${products_host}/health/ready/detailed"
  validate_request "Products swagger" "https://${products_host}/swagger"
  validate_request "Shopping health" "https://${shopping_host}/health/ready/detailed"
  validate_request "Shopping swagger" "https://${shopping_host}/swagger"
fi

backup_path="${appsettings_path}.bak"
cp "${appsettings_path}" "${backup_path}"

temp_path="$(mktemp)"

if command -v jq >/dev/null 2>&1; then
  jq \
    --arg productsBase "https://${products_host}" \
    --arg productsConnectionString "${postgres_connection_string}" \
    --arg shoppingBase "https://${shopping_host}" \
    --arg shoppingConnectionString "${sql_connection_string}" \
    '.E2E.Products.BaseAddress = $productsBase
    | .E2E.Products.ConnectionString = $productsConnectionString
    | .E2E.Shopping.BaseAddress = $shoppingBase
    | .E2E.Shopping.ConnectionString = $shoppingConnectionString' \
    "${appsettings_path}" > "${temp_path}"
elif command -v python3 >/dev/null 2>&1; then
  python3 - "${appsettings_path}" "${temp_path}" "https://${products_host}" "${postgres_connection_string}" "https://${shopping_host}" "${sql_connection_string}" <<'PY'
import json
import sys

source_path, temp_path, products_base, products_cs, shopping_base, shopping_cs = sys.argv[1:7]

with open(source_path, encoding='utf-8') as file:
    data = json.load(file)

data.setdefault('E2E', {})
data['E2E'].setdefault('Products', {})
data['E2E'].setdefault('Shopping', {})
data['E2E']['Products']['BaseAddress'] = products_base
data['E2E']['Products']['ConnectionString'] = products_cs
data['E2E']['Shopping']['BaseAddress'] = shopping_base
data['E2E']['Shopping']['ConnectionString'] = shopping_cs

with open(temp_path, 'w', encoding='utf-8') as file:
    json.dump(data, file, indent=2)
    file.write('\n')
PY
else
  echo "Either jq or python3 is required to update '${appsettings_path}'." >&2
  exit 1
fi

mv "${temp_path}" "${appsettings_path}"

echo "Updated E2E runner configuration: ${appsettings_path}"
echo "Backup created: ${backup_path}"
echo "Products BaseAddress: https://${products_host}"
echo "Shopping BaseAddress: https://${shopping_host}"
echo "Key Vault: ${key_vault_name}"
echo ""
echo "Next step:"
echo "  cd ${repo_root}/samples/tests/Contoso.E2E.Runner"
echo "  dotnet run --framework \"\${AZD_DOTNET_TARGET_FRAMEWORK:-\$DOTNET_TARGET_FRAMEWORK}\""