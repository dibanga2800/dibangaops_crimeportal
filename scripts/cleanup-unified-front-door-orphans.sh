#!/usr/bin/env bash
# Removes legacy unified Front Door routes not managed by Terraform (prevents cert/routing drift).
set -euo pipefail

RESOURCE_GROUP="${1:-crimeportal-rg}"
PROFILE_NAME="${2:-crimeportal-frontend-unified-fd}"
ENDPOINT_NAME="${3:-crimeportal-frontend-unified-ep}"
ORPHAN_ROUTE="${4:-crimeportal-backend-unified-api-route}"
MANAGED_ROUTE="${5:-crimeportal-frontend-unified-spa-route}"

if ! command -v az >/dev/null 2>&1; then
	echo "az CLI not found; skipping orphan Front Door cleanup."
	exit 0
fi

if ! az group exists --name "${RESOURCE_GROUP}" -o tsv 2>/dev/null | grep -q true; then
	echo "Resource group ${RESOURCE_GROUP} not found; skipping orphan cleanup."
	exit 0
fi

if ! az afd route show \
	--resource-group "${RESOURCE_GROUP}" \
	--profile-name "${PROFILE_NAME}" \
	--endpoint-name "${ENDPOINT_NAME}" \
	--route-name "${ORPHAN_ROUTE}" \
	-o none 2>/dev/null; then
	echo "No orphan route ${ORPHAN_ROUTE}; nothing to delete."
	exit 0
fi

if ! az afd route show \
	--resource-group "${RESOURCE_GROUP}" \
	--profile-name "${PROFILE_NAME}" \
	--endpoint-name "${ENDPOINT_NAME}" \
	--route-name "${MANAGED_ROUTE}" \
	-o none 2>/dev/null; then
	echo "::error::Managed route ${MANAGED_ROUTE} missing; refusing to delete ${ORPHAN_ROUTE}."
	exit 1
fi

echo "Deleting orphan Front Door route: ${ORPHAN_ROUTE}"
az afd route delete \
	--resource-group "${RESOURCE_GROUP}" \
	--profile-name "${PROFILE_NAME}" \
	--endpoint-name "${ENDPOINT_NAME}" \
	--route-name "${ORPHAN_ROUTE}" \
	--yes

echo "Orphan route ${ORPHAN_ROUTE} deleted."
