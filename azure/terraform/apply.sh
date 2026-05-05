#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "${script_dir}"

usage() {
  cat <<EOF
Usage: ./apply.sh <dev|test|prod> [plan|apply|destroy]

Examples:
  ./apply.sh dev plan
  ./apply.sh dev apply
  ./apply.sh test apply
  ./apply.sh prod destroy
EOF
}

if [[ $# -lt 1 || $# -gt 2 ]]; then
  usage
  exit 1
fi

env_name="$1"
action="${2:-apply}"

case "${env_name}" in
  dev|test|prod) ;;
  *)
    echo "Invalid environment '${env_name}'. Use dev, test, or prod." >&2
    exit 1
    ;;
esac

case "${action}" in
  plan|apply|destroy) ;;
  *)
    echo "Invalid action '${action}'. Use plan, apply, or destroy." >&2
    exit 1
    ;;
esac

# Load azd environment values when available.
if command -v azd >/dev/null 2>&1; then
  if azd env get-values >/dev/null 2>&1; then
    set -a
    eval "$(azd env get-values)"
    set +a
  fi
fi

if [[ -z "${AZURE_SQL_ADMIN_PASSWORD:-}" ]]; then
  echo "AZURE_SQL_ADMIN_PASSWORD is required." >&2
  exit 1
fi

target_framework="${AZD_DOTNET_TARGET_FRAMEWORK:-${DOTNET_TARGET_FRAMEWORK:-net10.0}}"
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
    echo "Unsupported target framework '${target_framework}'." >&2
    exit 1
    ;;
esac

client_ip="$(curl -fsS https://api.ipify.org 2>/dev/null || true)"
if [[ -z "${client_ip}" ]]; then
  client_ip="$(curl -fsS https://ifconfig.me/ip 2>/dev/null || true)"
fi

if [[ -z "${client_ip}" ]]; then
  echo "Unable to determine public IPv4 address." >&2
  exit 1
fi

if [[ ! "${client_ip}" =~ ^([0-9]{1,3}\.){3}[0-9]{1,3}$ ]]; then
  echo "Resolved public IP '${client_ip}' is not a valid IPv4 address." >&2
  exit 1
fi

tfvars_file="${env_name}.tfvars"
if [[ ! -f "${tfvars_file}" ]]; then
  echo "Missing tfvars file '${tfvars_file}'." >&2
  exit 1
fi

export TF_VAR_sql_admin_password="${AZURE_SQL_ADMIN_PASSWORD}"

terraform fmt -recursive
terraform init
terraform validate

case "${action}" in
  plan)
    terraform plan \
      -var-file="${tfvars_file}" \
      -var "app_service_linux_fx_version=${app_service_linux_fx_version}" \
      -var "sql_firewall_client_ip=${client_ip}" \
      -var "key_vault_firewall_client_ip=${client_ip}"
    ;;
  apply)
    terraform apply -auto-approve \
      -var-file="${tfvars_file}" \
      -var "app_service_linux_fx_version=${app_service_linux_fx_version}" \
      -var "sql_firewall_client_ip=${client_ip}" \
      -var "key_vault_firewall_client_ip=${client_ip}"
    ;;
  destroy)
    terraform destroy -auto-approve \
      -var-file="${tfvars_file}" \
      -var "app_service_linux_fx_version=${app_service_linux_fx_version}" \
      -var "sql_firewall_client_ip=${client_ip}" \
      -var "key_vault_firewall_client_ip=${client_ip}"
    ;;
esac
