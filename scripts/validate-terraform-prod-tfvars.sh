#!/usr/bin/env bash
# Validates production tfvars guardrails before Terraform apply.
# When crimeportal-rg already exists, unified Front Door + www custom domain must stay enabled.
set -euo pipefail

TFVARS_PATH="${1:-Infrastructure/terraform.prod.tfvars}"
RESOURCE_GROUP="${2:-crimeportal-rg}"

if [ ! -f "${TFVARS_PATH}" ]; then
	echo "::error::Missing tfvars file: ${TFVARS_PATH}"
	exit 1
fi

hcl_bool() {
	local key="$1"
	local line
	line="$(grep -E "^[[:space:]]*${key}[[:space:]]*=" "${TFVARS_PATH}" | tail -n 1 || true)"
	if [ -z "${line}" ]; then
		echo ""
		return
	fi
	case "${line}" in
		*'= true' | *'=true' | *'= "true"' | *"= 'true'") echo "true" ;;
		*'= false' | *'=false' | *'= "false"' | *"= 'false'") echo "false" ;;
		*) echo "" ;;
	esac
}

hcl_string() {
	local key="$1"
	local line
	line="$(grep -E "^[[:space:]]*${key}[[:space:]]*=" "${TFVARS_PATH}" | tail -n 1 || true)"
	if [ -z "${line}" ]; then
		echo ""
		return
	fi
	echo "${line}" | sed -E 's/^[^=]*=[[:space:]]*"?([^"#]+)"?.*/\1/' | tr -d " \t\r" | sed "s/^'//;s/'$//"
}

RG_EXISTS="false"
if command -v az >/dev/null 2>&1; then
	if [ "$(az group exists --name "${RESOURCE_GROUP}" -o tsv 2>/dev/null || echo false)" = "true" ]; then
		RG_EXISTS="true"
	fi
fi

UNIFIED="$(hcl_bool enable_unified_front_door)"
CUSTOM_DOMAIN="$(hcl_bool enable_unified_front_door_custom_domain)"
HOSTNAME="$(hcl_string unified_front_door_hostname)"
WWW_HOST="$(hcl_string frontend_www_custom_domain)"

echo "validate-terraform-prod-tfvars: resource_group_exists=${RG_EXISTS} unified_fd=${UNIFIED:-<unset>} custom_domain=${CUSTOM_DOMAIN:-<unset>}"

if [ "${RG_EXISTS}" != "true" ]; then
	echo "Bootstrap mode: skipping strict production edge checks (resource group not found)."
	exit 0
fi

fail() {
	echo "::error::$1"
	exit 1
}

if [ "${UNIFIED}" != "true" ]; then
	fail "Production resource group exists but enable_unified_front_door is not true. Disabling unified Front Door breaks www TLS and same-origin auth. See docs/production-edge-runbook.md"
fi

if [ "${CUSTOM_DOMAIN}" != "true" ]; then
	fail "Production resource group exists but enable_unified_front_door_custom_domain is not true. This causes NET::ERR_CERT_COMMON_NAME_INVALID on www after DNS cutover. See docs/production-edge-runbook.md"
fi

if [ -z "${HOSTNAME}" ] && [ -z "${WWW_HOST}" ]; then
	fail "Set unified_front_door_hostname or frontend_www_custom_domain in terraform.prod.tfvars (e.g. www.dibangops.com)."
fi

echo "Production edge tfvars validation passed."
