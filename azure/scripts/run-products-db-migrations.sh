#!/usr/bin/env bash
set -euo pipefail

# Runs all Contoso *.Database DbEx migrations against the provisioned Azure SQL database.

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
azure_dir="$(cd "${script_dir}/.." && pwd)"
repo_root="$(cd "${azure_dir}/.." && pwd)"

if ! command -v azd >/dev/null 2>&1; then
  echo "The 'azd' command is required to resolve environment values." >&2
  exit 1
fi

target_framework="${AZD_DOTNET_TARGET_FRAMEWORK:-${DOTNET_TARGET_FRAMEWORK:-}}"
if [[ -z "${target_framework}" ]]; then
  if dotnet --list-runtimes | grep -q "Microsoft.NETCore.App 10\."; then
    target_framework="net10.0"
  elif dotnet --list-runtimes | grep -q "Microsoft.NETCore.App 9\."; then
    target_framework="net9.0"
  else
    target_framework="net8.0"
  fi
fi
sql_server="$(azd env get-value sqlServerName | tr -d '\r')"
sql_database="$(azd env get-value sqlDatabaseName | tr -d '\r')"
sql_admin_login="${AZURE_SQL_ADMIN_LOGIN:-coreexadmin}"
sql_password="${AZURE_SQL_ADMIN_PASSWORD:-}"

if [[ -z "${sql_server}" || -z "${sql_database}" ]]; then
  echo "Unable to resolve sqlServerName/sqlDatabaseName from the active azd environment." >&2
  exit 1
fi

if [[ -z "${sql_password}" ]]; then
  sql_password="$(azd env get-value AZURE_SQL_ADMIN_PASSWORD | tr -d '\r')"
fi

if [[ -z "${sql_password}" ]]; then
  echo "AZURE_SQL_ADMIN_PASSWORD is required to run DbEx migrations." >&2
  exit 1
fi

bash "${script_dir}/ensure-sql-firewall-rule.sh"

connection_string="Server=tcp:${sql_server}.database.windows.net,1433;Initial Catalog=${sql_database};User ID=${sql_admin_login};Password=${sql_password};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

readarray -t projects < <(find "${repo_root}/samples/src" -maxdepth 2 -type f -name 'Contoso.*.Database.csproj' | sort)
if [[ ${#projects[@]} -eq 0 ]]; then
  echo "No Contoso database projects were found under samples/src." >&2
  exit 1
fi

echo "Running DbEx migrations for ${#projects[@]} database project(s) using framework '${target_framework}' against database '${sql_database}'."
for project in "${projects[@]}"; do
  project_dir="$(dirname "${project}")"
  project_file_name="$(basename "${project}")"
  project_name="${project_file_name%.csproj}"
  domain_name="${project_name%.Database}"
  test_common_project="${repo_root}/samples/tests/${domain_name}.Test.Common/${domain_name}.Test.Common.csproj"
  migration_command="Migrate"
  extra_args=()
  prompt_args=()

  # Remove Windows zone marker sidecar files to avoid embedding/executing them as migration resources on Linux.
  zone_file_count="$(find "${project_dir}" -type f -name '*:Zone.Identifier' | wc -l | tr -d ' ')"
  if [[ "${zone_file_count}" != "0" ]]; then
    echo "Removing ${zone_file_count} Zone.Identifier sidecar file(s) from ${project_name}."
    find "${project_dir}" -type f -name '*:Zone.Identifier' -delete
  fi

  if [[ -f "${test_common_project}" ]]; then
    test_common_dir="$(dirname "${test_common_project}")"
    test_common_name="$(basename "${test_common_project}" .csproj)"

    zone_file_count="$(find "${test_common_dir}" -type f -name '*:Zone.Identifier' | wc -l | tr -d ' ')"
    if [[ "${zone_file_count}" != "0" ]]; then
      echo "Removing ${zone_file_count} Zone.Identifier sidecar file(s) from ${test_common_name}."
      find "${test_common_dir}" -type f -name '*:Zone.Identifier' -delete
    fi

    dotnet build "${test_common_project}" -c Release -f "${target_framework}" >/dev/null
    test_common_assembly="${test_common_dir}/bin/Release/${target_framework}/${test_common_name}.dll"
    if [[ -f "${test_common_assembly}" ]]; then
      migration_command="ResetAndAll"
      extra_args=(--assembly "${test_common_assembly}")
      prompt_args=(--accept-prompts)
    fi
  fi

  echo "Running ${project_name} migrations (${migration_command})."
  dotnet run --project "${project}" -c Release -f "${target_framework}" -- --connection-string "${connection_string}" "${prompt_args[@]}" "${extra_args[@]}" "${migration_command}"
done
