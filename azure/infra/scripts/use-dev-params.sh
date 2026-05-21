#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
infra_dir="$(cd "${script_dir}/.." && pwd)"

if [[ -z "${AZURE_SQL_ADMIN_PASSWORD:-}" ]]; then
	echo "AZURE_SQL_ADMIN_PASSWORD is not set. Export it before running azd provision." >&2
	exit 1
fi

if [[ -z "${AZURE_POSTGRES_ADMIN_PASSWORD:-}" ]]; then
  AZURE_POSTGRES_ADMIN_PASSWORD="${AZURE_SQL_ADMIN_PASSWORD}"
fi

if [[ -z "${AZURE_LOCATION:-}" ]]; then
  echo "AZURE_LOCATION is not set. Set it via 'azd env set AZURE_LOCATION <region>' before running azd provision." >&2
  exit 1
fi

if ! command -v az >/dev/null 2>&1; then
  echo "The 'az' command is required to validate an existing Key Vault from the azd environment." >&2
  exit 1
fi

client_ip="$(curl -fsS https://api.ipify.org 2>/dev/null || true)"
if [[ -z "${client_ip}" ]]; then
  client_ip="$(curl -fsS https://ifconfig.me/ip 2>/dev/null || true)"
fi

if [[ -z "${client_ip}" ]]; then
  echo "Unable to determine the current public IP address for the Azure SQL firewall rule." >&2
  exit 1
fi

if [[ ! "${client_ip}" =~ ^([0-9]{1,3}\.){3}[0-9]{1,3}$ ]]; then
  echo "Resolved public IP '${client_ip}' is not a valid IPv4 address." >&2
  exit 1
fi

target_framework="${AZD_DOTNET_TARGET_FRAMEWORK:-${DOTNET_TARGET_FRAMEWORK:-net8.0}}"
case "${target_framework}" in
  net8.0)
    app_service_linux_fx_version='DOTNETCORE|8.0'
    ;;
  net9.0)
    app_service_linux_fx_version='DOTNETCORE|9.0'
    ;;
  net10.0)
    app_service_linux_fx_version='DOTNETCORE|10.0'
    ;;
  *)
    echo "Unsupported target framework '${target_framework}'. Expected net8.0, net9.0, or net10.0." >&2
    exit 1
    ;;
esac

key_vault_name=""
existing_key_vault_name_raw="$(azd env get-value keyVaultName 2>/dev/null | tr -d '\r' || true)"
existing_key_vault_name=""

# azd can return key-not-found messages on stdout; only accept non-error values.
if [[ -n "${existing_key_vault_name_raw}" && "${existing_key_vault_name_raw}" != *ERROR:* ]]; then
  existing_key_vault_name="${existing_key_vault_name_raw}"
fi
if [[ -n "${existing_key_vault_name}" ]]; then
  if az keyvault show --name "${existing_key_vault_name}" --query name -o tsv >/dev/null 2>&1; then
    key_vault_name="${existing_key_vault_name}"
    echo "Reusing existing Key Vault '${key_vault_name}' from azd environment."
  else
    echo "Key Vault '${existing_key_vault_name}' from azd environment was not found. A new Key Vault will be provisioned." >&2
  fi
fi

# Use jq if available for safe JSON processing, otherwise fallback to sed
if command -v jq &> /dev/null; then
  jq \
    --arg pwd "${AZURE_SQL_ADMIN_PASSWORD}" \
    --arg pgpwd "${AZURE_POSTGRES_ADMIN_PASSWORD}" \
    --arg loc "${AZURE_LOCATION}" \
    --arg fx "${app_service_linux_fx_version}" \
    --arg ip "${client_ip}" \
    --arg kv "${key_vault_name}" \
    '.parameters.location.value = $loc | .parameters.sqlAdminPassword.value = $pwd | .parameters.postgresAdminPassword.value = $pgpwd | .parameters.appServiceLinuxFxVersion.value = $fx | .parameters.sqlFirewallClientIp.value = $ip | .parameters.postgresFirewallClientIp.value = $ip | .parameters.keyVaultName.value = $kv' \
    "${infra_dir}/main.dev.parameters.json" > "${infra_dir}/main.parameters.json"
else
  # Fallback: use printf to safely escape and substitute
  escaped_location=$(printf '%s\n' "${AZURE_LOCATION}" | sed -e 's/[&/\\]/\\&/g; s/$/\\/')
  escaped_location=${escaped_location%\\}
  escaped_password=$(printf '%s\n' "${AZURE_SQL_ADMIN_PASSWORD}" | sed -e 's/[&/\\]/\\&/g; s/$/\\/')
  escaped_password=${escaped_password%\\}
  escaped_postgres_password=$(printf '%s\n' "${AZURE_POSTGRES_ADMIN_PASSWORD}" | sed -e 's/[&/\\]/\\&/g; s/$/\\/')
  escaped_postgres_password=${escaped_postgres_password%\\}
  escaped_fx=$(printf '%s\n' "${app_service_linux_fx_version}" | sed -e 's/[&/\\]/\\&/g; s/$/\\/')
  escaped_fx=${escaped_fx%\\}
  escaped_ip=$(printf '%s\n' "${client_ip}" | sed -e 's/[&/\\]/\\&/g; s/$/\\/')
  escaped_ip=${escaped_ip%\\}
  escaped_key_vault_name=$(printf '%s\n' "${key_vault_name}" | sed -e 's/[&/\\]/\\&/g; s/$/\\/')
  escaped_key_vault_name=${escaped_key_vault_name%\\}
  sed \
    -e "s/__AZURE_LOCATION__/${escaped_location}/g" \
    -e "s/__AZURE_SQL_ADMIN_PASSWORD__/${escaped_password}/g" \
    -e "s/__AZURE_POSTGRES_ADMIN_PASSWORD__/${escaped_postgres_password}/g" \
    -e "s/__APP_SERVICE_LINUX_FX_VERSION__/${escaped_fx}/g" \
    -e "s/__AZURE_CLIENT_IP__/${escaped_ip}/g" \
    -e "s/__KEY_VAULT_NAME__/${escaped_key_vault_name}/g" \
    "${infra_dir}/main.dev.parameters.json" > "${infra_dir}/main.parameters.json"
fi
