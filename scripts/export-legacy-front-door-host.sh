#!/usr/bin/env bash
# Record legacy www rollback CNAME target before blue/green cutover.
set -euo pipefail

LEGACY_RG="${LEGACY_RG:-crimeportal-rg}"
PROFILE="${AZURE_PROFILE:-}"

if [ -n "${PROFILE}" ]; then
	az account set --subscription "${PROFILE}" 2>/dev/null || true
fi

echo "Legacy resource group: ${LEGACY_RG}"
echo "Run from repo root with Azure CLI logged in."
echo ""

HOST="$(az deployment group show \
	--resource-group "${LEGACY_RG}" \
	--name "crimeportal-frontend-unified-fd" 2>/dev/null \
	| jq -r '.properties.outputs.unified_front_door_endpoint_host.value // empty' 2>/dev/null || true)"

if [ -z "${HOST}" ]; then
	# Fallback: read from Terraform state in CI or local backend
	if command -v terraform >/dev/null 2>&1 && [ -f Infrastructure/terraform.prod.tfvars ]; then
		pushd Infrastructure >/dev/null
		HOST="$(terraform output -raw unified_front_door_endpoint_host 2>/dev/null || true)"
		popd >/dev/null
	fi
fi

if [ -z "${HOST}" ]; then
	FD_NAME="$(az afd endpoint list \
		--profile-name crimeportal-frontend-unified-fd \
		--resource-group "${LEGACY_RG}" \
		--query "[0].hostName" -o tsv 2>/dev/null || true)"
	HOST="${FD_NAME}"
fi

if [ -z "${HOST}" ]; then
	echo "::error::Could not resolve legacy unified Front Door endpoint host. Set LEGACY_RG or run terraform output in legacy state."
	exit 1
fi

OUT_FILE="${OUT_FILE:-legacy-front-door-rollback.txt}"
{
	echo "# Save before green cutover — repoint www CNAME here to roll back"
	echo "rollback_cname_target=${HOST}"
	echo "recorded_at=$(date -u +%Y-%m-%dT%H:%M:%SZ)"
} | tee "${OUT_FILE}"

echo ""
echo "Rollback: set www CNAME to ${HOST} (DNS only). Saved to ${OUT_FILE}"
