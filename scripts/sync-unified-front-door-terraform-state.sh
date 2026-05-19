#!/usr/bin/env bash
# Import CLI-created Front Door rule set/rule and drop legacy route from Terraform state.
# Run from Infrastructure/ with the same -var-file and -var flags as terraform apply.
set -euo pipefail

RESOURCE_GROUP="${1:-crimeportal-rg}"
PROFILE_NAME="${2:-crimeportal-frontend-unified-fd}"
RULE_SET_NAME="${3:-unifiedrouting}"
RULE_NAME="${4:-apiorigingroup}"
shift 4
TERRAFORM_ARGS=("$@")

if [ ! -f terraform.prod.tfvars ]; then
	echo "::error::Run from Infrastructure/ (terraform.prod.tfvars not found)."
	exit 1
fi

if ! command -v az >/dev/null 2>&1; then
	echo "::error::Azure CLI required."
	exit 1
fi

SUB="$(az account show --query id -o tsv)"
RULE_SET_ID="/subscriptions/${SUB}/resourceGroups/${RESOURCE_GROUP}/providers/Microsoft.Cdn/profiles/${PROFILE_NAME}/ruleSets/${RULE_SET_NAME}"
RULE_ID="${RULE_SET_ID}/rules/${RULE_NAME}"
LEGACY_ROUTE_ID="/subscriptions/${SUB}/resourceGroups/${RESOURCE_GROUP}/providers/Microsoft.Cdn/profiles/${PROFILE_NAME}/afdEndpoints/crimeportal-frontend-unified-ep/routes/crimeportal-backend-unified-api-route"

import_if_missing() {
	local addr="$1"
	local azure_id="$2"
	if terraform state show "${addr}" >/dev/null 2>&1; then
		echo "${addr} already in Terraform state."
		return 0
	fi
	if ! az resource show --ids "${azure_id}" -o none 2>/dev/null; then
		echo "${addr} not found in Azure; Terraform will create it on apply."
		return 0
	fi
	echo "Importing ${addr}..."
	terraform import -lock-timeout=10m "${TERRAFORM_ARGS[@]}" "${addr}" "${azure_id}"
}

import_if_missing 'azurerm_cdn_frontdoor_rule_set.unified_routing[0]' "${RULE_SET_ID}"
import_if_missing 'azurerm_cdn_frontdoor_rule.unified_api_override[0]' "${RULE_ID}"

if terraform state show 'azurerm_cdn_frontdoor_route.unified_api[0]' >/dev/null 2>&1; then
	echo "Removing legacy azurerm_cdn_frontdoor_route.unified_api from state (replaced by rule set on single SPA route)."
	terraform state rm 'azurerm_cdn_frontdoor_route.unified_api[0]'
fi

# Orphan route may still exist in state under old address after manual deletes
if terraform state list 2>/dev/null | grep -q 'azurerm_cdn_frontdoor_route.unified_api'; then
	terraform state list | grep 'azurerm_cdn_frontdoor_route.unified_api' | while read -r addr; do
		echo "Removing stale route from state: ${addr}"
		terraform state rm "${addr}"
	done
fi

echo "Unified Front Door Terraform state sync complete."
