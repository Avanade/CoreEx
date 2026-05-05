#!/usr/bin/env bash
set -euo pipefail

service_name="${1:?service name is required}"
project_file="${2:?project file is required}"
target_framework="${3:-${AZD_DOTNET_TARGET_FRAMEWORK:-${DOTNET_TARGET_FRAMEWORK:-}}}"

if [[ -z "${target_framework}" ]]; then
	echo "Target framework is required. Set AZD_DOTNET_TARGET_FRAMEWORK (or DOTNET_TARGET_FRAMEWORK), or pass a third argument." >&2
	exit 1
fi

azure_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
output_dir="${azure_dir}/.azd/packages/${service_name}"

rm -rf "${output_dir}"
mkdir -p "${output_dir}"

dotnet publish "${project_file}" -c Release -f "${target_framework}" -o "${output_dir}"
