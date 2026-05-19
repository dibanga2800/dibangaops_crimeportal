#!/usr/bin/env bash
# Azure-side checks: custom domain approved and linked to the managed SPA route.
set -euo pipefail

RESOURCE_GROUP="${1:-crimeportal-rg}"
PROFILE_NAME="${2:-crimeportal-frontend-unified-fd}"
ENDPOINT_NAME="${3:-crimeportal-frontend-unified-ep}"
ROUTE_NAME="${4:-crimeportal-frontend-unified-spa-route}"
CUSTOM_DOMAIN_NAME="${5:-www-dibangops-com}"
EXPECTED_HOST="${6:-www.dibangops.com}"

if ! command -v az >/dev/null 2>&1; then
	echo "az CLI not found; skipping Azure Front Door binding checks."
	exit 0
fi

if ! az group exists --name "${RESOURCE_GROUP}" -o tsv 2>/dev/null | grep -q true; then
	echo "Resource group ${RESOURCE_GROUP} not found; skipping Azure checks."
	exit 0
fi

validation="$(az afd custom-domain show \
	--resource-group "${RESOURCE_GROUP}" \
	--profile-name "${PROFILE_NAME}" \
	--custom-domain-name "${CUSTOM_DOMAIN_NAME}" \
	--query domainValidationState -o tsv 2>/dev/null || echo "")"

if [ "${validation}" != "Approved" ]; then
	echo "::error::Custom domain ${EXPECTED_HOST} validation state is '${validation}', expected Approved."
	exit 1
fi

custom_count="$(az afd route show \
	--resource-group "${RESOURCE_GROUP}" \
	--profile-name "${PROFILE_NAME}" \
	--endpoint-name "${ENDPOINT_NAME}" \
	--route-name "${ROUTE_NAME}" \
	--query "length(customDomains)" -o tsv 2>/dev/null || echo 0)"

if [ "${custom_count}" -lt 1 ]; then
	echo "::error::Route ${ROUTE_NAME} has no custom domains linked. www will serve the wrong TLS certificate."
	exit 1
fi

echo "Azure Front Door: ${EXPECTED_HOST} approved and linked to ${ROUTE_NAME}."
