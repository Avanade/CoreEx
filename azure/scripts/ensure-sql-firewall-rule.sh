#!/usr/bin/env bash
set -euo pipefail

# Ensures the current runner public IP has an Azure SQL firewall rule.

retry_command() {
  local attempts="${1:?attempt count is required}"
  local delay_seconds="${2:?delay is required}"
  local description="${3:?description is required}"
  shift 3

  local attempt=1
  while true; do
    if "$@"; then
      return 0
    fi

    if (( attempt >= attempts )); then
      return 1
    fi

    echo "Waiting on ${description} (${attempt}/${attempts}); retrying in ${delay_seconds}s." >&2
    attempt=$((attempt + 1))
    sleep "${delay_seconds}"
  done
}

if ! command -v azd >/dev/null 2>&1; then
  echo "The 'azd' command is required to resolve environment values." >&2
  exit 1
fi

if ! command -v az >/dev/null 2>&1; then
  echo "The 'az' command is required to manage Azure SQL firewall rules." >&2
  exit 1
fi

sql_server="$(azd env get-value sqlServerName | tr -d '\r')"
azure_resource_group="$(azd env get-value AZURE_RESOURCE_GROUP | tr -d '\r')"
azure_subscription_id="$(azd env get-value AZURE_SUBSCRIPTION_ID | tr -d '\r')"
azure_env_name="$(azd env get-value AZURE_ENV_NAME | tr -d '\r')"

if [[ -z "${sql_server}" || -z "${azure_resource_group}" ]]; then
  echo "Unable to resolve sqlServerName/AZURE_RESOURCE_GROUP from the active azd environment." >&2
  exit 1
fi

client_ip="$(curl -fsS https://api.ipify.org 2>/dev/null || true)"
if [[ -z "${client_ip}" ]]; then
  client_ip="$(curl -fsS https://ifconfig.me/ip 2>/dev/null || true)"
fi

if [[ -z "${client_ip}" ]]; then
  echo "Unable to determine the public IP address for the current runner." >&2
  exit 1
fi

if [[ ! "${client_ip}" =~ ^([0-9]{1,3}\.){3}[0-9]{1,3}$ ]]; then
  echo "Resolved public IP '${client_ip}' is not a valid IPv4 address." >&2
  exit 1
fi

firewall_rule_name="azd-${azure_env_name:-env}-$(echo "${client_ip}" | tr '.' '-')"
az_server_args=(--resource-group "${azure_resource_group}" --name "${sql_server}")
az_firewall_args=(--resource-group "${azure_resource_group}" --server "${sql_server}")
if [[ -n "${azure_subscription_id}" ]]; then
  az_server_args+=(--subscription "${azure_subscription_id}")
  az_firewall_args+=(--subscription "${azure_subscription_id}")
fi

if [[ "${AZD_SQL_FIREWALL_WAIT_FOR_SERVER:-0}" == "1" ]]; then
  echo "Waiting for Azure SQL server '${sql_server}' to become available."
  retry_command 12 10 "Azure SQL server readiness" az sql server show "${az_server_args[@]}" >/dev/null
else
  az sql server show "${az_server_args[@]}" >/dev/null
fi

if az sql server firewall-rule show "${az_firewall_args[@]}" --name "${firewall_rule_name}" >/dev/null 2>&1; then
  echo "Updating Azure SQL firewall rule '${firewall_rule_name}' for ${client_ip}."
  az sql server firewall-rule update "${az_firewall_args[@]}" --name "${firewall_rule_name}" --start-ip-address "${client_ip}" --end-ip-address "${client_ip}" >/dev/null
else
  echo "Creating Azure SQL firewall rule '${firewall_rule_name}' for ${client_ip}."
  az sql server firewall-rule create "${az_firewall_args[@]}" --name "${firewall_rule_name}" --start-ip-address "${client_ip}" --end-ip-address "${client_ip}" >/dev/null
fi

echo "Azure SQL firewall rule '${firewall_rule_name}' is ready."