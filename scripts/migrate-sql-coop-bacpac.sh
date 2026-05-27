#!/usr/bin/env bash
# Export COOP from legacy SQL and import into green SQL (BACPAC via blob staging).
# Set all required env vars before running; see docs/blue-green-cutover-runbook.md
set -euo pipefail

: "${LEGACY_RG:?}"
: "${LEGACY_SQL_SERVER:?}"
: "${GREEN_RG:?}"
: "${GREEN_SQL_SERVER:?}"
: "${STORAGE_ACCOUNT:?}"
: "${STORAGE_CONTAINER:?}"
: "${SQL_ADMIN_USER:?}"
: "${SQL_ADMIN_PASSWORD:?}"

BACPAC_NAME="coop-$(date +%Y%m%d%H%M).bacpac"
STORAGE_KEY="$(az storage account keys list -g "${LEGACY_RG}" -n "${STORAGE_ACCOUNT}" --query '[0].value' -o tsv)"
URI="https://${STORAGE_ACCOUNT}.blob.core.windows.net/${STORAGE_CONTAINER}/${BACPAC_NAME}"

echo "Exporting COOP from ${LEGACY_SQL_SERVER}..."
az sql db export \
	--resource-group "${LEGACY_RG}" \
	--server "${LEGACY_SQL_SERVER}" \
	--name COOP \
	--admin-user "${SQL_ADMIN_USER}" \
	--admin-password "${SQL_ADMIN_PASSWORD}" \
	--storage-key-type StorageAccessKey \
	--storage-key "${STORAGE_KEY}" \
	--storage-uri "${URI}"

echo "Importing COOP into ${GREEN_SQL_SERVER}..."
az sql db import \
	--resource-group "${GREEN_RG}" \
	--server "${GREEN_SQL_SERVER}" \
	--name COOP \
	--admin-user "${SQL_ADMIN_USER}" \
	--admin-password "${SQL_ADMIN_PASSWORD}" \
	--storage-key-type StorageAccessKey \
	--storage-key "${STORAGE_KEY}" \
	--storage-uri "${URI}"

echo "BACPAC migration complete: ${BACPAC_NAME}"
