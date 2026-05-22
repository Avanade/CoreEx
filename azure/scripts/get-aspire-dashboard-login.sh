#!/usr/bin/env bash
set -euo pipefail

usage() {
  cat <<'EOF'
Usage:
  ./scripts/get-aspire-dashboard-login.sh --resource-group <resource-group> [--dashboard-app-name <app-name>] [--token-timeout-seconds <seconds>]

Description:
  Prints the Aspire Dashboard URL.
  Attempts to retrieve a browser login token from the last 60 minutes of App Service logs and prints a ready-to-open login URL.

Options:
  --resource-group, -g         Azure resource group name (required).
  --dashboard-app-name, -n     Dashboard web app name (optional; auto-detected when omitted).
  --token-timeout-seconds, -t  Timeout when waiting for token logs (default: 20).
  --help, -h                   Show this help.
EOF
}

resource_group=""
dashboard_app_name=""
token_timeout_seconds="20"
log_lookback_minutes="60"

cleanup() {
  if [[ -n "${temp_dir:-}" && -d "${temp_dir}" ]]; then
    rm -rf "${temp_dir}"
  fi
}

extract_token_from_recent_logs() {
  local publish_user publish_password zip_path logs_dir token_value current_epoch cutoff_epoch command_payload command_response token_line token_timestamp token_epoch

  if ! command -v curl >/dev/null 2>&1; then
    return 0
  fi

  if ! command -v unzip >/dev/null 2>&1; then
    return 0
  fi

  publish_user="$(az webapp deployment list-publishing-credentials --resource-group "${resource_group}" --name "${dashboard_app_name}" --query publishingUserName -o tsv 2>/dev/null || true)"
  publish_password="$(az webapp deployment list-publishing-credentials --resource-group "${resource_group}" --name "${dashboard_app_name}" --query publishingPassword -o tsv 2>/dev/null || true)"

  if [[ -z "${publish_user}" || -z "${publish_password}" ]]; then
    return 0
  fi

  current_epoch="$(date -u +%s)"
  cutoff_epoch="$((current_epoch - (log_lookback_minutes * 60)))"

  command_payload=$(printf '{"command":"grep \\\"Login to the dashboard\\\" /appsvctmp/volatile/logs/runtime/container.log","dir":"/home"}')
  command_response="$(curl -fsS -u "${publish_user}:${publish_password}" -H 'Content-Type: application/json' -d "${command_payload}" "https://${dashboard_app_name}.scm.azurewebsites.net/api/command" 2>/dev/null || true)"

  token_line="$(printf '%s\n' "${command_response}" | grep -oE '[0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9]{2}:[0-9]{2}:[0-9]{2}(\.[0-9]+)?Z[^\"]*login\?t=[A-Za-z0-9]+' | head -n1 || true)"

  if [[ -n "${token_line}" ]]; then
    token_timestamp="$(printf '%s\n' "${token_line}" | grep -oE '^[0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9]{2}:[0-9]{2}:[0-9]{2}' | head -n1 || true)"
    if [[ -n "${token_timestamp}" ]]; then
      token_epoch="$(date -u -d "${token_timestamp}Z" +%s 2>/dev/null || true)"
      if [[ -n "${token_epoch}" && "${token_epoch}" -ge "${cutoff_epoch}" ]]; then
        token_value="$(printf '%s\n' "${token_line}" | grep -oE 'login\?t=[A-Za-z0-9]+' | head -n1 | cut -d= -f2 || true)"
      fi
    fi
  fi

  if [[ -n "${token_value}" ]]; then
    printf '%s\n' "${token_value}"
    return 0
  fi

  temp_dir="$(mktemp -d)"
  trap cleanup EXIT
  zip_path="${temp_dir}/logfiles.zip"
  logs_dir="${temp_dir}/logs"

  if ! curl -fsS -u "${publish_user}:${publish_password}" "https://${dashboard_app_name}.scm.azurewebsites.net/api/zip/LogFiles/" -o "${zip_path}"; then
    return 0
  fi

  mkdir -p "${logs_dir}"
  if ! unzip -qq -o "${zip_path}" -d "${logs_dir}"; then
    return 0
  fi

  token_value="$(find "${logs_dir}" -type f -print 2>/dev/null | while IFS= read -r file_path; do
    [[ -n "${file_path}" ]] || continue
    awk -v cutoff_epoch="${cutoff_epoch}" '
      match($0, /^([0-9]{4})-([0-9]{2})-([0-9]{2})T([0-9]{2}):([0-9]{2}):([0-9]{2})(\.[0-9]+)?Z/, ts) {
        entry_epoch = mktime(ts[1] " " ts[2] " " ts[3] " " ts[4] " " ts[5] " " ts[6], 1)
        if (entry_epoch >= cutoff_epoch && match($0, /login\?t=[A-Za-z0-9]+/)) {
          token = substr($0, RSTART, RLENGTH)
          sub(/^login\?t=/, "", token)
          print token
          exit
        }
      }
    ' "${file_path}"
  done | head -n1 || true)"

  if [[ -z "${token_value}" ]]; then
    return 0
  fi

  if [[ -n "${token_value}" ]]; then
    printf '%s\n' "${token_value}"
  fi
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --resource-group|-g)
      resource_group="${2:-}"
      shift 2
      ;;
    --dashboard-app-name|-n)
      dashboard_app_name="${2:-}"
      shift 2
      ;;
    --token-timeout-seconds|-t)
      token_timeout_seconds="${2:-}"
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

if [[ -z "${dashboard_app_name}" ]]; then
  dashboard_app_name="$(az webapp list --resource-group "${resource_group}" --query "[?contains(name, 'aspire-dashboard')].name | [0]" -o tsv)"
fi

if [[ -z "${dashboard_app_name}" ]]; then
  echo "Unable to auto-detect the dashboard app name in resource group '${resource_group}'." >&2
  echo "Re-run with --dashboard-app-name <app-name>." >&2
  exit 1
fi

host_name="$(az webapp show --resource-group "${resource_group}" --name "${dashboard_app_name}" --query defaultHostName -o tsv)"

if [[ -z "${host_name}" ]]; then
  echo "Unable to resolve dashboard host name for app '${dashboard_app_name}'." >&2
  exit 1
fi

token=""
token="$(extract_token_from_recent_logs)"

if [[ -z "${token}" ]] && command -v timeout >/dev/null 2>&1; then
  token="$(timeout "${token_timeout_seconds}s" az webapp log tail --resource-group "${resource_group}" --name "${dashboard_app_name}" 2>&1 | grep -oEm1 'login\?t=[A-Za-z0-9]+' | cut -d= -f2 || true)"
elif [[ -z "${token}" ]]; then
  token="$(az webapp log tail --resource-group "${resource_group}" --name "${dashboard_app_name}" 2>&1 | grep -oEm1 'login\?t=[A-Za-z0-9]+' | cut -d= -f2 || true)"
fi

echo "Dashboard app: ${dashboard_app_name}"
echo "Dashboard URL: https://${host_name}"

if [[ -n "${token}" ]]; then
  echo "Login URL: https://${host_name}/login?t=${token}"
else
  echo "Token not found in the last ${log_lookback_minutes} minutes of logs or within ${token_timeout_seconds}s of live tailing."
  echo "Open the dashboard URL and, if prompted, run this script again with a larger timeout."
fi
