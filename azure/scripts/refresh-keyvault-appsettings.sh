#!/usr/bin/env bash
set -euo pipefail

usage() {
  cat <<'EOF'
Usage:
  ./scripts/refresh-keyvault-appsettings.sh --resource-group <resource-group> [--app-name <app-name>]... [--wait-after-restart-seconds <seconds>]

Description:
  Refreshes App Service Key Vault app setting references and restarts web apps.
  When no app names are supplied, all web apps in the resource group are targeted.

Options:
  --resource-group, -g              Azure resource group name (required).
  --app-name, -n                    Specific web app name to refresh; repeat for multiple apps.
  --wait-after-restart-seconds, -w  Delay after each restart (default: 20).
  --help, -h                        Show this help.
EOF
}

resource_group=""
wait_after_restart_seconds="20"
declare -a app_names=()

while [[ $# -gt 0 ]]; do
  case "$1" in
    --resource-group|-g)
      resource_group="${2:-}"
      shift 2
      ;;
    --app-name|-n)
      app_names+=("${2:-}")
      shift 2
      ;;
    --wait-after-restart-seconds|-w)
      wait_after_restart_seconds="${2:-}"
      shift 2
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

if [[ ! "${wait_after_restart_seconds}" =~ ^[0-9]+$ ]]; then
  echo "--wait-after-restart-seconds must be a non-negative integer." >&2
  exit 1
fi

if [[ ${#app_names[@]} -eq 0 ]]; then
  mapfile -t app_names < <(az webapp list --resource-group "${resource_group}" --query "[].name" -o tsv)
fi

if [[ ${#app_names[@]} -eq 0 ]]; then
  echo "No web apps found in resource group '${resource_group}'." >&2
  exit 1
fi

for app_name in "${app_names[@]}"; do
  if [[ -z "${app_name}" ]]; then
    continue
  fi

  echo "Refreshing Key Vault app settings references for '${app_name}'."
  app_id="$(az webapp show --resource-group "${resource_group}" --name "${app_name}" --query id -o tsv)"

  if [[ -z "${app_id}" ]]; then
    echo "Unable to resolve app id for '${app_name}'." >&2
    exit 1
  fi

  az rest \
    --method post \
    --url "https://management.azure.com${app_id}/config/configreferences/appsettings/refresh?api-version=2023-12-01" \
    --output none

  echo "Restarting '${app_name}'."
  az webapp restart --resource-group "${resource_group}" --name "${app_name}" --output none

  if (( wait_after_restart_seconds > 0 )); then
    echo "Waiting ${wait_after_restart_seconds}s for '${app_name}' startup."
    sleep "${wait_after_restart_seconds}"
  fi
done

echo "Completed Key Vault app settings refresh for ${#app_names[@]} app(s)."
